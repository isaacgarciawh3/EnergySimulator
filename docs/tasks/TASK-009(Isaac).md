---
# === EXECUTION CONTEXT ===
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: feat/bounded-contexts
cliente: Utilus
projeto: EnergySimulator
modulo: Assumptions

# === TASK METADATA ===
task_id: TASK-009
titulo: Three bounded contexts with compiler-enforced isolation (Isaac)
tipo: refactor
prioridade: critica
status: em_revisao
criado_em: 2026-08-18
atualizado_em: 2026-08-18

# === GROUPING ===
epico: Utilus home assignment

# === DEPENDENCIES ===
depende_de: [TASK-003, TASK-004, TASK-005]
bloqueia: [TASK-007, TASK-008]
---

## Objective

Replace the two-context folder split (TASK-003) with three bounded contexts that
the compiler enforces: Simulation, Energy and Accounting, each its own project,
each with exactly one aggregate root, sharing no types beyond physical units.

## Context

TASK-003 delivered `Sim.Domain` with `Simulation/` and `Accounting/` folders.
Folders are a naming convention, not a boundary — nothing prevented an
accounting type from reaching into a heat pump. For an assignment scored on
"system design: modularity, separation of concerns, extensibility", the boundary
has to be structural or it is only a claim.

Isaac's direction: three contexts (Simulation, Energy, Accounting), DDD,
aggregate roots that do not reach into each other, hexagonal, and the scaling
seams (queue/worker/event stream) designed and DOCUMENTED but not built.

## Functional Requirements

- [x] RF-01: One project per bounded context. `Sim.Energy` has no project
      reference to `Sim.Accounting` — the dependency is not expressible.
- [x] RF-02: Exactly one aggregate root per context:
      `SimulationRun`, `Neighbourhood`, `EnergyLedger`.
- [x] RF-03: Shared kernel limited to `Kilowatts`, `KilowattHours` and the
      deterministic noise primitive. Documented as a deliberate exception.
- [x] RF-04: Anti-corruption layer in the Application layer translating
      `TickEnvironment` -> `MeasurementContext` -> readings -> `EnergyEntry`.
      Each translation narrows: Energy never sees Season/CloudCover,
      Accounting never sees AssetType.
- [x] RF-05: Grid settlement moves from Energy to Accounting. Energy measures;
      Accounting settles.
- [x] RF-06: SQLite behind `ISimulationConfigurationStore` and
      `IProjectionStore`; seeded on first container start, editable from the web.
- [x] RF-07: `ITickBus` port with in-process synchronous adapter standing in for
      the event stream; `SimulationWorker` BackgroundService standing in for a
      job runner. Both documented as seams, not as finished infrastructure.

## Non-Functional Requirements

- [x] RNF-01: Solution builds with `TreatWarningsAsErrors`, zero warnings.
- [x] RNF-02: Energy conservation holds at runtime
      (`generation + import == consumption + export`).
- [ ] RNF-03: Architecture test locking the context isolation and the
      dependency rule — DEFERRED to TASK-008.

## Technical Specification

```
src/Sim.SharedKernel     units + deterministic noise      (no dependencies)
src/Sim.Simulation       SimulationRun, WeatherModel      -> SharedKernel
src/Sim.Energy           Neighbourhood, House, assets     -> SharedKernel
src/Sim.Accounting       EnergyLedger, MeterAccount       -> SharedKernel
src/Sim.Application      ACL, ports, SimulationEngine     -> all three + kernel
src/Sim.Infrastructure   SQLite adapters, InProcessTickBus-> Application
src/Sim.Api              composition root, REST, worker   -> Infrastructure
```

## Acceptance Criteria

1. Adding a project reference from `Sim.Energy` to `Sim.Accounting` is the only
   way to couple them, and no such reference exists.
2. A fresh run reports 30 houses, 6 public chargers, 62 meters and a 97-point
   24-hour window, with conservation exact to 1e-6.
3. Reconfiguring the seed rebuilds the world and resets the projections.

## Verification performed

```
tickIndex 232  instant 2026-01-17T10:00Z  season Winter  temp 1.9C
meters 62  houses 30  publicChargers 6  last24Hours 97
CONSERVATION: 52.272000 == 52.272000 -> True
```

## Open points carried to review (NOT decided unilaterally)

- OP-01: `Neighbourhood` is the aggregate root and `House` an entity inside it,
  because the "exactly 30 / exactly 6" invariant spans all houses. The
  alternative (House as its own aggregate root) is defensible if houses ever
  become independently editable.
- OP-02: `EnergyEntry.Category` is currently `AssetType.ToString()` — a
  stringly-typed leak of an Energy enum into Accounting. Candidate fix: an
  Accounting-owned `MeterCategory` enum mapped in the ACL.
- OP-03: `MeterKind.Storage` exists with no implementation — kept as a declared
  extension point for batteries, or removed as speculative generality.
- OP-04: Assets hold session state (EV charging), so the tick loop is strictly
  sequential. The parallel-up-to-the-settlement-barrier design from the project
  RNFs is therefore not currently satisfiable without externalising that state.
- OP-05: Whether a fourth context (tariffs / energy retailer, euro pricing) is
  in scope or stays cut per A-007.

## Restrictions

- No new NuGet packages beyond Microsoft.Data.Sqlite.
- No commits to `main`; branch + PR with task context in the description.
