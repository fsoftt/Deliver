# Event catalogue

Every message on the wire uses the same envelope:

```json
{
  "messageId": "0199...",            // idempotency key (UUIDv7, time-ordered)
  "eventType": "ShipmentCreated",
  "occurredAt": "2026-01-01T12:00:00+00:00",
  "correlationId": "demo-1767268800", // shared by everything caused by the same request
  "payload": { }
}
```

AMQP properties: `message_id`, `correlation_id`, `type`, `content_type=application/json`, persistent delivery.
Headers: `traceparent` (W3C trace context), and on retries `x-retry-count`.

## Exchanges (topic, durable), one per producing context

| Exchange | Owner | Routing keys |
|---|---|---|
| `shipment.events` | Shipping | `shipment.created`, `shipment.assigned`, `shipment.picked_up`, `shipment.in_transit`, `shipment.delivered`, `shipment.cancelled`, `shipment.delivery_payment_failed` |
| `fleet.events` | Fleet | `driver.available`, `driver.unavailable`, `driver.assignment_rejected` |
| `dispatch.events` | Dispatch | `driver.assigned` |
| `billing.events` | Billing | `billing.price_calculated`, `billing.payment_captured`, `billing.payment_failed` |

## Queues, one per consuming service

Each has `<queue>.retry.1..N` (TTL tiers) and `<queue>.dlq`.

| Queue | Subscriptions |
|---|---|
| `shipping` | `driver.assigned`, `billing.price_calculated`, `billing.payment_captured`, `billing.payment_failed` |
| `fleet` | `driver.assigned`, `shipment.delivered`, `shipment.cancelled` |
| `dispatch` | `shipment.created`, `shipment.delivered`, `shipment.cancelled`, `driver.available`, `driver.unavailable`, `driver.assignment_rejected` |
| `billing` | `shipment.created`, `shipment.delivered`, `shipment.cancelled` |
| `notifications` | `shipment.created`, `driver.assigned`, `shipment.picked_up`, `shipment.in_transit`, `shipment.delivered`, `shipment.cancelled`, `shipment.delivery_payment_failed` |
| `analytics` | every `shipment.*` event, `billing.payment_captured` |

## Payloads

The approved, test-enforced schema is in
[`tests/Deliver.Contracts.Tests/Snapshots/contracts.approved.txt`](../tests/Deliver.Contracts.Tests/Snapshots/contracts.approved.txt).
The C# definitions are in [`src/BuildingBlocks/Deliver.Contracts`](../src/BuildingBlocks/Deliver.Contracts).

## Evolution rules

1. **Additive only.** Add optional fields, never rename or remove fields, never change their meaning.
2. **Primitive types only.** No domain types or enums leak onto the wire (the contract tests enforce this).
3. **Breaking change?** Publish a new event type (`ShipmentCreatedV2`) side by side, migrate consumers, then retire the old one.
4. **Consumers are tolerant readers.** Unknown fields are ignored, and `System.Text.Json` does that by default.
