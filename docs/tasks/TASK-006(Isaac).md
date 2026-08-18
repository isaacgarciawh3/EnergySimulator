---
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: main
client: Utilus
project: EnergySimulator
module: Assumptions
task_id: TASK-006
title: REST API + background simulation loop (Isaac)
type: feature
priority: critical
status: done
created: 2026-08-18
updated: 2026-08-18
epic: Utilus home assignment
depends_on: [TASK-005]
blocks: []
---

## Objective
Minimal API endpoints (GET /api/dashboard, GET/POST /api/config, POST /api/control), each handler <=5 lines delegating to a use case; BackgroundService advancing ticksPerSecond (fractional carry, capped burst); static files for the two pages.
