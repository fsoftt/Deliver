# Design decisions

Every significant choice is recorded as an Architecture Decision Record (ADR), with its context, the alternatives
considered and its consequences. They show *why*, which is usually more interesting than *what*.

| # | Decision | The short reason |
|---|---|---|
| [0001](https://github.com/fsoftt/Deliver/blob/main/docs/decisions/0001-microservices-per-bounded-context.md) | One microservice per bounded context | Independent capabilities with their own data and reasons to change. It also says honestly when a modular monolith would be the better start. |
| [0002](https://github.com/fsoftt/Deliver/blob/main/docs/decisions/0002-rabbitmq-with-a-thin-messaging-layer.md) | RabbitMQ with a thin in-repo messaging layer | Show the mechanics (confirms, DLX, TTL retries) instead of hiding them behind a framework. |
| [0003](https://github.com/fsoftt/Deliver/blob/main/docs/decisions/0003-transactional-outbox-and-idempotent-inbox.md) | Transactional outbox and idempotent inbox | Solve the dual-write problem, and make at-least-once delivery safe. |
| [0004](https://github.com/fsoftt/Deliver/blob/main/docs/decisions/0004-retries-with-ttl-queues-and-dead-lettering.md) | TTL retry tiers, then a DLQ | Delayed retries without head-of-line blocking or broker plugins. |
| [0005](https://github.com/fsoftt/Deliver/blob/main/docs/decisions/0005-clean-architecture-with-vertical-slices.md) | Clean Architecture, vertical slices, MediatR 12.5 | A pipeline for cross-cutting concerns, pinned to the last Apache-2.0 release. |
| [0006](https://github.com/fsoftt/Deliver/blob/main/docs/decisions/0006-database-per-service.md) | A database per service | Ownership is enforced by PostgreSQL permissions, not by convention. |
| [0007](https://github.com/fsoftt/Deliver/blob/main/docs/decisions/0007-choreographed-delivery-saga.md) | A choreographed saga | The flow is linear and each step has an obvious owner, so a central orchestrator would add coupling. |
| [0008](https://github.com/fsoftt/Deliver/blob/main/docs/decisions/0008-shared-contracts-with-snapshot-tests.md) | Contracts guarded by snapshot tests | A breaking change becomes a visible, reviewed diff. |
| [0009](https://github.com/fsoftt/Deliver/blob/main/docs/decisions/0009-observability-with-opentelemetry.md) | OpenTelemetry across async hops | One trace from the HTTP request, through the outbox and RabbitMQ, into consumers. |
| [0010](https://github.com/fsoftt/Deliver/blob/main/docs/decisions/0010-single-spa-organised-by-bounded-context.md) | One SPA, not microfrontends | Microfrontends solve an organisational problem this project doesn't have. |

## Guiding principle

> Every architectural decision should answer: **"What problem does this solve?"**
> If a technology doesn't solve a meaningful problem here, it isn't added just because it appears on a modern architecture diagram.
