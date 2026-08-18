---
# === EXECUTION CONTEXT ===
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: refactor/domain-refinement
client: Utilus
project: EnergySimulator
module: Assumptions

# === TASK METADATA ===
task_id: TASK-018
title: Class layout convention and explicit-input physics helpers (Isaac)
type: refactor
priority: high
status: done
created: 2026-08-18
updated: 2026-08-18

# === GROUPING ===
epic: Domain refinement

# === DEPENDENCIES ===
depends_on: [TASK-016, TASK-017]
blocks: []
---

## Objective

Apply the house class-layout convention to BatterySimulator and remove the two
hazards the review found: member comments doing the job names should do, and
computed members that silently read MUTABLE state.

## Design directives (Isaac, translated - standing for every class we touch)

1. "A class must have conventions: private fields on top, then constructors,
   then private methods, and public methods at the bottom."
2. "Remove the comments. Only the summary on the class itself. Clean code and
   BDD tests make comments unnecessary. If you stop writing rotten, untestable
   code, everything is 100 percent understood through organisation, shape,
   convention and good names."
3. "Methods must always have VERBS in their names; fields may be nouns."
4. "Naming convention in the Uncle Bob style, so EVERYONE understands the
   code" - intention-revealing names (Clean Code ch. 2): the name says what it
   does and why it exists, and if a name needs a comment, the name failed.
5. Expression bodies and purity, approved after review discussion: `=>` is
   used only when the body is ONE expression that reads as a sentence - two
   steps means a block body. Purity is promised by the VERB, never by the
   syntax: Convert*/Clamp*/Average* are pure and receive everything by
   parameter; Store*/Take* announce mutation and are the only members allowed
   to mutate. The functional features C# inherited from F# - switch
   expressions, records, pattern matching - are welcome where they make a rule
   read as a sentence.

## The technical finding behind the style point

`RoomLeftInMeteredEnergyKwh` and `DeliverableMeteredEnergyKwh` were computed
properties reading `StateOfChargeKwh` - MUTABLE state - implicitly. Inside
Charge() the property is read and the state mutated two lines later: reorder
those lines and the arithmetic changes silently. A getter over mutable state
looks like a field but behaves like a function of time.

The fix is not going back to fields; it is EXPLICIT INPUT: mutable state enters
the helper as a parameter, so the helper is a deterministic function and the
data flow is visible at the call site. Reading readonly configuration
(_legEfficiency, capacity) stays acceptable - it cannot change after
construction.

## Requirements

- [x] RF-01: BatterySimulator follows the layout: private fields, constructor,
      private methods, public members at the bottom.
- [x] RF-02: No member comments. One class summary carrying the loss model
      (A-010) in two sentences. Every rule previously in a comment must now be
      carried by the member NAME alone.
- [x] RF-03: No private member reads mutable state implicitly: state of charge
      enters every physics helper as a parameter.
- [x] RNF-01: Behaviour-preserving - identical expressions, suite green,
      boundary scenarios (TASK-017) untouched and passing.
- [x] RNF-02: 100 percent lines and branches held.
- [x] RNF-03: The convention governs every class touched from here on -
      applied as we alter, not as a project-wide sweep.
