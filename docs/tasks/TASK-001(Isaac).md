---
# === EXECUTION CONTEXT ===
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: main
cliente: Utilus
projeto: EnergySimulator
modulo: Assumptions

# === TASK METADATA ===
task_id: TASK-001
titulo: Build the Neighbourhood Energy Simulation end-to-end (Isaac)
tipo: feature
prioridade: critica
status: em_execucao
criado_em: 2026-08-18
atualizado_em: 2026-08-18

# === GROUPING ===
epico: Utilus home assignment

# === DEPENDENCIES ===
depende_de: []
bloqueia: []
---

## Objective

Deliver the complete "Neighbourhood Energy Simulation" home assignment in the
public repo within 3 hours: deterministic tick-based simulation of 30 houses +
6 public EV chargers, energy accounting per asset/meter, animated 2-page UI
with a 24-simulated-hour chart, tests proving energy conservation, and full
documentation. Work happens DIRECTLY on main with incremental narrative
commits — no PR (the deliverable IS the commit history; RF-24).

## Context

- Local clone: /home/isaac-garcia/Documents/Projects/EnergySimulator
- Pre-compiled starter available: ~/Documents/Projects/utilus-starter (Sim.*
  Clean Architecture skeleton). NuGet cache and Docker images pre-warmed.
- Three areas: Simulation BC (physics), Accounting BC (kWh sums), Projection
  (dashboard read model). Boundary contract: MeterReading. Enforced by
  NetArchTest.
- All assumptions closed: see module Assumptions register A-001..A-009 and
  ADR-001..005. They MUST ship in the repo under docs/.
- Assignment priority order governs cuts: 1) engine+accounting+clock,
  2) animated UI+24h chart, 3) per-asset counters, 4) weather influence.
  Weather CONTEXT is built from tick one (it gates PV/HP strategies); only its
  sophistication is cuttable.

## Functional Requirements

- [ ] RF-01: Controllable simulation clock (start/pause/speed/reset); current
      simulated date/time always visible. tick size CONFIGURABLE (default 15 min, ADR-003).
- [ ] RF-02: IEnergyAsset.Measure(TickContext) -> PowerSample strategy; five
      implementations: BaseLoad, HeatPump, PvArray, HomeEvCharger,
      PublicEvCharger. Consumption positive, generation negative (ADR-002).
- [ ] RF-03: Exactly 30 houses, exactly 6 public chargers; 40% PV / 30% HP /
      20% home EV seeded distribution (A-006); config page exposes seed +
      proportions and restarts the sim.
- [ ] RF-04: Cumulative kWh since sim start per asset AND per meter (house
      meter = signed sum of its assets; A-003 netting).
- [ ] RF-05: Neighbourhood aggregate power series + grid import/export
      settlement per tick.
- [ ] RF-06: Deterministic weather (A-009) influencing PV (irradiance x cloud)
      and heat pump (balance-point linear, A-005); season derived from month.
- [ ] RF-07: Dashboard page: sim date/time, weather + season, current
      neighbourhood kW, last-24-SIMULATED-hours chart (inline SVG, 96 points),
      per-asset/meter kWh table (30 houses + 6 chargers). Auto-updates
      (polling ~500ms, ADR-005).
- [ ] RF-08: AI - Prompts/ folder ships in the repo (already started) and is
      updated during the build.
- [ ] RF-09: README: what/how to run (one command), design overview, data
      model, assumptions, limitations + next steps. docs/adr/ + docs/assumptions.md.

## Non-Functional Requirements

- [ ] RNF-01: `docker compose up` runs everything; `dotnet run` fallback works
      without Docker (no database — in-memory read model).
- [ ] RNF-02: Same seed = identical output. No DateTime.Now / unseeded Random /
      Guid.NewGuid inside the hexagon (TimeProvider + injected seed only).
- [ ] RNF-03: Property test (FsCheck): per tick,
      generation + grid_import == consumption + grid_export (explicit float
      tolerance). Plus accounting closure: sum(asset kWh) == meter kWh.
- [ ] RNF-04: NetArchTest: Domain references nothing external; Simulation and
      Accounting do not reference each other's internals.
- [ ] RNF-05: Test suite < 30s; CI (GitHub Actions: build + test) green.
- [ ] RNF-06: Conventional Commits, English, no AI signature/co-author,
      incremental commits per phase.

## Technical Specification

### Stack

.NET 10 / C# 14, ASP.NET Core Minimal API, server-rendered pages, xUnit +
Shouldly + FsCheck, NetArchTest.Rules, Dockerfile multi-stage + compose.
No MediatR, no broker, no EF, no chart library.

### Solution layout

```
src/Sim.Domain          VOs (Kilowatts, KilowattHours), Weather, SimClock,
                        IEnergyAsset + 5 strategies, House, Neighbourhood,
                        GridSettlement, MeterReading
src/Sim.Application     Use cases (AdvanceTick, ConfigureSimulation, GetDashboard),
                        ports (ITickBus, ISimulationStateStore)
src/Sim.Infrastructure  InProcessTickBus (sync), InMemoryReadModel,
                        EnergyAccountant (kWh accumulators), SeededNeighbourhoodFactory
src/Sim.Api             Composition root, background sim loop (speed-controlled),
                        endpoints + 2 pages (Dashboard, Configuration)
tests/Sim.Domain.Tests  Unit + FsCheck property tests
tests/Sim.Architecture.Tests
docs/adr/  docs/assumptions.md  docs/design.md
AI - Prompts/
```

### Key contracts

```
IEnergyAsset.Measure(TickContext ctx) -> PowerSample      // signed kW
record MeterReading(MeterId, AssetType, Instant, Kilowatts Power, KilowattHours Energy)
ITickBus.Publish(TickCompleted evt)                        // port; in-proc today
GET  /api/dashboard        -> read model snapshot (UI polls)
POST /api/simulation       -> { seed, proportions, ticksPerSecond } restart
POST /api/simulation/pause | /resume
```

### 3-hour timebox (cut lines explicit)

| Phase | Window | Output (commit each) |
|-------|--------|----------------------|
| 1 Skeleton | 0:00–0:20 | Solution from starter, CI, compose, README stub |
| 2 Domain core | 0:20–1:10 | VOs, weather, clock, 5 strategies, tick loop + settlement, accounting |
| 3 Tests | 1:10–1:35 | Conservation property, closure, arch tests, edge cases |
| 4 UI | 1:35–2:20 | Dashboard (poll + SVG 24h chart + asset table), Config page |
| 5 Docs | 2:20–2:45 | README, 5 ADRs, assumptions, design overview, AI log |
| 6 Buffer | 2:45–3:00 | compose smoke, final push >=10 min slack |

CUT ORDER if late (assignment's own priority, reversed): fancy weather detail →
config page becomes JSON file + seed query param → per-asset table collapses to
per-house → UI degrades to auto-refresh meta tag. The engine, accounting,
clock and 24h chart are NEVER cut.

## Acceptance Criteria

1. Fresh clone + `docker compose up` → dashboard on localhost:8080 animating
   within 60s, no manual steps.
2. Same seed run twice → identical readings tick for tick (test proves it).
3. Property test proves energy conservation on every generated scenario.
4. A cloudy winter day shows lower PV and higher heat-pump load than a clear
   summer day (one named test each direction).
5. Chart always spans exactly the last 24 simulated hours (96 ticks),
   regardless of sim speed.
6. Repo shows >= 8 narrative commits across the phases; CI green on final push.

## Restrictions

- Do NOT touch: repo history rewrite, force-push, GitHub settings.
- No new external services or NuGet packages beyond the listed stack (cache is
  warm for those; anything else risks a mid-assignment download).
- No Portuguese anywhere in the repo.
- No AI signatures/co-authors in commits.

## Instructions for the Claude Agent

> You are working in /home/isaac-garcia/Documents/Projects/EnergySimulator on
> branch `main` (public repo isaacgarciawh3/EnergySimulator).
> This is the Utilus home assignment — client `Utilus`, project
> `EnergySimulator`, module `Assumptions`.
> Execute the phases in order, committing at the end of each with Conventional
> Commits in English. Push after every phase (the evaluator sees history).
> Keep the AI - Prompts log current. Respect the cut order under time pressure;
> never report a phase done without build + tests green.
