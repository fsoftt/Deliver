import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { http } from '../../shared/api/http'

export type VehicleType = 'Motorcycle' | 'Car' | 'Van'

export interface Vehicle {
  plate: string
  type: VehicleType
  capacityKg: number
}

export interface Driver {
  id: string
  name: string
  phone: string
  availability: 'Unavailable' | 'Available' | 'OnDelivery'
  vehicle: Vehicle | null
  currentShipmentId: string | null
  registeredAt: string
}

export interface RegisterDriverInput {
  name: string
  phone: string
  vehicle: Vehicle
}

const keys = {
  all: ['drivers'] as const,
  detail: (id: string) => ['drivers', id] as const,
}

export function useDrivers() {
  return useQuery({
    queryKey: keys.all,
    queryFn: () => http.get<Driver[]>('/api/drivers'),
    refetchInterval: 2000,
  })
}

export function useDriver(id: string | null) {
  return useQuery({
    queryKey: keys.detail(id ?? ''),
    queryFn: () => http.get<Driver>(`/api/drivers/${id}`),
    enabled: id !== null,
  })
}

export function useRegisterDriver() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: RegisterDriverInput) => http.post<{ id: string }>('/api/drivers', input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.all }),
  })
}

export function useChangeAvailability() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, available }: { id: string; available: boolean }) =>
      http.post(`/api/drivers/${id}/availability`, { available }),
    onSettled: () => queryClient.invalidateQueries({ queryKey: keys.all }),
  })
}
