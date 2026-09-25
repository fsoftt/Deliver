# ADR-0005: Clean Architecture layers, vertical slices inside, and pragmatism at the edges

- Status: Accepted (revised: MediatR 12.5 adopted for use cases)

## Decision
- Services with real business rules (Shipping, Fleet, Dispatch, Billing) use four projects: Domain, Application,
  Infrastructure and Api. The domain has no dependency on EF Core, ASP.NET Core or RabbitMQ, which the architecture tests enforce.
- Inside Application, code is organised **by feature** (`Features/CreateShipment`, `Features/AssignDriver`, ...), not by
  technical type. A feature is a vertical slice through the layers: a command or query, its validator and its handler, side by side.
- Endpoints send commands and queries through **MediatR**. Cross-cutting behaviour runs once as pipeline behaviours
  (`Deliver.Application.Pipeline`): **logging and timing**, then **FluentValidation** (all field errors → one 400
  `ValidationProblemDetails`), then the handler.
- Repositories are **per aggregate** (`IShipmentRepository`), not a generic `IRepository<T>`. Queries go through read
  stores that return DTOs (CQRS-lite).
- **Notifications and Analytics are single projects** organised by feature, with no mediator. They have no invariants
  to protect, so layers and a pipeline would be ceremony.

## Why MediatR 12.5.0 specifically
MediatR became commercially licensed from version 13 (2025). **12.5.0 is the last Apache-2.0 release** and is
pinned in `Directory.Packages.props`. The trade-off: that major version receives no new features. Our usage
(requests, handlers, open pipeline behaviours) is small and stable, and it sits behind our own extension method
(`AddApplicationPipeline`). Replacing it (with a hand-written dispatcher, or a source-generated mediator) would therefore touch only
the endpoints and handler signatures, not the domain.

## Validation vs invariants
Validators check the **shape** of the input and report every problem at once, which is friendly for API clients.
Aggregates enforce the **invariants** and are the only guarantee of correctness (e.g. "pickup and delivery addresses
must differ", "cannot deliver before pickup"). Both exist on purpose: removing a validator never makes the system incorrect.

## Integration events do not go through MediatR
Messages consumed from RabbitMQ are handled by `IIntegrationEventHandler<T>` inside the messaging host, which
already provides its own pipeline (inbox check, transaction, retries, tracing, logging). Sending them through
MediatR as well would duplicate that pipeline.

## Consequences
There are more projects than in a CRUD app. Each one is small, and the dependency rules are verifiable.
