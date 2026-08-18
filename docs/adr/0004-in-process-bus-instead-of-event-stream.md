# ADR-0004: An in-process bus standing in for the event stream

Status: accepted
Date: 2026-08-18

## Context

The natural production shape for this system is a stream: the simulation
publishes ticks, projections and consumers subscribe, and the read side is
rebuilt from the log. We have four hours in total.

The risk is symmetric. Building the stream burns the budget and delivers less
working software. Ignoring it entirely leaves an architecture with no answer to
"what happens when this grows".

## Decision

Define the port now, implement the smallest honest adapter.

`ITickBus` exposes `Publish` and `Subscribe` - the shape a broker client would
have. `InProcessTickBus` implements it with synchronous in-process dispatch.
The tick loop runs as a `BackgroundService` inside the API container.

What we did **not** build, and say so plainly: brokers, sagas, leases, reapers,
heartbeats, outboxes, replay.

## Consequences

- Moving to a real broker is a change to one adapter and one DI registration.
  No domain type moves, no aggregate changes.
- The seam is honest rather than decorative: the engine already publishes a
  single integration event and never calls a projection directly.
- Publication is synchronous, so a slow subscriber slows the tick loop. At this
  scope that is acceptable and it is stated rather than hidden.
- There is no delivery guarantee, no ordering guarantee across processes and no
  replay. Nothing in the system currently needs them.

## Alternatives rejected

**A real broker in a compose service.** Kafka or RabbitMQ would be honest
infrastructure and would consume a large share of a four hour budget to move
data between two objects in the same process.

**No port at all, call the projection directly.** Simpler and it forecloses the
growth path. The port costs one interface and one small class.

**Full event sourcing.** The read model rebuilt from an append-only log is a
genuinely good fit for this domain and is far beyond the budget. Recorded here
as the direction, not as a plan.
