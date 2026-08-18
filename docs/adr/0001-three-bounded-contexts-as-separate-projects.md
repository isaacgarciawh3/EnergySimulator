# ADR-0001: Bounded contexts, each its own project

Status: accepted
Date: 2026-08-18

## Context

The domain has three concerns that change for different reasons: what time it is
and what the weather is doing; how much power a heat pump draws; and what the
books say. A first cut put all of them in one `Sim.Domain` project separated by
folders named `Simulation/` and `Accounting/`.

Folders are a naming convention. Nothing prevented an accounting type from
referencing a heat pump, and once one such reference exists the boundary is
gone and no reviewer will find it.

## Decision

Three bounded contexts, each a separate project, each with exactly one
aggregate root:

| Context | Owns | Answers |
|---|---|---|
| Energy | `Neighbourhood`, `House`, `Asset`, `Battery` | what exists |
| Simulation | clock, weather, behaviours | what everything is doing right now |
| Control | `IStorageControlStrategy` | what the battery should do about it |
| Accounting | `EnergyLedger` | what all of that means for the books |

**Revised on 2026-08-18.** The first version of this decision put the physics
inside Energy: assets computed their own power from a seed and the weather. That
is the Simulation context living inside Energy, and it fails the test that
matters - replace the simulation with real IoT telemetry and Energy would have
had to be rewritten.

Energy now describes and does not behave. Simulation produces `PowerReading`.
Swapping the producer touches neither Energy nor Accounting. Control arrived
with the battery and is justified separately in ADR-0009.

Each references only `Sim.SharedKernel`. `Sim.Energy` has no project reference
to `Sim.Accounting`, so the coupling is not expressible - the compiler rejects
it before any reviewer has to notice.

## Consequences

- The boundary is structural. Violating it requires deliberately editing a
  `.csproj`, which shows up in a diff.
- Grid settlement moved out of Energy into Accounting. Energy measures;
  Accounting settles. This is the separation made concrete rather than claimed.
- Any cross-context flow needs translation, which costs code. See ADR-0005.
- Seven projects for a system this size is more ceremony than a single project
  would need. We accept that cost because modularity and separation of concerns
  are explicitly what this work is assessed on, and because it is the structure
  that would survive the system growing.

## Alternatives rejected

**One project, folders per context.** What we started with. Cheapest, and the
boundary is a promise rather than a fact. Rejected because the whole point of
naming bounded contexts is to stop them bleeding.

**One project per context per layer** (`Sim.Energy.Domain`,
`Sim.Energy.Application`, and so on). More faithful to a hexagon per context,
and the right answer if each context were separately deployable. Rejected as
disproportionate: it roughly doubles the project count to express a boundary we
already get from the compiler, inside a single deployable.

**Separate services.** Rejected outright. Microservices at this scope would
signal poor judgement rather than seniority, and the assignment is four hours.
