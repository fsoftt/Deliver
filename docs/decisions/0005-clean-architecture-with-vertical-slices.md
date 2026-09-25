# ADR-0005: Clean Architecture layers, vertical slices inside, and pragmatism at the edges

- Status: Accepted

## Decision
- Services with real business rules (Shipping, Fleet, Dispatch, Billing) use four projects: Domain, Application,
  Infrastructure and Api. The domain has no dependency on EF Core, ASP.NET Core or RabbitMQ, which the architecture tests enforce.
- Inside Application, code is organised **by feature** (`Features/CreateShipment`, `Features/AssignDriver`, ...), not by
  technical type. A feature is a vertical slice through the layers.
- Use cases are plain handler classes resolved from DI. There is **no mediator**: it adds indirection and
  (for MediatR 13+) a commercial license, without solving a problem at this size.
- Repositories are **per aggregate** (`IShipmentRepository`), not a generic `IRepository<T>`. Queries go through read
  stores that return DTOs (CQRS-lite).
- **Notifications and Analytics are single projects** organised by feature. They have no invariants to protect,
  so separate layers would be ceremony.

## Consequences
There are more projects than in a CRUD app. Each one is small, and the dependency rules are verifiable.
