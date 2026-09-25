import { useState } from 'react'
import { Link } from 'react-router'
import { Card, EmptyState, ErrorBanner, Pending, StatusBadge } from '../../shared/ui'
import { formatDateTime, formatMoney, shortId } from '../../shared/format'
import { useShipments } from './api'

const filters = ['', 'Created', 'Assigned', 'PickedUp', 'InTransit', 'Delivered', 'Cancelled']

export function ShipmentsPage() {
  const [status, setStatus] = useState('')
  const { data, error, isLoading } = useShipments(status)

  return (
    <>
      <header className="page-header">
        <div>
          <h1>Shipments</h1>
          <p className="lead">Owned by the Shipping service. Prices and drivers are filled in by other services as their events arrive.</p>
        </div>
        <Link className="button primary" to="/shipments/new">New shipment</Link>
      </header>

      <div className="tabs" role="tablist">
        {filters.map((filter) => (
          <button
            key={filter || 'all'}
            role="tab"
            aria-selected={status === filter}
            className={status === filter ? 'tab active' : 'tab'}
            onClick={() => setStatus(filter)}
          >
            {filter || 'All'}
          </button>
        ))}
      </div>

      <Card>
        <ErrorBanner error={error} />
        {isLoading && <EmptyState>Loading…</EmptyState>}
        {data && data.items.length === 0 && (
          <EmptyState>No shipments yet. <Link to="/shipments/new">Create the first one</Link>.</EmptyState>
        )}
        {data && data.items.length > 0 && (
          <table className="table">
            <thead>
              <tr>
                <th>Shipment</th>
                <th>Status</th>
                <th>Price</th>
                <th>Driver</th>
                <th>Created</th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((shipment) => (
                <tr key={shipment.id}>
                  <td><Link to={`/shipments/${shipment.id}`} className="mono">#{shortId(shipment.id)}</Link></td>
                  <td><StatusBadge status={shipment.status} /></td>
                  <td>{shipment.price === null && shipment.status !== 'Cancelled'
                    ? <Pending>pricing</Pending>
                    : formatMoney(shipment.price, 'CLP')}</td>
                  <td className="mono">{shipment.driverId ? `#${shortId(shipment.driverId)}` : '—'}</td>
                  <td>{formatDateTime(shipment.createdAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>
    </>
  )
}
