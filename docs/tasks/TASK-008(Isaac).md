---
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: main
client: Utilus
project: EnergySimulator
module: Assumptions
task_id: TASK-008
title: Tests and docs: conservation property, determinism, architecture, README/ADRs (Isaac)
type: test
priority: critical
status: done
created: 2026-08-18
updated: 2026-08-18
epic: Utilus home assignment
depends_on: [TASK-005]
blocks: []
---

## Objective
FsCheck property: generation + import == consumption + export every tick (explicit tolerance); ledger closure property; determinism fact (same seed twice = identical series); weather influence tests (cold->HP up, cloud->PV down); NetArchTest (Domain isolated; Simulation x Accounting mutually blind; Application !-> Infrastructure). Docs: README, ADR-001..005, docs/assumptions.md, design overview with mermaid. AI - Prompts kept current.
