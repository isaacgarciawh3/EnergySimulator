# 001 — Kickoff: assignment intake and domain model direction

- Date: 2026-08-18
- Tool: Claude Code (Opus) driving "Task-Creator", the author's own prompt/task generator
- Outcome: bounded context split, tick size decision, asset polymorphism decision, ADR-001 / A-001 seeded

## Prompt (verbatim)

> New project , client Utilus
>
> First module: Assumptions ... the project lives on /home/isaac-garcia/Documents/Projects/EnergySimulator
> and it is a public git repository
>
> You need to log all of our prompts in the folder called AI - Prompts.
>
> This is the assignment , it is for my job at Utilus. You need to show them at we are capable .
>
> Lets go Task-Creator : Hi future Boss, this is my own prompt creator. He helps me with my projects!
>
> [FULL ASSIGNMENT TEXT PASTED — "Neighbourhood Energy Simulation", 30 houses,
> 6 public EV chargers, heat pumps / PV / home EV chargers, simulation clock,
> weather and season, animated UI with 24h chart, per-asset cumulative kWh,
> configuration with fixed seed, 4 hour budget, deliverables: running app,
> source, docs, tests, AI chat log. Suggested priority order:
> 1) correct simulation + energy accounting + clock,
> 2) animated UI with aggregate + 24h chart,
> 3) per-asset cumulative counters,
> 4) weather/season influence.]
>
> Lets talk about the model a little bit
>
> I figure this after thinking a little bit
>
> I have 4 hours to complete a software architecture home assignment. I want to start by
> establishing a deliberately simple but evolution-friendly architecture, repository structure,
> documentation model, and runnable skeleton before implementing the simulation behavior.
>
> Some houses have: Heat pumps, PV / solar panels, Home EV chargers
> Exactly 6 public EV chargers is not negotiable, but the energy products that I mention above
> are an opportunity to design patterns.
>
> Maybe Strategy pattern if we have the same method to generate energy and consuming energy but
> different strategies.
>
> Maybe thats why they put those as optional. because with little time we need to use abstractions
> and more implementations but with design patterns we can achieve those without make the consumer
> has more than one signature call.
>
> So, We have here 2 large contexts. 1 it is the simulation, this is one bounded context.
>
> The other one is the energy company. and the relationship between the 2 it is a test for dont let
> one component make code smells and bad dependencies with each other.
>
> then we have another one, the accounting. No matter how energy consumption or generation works.
> Accounting is based on simple math. kWh and time, prices in euro.
>
> dont forget we need the configuration so i thinking that the web part is just 2 pages.
> 1 it is the dashboard like one simulation for the 30 houses and consumption simulations etc.
> And the second one is the random seed for configuration.
>
> Lets use .NET Core 10, docker containers to run this easily, I need a clean architecture,
> hexagonal, we just have 4 hours so it will be use services and a rich Domain model like
> Eric Evans domain model.
> Hexagonal will fit for a development based on ports and adapters so the adapter for today will be
> the service synchronously and tomorrow we can expand to event stream, saga, lease, reaper,
> heart beat.
>
> So the Dashboard runs in a real time database as a projection that is built from the apis that run
> through the event stream, using CQRS strategy. We just have 4 hours so we have to simulate this.
>
> Making assumptions, ADRs, tradeoffs and explaining why we are doing this and why we are choosing
> this architecture that creates room for scaling.
>
> A-001 — Each simulated asset is treated as a meter-like source of power measurements at every
> simulation interval.
>
> ADR-001 — Separate simulation behavior from energy accounting.

## Follow-up in the same turn

> Just a reminder I need everything in english, Portuguese is out of the menu today
