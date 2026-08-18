# ADR-0014: Tests are scenarios that prove requirements

Status: accepted
Date: 2026-08-18

## Context

The suite tested classes: one file per production class, one method per case,
arrange-act-assert inside each method. It was green and it proved little a
human could read: "tests of a class serve nothing; tests of scenarios that
prove requirements do" (reviewer).

## Decision

Domain tests follow Testcase-Class-per-Fixture (Meszaros) in its BDD form
(North; MSpec-era context/specification):

- a FOLDER per domain class;
- inside it, one CLASS per scenario, named as the scenario in plain words:
  `When_storage_is_commanded_twice_for_the_same_tick`;
- the CONSTRUCTOR loads the scenario and performs the act - including capturing
  refusals via `Record.Exception`;
- each METHOD is one observable consequence with ONE assert, named as the
  requirement it proves: `Should_refuse_the_second_command`;
- scenario docs cite the functional requirement (RF/R number) they defend;
- expensive scenarios run once via a shared fixture (`IClassFixture`) - the
  Fresh-vs-Shared Fixture distinction made explicit.

The suite is executable specification. A human reads the folder and knows the
rules; an AI agent reads it and learns both the requirements and the house
style before touching production code.

## Consequences

- Test count rises (each consequence is its own line in the report) - that is
  a feature: every green line names a requirement.
- Measured cost on this codebase: none. 146 tests/245 ms before, 166/242 ms
  after; unit scenarios re-run per assert in microseconds.
- The discipline cost is real: the act lives in the constructor, and an act
  inside a `Should_` method is a review defect.
- Applied to the Simulation domain (TASK-015 scope). Older test files remain in
  the previous style until a dedicated task converts them - deliberately not
  done in the same change.

## Alternatives rejected

**Method-per-case AAA.** Today's tooling default. Rejected: it documents the
test author's convenience, not the domain's rules.

**Nested context classes in one file.** Compact, but the folder-per-class
layout is what makes the spec discoverable from the file tree alone.
