import { useQuery } from '@tanstack/react-query'
import { getOptional } from '../../shared/api/http'

export interface Assignment {
  id: string
  shipmentId: string
  status: 'Pending' | 'Assigned' | 'Completed' | 'Cancelled'
  driverId: string | null
  driverName: string | null
  requiredCapacityKg: number
  rejectedDriverIds: string[]
  createdAt: string
  assignedAt: string | null
}

export function useAssignment(shipmentId: string) {
  return useQuery({
    queryKey: ['dispatch', shipmentId],
    queryFn: () => getOptional<Assignment>(`/api/dispatch/assignments/${shipmentId}`),
    refetchInterval: (query) =>
      query.state.data?.status === 'Completed' || query.state.data?.status === 'Cancelled' ? false : 1500,
  })
}
