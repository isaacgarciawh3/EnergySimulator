---
# === EXECUTION CONTEXT ===
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: refactor/domain-refinement
client: Utilus
project: EnergySimulator
module: Assumptions

# === TASK METADATA ===
task_id: TASK-017
title: Prove the battery's boundary rules by named scenario (Isaac)
type: test
priority: high
status: done
created: 2026-08-18
updated: 2026-08-18

# === GROUPING ===
epic: Domain refinement

# === DEPENDENCIES ===
depends_on: [TASK-016]
blocks: []
---

## Objective

TASK-016 named six physical rules. The scenario suite proves five of them by
name; the BOUNDARY behaviour of the two loss rules and half of the clamp rule
are only covered incidentally, by the hostile-sequence bounds. What must
improve: every named rule gets a scenario that proves it where it bites.

## What is missing, precisely

1. `RoomLeftInMeteredEnergyKwh` - a charge commanded near a full battery must
   deliver ONLY what the remaining room absorbs, loss-adjusted, and fill the
   cells exactly to capacity. No scenario asserts the partial delivery.
2. `DeliverableMeteredEnergyKwh` - a discharge beyond what the cells hold must
   deliver only their loss-adjusted content and empty them exactly. Same gap.
3. `ClampedToThePowerRating` is proven only in the CHARGE direction. The rule
   says "in both directions"; the discharge direction is untested.

The hostile-sequence fixture asserts the bounds are never crossed - it does not
assert the exact partial delivery, which is where the loss arithmetic lives.

## Requirements

- [x] RF-01: `When_a_charge_is_commanded_near_a_full_battery` - delivers only
      the loss-adjusted room, fills exactly to capacity.
- [x] RF-02: `When_a_discharge_exceeds_what_the_cells_hold` - delivers only the
      loss-adjusted content, empties exactly.
- [x] RF-03: `When_a_discharge_command_exceeds_the_power_rating` - the clamp
      holds in the discharge direction too.
- [x] RNF-01: Scenario standard (ADR-0014); expected values computed from the
      nameplate and the leg efficiency, never copied from the output.
- [x] RNF-02: Suite green; coverage stays 100 percent on the class.
