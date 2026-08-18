# Requirements and traceability

Every requirement stated in the assignment, numbered, with an honest status.
Requirements that are not yet satisfied say so.

Legend: **Done** implemented and observed at runtime; **Partial** implemented
but incomplete; **Open** not built yet; **Cut** deliberately out of scope, with
the reason recorded in `assumptions.md`.

## 1. Core simulation model

| # | Requirement | Status | Where |
|---|---|---|---|
| R-01 | Controllable simulation clock | Done | `SimulationRun`, `SimulationWorker`, `POST /api/simulation/pause`, `/resume` |
| R-02 | Current simulated date/time is clear | Partial | exposed on `GET /api/simulation`; UI still open |
| R-03 | Step size chosen and explained | Done | 15 min default, configurable. ADR-0003 |
| R-04 | Assets structured and extensible | Done | `IEnergyAsset` strategy, `Sim.Energy.Domain.Assets` |
| R-05 | Base household consumption, always present | Done | `BaseLoad`; enforced as a `House` invariant |
| R-06 | Heat pump, optional | Done | `HeatPump` |
| R-07 | PV, optional, generates | Done | `PvArray` |
| R-08 | Home EV charger, optional | Done | `HomeEvCharger` |
| R-09 | Public EV chargers, exactly 6 | Done | `Neighbourhood.RequiredPublicChargers`, enforced in the constructor |
| R-10 | Cumulative kWh per asset/meter since start | Done | `MeterAccount`, `meter_totals` table |
| R-11 | Neighbourhood aggregate power/energy over time | Done | `EnergyLedger`, `tick_history` table |
| R-12 | Document PV offset vs export | Done | A-003, ADR-0002 |

## 2. Weather and season

| # | Requirement | Status | Where |
|---|---|---|---|
| R-13 | At least one weather variable | Done | temperature, cloud cover and irradiance in `WeatherModel` |
| R-14 | Season representation | Done | `Season` derived from month |
| R-15 | Weather/season influences PV | Done | `PvArray` scales by irradiance, which carries cloud and day length |
| R-16 | Weather/season influences heat pump | Done | `HeatPump` balance-point model on temperature |
| R-17 | Deterministic, no external API | Done | `DeterministicNoise`, pure function of instant and seed |

## 3. Animated visualization

| # | Requirement | Status | Where |
|---|---|---|---|
| R-18 | Animated view, time advances automatically | Open | worker ticks; UI parked on `feat/dashboard-ui` |
| R-19 | UI shows simulated date/time | Open | same |
| R-20 | UI shows weather and season | Open | same |
| R-21 | UI shows current neighbourhood power | Open | same |
| R-22 | Chart of the last 24 SIMULATED hours | Open | data side done: `IProjectionStore.LoadWindow`, 97 points observed |
| R-23 | Per asset/meter total kWh since start | Open | data side done: `GET /api/simulation` returns 62 meters |

## 4. Configuration

| # | Requirement | Status | Where |
|---|---|---|---|
| R-24 | Neighbourhood configurable | Done | `SimulationConfiguration` persisted in SQLite, `PUT /api/simulation/configuration` |
| R-25 | Fixed seed reproducibility | Done | whole world is a pure function of the seed. ADR-0006 |
| R-26 | Exactly 30 houses | Done | `Neighbourhood.RequiredHouses`, constructor invariant |
| R-27 | Exactly 6 public chargers | Done | constructor invariant |
| R-28 | Documented asset distribution | Done | A-006: 40% PV, 30% heat pump, 20% home EV |

## 5. Quality expectations

| # | Requirement | Status | Where |
|---|---|---|---|
| R-29 | Readable, maintainable structure | Done | seven projects, dependency rule inward. ADR-0001 |
| R-30 | Clear domain modelling | Done | three bounded contexts, one aggregate root each. ADR-0001 |
| R-31 | Basic tests for core logic | Open | TASK-008 |
| R-32 | Documentation | Partial | this document set; README still thin |

## 6. Deliverables

| # | Requirement | Status | Where |
|---|---|---|---|
| R-33 | Running application, one-command startup | Partial | `docker compose up` builds and runs; UI not yet served |
| R-34 | Instructions to run locally | Partial | README |
| R-35 | Clean source structure | Done | see ADR-0001 |
| R-36 | Reasonable commit history | Done | incremental commits, branch per task, PR per branch |
| R-37 | Design overview | Done | `design.md`, `c4.md` |
| R-38 | Data model documented | Done | `design.md` |
| R-39 | Assumptions documented | Done | `assumptions.md` |
| R-40 | Known limitations and next steps | Done | `assumptions.md`, section "Limitations" |
| R-41 | Tests on simulation correctness and accounting | Open | TASK-008 |
| R-42 | AI chat log in the repository | Done | `AI - Prompts/` |

## Assignment priority order

The assignment states its own fallback order. It governs what gets cut if time
runs out, overriding personal preference:

1. Correct simulation, energy accounting and clock - **done**
2. Animated UI with aggregate and 24h chart - **open, highest priority**
3. Per-asset cumulative energy counters - data done, display open
4. Weather/season influence - **done**
