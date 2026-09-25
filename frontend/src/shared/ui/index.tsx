import type { ReactNode } from 'react'
import { ApiError } from '../api/http'
import { humanize } from '../format'

const toneByStatus: Record<string, string> = {
  Created: 'neutral',
  Pending: 'warning',
  Assigned: 'info',
  PickedUp: 'info',
  InTransit: 'info',
  Delivered: 'success',
  Completed: 'success',
  Paid: 'success',
  Captured: 'success',
  Available: 'success',
  Open: 'neutral',
  OnDelivery: 'info',
  Unavailable: 'neutral',
  Cancelled: 'muted',
  Voided: 'muted',
  Failed: 'danger',
  PaymentFailed: 'danger',
}

export function StatusBadge({ status }: { status: string }) {
  return (
    <span className={`badge badge-${toneByStatus[status] ?? 'neutral'}`} data-testid="status-badge">
      {humanize(status)}
    </span>
  )
}

export function Card({ title, subtitle, actions, children }: {
  title?: ReactNode
  subtitle?: ReactNode
  actions?: ReactNode
  children: ReactNode
}) {
  return (
    <section className="card">
      {(title || actions) && (
        <header className="card-header">
          <div>
            {title && <h2 className="card-title">{title}</h2>}
            {subtitle && <p className="card-subtitle">{subtitle}</p>}
          </div>
          {actions && <div className="card-actions">{actions}</div>}
        </header>
      )}
      {children}
    </section>
  )
}

/**
 * Makes eventual consistency visible instead of hiding it: the data is on its way from another
 * service, and the view keeps polling until it arrives.
 */
export function Pending({ children }: { children: ReactNode }) {
  return (
    <span className="pending" role="status">
      <span className="pending-dot" aria-hidden />
      {children}
    </span>
  )
}

export function EmptyState({ children }: { children: ReactNode }) {
  return <p className="empty">{children}</p>
}

export function ErrorBanner({ error }: { error: unknown }) {
  if (!error) return null
  const message = error instanceof Error ? error.message : 'Something went wrong.'
  const details = error instanceof ApiError ? error.fieldErrors : []
  return (
    <div className="error-banner" role="alert">
      <strong>{message}</strong>
      {details.length > 0 && (
        <ul>
          {details.map((detail) => <li key={detail}>{detail}</li>)}
        </ul>
      )}
    </div>
  )
}

export function Stat({ label, value, hint }: { label: string; value: ReactNode; hint?: string }) {
  return (
    <div className="stat">
      <span className="stat-label">{label}</span>
      <span className="stat-value">{value}</span>
      {hint && <span className="stat-hint">{hint}</span>}
    </div>
  )
}
