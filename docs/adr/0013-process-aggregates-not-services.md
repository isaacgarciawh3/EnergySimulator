# ADR-0013: The simulation is a process aggregate, not a service

Status: accepted
Date: 2026-08-18

## Context

The Simulation context's central object was named `NeighbourhoodSimulator`: a
doer-name, sitting outside the `Domain/` folder, with the aggregate's identity
scattered across three objects (clock, behaviour dictionary, battery). It read
as a service that merely orchestrates domain pieces.

The review position, recorded here because it governs every future change
(reviewer's words, translated):

> Most of the things we think are service classes are not (Evans). The
> Simulation is not a Pure Fabrication in the GRASP sense - simulation IS
> business rule, therefore it is domain. This is one of those cases where the
> orchestration itself is the business. When the software is BPM-like software,
> the process classes are business classes, not services. Simulation is an
> aggregate root that carries leaves and must know the process, its rules,
> invariants and value objects.

## Decision

`SimulationRun` is the AGGREGATE ROOT of the Simulation context. It owns every
piece of state that crosses ticks - the clock (`SimulationClock`, a leaf), the
per-meter behaviour state, the battery's physical charge - and it names its
invariants: time only moves forward; every advance yields exactly one reading
per meter; storage is commanded at most once per tick and only for an advanced
tick.

Visibility IS design: the public surface is the responsibility. Two commands
(`Advance()`, `ApplyStorageSetpoint()`) and one state view (`Storage`), each
doc-commented with who calls it and why. Everything else is private, and the
private methods read as the steps of the process.

A pure fabrication (GRASP) remains legitimate where it truly has no domain of
its own - the application `SimulationEngine` stays a thin coordinator across
contexts. The line: if replacing the implementation with real hardware would
delete the class, it is Simulation domain; if the class only wires contexts
together, it is application.

## Consequences

- A reader opening `Sim.Simulation` finds the root first, named as a noun,
  inside `Domain/`, stating its own invariants.
- The tick ordering that produces the peak-shaving counterfactual is now
  guarded by the root (storage commanded only for an advanced tick), not by
  the engine happening to call in the right order.
- `IsBusy(string)` and `LastWeather` are gone: telemetry carries the whole
  answer, so no caller interrogates behaviour internals.
- Proven behaviour-preserving by a golden master: 200 ticks, every meter, full
  double precision, identical hash before and after.

## Alternatives rejected

**Keep the simulator-as-service shape.** It compiled and passed tests, and it
misstated the domain: the process is the business here.

**Fold the engine into the root too.** The engine coordinates four contexts;
that part is genuine fabrication and folding it in would couple the root to
Accounting and Control.
