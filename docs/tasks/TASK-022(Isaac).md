---
# === EXECUTION CONTEXT ===
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: refactor/domain-refinement
client: Utilus
project: EnergySimulator
module: Assumptions

# === TASK METADATA ===
task_id: TASK-022
title: Decompose the three god methods (Isaac)
type: refactor
priority: high
status: in_progress
created: 2026-08-18
updated: 2026-08-18

# === GROUPING ===
epic: Domain refinement

# === DEPENDENCIES ===
depends_on: [TASK-021]
blocks: []
---

## Objective

Three methods carry a whole domain each in one body. Review verdict (Isaac):
"HomeEvChargerBehaviour - too many rules embedded in a single method. Same with
PublicChargerBehaviour. Same with EnergyLedger.Post - terrible writing, a GOD
method." Decompose each into named rules, battery-style, behaviour-preserving
under the golden master. Reprioritised ahead of the audit's own ordering by the
reviewer's call.

## The rules buried today

HomeEvChargerBehaviour.PowerAt - six rules in twenty lines: the seeded plug-in
window, the seeded session size, once-per-day (sentinel _lastPlugDay), the
07:00 departure, delivery limited by rating and remainder, interval-average
reporting. Salts ^7 and ^13 unnamed.

PublicChargerBehaviour.PowerAt - four rules: time-of-day arrival rate, seeded
arrival decision, seeded session size, delivery; busy-rejects implicit. Salts
^17 and ^31 unnamed. Profile method ArrivalsPerHour is a noun.

EnergyLedger.Post - five responsibilities interleaved in one loop: find-or-open
the account, post to it, split by sign in fixed order, settle import-XOR-export,
accumulate four running totals.

## Scope additions during review (Isaac)

- PeakShavingStrategy.Decide + Percentile: "terrible / badly written" - rules
  buried, the window sorted TWICE per tick, index arithmetic cryptic, and the
  "with no history nothing is a peak" rule hiding in a magic double.MaxValue.
- DeterministicNoise: "indecipherable class, magic numbers, zero explanation
  for the reader" - the SplitMix64 and FNV-1a constants carry no names and no
  provenance, and the mixing pipeline is one expression soup.

## Consequence of the standing rules

Altering EnergyLedger pulls the Accounting tests into the scenario standard
(ADR-0014, "converted as we alter"). Altering the two behaviours creates their
scenario folders - they had NONE: the state machines were covered only
indirectly through the run. Both charger scenarios are made fully deterministic
by zero-width profiles (SessionMin == SessionMax, PlugInFrom == PlugInTo) and
certain-or-never arrival rates, so no test reverse-engineers noise.

## Requirements

- [ ] RF-01: Each method reads as its process; every buried rule is a named
      member with a verb; salts become named constants.
- [ ] RF-02: House conventions applied to the three touched classes (layout,
      no member comments, verbs).
- [ ] RF-03: Scenario folders HomeEvChargerBehaviour/, PublicChargerBehaviour/
      and EnergyLedger/ proving the state machines and the ledger rules;
      EnergyAccountingTests converted, FsCheck properties kept as the recorded
      exception to the constructor-act rule.
- [ ] RNF-01: Golden master fingerprint identical; suite green.
- [ ] RNF-02: 100 percent lines and branches on the touched classes.
