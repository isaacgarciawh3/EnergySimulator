# Design overview

The short version of how this is built and why. `c4.md` has the diagrams,
`adr/` has the decisions, `assumptions.md` has what we made up.

## Key components and responsibilities

| Component | Project | Responsibility | Explicitly not its job |
|---|---|---|---|
| `SimulationRun` | Sim.Simulation | Owns simulated time. Advances the clock and reports the weather and season for each tick. | Anything about power, houses or kilowatt-hours |
| `WeatherModel` | Sim.Simulation | Temperature, cloud cover, irradiance as a pure function of instant and seed | Knowing what consumes the energy |
| `Neighbourhood` | Sim.Energy | Aggregate root. Describes 30 houses, 6 charge points and the battery, and protects those counts | Behaving. It has no physics, no clock and no weather |
| `Asset`, `Battery` | Sim.Energy | Nameplate data: meter id, type, rating, capacity, efficiency | What the thing is doing right now |
| `IAssetBehaviour` | Sim.Simulation | One call signature every asset behaviour answers, whatever its physics | - |
| `NeighbourhoodSimulator` | Sim.Simulation | Reads the Energy structure and emits a `PowerReading` per meter per interval | Deciding policy, keeping books |
| `BatterySimulator` | Sim.Simulation | Applies a setpoint, clamps to limits, applies losses, tracks state of charge | Deciding when to charge |
| `PeakShavingStrategy` | Sim.Control | Decides a battery setpoint from net load and the battery's limits | Knowing about houses, weather or the calendar |
| `EnergyLedger` | Sim.Accounting | Aggregate root. Cumulative energy per meter, and grid settlement per interval | Knowing what a heat pump is |
| `NeighbourhoodBuilder` | Sim.Application | Builds the world from configuration and the seed | Deciding physics |
| `SimulationParameters` | Sim.Application | Binds and validates appsettings.Simulation.json | - |
| `SimulationEngine` | Sim.Application | The one orchestrator. Runs a tick through all four contexts | Physics, policy, bookkeeping, timekeeping |
| `SimulationWorker` | Sim.Api | Drives the clock at the configured rate | Everything else |
| `SimulationEndpoints` | Sim.Api | REST surface. Every handler delegates and returns | Any logic at all |
| Sqlite stores | Sim.Infrastructure | Configuration persistence and the read-side projection | - |


## Data model

### Domain

```
ENERGY - what exists (no behaviour)
  Neighbourhood (aggregate root)      invariants: exactly 30 houses, exactly 6 charge points
    House (entity)                    invariant: base load always present
      Asset (meterId, ownerId, type, ratedPowerKw, responseCoefficient)
    Asset[] publicChargePoints
    Battery? (meterId, capacityKwh, maxPowerKw, roundTripEfficiency)

SIMULATION - what it is doing (replaceable by real telemetry)
  SimulationRun            clock: instant, duration, tickIndex
  WeatherModel             pure function of (instant, seed)
  IAssetBehaviour x5       one instance per asset, some stateful
  BatterySimulator         setpoint in, PowerReading out, tracks state of charge
  -> emits PowerReading(meterId, instant, signed kW)

CONTROL - what it should do (survives the telemetry swap)
  GridState in -> IStorageControlStrategy -> StorageSetpoint out

ACCOUNTING - what the books say (no asset vocabulary at all)
  EnergyLedger (aggregate root)
    MeterAccount (entity, one per meter)   consumed, generated, net, lastPower
    totals: consumed, generated, imported, exported
  -> emits GridSettlement per tick
```

The contract between them is `PowerReading`, which lives in the shared kernel
precisely so that neither the producer nor the consumer owns it.

Value objects, in the shared kernel: `Kilowatts` and `KilowattHours`, both
`readonly record struct`, converted only through an explicit duration.

### Persistence

Three tables. No relationships - this is a read model, not a normalised store.

| Table | Key | Holds |
|---|---|---|
| `simulation_configuration` | single row, `id = 1` | seed, start instant, tick minutes, ticks per second, the three asset proportions |
| `tick_history` | `instant` | net, consumption, generation, net-without-battery, battery power and state of charge. Trimmed to a rolling 48 hour window |
| `meter_totals` | `meter_id` | cumulative consumed, generated, net kWh and last power per meter |

Engine state is deliberately absent. See ADR-0006.

## How a tick works

1. `NeighbourhoodSimulator.Advance()` advances the clock, samples the weather,
   and walks the Energy structure in a fixed order, producing one signed
   `PowerReading` per meter. Fixed order matters: floating point addition is not
   associative, so a varying order would break reproducibility.
2. Those readings are summed. **That sum is the net load the neighbourhood would
   have had with no battery at all**, and it is kept.
3. `PeakShavingStrategy.Decide()` sees that number plus the battery's state of
   charge, capacity and power rating. Nothing else - no houses, no weather, no
   calendar. It returns a `StorageSetpoint`, which is a command.
4. `BatterySimulator.Apply()` clamps the command to what is physically possible,
   applies round-trip losses, updates state of charge, and returns a
   `PowerReading` - what actually happened, which differs from what was asked
   whenever the battery could not comply.
5. `EnergyLedger.Post()` takes every reading including the battery's, accumulates
   per meter, and settles against the grid. Net positive is an import, net
   negative an export, never both.
6. The snapshot is projected, carrying both the with-battery and without-battery
   figures.

Step 2 is why the peak-shaving visualisation needs no second simulation run.

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
sourcing and tariffs. An in-process tick bus was built and then deleted for
having no subscribers (ADR-0004). Each is discussed in `assumptions.md` under Limitations,
with the seam that would let it in. Building any of them at this scope would
signal poor judgement rather than depth.
