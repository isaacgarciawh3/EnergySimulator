---
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: main
cliente: Utilus
projeto: EnergySimulator
modulo: Assumptions
task_id: TASK-008
titulo: Tests and docs: conservation property, determinism, architecture, README/ADRs (Isaac)
tipo: test
prioridade: critica
status: aprovada
criado_em: 2026-08-18
atualizado_em: 2026-08-18
epico: Utilus home assignment
depende_de: [TASK-005]
bloqueia: []
---

## Objective
FsCheck property: generation + import == consumption + export every tick (explicit tolerance); ledger closure property; determinism fact (same seed twice = identical series); weather influence tests (cold->HP up, cloud->PV down); NetArchTest (Domain isolated; Simulation x Accounting mutually blind; Application !-> Infrastructure). Docs: README, ADR-001..005, docs/assumptions.md, design overview with mermaid. AI - Prompts kept current.
