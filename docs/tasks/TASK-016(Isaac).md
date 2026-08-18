---
# === EXECUTION CONTEXT ===
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: refactor/domain-refinement
client: Utilus
project: EnergySimulator
module: Assumptions

# === TASK METADATA ===
task_id: TASK-016
title: Name the battery's physical rules (Isaac)
type: refactor
priority: high
status: done
created: 2026-08-18
updated: 2026-08-18

# === GROUPING ===
epic: Domain refinement

# === DEPENDENCIES ===
depends_on: [TASK-015]
blocks: []
---

## Objective

`BatterySimulator.Apply()` hides six business rules inside arithmetic lines.
Each rule gets a name, so the method reads as the physics it implements.
Behaviour-preserving: same operations, same order, same doubles.

## Context - review finding (Isaac)

> BatterySimulator is hiding important business rules in the lines inside its
> methods.

The rules currently buried:

1. The power rating is law - a setpoint is a request, the clamp is unnamed.
2. Losses split evenly across the legs - sqrt(roundTrip) hides in a FIELD
   INITIALIZER, the single most important modelling decision in the class.
3. On charge, the METER pays the loss - `free / legEfficiency` converts cell
   room into metered energy, unexplained.
4. On discharge, the CELLS pay the loss - `SoC * legEfficiency`, unexplained.
5. Reported power is the interval average - `meteredKwh / hours`, the same
   energy-first honesty as the EV charger, unstated.
6. State of charge is physically bounded - the final clamp reads as a band-aid
   because the invariant it guards has no name.

## Functional Requirements

- [x] RF-01: `Apply()` reads as the process: clamp to the rating, then charge,
      discharge or idle, then report the average power. No arithmetic in it.
- [x] RF-02: Each of the six rules above becomes a named member whose doc
      comment states the rule in one sentence.
- [x] RF-03: The identical expressions move - not change. Same operations in
      the same order, so the doubles are bit-identical.

## Non-Functional Requirements

- [x] RNF-01: All battery scenarios pass unchanged; golden master intact;
      whole suite green.
- [x] RNF-02: 100 percent line and branch coverage holds on the class.
- [x] RNF-03: Simpler to read, not more ceremony (standing directive).

## Acceptance Criteria

1. A reader can state the loss model from the member names alone.
2. Suite 234 green, coverage 100 percent on BatterySimulator.
