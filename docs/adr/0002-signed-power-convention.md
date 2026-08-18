# ADR-0002: Power is signed, consumption positive

Status: accepted
Date: 2026-08-18

## Context

A neighbourhood both consumes and generates. Every asset has to report
something, and the aggregation has to combine them without the caller
branching on what kind of asset it is holding.

Mixing up power (kW) and energy (kWh) is the classic defect in this kind of
simulation, and it is silent - the numbers stay plausible.

## Decision

One signed quantity: **consumption is positive, generation is negative.**

`Kilowatts` and `KilowattHours` are separate `readonly record struct` value
objects. Converting between them requires an explicit duration
(`power.Over(duration)`). There is no implicit conversion and no bare `double`
crossing a boundary.

## Consequences

- Aggregation is addition. No branching on asset type anywhere in the sum.
- "PV offsets local load first" needs no special case: the house meter is the
  signed sum of its assets, so surplus only becomes an export once the whole
  neighbourhood nets negative. A documented assumption (A-003) falls out of the
  type rather than being implemented.
- Confusing kW with kWh does not compile.
- Reading a raw negative number as "generation" is a convention a newcomer must
  learn. It is stated in the value object's own documentation.

## Alternatives rejected

**Separate `Consumption` and `Generation` fields.** Explicit, self-documenting,
and it forces every aggregation to handle two cases and every asset to fill in a
zero. Rejected because it pushes branching into every consumer.

**Unsigned magnitude plus a direction enum.** The same problem with an extra
type to keep in sync.

**Plain `double` everywhere.** Rejected. It is exactly how kW gets added to kWh.
