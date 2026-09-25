# Event-driven architecture

## Why events?

When a shipment is created, **four** services must react: Billing prices it, Dispatch assigns a driver,
Notifications tells the customer, and Analytics counts it. With synchronous calls, Shipping would have to know all of
them, wait for all of them, and fail if any of them were down. With events, Shipping announces a fact once and
doesn't care who listens.

| | Synchronous calls | Events |
|---|---|---|
| Coupling | The caller knows every callee | The producer knows nobody |
| Availability | Fails if any callee is down | Consumers catch up later |
| Adding a consumer | Change the producer | Bind a new queue |
| Consistency | Immediate | Eventual (made visible in the UI) |

## RabbitMQ topology

- **One topic exchange per producing context**: `shipment.events`, `fleet.events`, `dispatch.events`, `billing.events`.
- **One durable queue per consuming service**, bound to the routing keys it needs (`shipment.created`, `driver.*`, ...).
- **Fan-out / pub-sub**: `shipment.created` is copied into the dispatch, billing, notifications and analytics queues.

## One envelope for every message

```json
{
  "messageId": "0199a0d9-…",
  "eventType": "ShipmentCreated",
  "occurredAt": "2026-01-01T12:00:00+00:00",
  "correlationId": "web-7f3c…",
  "payload": { "shipmentId": "…", "totalWeightKg": 5.25, "…": "…" }
}
```

- `messageId` is the idempotency key.
- `correlationId` ties together everything caused by the same request.
- The W3C `traceparent` travels in the AMQP headers.

## Events are contracts

Integration events are C# records in a contracts project, grouped by the context that owns them. They use primitive
types only, and each declares its exchange and routing key. A snapshot test turns any change to the wire schema into
a reviewed diff. The evolution rules are *additive only*, and *a breaking change means a new event type*.
