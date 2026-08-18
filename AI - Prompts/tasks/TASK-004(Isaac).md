---
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: main
cliente: Utilus
projeto: EnergySimulator
modulo: Assumptions
task_id: TASK-004
titulo: SQLite persistence: boot seeding + config from the web (Isaac)
tipo: feature
prioridade: critica
status: rascunho
criado_em: 2026-08-18
atualizado_em: 2026-08-18
epico: Utilus home assignment
depende_de: [TASK-003]
bloqueia: []
---

## Objective
Add SQLite (EF Core) as the driven persistence adapter: on container start the database is created and seeded with the default simulation config; the Configuration page reads/updates it; the dashboard projection (24h series + meter totals) is stored in SQLite — making the CQRS read side a real database, per the original architecture vision.

## Scope (proposed — awaiting Isaac's correction)
- ConfigRecord table (single row: seed, shares, tickMinutes, start) — seeded at boot if absent; POST /api/config updates row + restarts sim.
- SeriesPoint table: 1 row per tick (aggregate kW), pruned to the last 24 simulated hours.
- MeterTotal table: upsert per meter per tick (cumulative kWh) — batched in the same SaveChanges.
- Sim state (EV sessions) NOT persisted: restart = deterministic replay from seed. Documented.
- Port stays ISimulationStore / IDashboardQueries; SQLite is an adapter — swappable, arch-tested.

## Restrictions
- Sqlite file in a volume in compose; EF packages already cached (10.0.11).
- No migrations ceremony: EnsureCreated at boot (assignment scope, documented trade-off).
