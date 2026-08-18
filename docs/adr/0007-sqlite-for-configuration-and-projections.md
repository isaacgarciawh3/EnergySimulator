# ADR-0007: SQLite for configuration and the read-side projection

Status: accepted
Date: 2026-08-18

## Context

Two things need to outlive a request: the configuration the neighbourhood is
built from, and the 24 hour history the chart draws. The evaluator must be able
to run everything with one command on a machine we do not control.

## Decision

SQLite, behind two ports.

- `ISimulationConfigurationStore` - the configuration row. The table is empty on
  first container start, so the default seed is written and the simulation boots
  from it. The configuration page overwrites it and it survives restarts.
- `IProjectionStore` - the CQRS read side: `tick_history` for the chart and
  `meter_totals` for per-meter cumulative energy.

Schema creation is idempotent, so restarting against a mounted volume is a
no-op. Per-meter writes are batched in a transaction.

## Consequences

- No database container, no connection string to configure, no second service
  to wait for in a healthcheck. `docker compose up` and `dotnet run` behave the
  same way, which satisfies the "must also run without Docker" constraint.
- The read model is genuinely separate from the write model. Swapping in Redis
  or a time-series database is an adapter change.
- Not built to be concurrent across processes. One writer, and the tick loop
  holds a lock. Correct for a single container, and it would need revisiting the
  moment the worker moves out.
- `tick_history` is trimmed to a rolling window so it cannot grow without bound.

## Alternatives rejected

**Postgres in compose.** The realistic production choice and it adds a container,
a healthcheck, a startup dependency and a failure mode, to store a few thousand
rows the reviewer will never query directly.

**Entity Framework Core over SQLite.** Migrations, a `DbContext` and a mapping
layer for three tables with no relationships. `Microsoft.Data.Sqlite` with small
explicit repositories is less code and no ceremony.

**In-memory only.** Simplest, and the configuration would not survive a restart,
which is precisely what "configure the seed and re-run" needs.
