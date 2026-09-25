# How it was built

The project started from a written specification: the business flow, six bounded contexts, the required reliability
patterns, a Definition of Done, and an explicit list of non-goals. It was built in small, verifiable steps, following one rule:

> **Work vertically.** Each feature crosses API → Application → Domain → Infrastructure → database/messaging,
> and is proven to work before the next one starts.

## 1. Model the domain before writing infrastructure

The first question was *where are the boundaries?*, not *which framework?*. Each context got its own ubiquitous
language and its own model of shared words ("driver" means something different in Fleet and in Dispatch). Integration
events were written first, as explicit contracts that use only primitive types.

## 2. A small foundation

- **Central package management** and **warnings as errors** for the whole solution.
- A deliberately **tiny shared kernel**: `Entity`, `AggregateRoot`, `IDomainEvent`, and the domain exceptions. No shared business types.
- A **messaging building block** on top of the raw RabbitMQ client: topology, publisher confirms, the consumer host, the outbox, the inbox,
  and retries with dead-lettering. It is written in-repo on purpose, so the mechanics are visible
  ([ADR-0002](https://github.com/fsoftt/Deliver/blob/main/docs/decisions/0002-rabbitmq-with-a-thin-messaging-layer.md)).
- **Service defaults**: OpenTelemetry, JSON logs, correlation ids, health checks and error → ProblemDetails mapping.

## 3. One service at a time

Shipping, then Fleet, Dispatch and Billing. Each followed the same recipe:

1. Aggregate and value objects, with **unit tests for every invariant** (*cannot deliver before pickup*).
2. Use cases as MediatR commands and queries, with FluentValidation.
3. EF Core mapping (snake_case, strongly typed ids, `xmin` optimistic concurrency) and a migration.
4. Domain events translated into integration events through the outbox.
5. Consumers for the events the service reacts to, and minimal-API endpoints.

Notifications and Analytics came last, as single-project services. They have no invariants to protect.

## 4. Make the rules executable

- **Architecture tests** fail the build if the domain references EF Core, or if one service references another.
- **Contract tests** snapshot the wire schema of every integration event, so a breaking change shows up as a reviewed diff.
- **Integration tests with Testcontainers** run against real PostgreSQL and RabbitMQ: retries, dead-letter queues, duplicate suppression, the outbox, and a duplicated `ShipmentDelivered` charging exactly once.

## 5. Let CI prove the Definition of Done

GitHub Actions builds and tests everything, then starts the **whole system with Docker Compose** and runs:

- a scripted API walkthrough of the Definition of Done: create, assign, deliver, pay, duplicate protection, a declined payment, a retry, and a dead letter;
- **Playwright** driving the React UI against the same running system.

The first CI runs found real issues that no unit test would catch: RabbitMQ 4 rejecting transient non-exclusive
queues, and a polling helper that evaluated its condition only once. Both were fixed with a regression check in place.

## 6. Document the decisions, not just the code

Ten Architecture Decision Records explain the trade-offs: why microservices (and when a modular monolith would be
better), why choreography instead of an orchestrator, why tiered retry queues, why MediatR 12.5 (the last
Apache-2.0 version), and why not microfrontends.
