import { Card, EmptyState, ErrorBanner, Pending, StatusBadge } from '../../shared/ui'
import { formatTime, shortId } from '../../shared/format'
import { useAssignment } from './api'

export function AssignmentPanel({ shipmentId }: { shipmentId: string }) {
  const { data: assignment, error } = useAssignment(shipmentId)

  return (
    <Card title="Dispatch" subtitle="Dispatch service: who carries it, chosen from its own projection of Fleet's drivers">
      <ErrorBanner error={error} />
      {assignment === null && <Pending>Dispatch has not received ShipmentCreated yet</Pending>}
      {assignment === undefined && !error && <EmptyState>Loading…</EmptyState>}
      {assignment && (
        <dl className="details">
          <dt>Assignment</dt>
          <dd><StatusBadge status={assignment.status} /></dd>
          <dt>Driver</dt>
          <dd data-testid="assigned-driver">
            {assignment.driverName
              ?? (assignment.status === 'Pending' ? <Pending>no suitable driver available yet</Pending> : '—')}
          </dd>
          <dt>Needs capacity</dt>
          <dd>{assignment.requiredCapacityKg} kg</dd>
          {assignment.assignedAt && (<><dt>Assigned at</dt><dd>{formatTime(assignment.assignedAt)}</dd></>)}
          {assignment.rejectedDriverIds.length > 0 && (
            <>
              <dt>Rejected by</dt>
              <dd className="mono">{assignment.rejectedDriverIds.map((id) => `#${shortId(id)}`).join(', ')}</dd>
            </>
          )}
        </dl>
      )}
    </Card>
  )
}
