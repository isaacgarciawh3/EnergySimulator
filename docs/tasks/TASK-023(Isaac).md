---
# === EXECUTION CONTEXT ===
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: refactor/domain-refinement
client: Utilus
project: EnergySimulator
module: Assumptions

# === TASK METADATA ===
task_id: TASK-023
title: Weather rule classes under the conventions, with the fluent clause (Isaac)
type: refactor
priority: high
status: done
created: 2026-08-18
updated: 2026-08-18

# === GROUPING ===
epic: Domain refinement

# === DEPENDENCIES ===
depends_on: [TASK-021, TASK-022]
blocks: []
---

## Objective

Apply the house conventions to the weather rule cluster - the audit's largest
remaining block (F1+F2 in six files, all pure functions) - under the freshly
decided fluent clause. Plus the two stragglers: SimulationClock's member
comment and the shared kernel's two member docs.

## The fluent clause applied (TASK-018 directive 6)

KEEP their prepositions - pure, and the call site reads as prose:
AnnualCycle.At, WeatherModel.At, SmoothNoise.At, Seasons.Of, Kilowatts.Over,
KilowattHours.Over.

GET their verbs - nouns that do not read as sentences:

| Today | Becomes |
|---|---|
| SolarGeometry.DayLengthHours | MeasureTheDayLengthHours |
| SolarGeometry.SunriseHour / SunsetHour | FindTheSunriseHour / FindTheSunsetHour |
| SolarGeometry.ClearSkyFactor | RateTheClearSky |
| SolarGeometry.IrradianceFactor | AttenuateByCloud |
| TemperatureModel.SeasonalMeanC | AverageTheSeasonalTemperatureC |
| TemperatureModel.DiurnalOffsetC | OffsetByTimeOfDayC |
| TemperatureModel.NoiseOffsetC | OffsetByNoiseC |
| CloudModel.CoverFraction | CoverTheSky |

## Requirements

- [x] RF-01: Member comments stripped across AnnualCycle, TemperatureModel,
      CloudModel, SolarGeometry, SmoothNoise, WeatherParameters,
      SimulationClock and the kernel Units - one summary per type, rules in
      the names.
- [x] RF-02: The renames above, call sites and scenario tests updated.
- [x] RNF-01: Golden master identical; suite green; 100 percent lines and
      branches on every touched class.
