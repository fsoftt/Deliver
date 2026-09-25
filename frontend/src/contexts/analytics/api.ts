import { useQuery } from '@tanstack/react-query'
import { http } from '../../shared/api/http'

export interface ShipmentStatistics {
  total: number
  byStatus: Record<string, number>
  perDay: { date: string; shipments: number }[]
}

export interface DeliveryStatistics {
  delivered: number
  cancelled: number
  averageMinutesToAssign: number | null
  averageMinutesToDeliver: number | null
  revenueCaptured: number
  paymentFailures: number
}

export function useShipmentStatistics() {
  return useQuery({
    queryKey: ['analytics', 'shipments'],
    queryFn: () => http.get<ShipmentStatistics>('/api/analytics/shipments'),
    refetchInterval: 3000,
  })
}

export function useDeliveryStatistics() {
  return useQuery({
    queryKey: ['analytics', 'deliveries'],
    queryFn: () => http.get<DeliveryStatistics>('/api/analytics/deliveries'),
    refetchInterval: 3000,
  })
}
