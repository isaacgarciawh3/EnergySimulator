---
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: main
cliente: Utilus
projeto: EnergySimulator
modulo: Assumptions
task_id: TASK-003
titulo: Simulation and Accounting domain core (Isaac)
tipo: feature
prioridade: critica
status: substituida_por_TASK-009
criado_em: 2026-08-18
atualizado_em: 2026-08-18
epico: Utilus home assignment
depende_de: [TASK-002]
bloqueia: []
---

## Objective
Implement both bounded contexts: Contracts (Kilowatts/KilowattHours VOs, MeterReading, TickReport), Simulation (deterministic weather, clock, 5 asset strategies, neighbourhood fixed-order settlement, seeded factory), Accounting (EnergyLedger over the contract only).

## Acceptance
- Sign convention ADR-002 (consumption +, generation -)
- Determinism: pure hash noise (seed, stream, point); no DateTime.Now/Random/Guid
- House invariant: base load always present; exactly 30 houses / 6 chargers enforced in constructors

## Result
DONE — commit 53bfd4b. Domain builds with warnings-as-errors.
