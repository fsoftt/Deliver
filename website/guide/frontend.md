# The web UI

The **control tower** is a React + TypeScript single-page app for creating shipments, managing drivers and
watching the services react in real time.

![Shipment assigned](/screenshots/shipment-assigned.png){.screenshot}

## Design choices

- **Organised by bounded context.** `src/contexts/shipping`, `fleet`, `dispatch`, `billing`, `notifications` and `analytics`
  mirror the backend. Each context owns its API client (TanStack Query hooks) and its components.
- **UI composition.** The shipment page is assembled from panels, and each one reads from the service that owns the
  data. None of the panels knows about the others.
- **Eventual consistency is shown, not hidden.** Data another service has not produced yet appears as *pending*
  ("Billing is pricing it"), and the query keeps polling until it arrives. Polling stops once the shipment has settled.
- **One origin.** The browser only calls `/api` on its own origin. nginx (or the Vite dev proxy) forwards it to the
  API gateway, so there is no CORS and the internal topology stays hidden.
- **Validation errors are first-class.** A malformed request returns a `400` listing every field error, and the UI shows them all at once.

![Validation errors](/screenshots/validation-errors.png){.screenshot}

## Why not microfrontends?

Microfrontends solve an **organisational** problem: many teams shipping parts of one UI independently. This project
has one team and one release cadence, so they would add runtime composition, shared-dependency versioning and more
deployables without solving anything. The folder-per-context structure keeps the seam ready if that ever changed
([ADR-0010](https://github.com/fsoftt/Deliver/blob/main/docs/decisions/0010-single-spa-organised-by-bounded-context.md)).

## Tests

- **Vitest and Testing Library** component tests, for example *"the price shows as pending until Billing has priced the shipment"*.
- **Playwright** end-to-end tests drive the real UI against the whole system started with Docker Compose in CI:
  register a driver, create a shipment, wait for the price and the driver, move the shipment through its lifecycle, and
  check that the invoice is paid. The screenshots on this site come from that run.
