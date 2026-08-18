---
# === EXECUTION CONTEXT ===
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: refactor/domain-guards
client: Utilus
project: EnergySimulator
module: Assumptions

# === TASK METADATA ===
task_id: TASK-024
title: Accounting and Control get their invariant guards (Isaac)
type: bugfix
priority: critical
status: done
created: 2026-08-18
updated: 2026-08-18

# === GROUPING ===
epic: Domain guards

# === DEPENDENCIES ===
depends_on: [TASK-021]
blocks: []
---

## Objective

Close the audit's F5: two contexts accept nonsense silently. This is the only
finding in the inventory that is a LATENT BUG rather than style.

## The bug, precisely

- `EnergyLedger.Post()` accepts a zero or negative duration. `Power.Over()`
  multiplies by it, so every accumulator - per-meter and totals - would absorb
  negative or zero energy and corrupt silently. Nothing in the production path
  sends one today; nothing stops the next caller either.
- `GridState` accepts negative capacity, negative max power, and a state of
  charge outside the cells. The strategy would compute nonsense setpoints from
  it without complaint.

## Requirements

- [x] RF-01: `AccountingInvariantViolation`, owned by the context, below the
      public members of the ledger's file. `Post` refuses a non-positive
      interval, naming the rule.
- [x] RF-02: `ControlInvariantViolation`, owned by the context, co-located with
      the types that refuse. `GridState` is born valid or not born: positive
      capacity, positive max power, state of charge within the cells.
- [x] RF-03: Refusal scenarios in the existing folders, one assert per
      consequence, messages naming field and rule.
- [x] RNF-01: Golden master identical; suite green; 100 percent lines and
      branches on the touched classes.
