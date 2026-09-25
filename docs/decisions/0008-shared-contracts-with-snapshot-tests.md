# ADR-0008: Integration events in one contracts project, guarded by snapshot tests

- Status: Accepted

## Decision
Integration events are C# records in `Deliver.Contracts`, grouped by owning context (the namespace is the owner).
They use primitive types only. Each one declares its exchange and routing key as static members, so producers and
consumers cannot disagree about routing.

`Deliver.Contracts.Tests` checks that the events are immutable, use primitive types only, are routed on their owner's exchange with
unique keys, and that the wire schema equals an approved snapshot. Any contract change therefore shows up in code
review as a diff of `contracts.approved.txt`.

## Consequences
- Compile-time safety for producers and consumers in a mono-repo.
- In a multi-repo world, each owner would publish its contracts as a versioned package (or JSON Schema or AsyncAPI),
  and consumer-driven contract tests (e.g. Pact) would replace the snapshot.
