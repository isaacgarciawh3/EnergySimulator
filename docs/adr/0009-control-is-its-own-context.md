# ADR-0009: Control is its own bounded context

Status: accepted
Date: 2026-08-18

## Context

A neighbourhood battery was added to the requirements. It needs a strategy that
decides when to charge and discharge in order to shave peaks.

The obvious place to put that logic is Simulation, next to the other asset
behaviours. Obvious, and wrong.

## Decision

Control is a separate bounded context, `Sim.Control`, referencing only the
shared kernel. It owns `IStorageControlStrategy`, the peak-shaving strategy, and
the two records they exchange: `GridState` in, `StorageSetpoint` out.

The test that settles it is the same one that fixed the Energy boundary
(ADR-0001): **replace the simulation with real IoT telemetry and see what
survives.**

- Simulation disappears. Readings come from hardware.
- The battery's physical response disappears. State of charge is telemetry.
- **The peak-shaving policy survives unchanged.** You still want to shave peaks
  on real hardware. It is the same decision, on the same inputs.

Anything that survives that swap cannot be part of Simulation.

## Consequences

- Control sees a number and the battery's limits. It cannot see houses, assets,
  weather or the time of year, because it does not need to and because seeing
  them would make it untestable in isolation.
- A setpoint is a command, not a measurement. The distinction is carried in the
  types: `StorageSetpoint` is what we asked for, `PowerReading` is what happened.
  They differ whenever the battery cannot comply, and the difference is where
  clamping shows up.
- The tick ordering became meaningful. Non-storage assets are measured first,
  producing the net load the neighbourhood would have had without a battery.
  Control sees that, and both figures then exist naturally - which is exactly
  what the "with and without battery" visualisation needs. The requirement is
  satisfied by the ordering rather than by a second simulation run.
- A fourth project for two files is real ceremony. It is justified because this
  is the seam a real energy business would run its product on.

## Alternatives rejected

**Put the strategy in Simulation, next to the behaviours.** Cheapest, and it
means throwing the strategy away when the simulation is replaced - the exact
mistake ADR-0001 was written to correct.

**Put it in Energy, as a method on the battery.** Energy describes what exists;
it does not decide. It would also put policy on an entity that a telemetry-fed
system would treat as pure nameplate data.

**Put it in the application layer as orchestration.** Defensible, and it is
where the wiring lives. Rejected because a control strategy is a domain rule
with real behaviour worth testing on its own, not glue.
