---
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: main
client: Utilus
project: EnergySimulator
module: Assumptions
task_id: TASK-003
title: Simulation and Accounting domain core (Isaac)
type: feature
priority: critical
status: superseded_by_TASK-009
created: 2026-08-18
updated: 2026-08-18
epic: Utilus home assignment
depends_on: [TASK-002]
blocks: []
---

## Objective
Implement both bounded contexts: Contracts (Kilowatts/KilowattHours VOs, MeterReading, TickReport), Simulation (deterministic weather, clock, 5 asset strategies, neighbourhood fixed-order settlement, seeded factory), Accounting (EnergyLedger over the contract only).

## Acceptance
- Sign convention ADR-002 (consumption +, generation -)
- Determinism: pure hash noise (seed, stream, point); no DateTime.Now/Random/Guid
- House invariant: base load always present; exactly 30 houses / 6 chargers enforced in constructors

## Result
DONE — commit 53bfd4b. Domain builds with warnings-as-errors.
