---
# === EXECUTION CONTEXT ===
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: refactor/domain-refinement
client: Utilus
project: EnergySimulator
module: Assumptions

# === TASK METADATA ===
task_id: TASK-021
title: Convention review - audit of the whole source against the house rules (Isaac)
type: review
priority: high
status: draft
created: 2026-08-18
updated: 2026-08-18

# === GROUPING ===
epic: Domain refinement

# === DEPENDENCIES ===
depends_on: [TASK-020]
blocks: []
---

## Objective

Audit every source file against the house conventions (TASK-018 directives 1-5,
TASK-020 business exceptions, ADR-0013/0014) and record the violations as fact,
so the fixes can be prioritised as focused tasks instead of a blind sweep.
This task CHANGES NO CODE - it is the inventory.

## Method

Scripted scan over `src/**` counting member-level summaries beyond the class
summary (directive 2), inline `//` comments (directive 2), and method names
whose first word is not a verb (directive 3), plus manual reading for layout
(directive 1) and exception gaps (TASK-020).

## Findings

### F1 - Member comments and inline comments (directive 2)

Worst offenders, in violations-per-file order:

| File | Member summaries | Inline `//` |
|---|---|---|
| WeatherParameters.cs | 2 | 9 |
| SimulationEngine.cs (Application) | 5 | 8 |
| SolarGeometry.cs | 4 | 0 |
| SmoothNoise.cs | 3 | 2 |
| TemperatureModel.cs | 3 | 0 |
| PeakShavingStrategy.cs (Control) | 2 | 1 |
| Neighbourhood.cs (Energy) | 2 | 2 |
| Units.cs (SharedKernel), Ports.cs | 2 | 0 |
| SimulationClock, AnnualCycle, ScenarioSettings | 1 each | 0 |
| Program.cs (composition root) | 0 | 6 |

### F2 - Methods without verbs (directive 3)

The one that matters most: `IAssetBehaviour.PowerAt` - THE contract method of
every behaviour is a noun. Renaming it touches the interface, five behaviours
and the root.

Noun methods in the weather rules: `DayLengthHours`, `SunriseHour`,
`SunsetHour`, `IrradianceFactor` (SolarGeometry); `SeasonalMeanC`,
`DiurnalOffsetC`, `NoiseOffsetC` (TemperatureModel); `CoverFraction`
(CloudModel); `Percentile` (PeakShavingStrategy); `ArrivalsPerHour` (Profiles,
twice); `CategoryOf`, `OwnerOf` (engine); `TypeOf` (Neighbourhood);
`StreamOf` (DeterministicNoise); `Validated` (SimulationConfiguration).

### F3 - The fluent trio: a DECISION, not a finding

`WeatherModel.At(instant)`, `Seasons.Of(month)`, `Kilowatts.Over(duration)`,
`AssetDistribution.Of(houses)` read as prose precisely BECAUSE they are not
verbs - fluent DSL style. The rule says verbs always. Two coherent outcomes:

- (a) STRICT: rename to `Sample(instant)`, `Classify(month)`,
  `ConvertOver(duration)` - uniform, loses the prose.
- (b) FLUENT EXCEPTION: one written exception clause - prepositions/`Of` are
  allowed ONLY on pure functions whose call site reads as an English sentence
  (`model.At(noon)`, `power.Over(quarter)`). Everything else needs its verb.

The reviewer decides; the fix task inherits the decision.

### F4 - Layout (directive 1) not yet applied outside the battery and the root

PeakShavingStrategy, EnergyLedger, Neighbourhood, both SQLite adapters and the
engine still carry publics above privates. Mechanical to fix; zero risk under
the golden master.

### F5 - Exception gaps (TASK-020 pattern incomplete across contexts)

- Accounting has NO guards at all: `EnergyLedger.Post()` accepts a negative or
  zero duration and would silently corrupt every accumulator. The context needs
  its `AccountingInvariantViolation` and at least the duration rule.
- Control likewise: `GridState` accepts negative capacity and negative max
  power without complaint.
- Energy's exception exists but `House`/`Neighbourhood` inline comments still
  narrate what invariant names already say.

### F6 - Out of domain scope, listed for completeness

SimulationEngine (Application) is the single most commented file in the
repository and its helpers (`CategoryOf`, `OwnerOf`) are nouns; Program.cs
carries 6 inline comments in the composition root. Same treatment applies when
we decide to touch the application layer.

## Proposed follow-up tasks, in value order

1. TASK-022 - Weather rule classes: strip comments, verb the methods, layout
   (biggest cluster: F1+F2 in five files, all pure functions, golden-master safe).
2. TASK-023 - `IAssetBehaviour.PowerAt` rename + behaviours file split and
   cleanup (one file per behaviour class, matching the test folders).
3. TASK-024 - Accounting and Control invariant exceptions (F5) - new guards,
   new scenarios proving them.
4. TASK-025 - Energy and Control layout + comment sweep (F4).
5. Application layer (F6) - when its refactor series opens.

## Requirements

- [x] RF-01: Findings recorded as fact with counts and names, no code changed.
- [ ] RF-02: Reviewer decision on F3 (strict vs fluent exception) recorded here.
- [ ] RF-03: Follow-up tasks approved/reordered by the reviewer.
