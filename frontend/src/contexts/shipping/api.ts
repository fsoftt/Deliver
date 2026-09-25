import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { http } from '../../shared/api/http'

export type ShipmentStatus = 'Created' | 'Assigned' | 'PickedUp' | 'InTransit' | 'Delivered' | 'Cancelled'

export interface Address {
  street: string
  city: string
  postalCode: string
}

export interface ShipmentItem {
  description: string
  quantity: number
  unitWeightKg: number
}

export interface ShipmentDetails {
  id: string
  customerId: string
  status: ShipmentStatus
  driverId: string | null
  pickupAddress: Address
  deliveryAddress: Address
  items: ShipmentItem[]
  totalWeightKg: number
  price: number | null
  currency: string | null
  paymentStatus: 'Pending' | 'Captured' | 'Failed'
  createdAt: string
  assignedAt: string | null
  pickedUpAt: string | null
  inTransitAt: string | null
  deliveredAt: string | null
  cancelledAt: string | null
  cancellationReason: string | null
}

export interface ShipmentSummary {
  id: string
  customerId: string
  status: ShipmentStatus
  driverId: string | null
  price: number | null
  createdAt: string
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export interface CreateShipmentInput {
  customerId: string
  pickupAddress: Address
  deliveryAddress: Address
  items: ShipmentItem[]
}

export type LifecycleAction = 'pickup' | 'transit' | 'deliver'

/** The next step a driver can take, derived from the status (the backend still has the final word). */
export function nextAction(status: ShipmentStatus): LifecycleAction | null {
  switch (status) {
    case 'Assigned': return 'pickup'
    case 'PickedUp': return 'transit'
    case 'InTransit': return 'deliver'
    default: return null
  }
}

/** Shipments settle once they are finished AND other services have reported back. */
export function isSettled(shipment: ShipmentDetails): boolean {
  if (shipment.status === 'Cancelled') return true
  return shipment.status === 'Delivered' && shipment.paymentStatus !== 'Pending'
}

const keys = {
  all: ['shipments'] as const,
  list: (status: string) => ['shipments', 'list', status] as const,
  detail: (id: string) => ['shipments', id] as const,
}

export function useShipments(status: string) {
  return useQuery({
    queryKey: keys.list(status),
    queryFn: () => http.get<PagedResult<ShipmentSummary>>(`/api/shipments?pageSize=50${status ? `&status=${status}` : ''}`),
    refetchInterval: 3000,
  })
}

export function useShipment(id: string) {
  return useQuery({
    queryKey: keys.detail(id),
    queryFn: () => http.get<ShipmentDetails>(`/api/shipments/${id}`),
    // Keep polling while other services are still reacting to this shipment's events.
    refetchInterval: (query) => (query.state.data && isSettled(query.state.data) ? false : 1500),
  })
}

export function useCreateShipment() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: CreateShipmentInput) => http.post<{ id: string }>('/api/shipments', input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.all }),
  })
}

export function useShipmentAction(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (action: LifecycleAction) => http.post(`/api/shipments/${id}/${action}`),
    onSettled: () => queryClient.invalidateQueries({ queryKey: keys.all }),
  })
}

export function useCancelShipment(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (reason: string) => http.post(`/api/shipments/${id}/cancel`, { reason }),
    onSettled: () => queryClient.invalidateQueries({ queryKey: keys.all }),
  })
}
