# Neighbourhood Energy Simulation

Utilus home assignment — a deterministic, tick-based simulation of a
neighbourhood (30 houses, 6 public EV chargers) with live animated dashboard.

## Run

```
docker compose up --build
```

Then open http://localhost:8080 — dashboard. http://localhost:8080/config.html — configuration.

Without Docker:

```
dotnet run --project src/Sim.Api
```

(Design overview, assumptions and ADRs: see `docs/`. AI prompt log: `AI - Prompts/`.)
