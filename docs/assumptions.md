# Assumptions

The assignment states that assumptions may be made freely provided they are
documented. This is that register.

Each assumption is labelled with how it was reached:

- **Modelling choice** - a simplification we chose, defensible but arbitrary.
- **Derived** - follows from another decision recorded in an ADR.
- **Open** - not decided. Listed as open rather than presented as settled.

---

## A-001 - Every asset behaves as a meter

**Modelling choice.** Each simulated asset is treated as a meter-like source of
power measurements at every simulation interval. Consumers of the simulation see
readings, never asset internals.

Why it matters: it is what allows a heat pump, a solar array and a public
charge point to be handled by one call signature, and what lets the Accounting
context stay ignorant of physics.

## A-002 - Tick size is 15 simulated minutes by default

**Modelling choice**, configurable at runtime. 96 points per day keeps the 24
hour window light enough to redraw continuously, and EV charging sessions and
heat pump behaviour remain visible at that resolution. One minute would be 1440
points per day for no additional insight; one hour would erase the charging
sessions.

No claim is made that this matches any real metering standard. It is a
simulation parameter, chosen for legibility, and the configuration page can
change it. See ADR-0003.

## A-003 - PV offsets local load first, surplus is exported

**Derived** from the sign convention in ADR-0002. Assets sit behind their house
meter and the house meter is the signed sum of its assets, so generation cancels
consumption locally before anything reaches the grid. Only when the whole
neighbourhood nets negative does the surplus become an export.

Consequence: a house with PV can be a net exporter while the neighbourhood as a
whole is still importing. Both figures are reported separately.

## A-004 - EV charging behaviour

**Modelling choice.** The assignment explicitly leaves the usage model to us.

**Home chargers.** One plug-in per day, seeded, in a window starting between
17:30 and 19:00. The car needs 8 to 12 kWh and charges at 7.4 kW until either
full or the 07:00 departure. Reported power is the interval average, so the
final partial interval accounts for exactly the energy delivered rather than
overstating it.

**Public chargers.** Six shared points, used by residents and passers-by alike.
Arrivals follow a seeded time-of-day rate with a midday and an evening peak.
A session needs 10 to 40 kWh at 11 kW. A busy point rejects arrivals.

Known simplification: **there is no queue.** A driver arriving at a busy point
disappears rather than waiting or trying the next point. Real charging
behaviour would redistribute that demand; ours drops it, so public charger
utilisation is an underestimate at peak.

## A-005 - Heat pump is a balance-point linear model

**Modelling choice.** Electrical draw rises linearly as the outdoor temperature
falls below 15 degrees, capped at rated power. The coefficient of performance is
folded into the per-degree coefficient rather than modelled separately, so we
model electrical draw directly rather than heat output divided by an efficiency.

Known simplification: a real heat pump loses efficiency as it gets colder, so
its electrical draw grows faster than linearly in a hard freeze. Ours does not.
Domestic hot water and thermal inertia are absent; the house responds to outdoor
temperature instantly.

## A-006 - Asset distribution across the 30 houses

**Modelling choice**, matching the proportions suggested by the assignment:
40 per cent PV, 30 per cent heat pump, 20 per cent home EV charger.

These are independent per-house probabilities, not fixed counts, so a given seed
produces approximately rather than exactly those proportions, and a house may
hold several assets at once. The assignment explicitly allows houses to have
multiple assets. Base household consumption is not a probability - it is an
invariant, and a house without it cannot be constructed.

## A-007 - Money is out of scope

**Modelling choice.** The assignment asks for energy accounting in kWh and never
mentions tariffs or currency. Pricing was considered and cut to protect the time
budget. The ledger is the natural place for it: the same postings that
accumulate kWh would carry a tariff. See "Limitations" below.

## A-008 - Base household consumption

**Modelling choice.** A per-house baseline between 0.2 and 0.6 kW, derived from
the seed, shaped by a daily curve with a morning and an evening peak, with
deterministic jitter of plus or minus ten per cent.

## A-009 - Weather is synthetic and deterministic

**Modelling choice.** Temperature is an annual sinusoid plus a daily sinusoid
plus smoothed seeded noise. Cloud cover is smoothed seeded noise with a winter
bias. Irradiance is a clear-sky curve based on day length, attenuated by cloud.

The important property is that weather is a **pure function of the instant and
the seed**, not an accumulating random walk. The clock can therefore jump
forward and produce identical weather, which is what makes the 24 hour warm-up
at startup cheap and reproducible.

Known simplification: the weather is the same everywhere in the neighbourhood,
and there is no persistence between days beyond what the sinusoids give.

---

## Open points

These are genuinely undecided. They are recorded here rather than resolved
silently.

**OP-01 - Aggregate boundary for houses.** `Neighbourhood` is currently the
aggregate root and `House` an entity inside it, because the "exactly 30 houses
and exactly 6 chargers" invariant spans all of them and an invariant defines a
consistency boundary. The alternative, `House` as its own aggregate root, is
equally defensible and would be the right call the moment houses become
independently editable or independently persisted.

**OP-02 - Stringly-typed category crossing the boundary.** `EnergyEntry.Category`
is currently `AssetType.ToString()`. That is an Energy enum leaking into
Accounting through the very layer meant to prevent it. The clean fix is an
Accounting-owned `MeterCategory` enum, mapped explicitly in the translator.

**OP-03 - Unused storage concept.** `MeterKind.Storage` exists with nothing
implementing it. Either it is a declared extension point for batteries or it is
speculative generality and should be deleted.

**OP-04 - Sequential tick loop.** Assets hold session state, so measurement is
strictly sequential. The "parallel per house up to the grid settlement barrier"
design is not currently reachable without externalising that state. This is a
real constraint, not an oversight, and it is the honest answer to a question
about concurrency.

**OP-05 - Fourth context.** Whether a tariff or retailer context belongs in the
model at all, or whether A-007 stands.

---

## Limitations, and what would come next

**Not built, by choice, with the seam left in place:**

- **Event streaming.** `ITickBus` has a broker-shaped publish and subscribe
  signature but an in-process synchronous adapter. Replacing it is an
  infrastructure change. See ADR-0004.
- **Separate worker process.** The tick loop is a `BackgroundService` in the API
  container. The engine API it calls would not change if it moved out.
- **Saga, lease, reaper, heartbeat.** All belong to a distributed version of
  this system. None are justifiable at this scope, and building a lease manager
  for a single-process simulation would be a judgement error, not a strength.

**Genuine weaknesses of the current model:**

- No queueing at public chargers (A-004), so peak utilisation is understated.
- Heat pump efficiency does not degrade with cold (A-005).
- No thermal inertia; houses react instantly to outdoor temperature.
- No batteries, so there is no storage arbitrage and no reason for a house to
  shift load in time.
- Weather is uniform across the neighbourhood.
- Engine state is not persisted. This is deliberate rather than missing: the
  simulation is deterministic, so a restart replays the identical world from the
  seed. It does mean a restart returns to the configured start instant.

**What we would do next, in order:**

1. Finish the animated dashboard and the configuration page.
2. Property-based tests for energy conservation and accounting closure.
3. Resolve OP-02, since it is a real modelling defect rather than a preference.
4. Add batteries, which is the first asset that makes the model interesting,
   because it introduces state that must be carried between ticks and a real
   decision about when to charge.
5. Tariffs, which turn the ledger into something a person would act on.
