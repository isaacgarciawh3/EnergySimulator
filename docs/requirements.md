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
| R-02 | Current simulated date/time is clear | Done | large clock in the dashboard header; observed advancing 10:00 -> 16:00 in a browser |
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
| R-18 | Animated view, time advances automatically | Done | polls every 250 ms; clock observed advancing in a browser without interaction |
| R-19 | UI shows simulated date/time | Done | observed: `Sat, 07 Feb 2026 16:00` |
| R-20 | UI shows weather and season | Done | observed: `5.8 C / Winter`, plus cloud and sun percentages and a day/night sky |
| R-21 | UI shows current neighbourhood power | Done | observed: `38.2 kW`, with import/export direction stated |
| R-22 | Chart of the last 24 SIMULATED hours | Done | 97-point window over simulated time; 5 SVG series observed in the DOM |
| R-23 | Per asset/meter total kWh since start | Done | 63 meter rows observed in the table (62 assets + the battery) |

## 4. Configuration

| # | Requirement | Status | Where |
|---|---|---|---|
| R-24 | Neighbourhood configurable | Done | The requirement lists three acceptable options and we use **two**: a JSON configuration file (`appsettings.Simulation.json`, covering both the scenario and the physical parameters) plus a fixed seed with stated proportions. Editable at runtime via `PUT /api/simulation/configuration`. Precedence in A-012. |
| R-24a | Configuration file (JSON/YAML) | Done | `src/Sim.Api/appsettings.Simulation.json` — `Scenario` section (seed, start, tick, speed, shares, battery) and the physics sections. JSON rather than YAML: .NET binds it natively, YAML would add a dependency for an identical result. TASK-013, TASK-014, ADR-0011, ADR-0012 |
| R-25 | Fixed seed reproducibility | Done | whole world is a pure function of the seed. ADR-0006 |
| R-26 | Exactly 30 houses | Done | `Neighbourhood.RequiredHouses`, constructor invariant |
| R-27 | Exactly 6 public chargers | Done | constructor invariant |
| R-28 | Documented asset distribution | Done | A-006: 40% PV, 30% heat pump, 20% home EV |

## 4b. Neighbourhood battery and peak shaving (added mid-build)

New requirement delivered after the architecture was in place, 120 minutes.

| # | Requirement | Status | Where |
|---|---|---|---|
| R-43 | Battery has capacity (kWh) | Done | `Battery.CapacityKwh`, default 250 |
| R-44 | Battery has max charge/discharge power (kW) | Done | `Battery.MaxPowerKw`, default 80, clamped in `BatterySimulator` |
| R-45 | Round-trip efficiency (optional) | Done | `Battery.RoundTripEfficiency`, default 0.90, applied as sqrt per leg |
| R-46 | Control strategy aiming to reduce peaks | Done | `PeakShavingStrategy` in the Control context |
| R-47 | Strategy uses threshold or top N% periods | Done | Top 20% by rolling percentile; optional fixed ceiling on top. ADR-0010 |
| R-48 | Show net load with and without battery | Done | both series on the 24h chart, dashed counterfactual vs solid actual, difference band shaded by sign |
| R-49 | Show battery power and state of charge | Done | observed: power now, SoC 68.0% (169.9 / 250 kWh), capacity, max power, round trip, charged/discharged totals, and a 24h SoC trace |
| R-50 | Highlight peak shaving effect | Done | dedicated panel: peak without vs with battery, kW and % reduction, on two clearly labelled scopes |

Measured effect, seed 20260818, winter start:

```
peak WITHOUT battery  127.32 kW
peak WITH battery     107.61 kW   -> 19.71 kW reduction (15.5%, cumulative since start)
within the 24h window 127.3 -> 64.8 kW  (49.1% flatter)
battery state of charge cycling 2% - 68%
```

The two percentages measure different things and are labelled accordingly
everywhere they appear. The cumulative figure includes the controller's warm-up
day and therefore understates steady-state performance.

## 5. Quality expectations

| # | Requirement | Status | Where |
|---|---|---|---|
| R-29 | Readable, maintainable structure | Done | seven projects, dependency rule inward. ADR-0001 |
| R-30 | Clear domain modelling | Done | three bounded contexts, one aggregate root each. ADR-0001 |
| R-31 | Basic tests for core logic | Done | 153 tests: accounting conservation, determinism, domain invariants, control strategy, battery physics, weather, and 20 architecture rules |
| R-32 | Documentation | Done | README with architecture and tick-sequence diagrams, configuration and precedence; plus this document set, 12 ADRs and the assumption register |

## 6. Deliverables

| # | Requirement | Status | Where |
|---|---|---|---|
| R-33 | Running application, one-command startup | Done | `docker compose up --build` verified: container reports healthy, both pages and the API return 200 from inside the container |
| R-34 | Instructions to run locally | Done | README covers Docker and `dotnet run`, both on port 8181, plus the configuration file and reset path |
| R-35 | Clean source structure | Done | see ADR-0001 |
| R-36 | Reasonable commit history | Done | incremental commits, branch per task, PR per branch |
| R-37 | Design overview | Done | `design.md`, `c4.md` |
| R-38 | Data model documented | Done | `design.md` |
| R-39 | Assumptions documented | Done | `assumptions.md` |
| R-40 | Known limitations and next steps | Done | `assumptions.md`, section "Limitations" |
| R-41 | Tests on simulation correctness and accounting | Done | energy conservation and accounting closure as property-based tests; mutation-verified that the rules actually fail when the product is broken |
| R-42 | AI chat log in the repository | Done | `AI - Prompts/` |

## Verification performed, not assumed

Every "Done" above was checked against a running system rather than against
intent. The UI rows were read out of the live DOM in a browser; the Docker row
was a real `docker compose up --build`.

```
docker compose up --build   container healthy, / and /config.html and the API all 200
animation                   clock observed advancing 10:00 -> 16:00 unattended
energy conservation         generation + import == consumption + export, exact
peak shaving                24h peak 127.3 -> 64.8 kW (49.1% flatter), sustained
                            across successive simulated days
configuration               file seed drove a first boot; a saved configuration
                            survived restart; reset returned to the file; the
                            application still started with the file deleted
invariants                  a hostile API payload was clamped and the
                            neighbourhood remained exactly 30 houses / 6 chargers
architecture rules          mutation-tested: breaking a rule fails the build
API surface                 20 integration tests boot the real application in
                            memory and exercise every endpoint over HTTP
```

### What is proven by test, and what is not

Honesty about coverage, because "all requirements met" and "all requirements
tested" are different claims:

| Area | Proven by automated test |
|---|---|
| Energy accounting, conservation, closure | Yes - property-based |
| Determinism and reproducibility | Yes - including through the public API |
| Domain invariants (30 / 6, base load) | Yes - including against a hostile API payload |
| Weather and season influence | Yes |
| Battery physics and control strategy | Yes |
| Scenario configuration and precedence | Yes - including that a reset returns to the file |
| Architecture and dependency rules | Yes - mutation-verified |
| REST API, all six endpoints | Yes - in-memory integration tests |
| The rendered UI | **No** - verified by reading the live DOM in a browser |
| `docker compose up` | **No** - verified by running it once by hand |

The two "No" rows are deliberate. A browser-driving test and a container
smoke test are both worth having and neither was affordable here; they are
named rather than papered over.

## Assignment priority order

The assignment states its own fallback order. It governs what gets cut if time
runs out, overriding personal preference:

1. Correct simulation, energy accounting and clock - **done**
2. Animated UI with aggregate and 24h chart - **open, highest priority**
3. Per-asset cumulative energy counters - data done, display open
4. Weather/season influence - **done**
