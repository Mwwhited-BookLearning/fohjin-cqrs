# Architecture Overview

Fohjin.DDD is a reference implementation of CQRS + Event Sourcing: a WinForms bank
application where every state change is a domain command, every fact is a domain event,
and the UI reads from a separate, denormalized read model rather than the write-side
aggregates.

This document gives the system-level (C4 Context/Container) view. Each bounded piece of
functionality has its own doc alongside this one — see the index at the bottom.

> **Diagram style note**: all C4-flavored diagrams in this doc set are hand-drawn with
> plain PlantUML (colored `rectangle`/`database` blocks with `<<stereotype>>` labels)
> rather than the `C4-PlantUML` include library, so they render without needing network
> access to GitHub.

## System Context

```plantuml
@startuml
skinparam rectangle {
  BackgroundColor<<Person>> #08427b
  FontColor<<Person>> white
  BackgroundColor<<System>> #1168bd
  FontColor<<System>> white
  BackgroundColor<<External>> #999999
  FontColor<<External>> white
  BorderColor black
}
skinparam defaultTextAlignment center
skinparam wrapWidth 200
skinparam maxMessageSize 200

rectangle "Bank Employee\n<size:11><<Person>></size>\nManages clients, accounts, and money transfers" <<Person>> as employee
rectangle "Fohjin Bank Application\n<size:11><<Software System>></size>\nWinForms desktop app demonstrating CQRS + Event Sourcing" <<System>> as bankApp
rectangle "\"External\" Bank\n<size:11><<Software System>></size>\nSimulated - actually the same process/database, see 05-money-transfers.md" <<External>> as fakeExternalBank

employee --> bankApp : "Creates clients, opens accounts,\ndeposits/withdraws cash, transfers money"
bankApp --> fakeExternalBank : "Simulates outbound transfers to"
fakeExternalBank --> bankApp : "Simulates inbound transfers from"
@enduml
```

## Containers

```plantuml
@startuml
skinparam rectangle {
  BackgroundColor<<Person>> #08427b
  FontColor<<Person>> white
  BackgroundColor<<Container>> #438dd5
  FontColor<<Container>> white
  BorderColor black
}
skinparam database {
  BackgroundColor<<Container>> #438dd5
  FontColor<<Container>> white
  BorderColor black
}
skinparam defaultTextAlignment center
skinparam wrapWidth 200
skinparam maxMessageSize 200

rectangle "Bank Employee\n<size:11><<Person>></size>" <<Person>> as employee

rectangle "Fohjin Bank Application" {
  rectangle "BankApplication\n<size:11><<WinForms, .NET 10>></size>\nPresenter/View screens: client search,\nclient details, account details" <<Container>> as winforms
  rectangle "In-Process Bus\n<size:11><<.NET, Rx.NET>></size>\nRoutes commands to handlers; fans out\ndomain events to subscribers" <<Container>> as bus
  rectangle "Domain + Command/Event Handlers\n<size:11><<.NET libraries>></size>\nAggregates, command handlers,\nevent handlers" <<Container>> as domain
  database "Event Store\n<size:11><<SQLite via EF Core>></size>\nAppend-only domain events\n+ snapshots, keyed by aggregate id" <<Container>> as eventStoreDb
  database "Reporting Store\n<size:11><<SQLite via EF Core>></size>\nDenormalized read-model DTOs\nthe UI queries directly" <<Container>> as reportingDb
}

employee --> winforms : "Uses"
winforms --> bus : "Publishes commands to"
bus --> domain : "Dispatches commands /\ndelivers events to"
domain --> eventStoreDb : "Appends events to /\nloads aggregates from"
domain --> reportingDb : "Updates read models in\n(event handlers only)"
winforms --> reportingDb : "Queries directly\n(never touches the event store)"
@enduml
```

**The core CQRS rule enforced here**: `winforms` never reads from `eventStoreDb`, and
command handlers never read from `reportingDb`. The only bridge between write and read
sides is a domain event traveling through the bus.

## Data flow, one sentence per stage

1. A WinForms Presenter builds a command and calls `IBus.Publish` + `CommitAsync`.
2. The bus routes the command (by its runtime type) to the one `ICommandHandler<T>` that
   handles it, wrapped in a transaction against the event store.
3. The handler loads (or creates) an aggregate, calls a domain method, which raises one or
   more domain events.
4. The event store persists the new events (and updates a version counter used for
   optimistic concurrency and, in principle, snapshotting).
5. Each persisted event is re-published on the bus, this time as an `IDomainEvent` — the
   bus fans it out via Rx to every independently-subscribed event handler.
6. Event handlers update the reporting store's DTOs; the UI's next query (or refresh
   timer) picks up the change.

## Doc index

| Doc | Covers |
|---|---|
| `01-client-management.md` | Client aggregate, create/rename/move-client commands |
| `02-bank-cards.md` | BankCard child entity, assign/cancel/report-stolen |
| `03-account-management.md` | ActiveAccount/ClosedAccount, open/close/rename |
| `04-cash-operations.md` | Deposit/withdrawal |
| `05-money-transfers.md` | The simulated internal/external transfer routing |
| `06-event-sourcing-infrastructure.md` | Aggregate roots, event store, snapshots |
| `07-messaging-bus.md` | Command dispatch + Rx event fan-out |
| `08-reporting-read-models.md` | Read-model DTOs and their event-driven updates |
| `09-winforms-ui.md` | Presenter/View pattern, screen flows |
