# ADR-0006: Determinism from an explicit seed, not from stored state

Status: accepted
Date: 2026-08-18

## Context

The assignment requires the simulation to be deterministic or at least
reproducible. Separately, we need a persistence story, and persistence of a
running simulation is usually the expensive part.

## Decision

The entire world is a pure function of the configuration record, whose main
field is the seed. No `DateTime.Now`, no unseeded `Random`, no `Guid.NewGuid()`
anywhere inside the contexts.

Randomness comes from a stateless hash (`DeterministicNoise`): the same
`(seed, stream, point)` always produces the same value. Each asset derives its
own stream from its meter identity, so adding an asset does not shift any other
asset's sequence.

Weather is a pure function of instant and seed, not an accumulating random walk.

Aggregation runs in a fixed order, because floating point addition is not
associative and an order that varies would break reproducibility.

## Consequences

- **We do not persist engine state and do not need to.** A restart replays the
  identical world from the seed. Determinism buys a trivial persistence story;
  only the configuration and the read-side projection are stored.
- The 24 hour warm-up at startup is cheap and reproducible, so the chart is full
  and moving on the first paint instead of filling up from empty.
- Any future parallel aggregation must reduce in a fixed order, not in thread
  completion order.
- A restart returns to the configured start instant rather than resuming where
  it was. This is a real limitation, recorded in `assumptions.md`.

## Alternatives rejected

**A seeded `Random` instance per asset.** Deterministic only if every asset is
constructed and drawn from in exactly the same order every run. Adding one asset
shifts every subsequent sequence, so the seed stops meaning anything stable.

**Persisting engine state to resume.** Real work - serialise every charging
session, the clock, the ledger - to buy something determinism already gives.

**A global `Random.Shared`.** Not reproducible at all.
