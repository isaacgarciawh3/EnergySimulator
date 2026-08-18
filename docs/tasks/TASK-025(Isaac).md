---
# === EXECUTION CONTEXT ===
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: refactor/domain-guards
client: Utilus
project: EnergySimulator
module: Assumptions

# === TASK METADATA ===
task_id: TASK-025
title: The Energy invariants move into their aggregates (Isaac)
type: refactor
priority: high
status: in_progress
created: 2026-08-18
updated: 2026-08-18

# === GROUPING ===
epic: Domain guards

# === DEPENDENCIES ===
depends_on: [TASK-024]
blocks: []
---

## Objective

Execute the design the reviewer approved: "shouldn't an aggregate's invariants
live in the aggregate itself?" Yes - Information Expert: whoever holds the data
validates the data. The static `NeighbourhoodInvariants` holder dissolves into
private Refuse* members of `Neighbourhood` and `House` (the pattern
`SimulationRun` already set); the exception type stays and moves to the bottom
of the root's file per the layout convention. Messages preserved EXACTLY -
the scenario asserts pin them.

## Standing-rule consequence

Altering Energy converts its tests: `DomainInvariantTests.cs` and
`NeighbourhoodSpecification.cs` dissolve into `Neighbourhood/` and `House/`
scenario folders; the weather-influence scenarios inside the latter move to
`SimulationRun/` where they always belonged.

## Requirements

- [ ] RF-01: Each invariant is a private Refuse* member of the entity whose
      state it constrains; the static holder is deleted; conventions applied
      to both classes (layout, no member comments).
- [ ] RF-02: Energy test files converted to the scenario standard; the
      negative-rating invariant - which the audit shows was NEVER tested -
      gains its scenario.
- [ ] RNF-01: Golden master identical; suite green; 100 percent lines and
      branches on Neighbourhood, House and AssetDistribution.
