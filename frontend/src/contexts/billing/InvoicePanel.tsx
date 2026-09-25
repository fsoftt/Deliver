import { Card, EmptyState, ErrorBanner, Pending, StatusBadge } from '../../shared/ui'
import { formatMoney, formatTime } from '../../shared/format'
import { useInvoice } from './api'

export function InvoicePanel({ shipmentId }: { shipmentId: string }) {
  const { data: invoice, error } = useInvoice(shipmentId)

  return (
    <Card title="Billing" subtitle="Billing service: priced on creation, charged exactly once on delivery">
      <ErrorBanner error={error} />
      {invoice === null && <Pending>Billing has not priced this shipment yet</Pending>}
      {invoice === undefined && !error && <EmptyState>Loading…</EmptyState>}
      {invoice && (
        <>
          <dl className="details">
            <dt>Invoice</dt>
            <dd data-testid="invoice-status"><StatusBadge status={invoice.status} /></dd>
            <dt>Amount</dt>
            <dd>{formatMoney(invoice.amount, invoice.currency)}</dd>
          </dl>
          <h3 className="subheading">Payment attempts</h3>
          {invoice.payments.length === 0
            ? <EmptyState>Charged when the shipment is delivered.</EmptyState>
            : (
              <ul className="list">
                {invoice.payments.map((payment) => (
                  <li key={payment.id}>
                    <StatusBadge status={payment.succeeded ? 'Captured' : 'Failed'} />
                    <span>{formatMoney(payment.amount, invoice.currency)}</span>
                    <span className="muted">{payment.failureReason ?? payment.providerReference}</span>
                    <span className="muted">{formatTime(payment.attemptedAt)}</span>
                  </li>
                ))}
              </ul>
            )}
        </>
      )}
    </Card>
  )
}
