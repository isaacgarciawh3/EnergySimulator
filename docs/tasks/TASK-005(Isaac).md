---
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: main
client: Utilus
project: EnergySimulator
module: Assumptions
task_id: TASK-005
title: Application layer: ports, use cases, tick bus, projections (Isaac)
type: feature
priority: critical
status: done
created: 2026-08-18
updated: 2026-08-18
epic: Utilus home assignment
depends_on: [TASK-003]
blocks: []
---

## Objective
SimulationSession + SimulationConfig (validated), ports (ITickBus, ITickObserver, ISimulationState, IDashboardQueries), use cases (GetDashboard, RestartSimulation with 24h warm start, ControlSimulation), InProcessTickBus + projections in Infrastructure. IoC: everything wired by DI in the composition root.
