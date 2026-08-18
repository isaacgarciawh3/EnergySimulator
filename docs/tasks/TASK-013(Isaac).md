---
# === EXECUTION CONTEXT ===
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: feat/context-boundary-and-battery
client: Utilus
project: EnergySimulator
module: Assumptions

# === TASK METADATA ===
task_id: TASK-013
title: Move hardcoded asset parameters into an external configuration file (Isaac)
type: refactor
priority: high
status: done
created: 2026-08-18
updated: 2026-08-18

# === GROUPING ===
epic: Utilus home assignment

# === DEPENDENCIES ===
depends_on: [TASK-011, TASK-012]
blocks: []
---

## Objective

Move the asset parameter values out of C# and into a JSON configuration file
bound at startup, so the shape of the neighbourhood is data rather than code.

## Context

Review finding raised by Isaac: the simulation hardcodes values instead of
consuming a JSON or YAML file, which the assignment lists as a configuration
option.

The finding needs splitting, because not all of the numbers are the same kind of
thing and one group is hardcoded correctly.

**Correctly constant - must NOT become configuration.** The assignment says
"exactly 30 houses" and "exactly 6 public chargers". Those are constraints, not
settings. They live as constants enforced in the `Neighbourhood` constructor,
and making them configurable would let a configuration file violate a stated
requirement.

**Already configurable, persisted, editable at runtime.** The asset
distribution, seed, start instant, tick size, speed and every battery parameter
live in `SimulationConfiguration`, are stored in SQLite and are editable through
the API. Their DEFAULTS are C# literals, which is what makes it look hardcoded
at a glance.

**Genuinely hardcoded, and the real finding.** The physical parameters are magic
numbers buried in the builder and the behaviours:

- base load 0.2 to 0.6 kW per house
- PV 3.0 to 8.0 kWp
- heat pump 0.10 to 0.15 kW per degree, 3.0 kW cap, 15 C balance point
- home EV charger 7.4 kW, sessions 8 to 12 kWh, plug-in 17:30 to 19:00, departure 07:00
- public charger 11.0 kW, sessions 10 to 40 kWh, arrival rates per time band
- the base load daily shape curve

Changing any of these means editing and recompiling C#, and none of them are
visible to someone reading a config file to understand the scenario.

## Functional Requirements

- [x] RF-01: `appsettings.Simulation.json` holding every parameter in the third
      group above, bound to a typed options record at startup.
- [x] RF-02: The file ships with the current values, so behaviour is unchanged
      on upgrade and the diff is provably behaviour-neutral.
- [x] RF-03: Values validated on binding, with a clear failure at startup rather
      than a silent nonsense simulation.
- [x] RF-04: 30 houses and 6 chargers stay as constants and are explicitly NOT
      in the file. Document why in the file's own header comment.
- [x] RF-05: The runtime-editable configuration continues to win over the file,
      so the configuration page is unaffected.
- [ ] RF-06: README documents the file, what is in it and what deliberately is
      not.

## Non-Functional Requirements

- [x] RNF-01: Determinism preserved - same file plus same seed, same run.
- [x] RNF-02: The application still starts with the file absent, falling back to
      the shipped defaults.

## Acceptance Criteria

1. Changing PV capacity range in the JSON file visibly changes generation with
   no recompilation.
2. Deleting the file still starts the application.
3. No magic physical number remains in `NeighbourhoodBuilder` or the behaviours.

## Outcome - deferral reversed

Initially deferred with under an hour left, on the grounds that the visualisation
carried more assessed weight. Isaac reversed that: the company asked for a
JSON/YAML file, so it gets built.

Implemented on the same branch as TASK-011 and TASK-012:

- `src/Sim.Api/appsettings.Simulation.json` holds every group-3 parameter, with
  a header comment recording why the house and charger counts are NOT in it.
- `SimulationParameters` binds and validates it at startup; invalid values fail
  the boot rather than producing a plausible-but-wrong simulation.
- The Simulation context takes its own `SimulationProfiles` records rather than
  the options class, so the file format can change without touching a behaviour.
- The file is optional; absent, shipped defaults apply and the app still starts.
- Behaviour is unchanged with the shipped file, so the diff is provably neutral.

Decision recorded as ADR-0011.
