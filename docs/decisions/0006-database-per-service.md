# ADR-0006: Database per service (one server locally), EF Core migrations at startup

- Status: Accepted

## Decision
Each service has its own PostgreSQL database **and login role**. Locally they share one server to keep
`docker compose up` light, but `REVOKE ALL ON DATABASE ... FROM PUBLIC` means no service can even connect to
another service's database. Cross-service data is obtained only through events (local projections), never by queries.

Schemas are managed with EF Core migrations. For the demo, each service applies its migrations at startup.
Tables use snake_case (`EFCore.NamingConventions`), and aggregates use PostgreSQL `xmin` for optimistic concurrency.

## Consequences
- True data ownership, and independent schema evolution.
- No cross-service joins or foreign keys, so reporting needs a read model (the Analytics service).
- **Production:** migrations should run as a separate pipeline step (a migration bundle, job or init container) rather than on startup,
  to avoid races between replicas and to keep application credentials free of DDL rights.
