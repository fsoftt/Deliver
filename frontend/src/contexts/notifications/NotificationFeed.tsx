import { Card, EmptyState, ErrorBanner } from '../../shared/ui'
import { formatTime } from '../../shared/format'
import { useNotifications } from './api'

const channelIcon = { Email: '✉', Sms: '💬', Push: '🔔' } as const

export function NotificationFeed({ shipmentId }: { shipmentId: string }) {
  const { data, error } = useNotifications(shipmentId)

  return (
    <Card title="Customer notifications" subtitle="Notifications service: what the customer was told, and when">
      <ErrorBanner error={error} />
      {data && data.length === 0 && <EmptyState>Nothing sent yet.</EmptyState>}
      {data && data.length > 0 && (
        <ul className="feed" data-testid="notifications">
          {[...data].reverse().map((notification) => (
            <li key={notification.id}>
              <span className="feed-icon" aria-label={notification.channel}>{channelIcon[notification.channel]}</span>
              <div>
                <p>{notification.message}</p>
                <p className="muted">{notification.triggeredBy} · {formatTime(notification.sentAt)}</p>
              </div>
            </li>
          ))}
        </ul>
      )}
    </Card>
  )
}
