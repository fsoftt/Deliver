# Project overview

**Deliver** is a portfolio project: a simplified last-mile delivery platform. A business creates a
shipment. It is priced, assigned to an available driver, picked up, transported and delivered, and the
customer is charged. Each of those steps belongs to a different service, and the services only know each
other through events.

::: tip The point of the project
The goal is not a feature-rich delivery app. It is to show, in working and tested code, how to
**decompose a business domain into bounded contexts and independently deployable services, and how
those services communicate asynchronously and reliably**, without building infrastructure for its own sake.
:::

## What it demonstrates

| Skill | Where you can see it |
|---|---|
| Domain modelling (DDD) | Aggregates such as `Shipment`, `Driver`, `Invoice` and `DeliveryAssignment` enforce their own rules: *a shipment cannot be delivered before pickup*, *an invoice cannot be paid twice*. [More](/concepts/domain-driven-design) |
| Service decomposition | Six bounded contexts, each with its own model, its own database and its own public contract. [More](/guide/architecture) |
| Asynchronous integration | RabbitMQ topic exchanges and fan-out. No service calls another synchronously. [More](/concepts/event-driven-architecture) |
| Reliability engineering | Transactional outbox, idempotent consumers, delayed retries, dead-letter queues and optimistic concurrency. [More](/concepts/outbox-and-inbox) |
| Distributed workflows | A choreographed saga with compensations, and eventual consistency by design. [More](/concepts/sagas-and-eventual-consistency) |
| Clean code and architecture | Clean Architecture layers, vertical slices, a MediatR pipeline with validation, and architecture rules as tests. [More](/concepts/clean-architecture) |
| Observability | One trace follows a request across HTTP, the outbox and RabbitMQ into other services. [More](/concepts/observability) |
| Testing and CI | 100+ automated tests, from pure domain tests to Playwright driving the UI against the real system in CI. [More](/guide/testing) |
| Frontend | A React/TypeScript UI organised by bounded context, which shows the asynchrony instead of hiding it. [More](/guide/frontend) |
| Engineering judgement | Ten Architecture Decision Records, including what was deliberately **not** built. [More](/guide/decisions) |

## The system in one picture

```mermaid
flowchart LR
    ui([React UI]) --> nginx[nginx] --> gw[API Gateway<br/>YARP]
    gw --> shipping[Shipping]
    gw --> fleet[Fleet]
    gw --> dispatch[Dispatch]
    gw --> billing[Billing]
    gw --> notifications[Notifications]
    gw --> analytics[Analytics]

    shipping -. shipment.* .-> mq{{RabbitMQ}}
    fleet -. driver.* .-> mq
    dispatch -. driver.assigned .-> mq
    billing -. billing.* .-> mq
    mq -. events .-> shipping & fleet & dispatch & billing & notifications & analytics
```

## Screenshots

The screenshots are captured automatically by the Playwright end-to-end test running against the full system in CI.

**Operations overview**: served by the Analytics service's own read model, which is built only from events.

![Overview](/screenshots/overview.png){.screenshot}

**A delivered shipment**: one page composed from Shipping, Dispatch, Billing and Notifications data.

![Shipment detail](/screenshots/shipment-delivered.png){.screenshot}

**Drivers**: owned by the Fleet service. Starting a shift publishes `driver.available`.

![Drivers](/screenshots/drivers.png){.screenshot}

## Scope, and what was left out on purpose

Real payment, SMS or GPS providers, maps, route optimisation, authentication, Kubernetes and cloud deployment are
**explicit non-goals**. They would add scope without demonstrating anything new about service boundaries,
messaging or domain modelling. Fake providers make the failure paths (declined payments, flaky or unreachable
notification channels) reproducible on demand.
