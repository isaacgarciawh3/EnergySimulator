---
# === EXECUTION CONTEXT ===
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: refactor/domain-refinement
client: Utilus
project: EnergySimulator
module: Assumptions

# === TASK METADATA ===
task_id: TASK-020
title: Business exceptions belong to their domain (Isaac)
type: refactor
priority: high
status: done
created: 2026-08-18
updated: 2026-08-18

# === GROUPING ===
epic: Domain refinement

# === DEPENDENCIES ===
depends_on: [TASK-019]
blocks: []
---

## Objective

Every business-rule refusal in the Simulation context flies as a technical BCL
exception. The rules are business; the exceptions must be too. This closes the
convention series in PR #8.

## Design directives (Isaac, translated)

1. "Pure business exceptions of each domain are handled INSIDE the domain,
   because they are business rules. The aggregate is born with everything or
   not born - the caller passes everything, the aggregate validates, and
   returns a BUSINESS exception whenever a rule is violated."
2. "Exception types sit BELOW the public methods, so they do not disturb the
   reading of the class."
3. One exception type PER CONTEXT, not per rule - the Require message already
   names the field and the rule; thirty classes for thirty sentences is
   ceremony. The caller's only question is: did the domain refuse me, and
   which rule?

## The inconsistency, mapped

Energy already does it right: NeighbourhoodInvariantViolation. Simulation does
not - its rules fly as ArgumentException (WeatherParameters.Require),
ArgumentOutOfRangeException (SimulationClock, SmoothNoise, unknown asset type)
and InvalidOperationException (storage guards). A caller catching those cannot
tell a domain refusal from an escaped null.

## Decision deferred with named triggers (review discussion)

A kernel-level AggregateRoot base with a shared InvariantViolation was proposed
and REJECTED for now: the base would be empty (no shared Id, no domain events,
no identity equality), the kernel must stay minimal (ADR-0005), and nothing
catches these exceptions yet, so a shared base has no consumer. Two named
triggers flip the verdict: (1) domain events / outbox arrive - the base earns
its Events/Raise/Clear body; (2) domain refusals start crossing the HTTP
boundary - the shared base pays for a uniform 422 mapping, and re-parenting the
per-context exceptions is a one-commit, non-breaking change.

## Requirements

- [x] RF-01: SimulationInvariantViolation owned by Sim.Simulation.Domain,
      placed at the bottom of the aggregate root's file, below the public
      members, beside StorageState.
- [x] RF-02: Every business refusal in the context throws it: WeatherParameters
      (keeping the Require shape exactly as praised), SimulationClock,
      SmoothNoise, and the run's storage guards and unknown-asset refusal.
- [x] RF-03: Scenario asserts tighten from the technical types to the business
      type - the tests now prove the refusal is a DOMAIN refusal.
- [x] RNF-01: Golden master identical; suite green; coverage held at 100 percent
      on the touched classes.
