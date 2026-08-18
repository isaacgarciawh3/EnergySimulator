---
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: main
cliente: Utilus
projeto: EnergySimulator
modulo: Assumptions
task_id: TASK-005
titulo: Application layer: ports, use cases, tick bus, projections (Isaac)
tipo: feature
prioridade: critica
status: aprovada
criado_em: 2026-08-18
atualizado_em: 2026-08-18
epico: Utilus home assignment
depende_de: [TASK-003]
bloqueia: []
---

## Objective
SimulationSession + SimulationConfig (validated), ports (ITickBus, ITickObserver, ISimulationState, IDashboardQueries), use cases (GetDashboard, RestartSimulation with 24h warm start, ControlSimulation), InProcessTickBus + projections in Infrastructure. IoC: everything wired by DI in the composition root.
