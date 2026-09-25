# ADR-0009: OpenTelemetry tracing across asynchronous hops, and correlation ids

- Status: Accepted

## Decision
- Every host uses the OpenTelemetry SDK (ASP.NET Core, HttpClient, Npgsql, and the custom `Deliver.Messaging` source), exporting
  over OTLP to Jaeger. Nothing is exported when `OTEL_EXPORTER_OTLP_ENDPOINT` is unset.
- The W3C `traceparent` is captured when an event is written to the outbox. It becomes the parent of the publish span,
  travels in the AMQP headers, and is the parent of the consumer's process span. One trace therefore follows a business flow
  even through the outbox's asynchronous gap.
- A business `X-Correlation-Id` is created at the edge, carried in every envelope and added to every log scope.
- Logs are structured JSON on stdout.

## Consequences
One request can be followed across every service in Jaeger. Metrics are collected but, to keep the local setup small,
there is no Prometheus or Grafana. Adding them only requires configuring exporters.
