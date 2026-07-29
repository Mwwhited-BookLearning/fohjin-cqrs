# Fohjin.DDD

A reference implementation for learning **CQRS (Command Query Responsibility
Segregation) and Event Sourcing** by building a real, modern .NET application around
them — not a slide deck, an actual bank application with a write side, a read side, two
UI clients, an identity provider, and a distributed-hosting story, all wired together so
you can run it, break it, and see exactly what each pattern buys you.

**Start here → [`docs/README.md`](docs/README.md)** for the full documentation index:
the system architecture, one doc per domain feature, the infrastructure patterns
(Event Sourcing, the messaging bus), and — if you're here to *learn* CQRS rather than
just locate it in this codebase — [`docs/patterns/`](docs/patterns/README.md) explains
every pattern used from first principles, with diagrams.

## What's actually in here

- **CQRS + Event Sourcing** at the core: every write is a `Command` handled against an
  event-sourced aggregate; every read comes from a separate, denormalized read model —
  never the reverse.
- **Three UI clients** talking to the same API: a retargeted WinForms desktop app (MVP), a
  WPF desktop app (full MVVM), and a Vue 3 SPA — all authenticating against a real (if
  minimal) OIDC identity provider.
- **A modern .NET 10 / ASP.NET Core stack**: minimal APIs, OData + the new HTTP `QUERY`
  method, native OpenAPI generation with a Scalar UI, Server-Sent Events for live
  updates, and OpenTelemetry traces/metrics from both the backend *and* the browser.
- **.NET Aspire** for local orchestration (`dotnet run` on `Fohjin.DDD.AppHost` boots
  SQL Server, the identity provider, the API, and the Vue dev server together) and a
  generated Docker Compose artifact for deployment.

See [`docs/00-architecture-overview.md`](docs/00-architecture-overview.md) for the full
picture, and [`CLAUDE.md`](CLAUDE.md) for the dev environment conventions (ports,
credentials, how to run the test suites) if you're working in this repo rather than just
reading it.

## Where this came from

In 2009, Mark Nijhof spent a two-day course (and many geek beers) with Greg Young
talking through Domain-Driven Design and CQRS specifically. The example project he built
from those discussions was well received as a reference for learning the patterns that
make up CQRS, and the blog posts he wrote about it became a book:
https://leanpub.com/cqrs

That original example is the seed this repository grew from. Everything past that seed —
the .NET 10 modernization, Event Sourcing infrastructure fixes, the HTTP API, both UI
clients, the identity provider, Aspire hosting, OpenTelemetry, and the documentation set
linked above — was built by Matt Whited (matt@whited.us), continuing the project's
original goal: give people a real, runnable thing to learn CQRS from, kept current with
how a modern .NET application actually looks.

Mark's original contact, if you have feedback on the pattern content itself:
Mark.Nijhof@Cre8iveThought.com

## Known limitations

These are documented, deliberate, or accepted trade-offs, not hidden bugs — see
`docs/README.md`'s linked docs for the reasoning behind each one where it's called out
in place:

- The event-store Snapshot pattern's *write* path is implemented but never invoked from
  the production commit path (`docs/06-event-sourcing-infrastructure.md`) — a documented
  gap, left as a ready-made exercise (`docs/patterns/event-sourcing.md`).
- Neither desktop client (WinForms or WPF) refreshes its OIDC access token; it just
  expires after an hour with no silent renewal (`docs/09-client-uis.md`) — an accepted
  limitation for a dev sample.
