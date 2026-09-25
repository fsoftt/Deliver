# ADR-0004: Delayed retries with TTL tier queues, then a dead-letter queue

- Status: Accepted

## Context
Consumers fail for transient reasons (a database blip, a provider timeout, or an event that arrives before its prerequisite)
and for permanent ones (bugs, poison messages). Requeueing immediately creates hot loops, and dropping messages loses data.

## Decision
For each consuming queue `q`:

- `q.retry.1..N` are queues with `x-message-ttl` set to the tier delay (default 2s, 10s, 30s) and a dead-letter route back to `q`;
- `q.dlq` holds messages that exhausted their retries (headers: exception type and message, failure time, retry count);
- `q` itself dead-letters to `q.dlq`, so rejected, unreadable messages are parked immediately.

The consumer republishes a failed message to the next tier with `x-retry-count + 1` (confirmed), then acks the original.

## Alternatives considered
- *Per-message TTL on a single delay queue:* rejected because of head-of-line blocking (RabbitMQ only expires the head of a queue).
- *The delayed-message-exchange plugin:* works, but adds a broker plugin dependency.
- *In-process retry (Polly) only:* holds the message and the consumer slot during the delay, and loses the retry if the process crashes.

## Consequences
Fixed, configurable backoff without plugins. A retried message may be processed after newer messages, so handlers
must tolerate reordering (see architecture section 4).
