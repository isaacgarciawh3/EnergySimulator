# Design overview

The short version of how this is built and why. `c4.md` has the diagrams,
`adr/` has the decisions, `assumptions.md` has what we made up.

## Key components and responsibilities

| Component | Project | Responsibility | Explicitly not its job |
|---|---|---|---|
| `SimulationRun` | Sim.Simulation | Owns simulated time. Advances the clock and reports the weather and season for each tick. | Anything about power, houses or kilowatt-hours |
| `WeatherModel` | Sim.Simulation | Temperature, cloud cover, irradiance as a pure function of instant and seed | Knowing what consumes the energy |
| `Neighbourhood` | Sim.Energy | Aggregate root. Holds 30 houses and 6 charge points, measures every asset in a fixed order | Settling with the grid, accumulating totals |
| `IEnergyAsset` | Sim.Energy | One call signature every asset answers, whatever its physics | - |
| `EnergyLedger` | Sim.Accounting | Aggregate root. Cumulative energy per meter, and grid settlement per interval | Knowing what a heat pump is |
| `ContextTranslator` | Sim.Application | Anti-corruption layer between the three contexts | Any business rule |
| `SimulationEngine` | Sim.Application | The one orchestrator. Runs a tick through all three contexts | Physics, bookkeeping, timekeeping |
| `SimulationWorker` | Sim.Api | Drives the clock at the configured rate | Everything else |
| `SimulationEndpoints` | Sim.Api | REST surface. Every handler delegates and returns | Any logic at all |
| Sqlite stores | Sim.Infrastructure | Configuration persistence and the read-side projection | - |
| `InProcessTickBus` | Sim.Infrastructure | Publishes the single integration event | - |

## Data model

### Domain

```
SimulationRun (aggregate root)
  seed, startedAt, currentInstant, tickDuration, tickIndex
  -> emits TickEnvironment

Neighbourhood (aggregate root)          invariants: exactly 30 houses, exactly 6 chargers
  House (entity)                        invariant: base load always present
    IEnergyAsset (BaseLoad | HeatPump | PvArray | HomeEvCharger)
  PublicEvCharger (entity)
  -> emits MeterReading per asset per tick

EnergyLedger (aggregate root)
  MeterAccount (entity, one per meter)
    consumed, generated, net, lastPower
  totals: consumed, generated, imported, exported
  -> emits GridSettlement per tick
```

Value objects, in the shared kernel: `Kilowatts` and `KilowattHours`, both
`readonly record struct`, converted only through an explicit duration.

### Persistence

Three tables. No relationships - this is a read model, not a normalised store.

| Table | Key | Holds |
|---|---|---|
| `simulation_configuration` | single row, `id = 1` | seed, start instant, tick minutes, ticks per second, the three asset proportions |
| `tick_history` | `instant` | net, consumption and generation in kW. Trimmed to a rolling 48 hour window |
| `meter_totals` | `meter_id` | cumulative consumed, generated, net kWh and last power per meter |

Engine state is deliberately absent. See ADR-0006.

## How a tick works

1. `SimulationRun.Advance()` returns a `TickEnvironment` - the instant, the
   duration, and the weather.
2. The translator narrows it to a `MeasurementContext`. The Energy context
   receives temperature and irradiance and never learns what a season is.
3. `Neighbourhood.Measure()` walks its assets in a fixed order and returns one
   signed `MeterReading` each. Fixed order matters: floating point addition is
   not associative, so a varying order would break reproducibility.
4. The translator maps each reading to an `EnergyEntry`. The asset type collapses
   to consumer or generator; the Accounting context never learns what a heat
   pump is.
5. `EnergyLedger.Post()` accumulates per meter and settles against the grid.
   Net positive is an import, net negative is an export, never both.
6. The snapshot is projected and `TickCompleted` is published on the bus.

## The physical assumptions, in prose

**PV.** Generation is installed capacity multiplied by the tick's irradiance
factor, which already carries day length, season and cloud. It is negative
power, so it cancels household consumption at the house meter before anything
reaches the grid. A house can therefore be a net exporter while the
neighbourhood is still importing, and both numbers are reported.

**Heat pump.** Electrical draw rises linearly as the outdoor temperature falls
below 15 degrees, capped at rated power, with the coefficient of performance
folded into the per-degree coefficient. In a hard freeze a real unit degrades
faster than linearly; ours does not.

**Home EV charging.** One seeded plug-in per day between 17:30 and 19:00,
needing 8 to 12 kWh at 7.4 kW, charging until full or until the 07:00 departure.
Reported power is the interval average so the final partial interval accounts
for exactly the energy delivered.

**Public charging.** Six shared points used by residents and passers-by.
Seeded arrivals with a midday and an evening peak, sessions of 10 to 40 kWh at
11 kW. A busy point rejects arrivals - there is no queue, so peak utilisation is
an underestimate.

**Base load.** Always present, 0.2 to 0.6 kW per house from the seed, shaped by
a morning and evening curve.

Full rationale and the known simplifications are in `assumptions.md`.

## Correctness

The invariant the whole thing rests on, checked every tick:

```
generation + grid_import == consumption + grid_export
```

Observed at runtime on a fresh database at tick 232:

```
52.272000 == 52.272000
```

The second invariant is accounting closure: the sum of per-meter energy equals
the metered aggregate. Both are targeted by property-based tests in TASK-008,
which is outstanding.

## What is deliberately not here

Brokers, sagas, leases, reapers, heartbeats, separate worker processes, event
sourcing and tariffs. Each is discussed in `assumptions.md` under Limitations,
with the seam that would let it in. Building any of them at this scope would
signal poor judgement rather than depth.
