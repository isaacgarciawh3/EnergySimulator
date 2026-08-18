---
# === EXECUTION CONTEXT ===
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: refactor/domain-refinement
client: Utilus
project: EnergySimulator
module: Assumptions

# === TASK METADATA ===
task_id: TASK-019
title: Apply the house conventions to the SimulationRun root (Isaac)
type: refactor
priority: high
status: done
created: 2026-08-18
updated: 2026-08-18

# === GROUPING ===
epic: Domain refinement

# === DEPENDENCIES ===
depends_on: [TASK-018]
blocks: []
---

## Objective

SimulationRun was written in TASK-015, BEFORE the conventions existed
(TASK-018 directives 1-5). Bring the root up to the same standard as the
battery, and close the convention PR - the series must not grow further.

## What violates the conventions today, precisely

1. Layout: public members sit ABOVE the private ones (directive 1 wants
   privates before publics).
2. Member comments everywhere - Advance, ApplyStorageSetpoint, Storage,
   MeasureEveryMeter, CreateBehaviourFor all carry docs whose content belongs
   in the class summary or in the names (directive 2).
3. `OccupiedChargePoints()` is a method named as a NOUN (directive 3).

What already complies and must not churn: fields on top, constructor next,
verbs on the other methods, no member reading mutable state implicitly.

## Requirements

- [x] RF-01: Layout per directive 1; all member comments removed; one class
      summary carrying the root's invariants, the step ordering that yields
      the counterfactual, and the IoT-swap note.
- [x] RF-02: `OccupiedChargePoints` renamed with a verb
      (CollectOccupiedChargePoints).
- [x] RNF-01: Behaviour-preserving: golden master fingerprint identical,
      suite green, 100 percent lines and branches held on the root.
- [x] RNF-02: This closes the convention series - PR ready to merge.
