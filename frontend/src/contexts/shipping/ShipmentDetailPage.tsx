import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { InvoicePanel } from '../billing/InvoicePanel'
import { AssignmentPanel } from '../dispatch/AssignmentPanel'
import { NotificationFeed } from '../notifications/NotificationFeed'
import { Card, ErrorBanner, Pending, StatusBadge } from '../../shared/ui'
import { formatMoney, humanize, shortId } from '../../shared/format'
import { ShipmentTimeline } from './ShipmentTimeline'
import { nextAction, useCancelShipment, useShipment, useShipmentAction, type ShipmentDetails } from './api'

const actionLabels = { pickup: 'Mark picked up', transit: 'Start transit', deliver: 'Mark delivered' } as const

/**
 * UI composition: one page, several bounded contexts. Each panel reads from the service that owns the
 * data (Shipping, Dispatch, Billing, Notifications) through the gateway, and none of them knows about the others.
 */
export function ShipmentDetailPage() {
  const { id = '' } = useParams()
  const { data: shipment, error } = useShipment(id)

  if (error) return <ErrorBanner error={error} />
  if (!shipment) return <p className="empty">Loading…</p>

  return (
    <>
      <header className="page-header">
        <div>
          <p className="breadcrumb"><Link to="/shipments">Shipments</Link> / #{shortId(shipment.id)}</p>
          <h1>
            Shipment <span className="mono">#{shortId(shipment.id)}</span> <StatusBadge status={shipment.status} />
          </h1>
          <p className="lead">{shipment.pickupAddress.city} → {shipment.deliveryAddress.city} · {shipment.totalWeightKg} kg</p>
        </div>
        <LifecycleActions shipment={shipment} />
      </header>

      <Card>
        <ShipmentTimeline shipment={shipment} />
      </Card>

      <div className="grid-2">
        <ShipmentPanel shipment={shipment} />
        <AssignmentPanel shipmentId={shipment.id} />
        <InvoicePanel shipmentId={shipment.id} />
        <NotificationFeed shipmentId={shipment.id} />
      </div>
    </>
  )
}

function LifecycleActions({ shipment }: { shipment: ShipmentDetails }) {
  const action = nextAction(shipment.status)
  const progress = useShipmentAction(shipment.id)
  const cancel = useCancelShipment(shipment.id)
  const [reason, setReason] = useState('Customer request')
  const canCancel = shipment.status === 'Created' || shipment.status === 'Assigned'

  return (
    <div className="actions">
      <ErrorBanner error={progress.error ?? cancel.error} />
      {action && (
        <button className="button primary" disabled={progress.isPending} onClick={() => progress.mutate(action)}>
          {actionLabels[action]}
        </button>
      )}
      {shipment.status === 'Created' && <Pending>waiting for Dispatch to assign a driver</Pending>}
      {canCancel && (
        <div className="inline-form">
          <input aria-label="Cancellation reason" value={reason} onChange={(e) => setReason(e.target.value)} />
          <button className="button danger" disabled={cancel.isPending} onClick={() => cancel.mutate(reason)}>Cancel</button>
        </div>
      )}
    </div>
  )
}

function ShipmentPanel({ shipment }: { shipment: ShipmentDetails }) {
  return (
    <Card title="Shipment" subtitle="Shipping service">
      <dl className="details">
        <dt>Price</dt>
        <dd data-testid="shipment-price">
          {shipment.price === null && shipment.status !== 'Cancelled'
            ? <Pending>Billing is pricing it (eventual consistency)</Pending>
            : formatMoney(shipment.price, shipment.currency)}
        </dd>
        <dt>Payment</dt>
        <dd>{shipment.status === 'Delivered' ? <StatusBadge status={shipment.paymentStatus} /> : humanize(shipment.paymentStatus)}</dd>
        <dt>Customer</dt>
        <dd className="mono">#{shortId(shipment.customerId)}</dd>
        <dt>Pickup</dt>
        <dd>{shipment.pickupAddress.street}, {shipment.pickupAddress.city}</dd>
        <dt>Delivery</dt>
        <dd>{shipment.deliveryAddress.street}, {shipment.deliveryAddress.city}</dd>
        <dt>Items</dt>
        <dd>{shipment.items.map((item) => `${item.quantity} × ${item.description} (${item.unitWeightKg} kg)`).join(', ')}</dd>
        {shipment.cancellationReason && (<><dt>Cancelled</dt><dd>{shipment.cancellationReason}</dd></>)}
      </dl>
    </Card>
  )
}
