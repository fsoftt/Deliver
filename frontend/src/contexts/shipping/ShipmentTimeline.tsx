import { formatTime } from '../../shared/format'
import type { ShipmentDetails } from './api'

interface Step {
  label: string
  at: string | null
}

/** The shipment lifecycle as the Shipping aggregate enforces it: each step only after the previous one. */
export function ShipmentTimeline({ shipment }: { shipment: ShipmentDetails }) {
  const steps: Step[] = [
    { label: 'Created', at: shipment.createdAt },
    { label: 'Assigned', at: shipment.assignedAt },
    { label: 'Picked up', at: shipment.pickedUpAt },
    { label: 'In transit', at: shipment.inTransitAt },
    { label: 'Delivered', at: shipment.deliveredAt },
  ]

  if (shipment.status === 'Cancelled') {
    const reached = steps.filter((step) => step.at !== null)
    steps.splice(0, steps.length, ...reached, { label: 'Cancelled', at: shipment.cancelledAt })
  }

  const current = steps.findLastIndex((step) => step.at !== null)

  return (
    <ol className="timeline" aria-label="Shipment lifecycle">
      {steps.map((step, index) => {
        const state = index < current ? 'done' : index === current ? 'current' : 'todo'
        const cancelled = step.label === 'Cancelled'
        return (
          <li key={step.label} className={`timeline-step ${state}${cancelled ? ' cancelled' : ''}`}>
            <span className="timeline-dot" aria-hidden />
            <span className="timeline-label">{step.label}</span>
            <span className="timeline-time">{formatTime(step.at)}</span>
          </li>
        )
      })}
    </ol>
  )
}
