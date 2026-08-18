# ADR-0011: Physical parameters in a JSON file, constraints in code

Status: accepted
Date: 2026-08-18

## Context

The requirement offers three acceptable ways to define the neighbourhood: a
fixed seed with stated proportions, a configuration file, or code-based
configuration. Any one of them satisfies it, and we already satisfied it with
the first.

Review nevertheless found a real problem. The numbers in the system are not all
the same kind of thing, and they were being treated as if they were:

1. **Constraints.** "Exactly 30 houses", "exactly 6 public chargers".
2. **Scenario settings.** Seed, asset proportions, tick size, speed, battery
   sizing. Already configurable, persisted in SQLite, editable at runtime.
3. **Physical parameters.** Base load 0.2 to 0.6 kW, PV 3 to 8 kWp, heat pump
   0.10 to 0.15 kW per degree and a 15 C balance point, home charger 7.4 kW and
   8 to 12 kWh sessions, public charger 11 kW and 10 to 40 kWh sessions,
   arrival rates per time band, the daily load shape.

Group 3 was magic numbers scattered through the builder and the behaviours.
Changing the scenario meant editing and recompiling C#, and a reviewer could not
see what the scenario was without reading the source.

## Decision

Group 3 moves to `appsettings.Simulation.json`, bound to a typed
`SimulationParameters` at startup and validated before the application accepts
it.

Group 1 stays as constants enforced in the `Neighbourhood` constructor, and the
file says so in its own header. **This is the load-bearing part of the decision:
if a configuration file could set the house count to 25, then the file could
violate a stated requirement.** A constraint that a config file can break is not
a constraint. Group 2 is unchanged, because runtime-editable configuration is
what the configuration page needs.

The file is optional. Absent, the shipped defaults apply and the application
starts normally.

## Consequences

- The scenario is readable as data. Someone can understand what is being
  simulated without reading C#.
- Changing PV capacity or charger power needs no recompilation.
- Validation happens at startup, so a bad file fails loudly instead of producing
  a plausible but wrong simulation.
- The Simulation context does not consume the options class directly. It defines
  its own small profile records and the application maps into them, so the file
  format can change without touching a behaviour. That is the same
  producer-independence principle as ADR-0009, applied to configuration.
- Two configuration mechanisms now exist - a JSON file for physics and a SQLite
  row for scenario settings. That needs explaining, and the split is: the file
  is what the world is made of, the database is what the operator changed. The
  database wins where they overlap, which is nowhere by construction.

## Alternatives rejected

**Leave it as code.** Permitted by the requirement, and it was already the
weakest part of the submission under a reviewer's eye. "The requirement allows
it" is a poor answer when the criticism is that the code is unreadable.

**Move everything to the file, including the house count.** Rejected for the
reason above: it would let configuration break a stated constraint.

**YAML.** Equivalent in every way that matters here and would add a package.
.NET binds JSON natively.

**Everything into SQLite alongside the scenario settings.** Would make the
physics editable at runtime through the UI, which sounds like a feature and is
mostly a way to let someone accidentally invalidate every accumulated total
mid-run.
