# ADR-0003: Tick size of 15 simulated minutes, configurable

Status: accepted
Date: 2026-08-18

## Context

The assignment leaves the step size to us and asks for the reasoning. It also
asks for a chart covering the last 24 simulated hours, and for an animation that
is worth watching.

Two independent quantities get confused here: how much simulated time one tick
represents, and how fast ticks are produced in real time.

## Decision

Tick size defaults to **15 simulated minutes** and is a configuration value, not
a constant. Simulation speed is a **separate** configuration value in ticks per
real second.

At the default, one simulated day is 96 ticks. The chart holds 96 points and the
window sweeps in about twelve seconds at eight ticks per second.

## Consequences

- 96 points per day is enough to redraw continuously without downsampling.
- EV charging sessions and heat pump behaviour stay visible. At an hourly step,
  a 90 minute charging session becomes one or two indistinguishable samples.
- Energy per tick is power multiplied by a quarter hour, held constant across
  the interval - a left Riemann sum. At 15 minutes this is a real approximation
  and it is the reason charging sessions report interval-average power rather
  than instantaneous power, so the final partial interval accounts exactly.
- Because the two knobs are separate, changing how fast the animation runs
  cannot change the physics. That separation is what keeps a fast run and a slow
  run byte-identical.

## Alternatives rejected

**One minute.** 1440 points per day, fourteen times the storage and redraw cost,
no additional insight at neighbourhood scale.

**One hour.** 24 points. Cheap and smooth, but it erases the charging sessions
and the morning and evening peaks, which are most of what makes the chart
interesting.

**Hard-coding the value.** Rejected because the assignment asks for a
configurable neighbourhood, and because a constant invites the two quantities
above to be conflated.
