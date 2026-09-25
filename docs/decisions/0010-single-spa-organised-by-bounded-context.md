# ADR-0010: One React SPA organised by bounded context, not microfrontends

- Status: Accepted

## Context
The platform needed a UI to make the distributed behaviour visible: shipments being priced, assigned,
delivered and charged by different services. Microfrontends (Module Federation, single-spa, web components)
were considered, since the backend is split into services.

## Decision
Build **one** React + TypeScript SPA (Vite, TanStack Query, React Router), and organise its code **by bounded
context** (`src/contexts/<context>`), each with its own API client and components. Pages are composed from panels
owned by different contexts (the shipment page shows Shipping, Dispatch, Billing and Notifications data side by side),
and all calls go through the API gateway.

## Why not microfrontends
Microfrontends solve an **organisational** problem: many teams that need to build and deploy parts of one UI
independently. Here there is one team and one UI release cadence, so microfrontends would add runtime composition,
shared-dependency versioning, cross-app routing and styling, and several more deployables, without solving any
problem this project has. That is the "architectural maximalism" the project deliberately avoids.

The context-oriented folder structure keeps the door open: if a context ever got its own team, its folder is the
natural seam along which to extract a remote module.

## Consequences
- One build, one deployment (nginx container), and simple end-to-end tests (Playwright against docker compose).
- Context boundaries in the UI are a convention, not a runtime isolation. Lint rules or path aliases could enforce them if needed.
