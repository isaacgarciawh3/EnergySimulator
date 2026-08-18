# ADR-0008: Two server-served pages, polling, no framework

Status: accepted
Date: 2026-08-18

## Context

The assignment asks for an animated view that updates as simulated time
advances, plus a way to configure the neighbourhood. It also asks for a
reasonable setup and one-command startup.

## Decision

Two static pages served by the API container: a dashboard and a configuration
page. Vanilla JavaScript, no build step, no npm, no framework, no CDN. The
chart is hand-rolled inline SVG.

The dashboard polls `GET /api/simulation` roughly four times a second and
redraws from the returned snapshot.

## Consequences

- No build step means no `node_modules`, no bundler and no chance of the
  evaluator hitting a JavaScript toolchain failure. `docker compose up` and the
  page is there.
- No CDN means the page works with no network access.
- The API stays a plain REST surface consumed by an ordinary client, which is
  what makes it a driving adapter rather than a template engine.
- Polling re-sends the full snapshot every time, including all 62 meter totals.
  At this size that is a few kilobytes and simpler than the alternative. It
  would not scale to a thousand houses, and the fix would be a delta endpoint or
  a push transport.
- Rendering is throwaway innerHTML rather than a reconciling view layer. Fine at
  this size; it is the first thing that would hurt if the page grew.

## Alternatives rejected

**Blazor Server.** Push updates over SignalR with no JavaScript, and we had
already validated it. Rejected on two counts: the static asset publishing
failure mode is silent and costly under time pressure, and the persistent
circuit plus antiforgery key ring adds container state we would otherwise not
have.

**React or similar.** A build step and a dependency tree for two pages.

**Server-Sent Events instead of polling.** Genuinely better - push instead of
pull, less traffic, no fixed interval. Deferred only because polling was
already working; the endpoint shape would not change.
