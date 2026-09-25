# ADR-0001: One microservice per bounded context

- Status: Accepted

## Context
The purpose of the project is to show how a domain is decomposed into independently deployable services
that communicate asynchronously. Last-mile delivery has clearly different capabilities: the shipment lifecycle,
the workforce, matching work to drivers, money, customer communication and reporting.

## Decision
Six services map 1:1 to the bounded contexts Shipping, Fleet, Dispatch, Billing, Notifications and Analytics.
Each owns its data and its model, and publishes a public contract (integration events). A YARP gateway is the only public entry point.

## Consequences
- Each service can be read, tested, deployed and scaled on its own, and a failure is contained (Billing down does not mean no shipments).
- We pay the distributed-systems tax: eventual consistency, duplicate messages, tracing across hops, more moving parts.
- **Honest note:** for a real product of this size, a modular monolith with the same boundaries would be the
  better first step. The boundaries here are designed so that either deployment model works.
