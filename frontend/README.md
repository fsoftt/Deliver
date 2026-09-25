# Deliver: control tower (React)

An operations UI for the Deliver platform: create shipments, manage drivers, watch every service react.

- **React 19 + TypeScript + Vite**, **TanStack Query** for server state, **React Router**.
- **Organised by bounded context** (`src/contexts/shipping`, `fleet`, `dispatch`, `billing`, `notifications`, `analytics`),
  mirroring the backend. Each context owns its API client and components.
- **UI composition:** the shipment page is assembled from panels that each read from the service that owns
  the data (Shipping, Dispatch, Billing, Notifications), always through the API gateway.
- **Eventual consistency is visible, not hidden:** data that another service has not produced yet is shown as
  *pending*, and the query keeps polling until it arrives (`refetchInterval` stops once the shipment settles).
- Talks only to `/api` on its own origin (the Vite dev proxy, or nginx in the container), so there's no CORS and no knowledge of internal services.

```bash
npm install
npm run dev        # http://localhost:3000, proxies /api to the gateway on :5000
npm test           # unit/component tests (Vitest + Testing Library)
npm run e2e        # Playwright against a running `docker compose up` (http://localhost:3000)
```

Why one app and not microfrontends: see [ADR-0010](../docs/decisions/0010-single-spa-organised-by-bounded-context.md).
