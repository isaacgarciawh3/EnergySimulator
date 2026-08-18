# ADR-0004: An in-process bus standing in for the event stream

Status: superseded in part on 2026-08-18 - the port was deleted, see below
Date: 2026-08-18

## Context

The natural production shape for this system is a stream: the simulation
publishes ticks, projections and consumers subscribe, and the read side is
rebuilt from the log. We have four hours in total.

The risk is symmetric. Building the stream burns the budget and delivers less
working software. Ignoring it entirely leaves an architecture with no answer to
"what happens when this grows".

## Decision

Originally: define the port now, implement the smallest honest adapter.
`ITickBus` exposed `Publish` and `Subscribe`, with an in-process synchronous
implementation.

**Revised on 2026-08-18: the port was deleted.** Review found that nothing ever
subscribed to it. A publish/subscribe seam with zero subscribers is not a seam,
it is a class that runs on every tick to hand an object to nobody - speculative
generality dressed up as foresight.

The engine writes to the projection store through its own port, which is a real
dependency with a real adapter. The tick loop remains a `BackgroundService`
inside the API container.

What a stream would be for, when there is something to say: publishing
`TickCompleted` to Kafka or Azure Event Hub so that projections, alerting and
analytics consume independently of the simulation, with replay. That is a real
design and it is worth doing when a second consumer exists. Today there is one
consumer and it is in the same process.

What we did **not** build, and say so plainly: brokers, sagas, leases, reapers,
heartbeats, outboxes, replay.

## Consequences

- Less code, and no interface implying a capability the system does not have.
- Introducing a broker later means adding a publish call where the engine
  already completes a tick. That is a small, obvious change, and it does not
  need an unused interface sitting in the codebase to make it possible.
- The lesson recorded: a port earns its place when something is on the other
  side of it. `IProjectionStore` and `ISimulationConfigurationStore` have real
  adapters and stay. `ITickBus` had none and went.
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
