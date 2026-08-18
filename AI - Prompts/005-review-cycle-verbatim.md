# 004 — Review cycle: boundary corrections, new requirement, accountability

- Date: 2026-08-18
- Tool: Claude Code (Opus) driving Task-Creator
- Format: every prompt verbatim, in order. Corrections appear as later entries, never as rewrites.

These are the review prompts that shaped the architecture. Several are
corrections of real mistakes; they are recorded as given.

---

## 4.1 — Process and persistence

> Dont do commits in the main. for the next commits do PRs with the task context and
> Description explanation , then you can run multiple tasks and I can evalutate then

> For the simulation I think the best course of action is to use some SqLite. And run the
> seed as the container it started,
> after that we can reconfigured the seed values in the web
> Please dont do everything without output ohterwise I wont have time to correct you

> And we need to write the tasks. it is better this way.
> ans them put the prompt tasks in the project as well

## 4.2 — Bounded context separation (first correction)

> I didint like your folders separation.
> It is not clear the bounded context.
> yes for the time we will have to endure a monolith project. but dont need to be messy.
> We have 3 bounded contexts.. 1) Simulation Context, 2) Energy Context, 3) Accounting context.
> We need to user DDD, and Dont let Aggregation Roots mess with each other
> Clean architecture, hexagonal architecture. Dont have time for Queue, Workers, JObs, Event
> stream, But We need to simulate then and explain our architecture in our ADRs, Assumptions,
> And Tradeoffs.
> One thing is to do a fast solution for a test, other is to explain that we planed for the
> future scaling

## 4.3 — Animation and timeframe

> And one thing is the actual time frame of the simulation..... like we need 24 hours of data
> but in the dashboard we need a great animation , exciting to watch

## 4.4 — Process correction: model before code

> Stop for a moment

> I gonna review the PR but Dont tackle anymore without write the correct Tasks
> I Expect more from you to chat about the domain model before it becomes a thing to ask corrections

## 4.5 — Missing documentation

> I dont see Adr, Assumptions or C4 doc on /docs
> and this is boder me because we do everything right everytime, but i know 2 hours is little
> time, but betrail the process wont help us.
> Can you land a little bit and organize the ADr, Assumptions, requirements from the first
> prompt. and organize the work in tasks as we always do?

## 4.6 — Boundary correction on PR #1 (the decisive one)

> About PR 1
> I think we mixed the concepts a little.
> Sim.Energy should not simulate behaviour. If Energy knows about seed, noise, weather logic,
> EV schedules or how PV/heat pump generates a fake value, then Simulation is leaking inside Energy.
> the separation I want is simple:
> 1) Simulation decides what each asset is doing now and produces PowerReading
> 2) Energy describes the real energy world: neighbourhood, houses, assets, meters and their relationships
> 3) Accounting receives readings and calculates consumption, generation, totals, import/export and history
> So later I should be able to replace: TODAY = Simulation and FUTURE = PowerReading
> with Real IoT , telemetry and PowerReading, without changing Energy or Accounting.
> You need to double check if this is needed in PR 1 or if you just fixed that in PR 2 as follows:
> move deterministic noise and all synthetic asset behaviour to Simulation
> remove Seed from Energy
> remove the old Sim.Domain if it is obsolete
> do not commit sim.db
> make the PV assumption consistent with the actual neighbourhood netting model
> remove ITickBus if it has no real purpose now; document Kafka/Event Hub as future evolution instead
> Keep it simple. We have very little time and I don't want more abstractions, I want the domain
> boundaries to be correct.

This prompt produced the single most important change in the codebase. All six
items were verified against the source before acting; the findings and the fix
are recorded in TASK-011 and ADR-0001.

## 4.7 — New requirement mid-build

> new requirement. Dont leave ever, tasks, ADR, Assumptions, they are our source of truth and
> when requirement changes we need to keep in mind that , bounded context, Composable Domains,
> Will help us and prevent us to make mistakes
>
> [Neighbourhood battery + peak shaving (120 min): battery with capacity kWh, max charge/discharge
> power kW, optional round-trip efficiency; a control strategy that aims to reduce peaks, e.g. keep
> neighbourhood load below a threshold or discharge during top N% load periods; visualization that
> demonstrates impact - net neighbourhood load with and without battery, or battery power and
> state-of-charge; highlight peak shaving effect.]

## 4.8 — Merges

> PR 1 is merged, revolve PR 2 conflicts

> PR#2 - Merged

## 4.9 — Accountability check

> I saw that we have open issues wihtin the assumptions.
> lets talk about it before go any futther.. have you take accountability with those new requirements?

Answer given: partially. OP-02 and OP-03 were genuinely resolved by the boundary
fix; requirements.md, c4.md and design.md had NOT been updated for the battery,
and TASK-012 had not been written before the code. Recorded rather than glossed.

## 4.10 — Hardcoding and traceability

> * Exactly 30 houses
> * Exactly 6 public chargers
> * A documented distribution of assets across houses (e.g., 40% PV, 30% heat pumps, 20% home EV).
> were are those requiments and the new ones because those are constraints with actual fixed numbers.
> I need the link and line to them at github
> I saw the simulation code at Visual Code and you did a lot of Hard coding without cosuming data
> from JSON, YAML File that was in the Main requirements.
> We can do this later than have you write those in the tasks?

> This is the configuration requiment
> [section 4 Configuration, verbatim from the assignment]
> and those are the architecture constraints
> [section 5 Quality expectations, verbatim from the assignment]

Outcome: the configuration requirement offers three acceptable options and is
satisfied by "fixed seed + stated proportions". The genuine finding was the
physical asset parameters being magic numbers; deferred deliberately as TASK-013
with the reason recorded. Section 5 lists tests, which were still missing - that
gap drove the final work.

## 4.11 — Deliverables and priority reminder

> [Deliverables 1-5 and the Suggested scope priority order, verbatim from the assignment]
