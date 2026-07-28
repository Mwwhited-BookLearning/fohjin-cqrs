# Patterns and Practices

The architectural and design patterns this codebase is built from, each with a reference
to where it's described more formally and where to find it in this repo. `00` through
`09` describe *what* each part of the system does; this doc is about *which named
pattern* it's an instance of and *why* that pattern was the right tool.

## Architectural patterns

### CQRS — Command Query Responsibility Segregation

Every write goes in through a `Command` (`Fohjin.DDD.Commands`) and every read comes back
out through a `Report` DTO (`Fohjin.DDD.Reporting.Dtos`) — never the reverse, and never
the same object doing both jobs. This is the organizing principle of the whole codebase;
see `00-architecture-overview.md` for the container-level view of the split.

- **Reference**: Greg Young, *CQRS Documents* (the original write-up:
  https://cqrs.wordpress.com/documents/cqrs-documents/); this project is itself derived
  from a Greg Young workshop (see the root `ReadMe.txt`).
- **In this repo**: `Fohjin.DDD.CommandHandlers` (write side) vs. `IReportingRepository`
  (read side) never reference each other's storage. See `00-architecture-overview.md`.

### Event Sourcing

Aggregate state isn't stored directly — it's derived by replaying the sequence of domain
events that produced it. The event log is the source of truth; current state is a cache.

- **Reference**: Martin Fowler, *Event Sourcing*
  (https://martinfowler.com/eaaDev/EventSourcing.html).
- **In this repo**: `BaseAggregateRoot<T>.Apply`/`LoadFromHistory`, `DomainEventStorage`,
  the `EventProviders`/`Events` tables. See `06-event-sourcing-infrastructure.md`.

### Snapshot pattern

Replaying every event since the beginning of time gets expensive as history grows; a
snapshot is a point-in-time memento you can restore from and replay only the events after
it.

- **Reference**: Martin Fowler, *Memento* (https://martinfowler.com/eaaDev/EventSourcing.html#UsingMementosToStoreState)
  and the GoF Memento pattern (*Design Patterns: Elements of Reusable Object-Oriented
  Software*, Gamma/Helm/Johnson/Vlissides, 1994).
- **In this repo**: `IOriginator.CreateMemento`/`SetMemento`, `SnapShotEntity`,
  `EventStoreUnitOfWork.LoadSnapShotIfExistsAsync`. See `06-event-sourcing-infrastructure.md`
  — including the gap where the write side of this pattern (`SaveShapShotAsync`) is never
  triggered automatically.

### Domain-Driven Design building blocks

- **Aggregate root** — a cluster of objects treated as a single unit for data changes, with
  one member (the root) as the only entry point from outside. `Client` and `ActiveAccount`
  are aggregate roots; `BankCard` is an entity *inside* the `Client` aggregate, never
  addressed directly.
- **Entity** — has identity that persists across state changes. `BankCard` (identity =
  `Id`, but its behavior/state changes over its lifetime — see `02-bank-cards.md`).
- **Value object** — defined entirely by its data, immutable, no identity.
  `ClientName`/`Address`/`PhoneNumber`/`Amount`/`Balance`/`AccountNumber` — all C# `record`
  types, replaced wholesale rather than mutated in place.
- **Repository** — see below.
- **Reference**: Eric Evans, *Domain-Driven Design: Tackling Complexity in the Heart of
  Software* (Addison-Wesley, 2003) — the source of all four terms above.
- **In this repo**: `01-client-management.md` and `03-account-management.md` have the
  class diagrams.

## Design patterns

### Repository

`IDomainRepository<T>`/`IReportingRepository` both hide the storage mechanism behind a
collection-like interface (`GetByIdAsync`, `Add`, `SaveAsync`) — callers never see SQL, EF
Core, or the event store's serialization format.

- **Reference**: Eric Evans, *DDD* (as above); also GoF's underlying idea of encapsulating
  data access behind an interface.
- **In this repo**: `DomainRepository<T>` (write side, `06-event-sourcing-infrastructure.md`),
  `SqliteReportingRepository` (read side, `08-reporting-read-models.md`).

### Unit of Work

Tracks a batch of changes and commits or rolls them back as one transaction.

- **Reference**: Martin Fowler, *Patterns of Enterprise Application Architecture*
  (Addison-Wesley, 2002), the *Unit of Work* pattern.
- **In this repo**: `EventStoreUnitOfWork<T>.CommitAsync`/`RollbackAsync` wraps the event
  store transaction; `TransactionHandler<TCommand,THandler>` is what actually invokes it
  around a command handler's execution. See `07-messaging-bus.md`. (Note the naming
  collision flagged there — two unrelated `IUnitOfWork` interfaces share a name.)

### Mediator / Message Bus

Callers publish a message without knowing which handler(s) will process it; the bus looks
that up at runtime.

- **Reference**: GoF *Mediator* pattern (as above), and Udi Dahan's writing on buses vs.
  mediators in CQRS systems (https://udidahan.com/2011/06/06/mediating-in-cqrs/ discusses
  the distinction; this project uses a real in-process bus, not a pure request/handler
  mediator, since it also carries the event stream).
- **In this repo**: `IBus`/`DirectBus`, `MessageRouter`, `CommandHandlerHelper`. See
  `07-messaging-bus.md` for the full dispatch chain.

### Observer (via Rx.NET)

Event handlers subscribe to a stream of domain events without the publisher knowing or
caring who's listening or how many there are.

- **Reference**: GoF *Observer* pattern (as above); Rx.NET's specific take on it is
  documented at https://reactivex.io/documentation/observable.html (`IObservable<T>` /
  `IObserver<T>`, the reactive-extensions formalization of Observer with LINQ-style
  composition).
- **In this repo**: `IBus.Events : IObservable<IDomainEvent>`, `EventSubscriptionBootstrapper`
  subscribing each `IEventHandler` via `.OfType<TEvent>().Subscribe(...)`. See
  `07-messaging-bus.md`.

### Model-View-Presenter (MVP)

The View is a passive interface (`IClientDetailsView`, etc.) with events and settable
properties; the Presenter contains all the logic and is unit-testable without any real
UI. This codebase's specific flavor auto-wires View events to Presenter methods by naming
convention (`OnXxx` ↔ `Xxx`) via reflection, rather than manual event subscriptions.

- **Reference**: Martin Fowler, *GUI Architectures*
  (https://martinfowler.com/eaaDev/uiArchs.html) — covers MVP alongside MVC and
  Presentation Model and the tradeoffs between them.
- **In this repo**: `Presenter<TView>`'s reflection-based `HookUpViewEvents`, and every
  `*Presenter`/`I*View` pair under `Fohjin.DDD.BankApplication.Core`. See
  `09-winforms-ui.md`.

### Specification / dynamic query object

Rather than writing a LINQ query per DTO/filter combination, `GetByExampleAsync` builds an
`Expression<Func<TDto,bool>>` at runtime from an anonymous object's properties — one
generic query mechanism instead of N hand-written ones.

- **Reference**: Eric Evans & Martin Fowler, *Specification*
  (https://martinfowler.com/apsupp/spec.pdf) — the general idea of representing a business
  rule/query as a composable object rather than inline code. `SqliteReportingRepository`'s
  version is a simplified, reflection-driven take on the same idea (equality-only
  predicates, not full boolean composition).
- **In this repo**: `SqliteReportingRepository.BuildPredicate<TDto>`. See
  `08-reporting-read-models.md`.

### Compensating transaction

Rather than a two-phase distributed transaction across "banks," a failed transfer is
undone by *another* forward-moving domain event/command (a refund), not a rollback.

- **Reference**: Pat Helland's writings on compensating transactions in distributed
  systems, and the broader Saga pattern description in Chris Richardson, *Microservices
  Patterns* (Manning, 2018), ch. 4 — this codebase implements a single-step compensation,
  not a full saga orchestrator/choreographer.
- **In this repo**: `MoneyTransferService`'s catch-all → `MoneyTransferFailedCompensatingCommand`
  → `ActiveAccount.PreviousTransferFailed` (refund deposit). See `05-money-transfers.md`
  for the full activity diagram.

### Optimistic concurrency

Instead of locking an aggregate while it's in memory, the event store checks the expected
version number at save time and rejects the write if another writer got there first.

- **Reference**: general pattern, well described in Fowler's *PoEAA* under
  *Optimistic Offline Lock*.
- **In this repo**: `DomainEventStorage.SaveAsync`'s version check → throws
  `ConcurrencyViolationException` on mismatch. See `06-event-sourcing-infrastructure.md`.

## Practices

- **Constructor injection everywhere** — every class declares its dependencies as
  constructor parameters; the only classes that take `IServiceProvider` directly are bus
  infrastructure resolving open generics at runtime (`ITransactionHandler<,>`), a
  deliberate, narrow exception rather than a general service-locator habit. See
  `07-messaging-bus.md`.
- **Async/await (TAP — Task-based Asynchronous Pattern)** throughout the write and read
  paths. Reference: Microsoft's TAP documentation
  (https://learn.microsoft.com/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap).
- **Structured logging** — `ILogger` message templates with named placeholders and
  separate arguments (`_log.LogInformation("Publish: {message}", message)`), not string
  interpolation baked into the message text, so a log backend can query/filter by field.
  Reference: Microsoft's high-performance logging guidance
  (https://learn.microsoft.com/dotnet/core/extensions/high-performance-logging).
