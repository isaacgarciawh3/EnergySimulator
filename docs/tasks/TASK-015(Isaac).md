---
# === EXECUTION CONTEXT ===
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: refactor/domain-refinement
client: Utilus
project: EnergySimulator
module: Assumptions

# === TASK METADATA ===
task_id: TASK-015
title: Make the simulation's aggregate root explicit and readable (Isaac)
type: refactor
priority: high
status: done
created: 2026-08-18
updated: 2026-08-18

# === GROUPING ===
epic: Domain refinement

# === DEPENDENCIES ===
depends_on: []
blocks: [TASK-016, TASK-017, TASK-018, TASK-019]
---

## Objective

A human opening `Sim.Simulation` must be able to answer "what is the aggregate
root?" without difficulty. Today they cannot: the candidate is named like a
service, sits outside the Domain folder, and the aggregate's identity is spread
across three objects. This task makes the root explicit, shrinks its public
surface to what it actually serves, and does it under TDD so the refactor is
provably behaviour-preserving.

## Design directives (Isaac, translated)

These are the reviewer's own words, translated, and they govern this task and
every domain task after it:

1. "Most of the things we think are service classes are not (Evans). The
   Simulation is NOT a Pure Fabrication in the GRASP sense. Simulation IS
   business rule, therefore it is domain - this is one of those cases where the
   orchestration itself is the business."
2. "A true service class only orchestrates and has no domain of its own -
   that is Pure Fabrication in GRASP terms. But when the software is BPM-like
   software, the process classes are business classes, not services. That is
   this case: Simulation is an aggregate root class that carries leaves and
   must know the simulation process, its rules, invariants and value objects."
3. "It needs to become clearer. I also like visibility separation: many private
   methods and very few public ones. Public methods exist because visibility
   demonstrates what the class is really serving to its callers - it shows the
   Single Responsibility. SOLID must be followed together with TDD to preserve
   testability."
4. "I want to read the code and understand it without difficulty."

## Context - what is wrong today, with evidence

Opening `src/Sim.Simulation` a reader sees `NeighbourhoodSimulator.cs` and
`BatterySimulator.cs` at the project root and a `Domain/` folder that contains
the clock and the weather. Three signals contradict each other:

- **Naming.** `NeighbourhoodSimulator` ends in *-or*: the name of a doer, a
  mechanism - not a domain concept. Per directive 1, this object IS the domain
  process, and its name denies it.
- **Location.** The would-be root lives outside `Domain/`; the folder structure
  claims the domain is the weather and the root is something else.
- **Split identity.** The state that survives across ticks is scattered: the
  clock in `SimulationRun`, EV sessions inside the `_behaviours` dictionary of
  `NeighbourhoodSimulator`, battery state of charge in `BatterySimulator`. The
  aggregate invariant - time only moves forward, and each advance emits exactly
  one reading per meter - is enforced by nobody; it works because the engine
  happens to call things in the right order.

Public-surface smells on `NeighbourhoodSimulator`, found by reading:

- `Advance()` returns an unnamed tuple `(SimulationTick, IReadOnlyList<PowerReading>)` -
  a domain concept ("what this interval produced") with no type.
- `IsBusy(string meterId)` pattern-matches `PublicChargerBehaviour { Busy: true }` -
  a UI question reaching through the root into a concrete behaviour via a
  string key. Leaky abstraction.
- `LastWeather` is a query answered by the memory of the last command - call
  `Advance()` first or get null. Temporal coupling.
- The constructor does real work (builds the run and the whole behaviour
  dictionary), so nothing can be substituted in a test.
- `Create()` gives the same class a second responsibility: composition.

## Functional Requirements

- [x] RF-01: One class IS the aggregate root of the Simulation context, named as
      a domain noun (proposal: `SimulationRun` - "one run of the simulation
      process"), living in `Sim.Simulation.Domain`, documented as the root. It
      owns the leaves: the clock, the per-meter behaviour state, and the
      battery's physical state.
- [x] RF-02: The public surface states the responsibility and nothing else.
      Target: ONE command - `Advance()` - returning a named value object
      (proposal: `TickTelemetry`: instant, duration, tick index, weather,
      readings, charge-point occupancy). Queries that duplicate what the
      telemetry already carries are removed, not kept for convenience.
- [x] RF-03: `IsBusy(string)` is deleted. Occupancy travels INSIDE the
      telemetry as data; no caller interrogates behaviour internals.
- [x] RF-04: `LastWeather` is deleted. Weather is part of the telemetry.
- [x] RF-05: Everything else becomes private: weather sampling, behaviour
      creation, per-meter measurement, clock advancement. The reader learns the
      process by reading one public method whose private calls read as the
      process steps, in order.
- [x] RF-06: The clock stops being a second "root-sounding" class: it becomes a
      leaf value/entity of the run (proposal: rename to `SimulationClock`),
      invisible from outside the aggregate.
- [x] RF-07: The invariant "exactly one reading per meter per tick" moves from
      accident to assertion - the root guarantees it and a test proves it.

## Non-Functional Requirements

- [x] RNF-01: **TDD, in this order.** FIRST a characterization test locks the
      current behaviour: same seed and configuration produce the exact reading
      sequence the current code produces (golden master over ~200 ticks,
      asserting meter ids, instants and power values). THEN the refactor runs
      under that lock. The refactor is provably behaviour-preserving or it does
      not merge.
- [x] RNF-02: Public method count on the root is a review criterion, not a
      style preference. Every public member must answer "who calls this and
      why" in its doc comment.
- [x] RNF-03: No new features. No queue, no tick-contract change, no unit
      types in Energy - those are TASK-016..019. Scope here is structure and
      readability only.
- [x] RNF-04: Zero warnings; whole suite green; determinism tests untouched
      and passing.
- [x] RNF-05: ADR-0013 records directive 1-3 as the project's position on
      domain services vs process aggregates, so the next contributor does not
      "helpfully" extract a SimulationService again.

## Acceptance Criteria

1. A reader opening `Sim.Simulation` sees the aggregate root first, named as a
   noun, inside `Domain/`, and can state its invariant from its doc comment.
2. The root has one public command and its public members each justify their
   visibility.
3. `grep -rn "IsBusy\|LastWeather" src/` returns nothing.
4. The characterization test passes unchanged before and after the refactor -
   same seed, byte-identical telemetry.
5. The whole suite is green and no test that existed before was weakened.

## Directives added during execution

- Extract hidden concepts freely (methods, value objects, entities) where a
  concept is hidden or a responsibility duplicated - but only ever to make the
  code SIMPLER to read, never to add ceremony. Do not lose requirements.
- 100 percent line AND branch coverage on every domain class altered by the
  task. Achieved: SimulationRun 106/106 lines 44/44 branches, SimulationClock
  32/32 + 4/4, BatterySimulator 50/50 + 8/8, TickTelemetry, StorageState.
- Tests follow the scenario standard now recorded as ADR-0014: folder per
  domain class, class per scenario named in plain words, constructor loads the
  scenario and acts, methods carry exactly one assert each, names cite the
  requirement. Applied to the Simulation domain only - converting the older
  test files is deliberately out of scope.

## Result

- SimulationRun is the explicit aggregate root (ADR-0013): owns clock,
  behaviour state and battery charge; public surface is Advance(),
  ApplyStorageSetpoint() and Storage; IsBusy(string), LastWeather, the unnamed
  tuple and NeighbourhoodSimulator itself are gone.
- New invariant guarded by the root, not by call order: storage is commanded
  at most once per tick, only for an advanced tick.
- Golden master: 200 ticks, every meter, full precision - identical hash
  before and after the refactor.
- Suite: 206 tests green (166 domain + 20 architecture + 20 API).

## Restrictions

- Behaviour-preserving only. Any output difference is a bug in the refactor.
- No changes outside `src/Sim.Simulation`, `src/Sim.Application` (call-site
  adjustments only) and `tests/`.
- English throughout. One PR, reviewed before merge.
