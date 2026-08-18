---
# === EXECUTION CONTEXT ===
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: feat/scenario-config-from-file
cliente: Utilus
projeto: EnergySimulator
modulo: Assumptions

# === TASK METADATA ===
task_id: TASK-014
titulo: Boot the scenario from the configuration file, behind a repository (Isaac)
tipo: feature
prioridade: critica
status: done
criado_em: 2026-08-18
atualizado_em: 2026-08-18

# === GROUPING ===
epico: Utilus home assignment

# === DEPENDENCIES ===
depende_de: [TASK-013]
bloqueia: []
---

## Objective

Fill the simulation configuration at boot from the JSON configuration file
rather than from C# literals, expose it for editing through the API and the
configuration page, and put the persistence behind an explicit repository.

## Context

Requirement 4 of the assignment has been there since the first prompt:

> The system must allow the neighbourhood to be defined in a configurable way,
> e.g.: a fixed seed random generator + stated proportions; a configuration file
> (JSON/YAML); code-based configuration.

TASK-013 moved the PHYSICS into `appsettings.Simulation.json`. The SCENARIO did
not move, and that is the half that matters most to a reader: seed, start
instant, tick size, speed, the 40/30/20 distribution and the battery sizing are
still twelve literals in `SimulationConfiguration.Default`.

Verified on `feat/context-boundary-and-battery`:

- `src/Sim.Api/Program.cs:12` reads the JSON file, but only binds
  `SimulationParameters` from it.
- `src/Sim.Application/Configuration/SimulationConfiguration.cs` declares
  `Default` as twelve hardcoded literals.
- `src/Sim.Infrastructure/Persistence/SqliteConfigurationStore.cs:32` calls
  `Save(SimulationConfiguration.Default)` when the table is empty, so on a first
  boot the hardcoded literal IS the source of truth for the whole world.

Isaac's direction: wire the boot to fill configuration from the file, allow the
configuration page to change it through the API, and route model modification
through a repository.

## Functional Requirements

- [x] RF-01: `appsettings.Simulation.json` gains a `Scenario` section holding
      seed, start instant, tick minutes, ticks per second, the three asset
      shares, and every battery field.
- [x] RF-02: Bound to a validated `ScenarioSettings` at startup. Invalid values
      fail the boot with a message naming the field, never a silent fallback.
- [x] RF-03: `SimulationConfiguration.Default` stops being the seed of record.
      The file is authoritative; the literal survives only as the last-resort
      fallback when the file is absent, and says so.
- [x] RF-04: On first boot, the repository is empty and is seeded from the file.
      On later boots the persisted row wins, because the operator has since
      changed it through the UI.
- [x] RF-05: `ISimulationConfigurationStore` becomes
      `ISimulationConfigurationRepository` with repository semantics: `Find()`
      returning null when absent, `Save()`, and `Exists()`. The "load or seed"
      decision moves OUT of the adapter and into the application layer, where
      the policy belongs - an adapter should not decide what the defaults are.
- [x] RF-06: `PUT /api/simulation/configuration` persists through the repository
      and rebuilds the model. Already partly true; must remain true after the
      refactor and be covered by a test.
- [x] RF-07: The configuration page can change every field in RF-01 and the
      change survives a restart.
- [x] RF-08: A documented reset path back to the file-provided scenario, so a
      reviewer who breaks the configuration can recover without deleting
      the database by hand.

## Non-Functional Requirements

- [x] RNF-01: The invariants stay untouchable. No file value and no API payload
      can produce anything other than 30 houses and 6 public chargers.
      `NeighbourhoodInvariants` remains the guard; this task must not add a
      second, weaker check.
- [x] RNF-02: Determinism preserved - same file plus same seed, same run.
- [x] RNF-03: The application still starts with the file absent.
- [x] RNF-04: Zero build warnings; the whole suite stays green.
- [x] RNF-05: No domain project learns that a file or a database exists.

## Technical Specification

```
appsettings.Simulation.json
  Simulation:Scenario   -> ScenarioSettings -> SimulationConfiguration   (NEW)
  Simulation:*          -> SimulationParameters                          (TASK-013)

Sim.Application/Ports
  ISimulationConfigurationRepository { Find(); Save(); Exists(); }        (RENAMED)

Sim.Application/Engine
  SimulationEngine.Start() -> repository.Find() ?? scenario from file     (POLICY MOVES HERE)

Sim.Infrastructure/Persistence
  SqliteSimulationConfigurationRepository                                 (RENAMED)
```

## Acceptance Criteria

1. Editing the seed in the JSON file and deleting the database produces a
   visibly different neighbourhood, with no recompilation.
2. Changing the seed on the configuration page overrides the file, and the
   change survives a restart.
3. Deleting the JSON file still starts the application.
4. No value in the file or any API payload can change the house or charger
   count, proven by a test.
5. Whole suite green.

## Restrictions

- No new NuGet packages. JSON only - .NET binds it natively and YAML would add
  a dependency for no gain. The requirement says "JSON/YAML", not "YAML".
- Do not weaken or duplicate the invariants.
