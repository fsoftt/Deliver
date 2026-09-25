import { useQuery } from '@tanstack/react-query'
import { http } from '../../shared/api/http'

export interface CustomerNotification {
  id: string
  shipmentId: string
  customerId: string
  channel: 'Email' | 'Sms' | 'Push'
  message: string
  triggeredBy: string
  sentAt: string
}

export function useNotifications(shipmentId: string) {
  return useQuery({
    queryKey: ['notifications', shipmentId],
    queryFn: () => http.get<CustomerNotification[]>(`/api/notifications?shipmentId=${shipmentId}`),
    refetchInterval: 2000,
  })
}
