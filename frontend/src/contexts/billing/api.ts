import { useQuery } from '@tanstack/react-query'
import { getOptional } from '../../shared/api/http'

export interface PaymentAttempt {
  id: string
  amount: number
  succeeded: boolean
  providerReference: string | null
  failureReason: string | null
  attemptedAt: string
}

export interface Invoice {
  invoiceId: string
  shipmentId: string
  customerId: string
  amount: number
  currency: string
  status: 'Open' | 'Paid' | 'PaymentFailed' | 'Voided'
  issuedAt: string
  paidAt: string | null
  voidedAt: string | null
  payments: PaymentAttempt[]
}

export function useInvoice(shipmentId: string) {
  return useQuery({
    queryKey: ['billing', shipmentId],
    // 404 means "Billing has not processed ShipmentCreated yet", not an error.
    queryFn: () => getOptional<Invoice>(`/api/billing/shipments/${shipmentId}/invoice`),
    refetchInterval: (query) => (query.state.data && query.state.data.status !== 'Open' ? false : 1500),
  })
}
