import { Link } from 'react-router'
import { Card, EmptyState, ErrorBanner, Stat } from '../../shared/ui'
import { formatMinutes, formatMoney, humanize } from '../../shared/format'
import { useDeliveryStatistics, useShipmentStatistics } from './api'

const statusOrder = ['Created', 'Assigned', 'PickedUp', 'InTransit', 'Delivered', 'Cancelled']

export function OverviewPage() {
  const shipments = useShipmentStatistics()
  const deliveries = useDeliveryStatistics()

  return (
    <>
      <header className="page-header">
        <div>
          <h1>Operations overview</h1>
          <p className="lead">
            Served by the Analytics service from its own read model, built only from events. It never queries
            another service's database, so these numbers lag the source by a few hundred milliseconds.
          </p>
        </div>
        <Link className="button primary" to="/shipments/new">New shipment</Link>
      </header>

      <ErrorBanner error={shipments.error ?? deliveries.error} />

      <div className="stats">
        <Stat label="Shipments" value={shipments.data?.total ?? '—'} />
        <Stat label="Delivered" value={deliveries.data?.delivered ?? '—'} />
        <Stat label="Avg. time to assign" value={formatMinutes(deliveries.data?.averageMinutesToAssign)} hint="created → driver assigned" />
        <Stat label="Avg. delivery time" value={formatMinutes(deliveries.data?.averageMinutesToDeliver)} hint="created → delivered" />
        <Stat label="Revenue captured" value={formatMoney(deliveries.data?.revenueCaptured, 'CLP')} />
        <Stat label="Payment failures" value={deliveries.data?.paymentFailures ?? '—'} />
      </div>

      <div className="grid-2">
        <Card title="Shipments by status">
          {shipments.data && shipments.data.total === 0 && <EmptyState>No shipments yet.</EmptyState>}
          {shipments.data && shipments.data.total > 0 && (
            <ul className="bars" data-testid="status-bars">
              {statusOrder.map((status) => {
                const count = shipments.data.byStatus[status] ?? 0
                return (
                  <li key={status}>
                    <span className="bar-label">{humanize(status)}</span>
                    <span className="bar-track"><span className="bar-fill" style={{ width: `${(count / shipments.data.total) * 100}%` }} /></span>
                    <span className="bar-value">{count}</span>
                  </li>
                )
              })}
            </ul>
          )}
        </Card>

        <Card title="Shipments per day" subtitle="Last 30 days">
          <DailyChart days={shipments.data?.perDay ?? []} />
        </Card>
      </div>
    </>
  )
}

function DailyChart({ days }: { days: { date: string; shipments: number }[] }) {
  if (days.length === 0) return <EmptyState>No data yet.</EmptyState>
  const max = Math.max(...days.map((day) => day.shipments))
  return (
    <div className="columns" role="img" aria-label="Shipments per day">
      {days.map((day) => (
        <div key={day.date} className="column" title={`${day.date}: ${day.shipments}`}>
          <span className="column-value">{day.shipments}</span>
          <span className="column-fill" style={{ height: `${(day.shipments / max) * 100}%` }} />
          <span className="column-label">{day.date.slice(5)}</span>
        </div>
      ))}
    </div>
  )
}
