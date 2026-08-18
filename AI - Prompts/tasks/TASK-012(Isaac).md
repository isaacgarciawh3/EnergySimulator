---
# === EXECUTION CONTEXT ===
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: feat/context-boundary-and-battery
client: Utilus
project: EnergySimulator
module: Assumptions

# === TASK METADATA ===
task_id: TASK-012
title: Neighbourhood battery and peak shaving (Isaac)
type: feature
priority: critical
status: done
created: 2026-08-18
updated: 2026-08-18

# === GROUPING ===
epic: Utilus home assignment

# === DEPENDENCIES ===
depends_on: [TASK-011]
blocks: [TASK-007]
---

## Objective

Add a neighbourhood battery that charges and discharges to reduce peak load,
with a control strategy and a visualisation that demonstrates the effect.

## Process note - written retroactively

This task was written AFTER the code, which is the second time in this project
that has happened and it should not have. The requirement arrived mid-refactor
and was folded into the TASK-011 branch without a task of its own. Recorded here
rather than backdated, because the task register is meant to be the source of
truth and quietly pretending the order was correct would defeat its purpose.

## Context

New requirement, 120 minutes, delivered mid-build:

> Add a neighbourhood battery that can charge/discharge to reduce peak load.
> Battery has capacity (kWh), max charge/discharge power (kW), round-trip
> efficiency (optional). A control strategy that aims to reduce peaks. A
> visualisation that demonstrates impact, highlighting the peak shaving effect.

The battery is the first requirement that tests whether the bounded contexts
were drawn correctly, because it does not fit in any existing one. Applying the
same test that fixed the Energy boundary - replace the simulation with real IoT
telemetry and see what survives - gives the answer:

- the battery's nameplate data is real-world description -> Energy
- its physical response to a command is simulated today, telemetry tomorrow -> Simulation
- the peak-shaving policy survives the swap unchanged -> its own context, Control

## Functional Requirements

- [x] RF-01: `Battery` in Energy: meter id, capacity kWh, max power kW,
      round-trip efficiency. Nameplate data only, no behaviour.
- [x] RF-02: `Sim.Control` context with `IStorageControlStrategy`, `GridState`
      in and `StorageSetpoint` out. References only the shared kernel.
- [x] RF-03: `BatterySimulator` in Simulation applies a setpoint, clamps to
      power rating and available or free energy, applies losses on each leg as
      the square root of round-trip efficiency, tracks state of charge.
- [x] RF-04: Accounting settles the battery as an ordinary meter. No new
      concept, no new enum - a battery is a meter whose reading changes sign.
- [x] RF-05: Tick order produces both figures for free: non-storage assets are
      measured first, giving net load WITHOUT the battery; control then acts,
      giving net load WITH it.
- [x] RF-06: Peak shaving strategy. Discharge above the 80th percentile of load
      over a rolling 24 hour window, recharge below the 40th. Optional fixed
      ceiling on top, default off.
- [ ] RF-07: Dashboard shows net load with and without battery on the same
      chart, battery power, state of charge, and the peak reduction achieved.
      OUTSTANDING - carried by TASK-007.

## Non-Functional Requirements

- [x] RNF-01: Energy conservation still exact with the battery in circuit, its
      losses appearing as consumption.
- [x] RNF-02: Deterministic. The controller carries a rolling window, which is
      state, but it is filled in fixed order from a deterministic simulation.
- [ ] RNF-03: Tests for the control strategy in isolation - it is a pure
      function of GridState and duration, so it is the cheapest thing in the
      system to test. OUTSTANDING - carried by TASK-008.

## Verification performed

First implementation used a fixed 45 kW threshold and MEASURABLY FAILED:

```
peak WITHOUT battery  107.61 kW
peak WITH battery     107.61 kW
reduction             0.00 kW (0.0%)
battery SoC           0% - pinned empty
```

Winter load sits above 45 kW most of the day, so the battery drained from the
first interval and had nothing left for the evening peak. Replaced with the
percentile strategy (ADR-0010):

```
peak WITHOUT battery  127.32 kW
peak WITH battery     107.61 kW
reduction             19.71 kW (15.5% cumulative since start)
within the 24h window 127.3 -> 64.8 kW (49.1% flatter)
battery SoC           cycling 2% - 68%
conservation          37.582000 == 37.582000 exact
```

Note the two peak figures measure different things. 15.5% is the cumulative
peak since simulation start and is dragged down by the controller's warm-up
day; 49.1% is within the currently visible 24 hour window. Both must be labelled
in the UI, never quoted interchangeably.

## Open points raised by this task

- OP-06: no minimum state-of-charge reserve; the battery runs down to 2%.
- OP-07: the controller is reactive, not predictive. The first simulated day is
  a warm-up, so headline peak reduction understates steady-state performance.
- OP-08: no ramp limits, no degradation, no cycle counting.
- OP-09: peak reduction is reported on two different bases and each must be
  labelled explicitly wherever it appears.

## Restrictions

- No new NuGet packages.
- Control must not see houses, assets, weather or the calendar.
