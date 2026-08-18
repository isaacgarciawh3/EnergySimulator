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

**Derived** from the sign convention in ADR-0002.

Stated precisely, because an earlier version of this document overclaimed:
settlement happens **once, at the neighbourhood level**. Every reading in the
interval is summed; if the total is positive the neighbourhood imports, if
negative it exports. There is no separate per-house netting step in the code,
and no house-level import or export figure is produced.

What is true is that PV cancels local consumption *within that single sum*,
because generation is negative and consumption positive. So a house's own meters
can net negative while the neighbourhood as a whole still imports - the
dashboard shows that per-house net figure - but the export decision is only ever
made once, for the whole neighbourhood.

The distinction matters for anyone extending this: introducing per-house billing
or a per-house grid connection would require a real netting step that does not
currently exist.

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

## A-012 - Configuration precedence

**Modelling choice.** Three sources can supply the scenario, and they are
consulted in this order:

1. A row persisted in SQLite. It wins, because its existence means an operator
   changed something through the UI and we should not overrule that on restart.
2. The `Scenario` section of `appsettings.Simulation.json`. Authoritative on a
   first boot, when nothing is stored yet.
3. Hardcoded fallback values in C#, used only when the file is absent, so that
   the application still starts on a machine where the file was deleted.

The consequence worth stating: editing the file after the first run appears to
do nothing, because the persisted row is winning. That is intended, not a bug,
and the documented recovery is to reset back to the file scenario.

What no source can do, at any precedence: change the number of houses or public
chargers. Those are invariants (A-013), not configuration.

## A-013 - Constraints are not configuration

**Modelling choice**, and the reason the configuration file has a hole in it.
"Exactly 30 houses" and "exactly 6 public chargers" are stated by the assignment
as absolutes. They are enforced by `NeighbourhoodInvariants` in the domain and
deliberately absent from the configuration file, because a file that can set the
house count to 25 is a file that can violate a requirement.

The asset distribution is the opposite case: the assignment gives 40/30/20 as an
example, so it is a genuine setting and lives in the file.

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

## A-010 - Neighbourhood battery

**Modelling choice.** One shared battery for the whole neighbourhood: 250 kWh
capacity, 80 kW maximum charge and discharge, 90 per cent round-trip efficiency,
starting half charged so it has something to give on the first peak.

Losses are applied as the square root of the round-trip efficiency on each leg,
so charging costs more at the meter than the cells store, and discharging
delivers less than the cells give up. The battery is metered like any other
asset, so its losses appear as consumption and the energy conservation invariant
still holds exactly.

Known simplifications: no degradation, no cycle counting, no temperature
dependence, no minimum state of charge reserve, and it can go from full
discharge to full charge in one interval with no ramp limit.

## A-011 - Peak shaving control

**Modelling choice**, and the second attempt. The strategy discharges above the
80th percentile and recharges below the 40th percentile of load seen over a
rolling 24 hour window.

The first attempt used a fixed 45 kW threshold and measurably did not work - the
battery drained to empty before the evening peak and reduced the peak by 0 kW.
That failure and the fix are recorded in ADR-0010 rather than quietly patched,
because the reason it failed is the interesting part: a fixed threshold encodes a
guess about the load, and the guess was wrong.

The controller is reactive, not predictive. It responds to the distribution it
has already observed, so the first simulated day is a warm-up.

## Open points

These are genuinely undecided. They are recorded here rather than resolved
silently.

**OP-01 - Aggregate boundary for houses.** `Neighbourhood` is currently the
aggregate root and `House` an entity inside it, because the "exactly 30 houses
and exactly 6 chargers" invariant spans all of them and an invariant defines a
consistency boundary. The alternative, `House` as its own aggregate root, is
equally defensible and would be the right call the moment houses become
independently editable or independently persisted.

**OP-02 - RESOLVED 2026-08-18.** The stringly-typed category no longer crosses
the boundary. Accounting takes `PowerReading` and classifies by the sign of the
reading, so it has no asset vocabulary at all. The dashboard's per-type
breakdown is now a read-time join in the application layer. The defect was
removed by fixing the boundary rather than by patching the symptom.

**OP-03 - RESOLVED 2026-08-18.** `MeterKind` was deleted along with the rest of
Accounting's asset vocabulary. Storage arrived for real as `Battery`, and it
needed no enum in Accounting: a battery is just a meter whose reading changes
sign.

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
