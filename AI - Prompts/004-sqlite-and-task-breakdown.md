# 004 — SQLite persistence direction + granular task breakdown

- Date: 2026-08-18
- Outcome: work split into TASK-002..008 (mirrored in tasks/), SQLite adopted
  as the persistence adapter, checkpoint cadence increased

## Prompts (verbatim, mid-build)

> For the simulation I think the best course of action is to use some SqLite.
> And run the seed as the container it started,
> after that we can reconfigured the seed values in the web
> Please dont do everything without output ohterwise I wont have time to correct you

> And we need to write the tasks. it is better this way.
> ans them put the prompt tasks in the project as well

## Resulting decisions

- SQLite (EF Core) becomes the driven persistence adapter: config seeded at
  container start, editable from the Configuration page; the dashboard
  projection (24h series + meter totals) lives in SQLite — the CQRS read side
  becomes a real database as originally envisioned.
- Simulation state (EV sessions) is NOT persisted: a restart is a
  deterministic replay from the seed. Documented trade-off.
- Tasks TASK-001..008 are copied into this folder under tasks/ as part of the
  AI-workflow evidence.
