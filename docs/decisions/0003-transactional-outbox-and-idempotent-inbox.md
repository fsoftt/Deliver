# ADR-0003: Transactional outbox for publishing, inbox for idempotent consumption

- Status: Accepted

## Context
Saving to PostgreSQL and publishing to RabbitMQ are two writes to two systems (a "dual write"). Without
coordination, a crash between them loses events or publishes events for state that was rolled back.
RabbitMQ provides at-least-once delivery, so consumers will see duplicates.

## Decision
**Outbox.** Aggregates raise domain events. An EF Core `SaveChangesInterceptor` dispatches them in-process to
handlers that translate them into integration events and insert `outbox_messages` rows **in the same
transaction** as the business change. A background `OutboxProcessor` publishes pending rows in order
(`FOR UPDATE SKIP LOCKED`, so several instances can run) and marks them processed only after the broker confirms.

**Inbox.** Each consumer runs `check processed_messages → handle → insert processed_messages` in one database
transaction. The primary key `(message_id, consumer)` also protects against concurrent duplicates.

**Domain-level idempotency** complements the inbox where it matters (e.g. an `Invoice` cannot be paid twice,
and `Shipment.AssignDriver` with the same driver is a no-op), because the same *fact* can arrive with a different message id.

## Consequences
- Exactly-once *effects* on top of at-least-once *delivery*.
- The latency of one polling interval (1 second by default) between commit and publish.
- The outbox and inbox tables grow, and need a retention job in production.
- A handler with an external side effect (the payment provider) is not covered by the database transaction.
  The provider idempotency key (the invoice id) closes that gap.
