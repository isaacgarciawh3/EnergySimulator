# ADR-0012: The scenario is configuration, and configuration lives behind a repository

Status: accepted
Date: 2026-08-18

## Context

ADR-0011 split the numbers in this system into three groups: constraints,
scenario settings and physical parameters. It moved the physical parameters into
`appsettings.Simulation.json` and left the scenario alone, on the grounds that
the scenario was already runtime-editable through the configuration page.

That reasoning was incomplete. The scenario was editable, but its *defaults*
were twelve C# literals in `SimulationConfiguration.Default`, and on a first
boot the persistence adapter wrote those literals into an empty database. The
hardcoded value was therefore the source of truth for every fresh clone - which
is exactly the state a reviewer sees.

There was a second problem hiding underneath. The port was called
`ISimulationConfigurationStore` and its method was `LoadOrSeedDefault()`. That
name is doing two jobs: retrieving a row, and deciding what should exist when
there is no row. The second is a policy decision, and it had leaked into the
SQLite adapter - the one place in the system that has no business knowing what
a sensible default seed is.

## Decision

**The scenario moves into the file.** `appsettings.Simulation.json` gains a
`Scenario` section, bound to a validated `ScenarioSettings`. The file is
authoritative on a first boot. `SimulationConfiguration.Default` survives only
as the last-resort fallback for a missing file, and is documented as such.

**The port becomes a repository** with honest semantics:

```
ISimulationConfigurationRepository
    Find()   -> SimulationConfiguration?    null means "nothing stored yet"
    Save()
    Exists()
```

`Find()` answers a question about storage and nothing else. The "and otherwise
use these defaults" policy moves up into the application layer, where the
defaults come from the file.

**Precedence, stated once:** a persisted row wins over the file, because a
persisted row means an operator has since made a decision through the UI and we
should not silently overrule them on restart. The file wins over the C#
fallback. The C# fallback exists only so the application still starts with no
file at all.

## Consequences

- A fresh clone is configured by a file a reviewer can read, not by a literal
  they have to go find in C#.
- Changing the seed and deleting the database produces a different
  neighbourhood with no recompilation.
- The adapter no longer decides anything. It stores and retrieves.
- Precedence has to be explained, because "the file changed but nothing
  happened" is now a real question a user can ask. It is answered in the README
  and by a reset path back to the file scenario.
- One more binding to validate at startup, and one more way to fail the boot -
  deliberately, because a nonsense scenario should stop the application rather
  than quietly produce a nonsense simulation.

**What does NOT change, and must not:** the house and charger counts stay out
of the file. `NeighbourhoodInvariants` remains the only guard, and no value in
the file or in an API payload can move it. A configuration file that can break a
constraint is not a constraint.

## Alternatives rejected

**Leave the scenario defaults in C#.** Defensible against the letter of the
requirement, since it lists code-based configuration as acceptable. Rejected
because the criticism was never "this breaks the rules", it was "a reader cannot
see the scenario", and that criticism is correct.

**Put the scenario in the database only, with no file.** The database is not
readable in a pull request, does not exist on a fresh clone, and cannot be
reviewed.

**Use YAML.** The requirement says "JSON/YAML". .NET binds JSON natively;
YAML would add a package to produce an identical result.

**A repository over the Neighbourhood aggregate itself.** Tempting for symmetry,
and it would be a factory wearing a repository's name: the neighbourhood is
derived from the seed rather than stored, and calling that retrieval would be
a lie. Configuration is the thing that is genuinely persisted, so configuration
is what gets the repository.
