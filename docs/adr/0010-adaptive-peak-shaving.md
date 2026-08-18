# ADR-0010: Peak shaving on percentiles, not on a fixed threshold

Status: accepted
Date: 2026-08-18

## Context

The requirement offers two shapes for the control strategy: keep load below a
threshold, or discharge during the top N per cent of load periods.

We implemented the fixed threshold first, at 45 kW. It was measured at runtime
and **it did not work**:

```
threshold             45 kW
peak WITHOUT battery  107.61 kW
peak WITH battery     107.61 kW
reduction             0.00 kW  (0.0%)
battery SoC           0%       (pinned at empty)
intervals above threshold: 42 without battery -> 34 with battery
```

The controller was doing exactly what it was told. Winter neighbourhood load
sits above 45 kW for most of the day, so the battery discharged from the first
interval, hit empty hours before the evening peak, and had nothing left for the
peak it existed to shave. Meanwhile the recharge window - below 60 per cent of
the threshold, so below 27 kW - almost never occurred, so it never refilled.

A fixed threshold is only as good as the guess behind it, and the correct guess
differs by season, by weather and by how many people bought an EV.

## Decision

The strategy discharges above the 80th percentile and recharges below the 40th
percentile of the load observed over a rolling 24 hour window.

An optional fixed ceiling is still supported and applies on top, for the case
where a real connection has a contractual limit. It defaults to off.

## Consequences

- The top band is, by definition, a small minority of intervals whatever the
  season, so there is always stored energy left when the real peak arrives.
- Measured after the change, same seed:

  ```
  peak WITHOUT battery  127.32 kW
  peak WITH battery     107.61 kW
  reduction             19.71 kW  (15.5%)
  within the 24h window 127.3 -> 64.8 kW  (49% flatter)
  battery SoC           cycling 2% - 68%
  ```

- The controller now carries state: a rolling window of recent load. That is
  legitimate control state, and it stays inside the Control context.
- It is reactive, not predictive. It responds to the load distribution it has
  already seen, so the first day is a warm-up and a genuinely novel peak is
  shaved only partially. A forecast-driven controller would do better and is the
  obvious next step.
- Percentiles over a rolling window mean the strategy is not a pure function of
  the current instant. It remains fully deterministic, because the window is
  filled in a fixed order from a deterministic simulation.

## Alternatives rejected

**Fixed threshold alone.** Measured, failed, documented above. Kept as an
optional ceiling because a contractual connection limit is a real thing.

**Charge whenever there is PV surplus.** Simple and appealing, and it optimises
self-consumption rather than peak load. That is a different objective and the
requirement asked for peak shaving.

**Forecast-based optimisation.** Genuinely better and out of scope at this
budget. Named as the next step rather than attempted.
