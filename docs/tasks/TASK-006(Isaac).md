---
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: main
cliente: Utilus
projeto: EnergySimulator
modulo: Assumptions
task_id: TASK-006
titulo: REST API + background simulation loop (Isaac)
tipo: feature
prioridade: critica
status: concluida
criado_em: 2026-08-18
atualizado_em: 2026-08-18
epico: Utilus home assignment
depende_de: [TASK-005]
bloqueia: []
---

## Objective
Minimal API endpoints (GET /api/dashboard, GET/POST /api/config, POST /api/control), each handler <=5 lines delegating to a use case; BackgroundService advancing ticksPerSecond (fractional carry, capped burst); static files for the two pages.
