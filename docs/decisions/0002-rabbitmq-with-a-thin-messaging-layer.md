# ADR-0002: RabbitMQ with a thin, in-repo messaging layer (no MassTransit or NServiceBus)

- Status: Accepted

## Context
Several services must react to one business fact (fan-out), and none should block the others.
We need topic routing, durable queues, delayed retries, dead-lettering, publisher confirms and trace propagation.
Frameworks such as MassTransit provide all of this, but MassTransit v9+ is commercially licensed, and a framework
hides exactly the mechanics this project is meant to demonstrate.

## Decision
Use `RabbitMQ.Client` 7 (async API) directly, behind a small building block (`Deliver.Messaging`):

- one topic exchange per producing context (`shipment.events`, ...), and one durable queue per consuming service;
- a single JSON envelope (`messageId`, `eventType`, `occurredAt`, `correlationId`, `payload`);
- publisher confirms on every publish, and manual acks after processing;
- a consumer host that dispatches by `eventType` to typed `IIntegrationEventHandler<T>` implementations.

Application code depends only on `Deliver.Messaging.Abstractions` (`IOutbox`, `IIntegrationEventHandler<T>`),
so the transport can be swapped (e.g. for MassTransit or Azure Service Bus) without touching use cases.

## Consequences
- The mechanics are visible and explained in code (`Topology`, `RabbitMqConsumer`, `OutboxProcessor`).
- We own the code: no sagas-as-a-service, no scheduling, no automatic topology conventions. That is acceptable for this scope.
- Deliberately not a generic framework: no plugin model and no pipeline abstraction, just what the services use.
