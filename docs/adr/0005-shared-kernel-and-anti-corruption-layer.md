# ADR-0005: A minimal shared kernel, translation for everything else

Status: accepted
Date: 2026-08-18

## Context

Three contexts that cannot reference each other still have to exchange data
every tick. Something has to carry it across, and the choice of what is shared
determines how coupled they really are.

## Decision

**Shared kernel, deliberately tiny:** `Kilowatts`, `KilowattHours` and the
deterministic noise primitive. Physical units are universal vocabulary with no
business semantics; a kilowatt does not mean something different to accounting
than it does to physics. Nothing else is shared.

**Everything else is translated** by `ContextTranslator` in the application
layer. Never by a domain.

The translations deliberately lose information:

| From | To | Dropped |
|---|---|---|
| `TickEnvironment` | `MeasurementContext` | season, cloud cover |
| `MeterReading` | `EnergyEntry` | asset type collapses to consumer or generator |

The Energy context never learns what a season is - physics needs temperature
and irradiance, and irradiance already carries cloud and day length. The
Accounting context never learns what a heat pump is - bookkeeping needs to know
whether something consumed or generated.

## Consequences

- That narrowing is the extensibility argument. Either context can restructure
  its internals without the other noticing, because the other never had access
  to the internals in the first place.
- Every new cross-context field costs an explicit translator edit. That is the
  friction working as intended.
- The shared kernel is a real coupling and must stay small. Growing it is how a
  shared kernel quietly becomes a shared database.
- Known defect: `EnergyEntry.Category` is currently `AssetType.ToString()`,
  which is an Energy enum crossing into Accounting as a string through the very
  layer meant to stop it. Tracked as OP-02.

## Alternatives rejected

**Share the contract types directly.** One less mapping, and every context is
then coupled to every other context's vocabulary. Rejected because it makes the
project split cosmetic.

**Translate inside the domains.** Would force a domain to reference a foreign
context, defeating ADR-0001.

**A canonical shared model for all three.** The classic outcome is a type that
serves nobody: it carries season for the benefit of a context that ignores it
and asset types for a context that does not care.
