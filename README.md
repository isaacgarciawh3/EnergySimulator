# Neighbourhood Energy Simulation

A deterministic, tick-based simulation of a neighbourhood - 30 houses and 6
public EV chargers - with energy accounting and a live view of what is
happening over time.

Built for the Utilus home assignment.

## Run it

```
docker compose up --build
```

Then open http://localhost:8080.

Without Docker:

```
dotnet run --project src/Sim.Api
```

No database to install, no npm, no build step, no network access required.

## What it does

- Simulates 30 houses and 6 public chargers at a configurable tick, 15 simulated
  minutes by default.
- Houses carry base consumption always, plus optional PV, heat pump and home EV
  charger, distributed 40 / 30 / 20 per cent from a seed.
- Synthetic deterministic weather drives PV generation and heat pump demand,
  with a season derived from the simulated month.
- Tracks cumulative kWh for every one of the 62 meters since the simulation
  started, plus neighbourhood aggregate power and grid import and export.
- The same seed always produces the same run.

## API

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/simulation` | Full dashboard snapshot |
| GET | `/api/simulation/configuration` | Current configuration |
| PUT | `/api/simulation/configuration` | Reconfigure and restart from a new seed |
| POST | `/api/simulation/pause` | Pause the clock |
| POST | `/api/simulation/resume` | Resume the clock |
| GET | `/healthz` | Liveness |

## Documentation

| Document | What is in it |
|---|---|
| [docs/design.md](docs/design.md) | Design overview, components, data model, physical assumptions |
| [docs/c4.md](docs/c4.md) | C4 levels 1 to 3, the tick sequence, the dependency rule |
| [docs/assumptions.md](docs/assumptions.md) | Every assumption, the open points, limitations and next steps |
| [docs/adr/](docs/adr/) | Eight architecture decision records, each with the alternatives rejected |
| [docs/requirements.md](docs/requirements.md) | Every assignment requirement with an honest status |
| [docs/tasks/](docs/tasks/) | The task breakdown the work was executed from |
| [AI - Prompts/](AI%20-%20Prompts/) | The AI prompt log required by the assignment |

## Structure

```
src/Sim.SharedKernel     units and deterministic noise      no dependencies
src/Sim.Simulation       SimulationRun: clock, weather      bounded context
src/Sim.Energy           Neighbourhood: houses, physics     bounded context
src/Sim.Accounting       EnergyLedger: kWh, settlement      bounded context
src/Sim.Application      translation, ports, engine
src/Sim.Infrastructure   SQLite adapters, tick bus
src/Sim.Api              composition root, REST, worker
```

Three bounded contexts, one aggregate root each, no references between them -
`Sim.Energy` cannot reference `Sim.Accounting` because the project reference
does not exist. Everything crossing between them is explicitly translated.
See [ADR-0001](docs/adr/0001-three-bounded-contexts-as-separate-projects.md).

## Current status

Engine, accounting, clock, weather, configuration and persistence are done and
verified at runtime. The animated dashboard is in progress on
`feat/dashboard-ui`; tests are outstanding. Full detail in
[docs/requirements.md](docs/requirements.md).
