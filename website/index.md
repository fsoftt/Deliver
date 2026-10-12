---
layout: page
sidebar: false
aside: false
title: Deliver
titleTemplate: A last-mile delivery platform, built like a real distributed system

# Content of the home page, rendered by the <DeliverHome> component (.vitepress/theme/DeliverHome.vue).
hero:
  name: Deliver
  text: A last-mile delivery platform, built like a real distributed system
  tagline: Six .NET 10 microservices that share no database, talk through RabbitMQ events, survive duplicates and failures, and can be traced end to end. Plus a React UI that makes the asynchrony visible.
  actions:
    - theme: brand
      text: Project overview
      link: /guide/overview
    - theme: alt
      text: Architecture
      link: /guide/architecture
    - theme: alt
      text: View the code on GitHub
      link: https://github.com/fsoftt/Deliver

features:
  - title: Domain-Driven Design
    details: Six bounded contexts, rich aggregates that protect their invariants, value objects, and domain events kept separate from public integration events.
    link: /concepts/domain-driven-design
  - title: Clean Architecture and vertical slices
    details: Domain → Application → Infrastructure → API per service, use cases grouped by feature, MediatR pipeline with validation, rules enforced by architecture tests.
    link: /concepts/clean-architecture
  - title: Event-driven with RabbitMQ
    details: Topic exchanges, one queue per consumer, fan-out/pub-sub, and a single message envelope carrying correlation and trace context.
    link: /concepts/event-driven-architecture
  - title: Reliable messaging
    details: Transactional outbox, idempotent inbox, publisher confirms, delayed retries and dead-letter queues. The customer is never charged twice.
    link: /concepts/outbox-and-inbox
  - title: Sagas and eventual consistency
    details: A choreographed delivery-and-payment workflow with compensations, local projections instead of cross-service queries, and consistency that is visible in the UI.
    link: /concepts/sagas-and-eventual-consistency
  - title: Observability and testing
    details: OpenTelemetry traces across async hops, correlation ids, structured logs. Unit, architecture, contract, Testcontainers and Playwright tests run on every push.
    link: /guide/testing

stack:
  - label: Backend
    text: .NET 10, ASP.NET Core minimal APIs, EF Core 10, MediatR 12, FluentValidation
  - label: Messaging
    text: RabbitMQ 4 (topic exchanges, DLX, TTL retry tiers, publisher confirms)
  - label: Data
    text: "PostgreSQL 17: one database and one login per service"
  - label: Frontend
    text: React 19, TypeScript, Vite, TanStack Query, React Router
  - label: Edge
    text: YARP API gateway and nginx
  - label: Observability
    text: OpenTelemetry, Jaeger, structured JSON logs, correlation ids
  - label: Testing
    text: xUnit v3, Testcontainers, WebApplicationFactory, Vitest, Playwright
  - label: Delivery
    text: Docker Compose, GitHub Actions CI with an end-to-end stage

screenshot:
  src: /screenshots/shipment-delivered.png
  alt: Shipment detail page composed from four services
---

<DeliverHome />
