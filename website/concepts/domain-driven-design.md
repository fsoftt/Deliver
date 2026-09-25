# Domain-Driven Design

## Bounded contexts

A **bounded context** is a boundary inside which a model and its language are consistent. Deliver has six:
Shipping, Fleet, Dispatch, Billing, Notifications and Analytics. The same word can mean different things in
different contexts, and that is expected:

| Context | What "driver" means there |
|---|---|
| Fleet | A person with a phone, a vehicle and a shift: `Unavailable ⇄ Available → OnDelivery` |
| Dispatch | A candidate for work: capacity, availability, and when they were last assigned |
| Shipping | Only a `DriverId` on the shipment |

Sharing one `Driver` class across services would couple them all to every change. Each context keeps
its own model, and they exchange **facts** (events) instead of objects.

## Aggregates protect invariants

An aggregate is a consistency boundary. All changes go through its root, which enforces the rules:

```csharp
public void MarkAsDelivered(DateTimeOffset now)
{
    EnsureStatus("deliver", ShipmentStatus.InTransit);   // cannot deliver before transit

    Status = ShipmentStatus.Delivered;
    DeliveredAt = now;
    Raise(new ShipmentDeliveredDomainEvent(Id, CustomerId, DriverId!.Value, now));
}
```

There are no public setters (an architecture test enforces it), so `shipment.Status = Delivered` can't even be written.
Other examples:

- `Invoice` can be charged successfully **at most once**.
- `Driver.AcceptAssignment` rejects the job (an expected outcome, published as an event) if the driver is no longer available.
- `DeliveryAssignment` never offers a shipment again to a driver who rejected it.

## Value objects

`Address`, `Weight`, `Money`, `Vehicle` and strongly typed ids (`ShipmentId`, `DriverId`) validate themselves on
creation and compare by value. A negative weight or an empty street can't exist.

## Domain services

Logic that doesn't belong to a single entity lives in a domain service:

- `DriverSelectionPolicy` picks a driver who can carry the load and has waited longest since their last job, preferring the smallest vehicle that fits.
- `DeliveryPricing` charges a base fee, plus every started kilogram, plus extra items.

## Domain events vs integration events

| | Domain event | Integration event |
|---|---|---|
| Scope | Inside one service | Between services |
| Types | Domain types (`ShipmentId`, `Address`) | Primitives only (`Guid`, `string`, `decimal`) |
| Delivery | In process, inside the same transaction | RabbitMQ, via the outbox |
| Can change freely? | Yes | No: it's a public contract, and snapshot tests guard it |

A translator in each service's Application layer maps one to the other, so the domain can evolve without breaking other services.
