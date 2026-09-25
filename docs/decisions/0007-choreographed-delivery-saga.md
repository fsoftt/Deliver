# ADR-0007: A choreographed saga for delivery and payment

- Status: Accepted

## Context
The delivery workflow spans Shipping, Dispatch, Fleet and Billing, and there is no distributed transaction. We need
to coordinate the steps and compensate on failure (cancellation, driver rejection, a declined payment).

## Decision
Use **choreography**: each service reacts to the previous step's event and publishes its own. The state of
the process is the Shipment aggregate's status plus its payment status, owned by Shipping.
Compensations are local reactions: the invoice is voided, the driver released, and the assignment cancelled or re-dispatched.

## Alternatives considered
An **orchestrator** (a process manager with its own persisted state machine, sending commands) gives one
place to see the whole flow and to handle timeouts. It is the better choice when there are many branches, timeouts or
human steps. Here the flow is linear and each step has an obvious owner, so an orchestrator would add a
central component that knows every service, which is exactly the coupling the design avoids.

## Consequences
- Low coupling, with no central point of failure.
- The end-to-end flow is harder to see in one place. This document, the event catalogue and distributed traces mitigate that.
- Timeouts (e.g. "no driver after 30 minutes") are not modelled. They would be the trigger to introduce a process manager.
