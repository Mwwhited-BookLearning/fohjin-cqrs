# Hosting: .NET Aspire (local dev) + Docker Compose (deployable artifact)

## The requirement

Local orchestration for what's about to become a multi-process system (WebAPI + STS +
Vue dev server + database), plus a way to package it for deployment somewhere that isn't
"a developer's machine running five terminals."

## The choice: Aspire as the primary orchestrator, Docker Compose as its output

.NET Aspire (an `AppHost` project referencing every other piece as a "resource") gives:
service discovery, a local dashboard (logs/traces/metrics across every resource — directly
useful for watching the SSE/event-stream pieces work), and orchestrated `dotnet run`
startup for the whole system with one command.

Aspire's `Aspire.Hosting.Docker` integration then **publishes that same app model as a
real `docker-compose.yml` + `.env`** — so the AppHost project is simultaneously "how a
developer runs everything locally" and "the source of truth Docker Compose is generated
from," rather than maintaining two separate descriptions of the same topology by hand.

## Design implication

- Add an `AppHost` project (Aspire convention name) plus the companion
  `ServiceDefaults` project (Aspire's standard shared OpenTelemetry/health-check/service-discovery
  wiring, referenced by every other host-able project: WebAPI, STS).
- Model the WebAPI, the STS, the reporting/event-store SQLite (or a real DB for
  container scenarios — SQLite doesn't containerize as a separate resource the way
  Postgres would; this is a decision to revisit in the phase that adds Aspire, not now),
  and the Vue app (Aspire has a Node.js/npm resource type) as Aspire resources with
  explicit dependency ordering (STS before WebAPI, WebAPI before Vue dev server).
- Treat `docker-compose.yml` as **generated output**, not hand-maintained — regenerate it
  via the Aspire publish command whenever the AppHost model changes, the same way the
  OpenAPI/AsyncAPI docs and NSwag clients are generated rather than hand-written.

## Sources

- [Aspire Docker integration for containerized resources](https://aspire.dev/integrations/compute/docker/)
- [Deploy Aspire apps with Docker Compose to any host](https://aspire.dev/deployment/docker-compose/)
- [Using .NET Aspire With the Docker Publisher (Milan Jovanović)](https://milanjovanovic.tech/blog/using-dotnet-aspire-with-the-docker-publisher)
- [Converting a docker-compose file to .NET Aspire (Andrew Lock)](https://andrewlock.net/converting-a-docker-compose-file-to-aspire/)
