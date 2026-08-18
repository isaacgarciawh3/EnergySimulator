---
# === EXECUTION CONTEXT ===
git: git@github-wh3:isaacgarciawh3/EnergySimulator.git
branch: docs/architecture-baseline
cliente: Utilus
projeto: EnergySimulator
modulo: Assumptions

# === TASK METADATA ===
task_id: TASK-010
titulo: Documentation baseline - requirements, assumptions, ADRs, C4 (Isaac)
tipo: docs
prioridade: critica
status: aprovada
criado_em: 2026-08-18
atualizado_em: 2026-08-18

# === GROUPING ===
epico: Utilus home assignment

# === DEPENDENCIES ===
depende_de: []
bloqueia: [TASK-007, TASK-008]
---

## Objective

Publish the documentation that the code was supposed to be derived from:
the assignment requirements with traceability, the assumption register, the
ADRs, and the C4 model. `docs/` currently holds only mirrored task files.

## Context

Process failure to correct: TASK-003 and TASK-009 produced code before the ADRs
that justify it existed. The assignment scores "Communication: assumptions,
documentation, and tradeoffs" as a first-class criterion, and the project RNFs
require ADRs written at decision time (RNF-08 of the project, RNF-14 of the
preparation). Writing them after the fact is already a compromise; leaving them
unwritten forfeits the criterion outright.

This task does not change a single line of production code. It is documentation
only, on its own branch, reviewable independently of the open PR #1.

## Functional Requirements

- [ ] RF-01: `docs/requirements.md` - every requirement from the assignment
      text, numbered, with a traceability column stating where it is satisfied
      or that it is not yet satisfied. No requirement silently dropped.
- [ ] RF-02: `docs/assumptions.md` - the A-001..A-009 register with the
      rationale for each, plus the open points OP-01..OP-05 that are awaiting a
      decision rather than being presented as settled.
- [ ] RF-03: `docs/adr/` - one file per architectural decision, in the format
      Context / Decision / Consequences / Alternatives rejected. Minimum set:
      the three-context split, the shared kernel and ACL, the simulation vs
      accounting separation, the sign convention, the configurable tick, the
      in-process bus standing in for the event stream, SQLite as configuration
      and projection store, the two-page polling UI.
- [ ] RF-04: `docs/c4.md` - C4 levels 1 to 3 in Mermaid: system context,
      containers, and the component view of the three bounded contexts with the
      translation points marked.
- [ ] RF-05: `docs/design.md` - design overview required by the assignment
      deliverables: key components and responsibilities, the data model, and
      the EV / PV / heat pump assumptions in prose.
- [ ] RF-06: README links to all of the above so a reviewer reaches them in
      one hop.

## Non-Functional Requirements

- [ ] RNF-01: Every ADR states what was rejected and why. An ADR without a
      rejected alternative is a description, not a decision.
- [ ] RNF-02: No claim in the docs that the code does not support. Anything
      aspirational is labelled as not built.
- [ ] RNF-03: Clean markdown, no emojis, diagrams in Mermaid.
- [ ] RNF-04: Documentation branch contains no production code changes.

## Acceptance Criteria

1. A reviewer opening `docs/` can reconstruct why the architecture looks the way
   it does without reading the source.
2. Every assignment requirement appears in the traceability table with an
   honest status, including the ones not yet done.
3. Each open point OP-01..OP-05 is visible as an open decision, not hidden.

## Restrictions

- Documentation only. No changes under `src/` or `tests/`.
- Branch off `main`, open a PR; no direct commits to `main`.
