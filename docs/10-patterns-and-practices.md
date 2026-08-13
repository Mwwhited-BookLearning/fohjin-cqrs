# Patterns and Practices

The architectural and design patterns this codebase is built from, each with a reference
to where it's described more formally and where to find it in this repo. `00` through
`09` describe *what* each part of the system does; this doc is about *which named
pattern* it's an instance of and *why* that pattern was the right tool.

This is the concise catalog — one paragraph and a source pointer per pattern. For a
from-first-principles explanation of each one (with diagrams and a detailed walk-through of
this codebase's implementation), see `patterns/README.md`.

## Architectural patterns

### CQRS — Command Query Responsibility Segregation

*Deep dive: `patterns/cqrs.md`.*

Every write goes in through a `Command` (the `Fohjin.DDD.Commands` namespace, physically in
`Fohjin.DDD.Abstractions/Commands` — see the note in `patterns/cqrs.md`) and every read
comes back out through a `Report` DTO (`Fohjin.DDD.Reporting.Dtos`) — never the reverse, and
never the same object doing both jobs. This is the organizing principle of the whole
codebase; see `00-architecture-overview.md` for the container-level view of the split. All
three clients (WinForms, WPF, and Vue, `09-client-uis.md`) reach this split over HTTP now
rather than an in-process reference, but the split itself — enforced inside
`Fohjin.DDD.WebApi` — is
exactly the same rule it always was.

- **Reference**: Greg Young, *CQRS Documents* (the original write-up:
  https://cqrs.wordpress.com/documents/cqrs-documents/); this project is itself derived
  from a Greg Young workshop (see the root `README.md`).
- **In this repo**: `Fohjin.DDD.CommandHandlers` (write side) vs. `IReportingRepository`
  (read side) never reference each other's storage. See `00-architecture-overview.md`.

### Event Sourcing

*Deep dive: `patterns/event-sourcing.md`.*

Aggregate state isn't stored directly — it's derived by replaying the sequence of domain
events that produced it. The event log is the source of truth; current state is a cache.

- **Reference**: Martin Fowler, *Event Sourcing*
  (https://martinfowler.com/eaaDev/EventSourcing.html).
- **In this repo**: `BaseAggregateRoot<T>.Apply`/`LoadFromHistory`, `DomainEventStorage`,
  the `EventProviders`/`Events` tables. See `06-event-sourcing-infrastructure.md`.

### Snapshot pattern

*Deep dive: `patterns/event-sourcing.md` (covered alongside Event Sourcing itself).*

Replaying every event since the beginning of time gets expensive as history grows; a
snapshot is a point-in-time memento you can restore from and replay only the events after
it.

- **Reference**: Martin Fowler, *Memento* (https://martinfowler.com/eaaDev/EventSourcing.html#UsingMementosToStoreState)
  and the GoF Memento pattern (*Design Patterns: Elements of Reusable Object-Oriented
  Software*, Gamma/Helm/Johnson/Vlissides, 1994).
- **In this repo**: `IOriginator.CreateMemento`/`SetMemento`, `SnapShotEntity`,
  `EventStoreUnitOfWork.LoadSnapShotIfExistsAsync`/`CommitAsync` (write side, cadence
  controlled by `EventStoreOptions.SnapshotFrequency`). See
  `06-event-sourcing-infrastructure.md`.

### Domain-Driven Design building blocks

*Deep dive: `patterns/ddd-building-blocks.md`.*

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

*Deep dive: `patterns/repository-and-unit-of-work.md`.*

`IDomainRepository<T>`/`IReportingRepository` both hide the storage mechanism behind a
collection-like interface (`GetByIdAsync`, `Add`, `SaveAsync`) — callers never see SQL, EF
Core, or the event store's serialization format.

- **Reference**: Eric Evans, *DDD* (as above); also GoF's underlying idea of encapsulating
  data access behind an interface.
- **In this repo**: `DomainRepository<T>` (write side, `06-event-sourcing-infrastructure.md`),
  `SqlServerReportingRepository` (read side, `08-reporting-read-models.md`).

### Unit of Work

*Deep dive: `patterns/repository-and-unit-of-work.md`.*

Tracks a batch of changes and commits or rolls them back as one transaction.

- **Reference**: Martin Fowler, *Patterns of Enterprise Application Architecture*
  (Addison-Wesley, 2002), the *Unit of Work* pattern.
- **In this repo**: `EventStoreUnitOfWork<T>.CommitAsync`/`RollbackAsync` wraps the event
  store transaction; `TransactionHandler<TCommand,THandler>` is what actually invokes it
  around a command handler's execution. See `07-messaging-bus.md`. (Note the naming
  collision flagged there — two unrelated `IUnitOfWork` interfaces share a name.)

### Mediator / Message Bus

*Deep dive: `patterns/messaging-mediator-observer.md`.*

Callers publish a message without knowing which handler(s) will process it; the bus looks
that up at runtime.

- **Reference**: GoF *Mediator* pattern (as above), and Udi Dahan's writing on buses vs.
  mediators in CQRS systems (https://udidahan.com/2011/06/06/mediating-in-cqrs/ discusses
  the distinction; this project uses a real in-process bus, not a pure request/handler
  mediator, since it also carries the event stream).
- **In this repo**: `IBus`/`DirectBus`, `MessageRouter`, `CommandHandlerHelper`. See
  `07-messaging-bus.md` for the full dispatch chain.

### Observer (via Rx.NET)

*Deep dive: `patterns/messaging-mediator-observer.md` (covered alongside Mediator/Message Bus).*

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

*Deep dive: `patterns/mvp.md`. See also `patterns/vue-architecture.md`,
`patterns/winforms-architecture.md`, and `patterns/wpf-architecture.md` for the full
layered architecture standard (not just this one pattern) each of the three clients
follows.*

The View is a passive interface (`IClientDetailsView`, etc.) with events and settable
properties; the Presenter contains all the logic and is unit-testable without any real
UI. This codebase's specific flavor auto-wires View events to Presenter methods by naming
convention (`OnXxx` ↔ `Xxx`) via reflection, rather than manual event subscriptions. This
is a WinForms-only pattern — Vue's equivalent (`Fohjin.DDD.WebUI`) is a plain Vue 3
Options/Composition-API SPA with no MVP-style indirection; it's a sibling client, not a
second implementation of this pattern.

- **Reference**: Martin Fowler, *GUI Architectures*
  (https://martinfowler.com/eaaDev/uiArchs.html) — covers MVP alongside MVC and
  Presentation Model and the tradeoffs between them.
- **In this repo**: `Presenter<TView>`'s reflection-based `HookUpViewEvents`, and every
  `*Presenter`/`I*View` pair under `Fohjin.DDD.BankApplication.Core`. See
  `09-client-uis.md`.

### Specification / dynamic query object

*Deep dive: `patterns/ddd-building-blocks.md` (covered alongside the DDD building blocks).*

Rather than writing a predicate per DTO/filter combination, `UpdateAsync`/`DeleteAsync`'s
"where" object builds an `Expression<Func<TDto,bool>>` at runtime from an anonymous object's
properties — one generic mechanism instead of N hand-written ones. This used to also cover
reads (`GetByExampleAsync`); that's gone in favor of a genuinely composable `IQueryable<TDto>`
(the pattern below) — equality-only predicates built by reflection were a worse fit for
`$filter`/`$orderby`/paging than for the fixed "set/remove rows matching these fields" shape a
write naturally has.

- **Reference**: Eric Evans & Martin Fowler, *Specification*
  (https://martinfowler.com/apsupp/spec.pdf) — the general idea of representing a business
  rule/query as a composable object rather than inline code. `SqlServerReportingRepository`'s
  version is a simplified, reflection-driven take on the same idea (equality-only
  predicates, not full boolean composition).
- **In this repo**: `SqlServerReportingRepository.BuildPredicate<TDto>`. See
  `08-reporting-read-models.md`.

### IQueryable repository + OData

*Deep dive: `patterns/repository-and-unit-of-work.md`.*

The read side's Repository (`IReportingRepository`) exposes composition, not a menu of
pre-built queries: `Query<TDto>()` returns a plain `IQueryable<TDto>` that callers
`.Where()`/`.OrderBy()`/etc themselves, and a `GetByIdAsync<TDto>(object id)` fast path covers
the common single-row-by-primary-key case. Both plain REST endpoints and every OData endpoint
(top-level entity sets *and* nested/contained routes like
`/odata/ClientDetails(id)/Accounts?$filter=...`) apply their query options to the exact same
`IQueryable` — one query mechanism for every DTO, rather than a hand-rolled example-object shape
per call site. The oft-cited "IQueryable repositories are a leaky abstraction" critique (e.g.
Ayende Rahien) is real but narrow: the leak is that LINQ-provider translation differs between a
real EF Core `DbSet<T>` and, say, `List<T>.AsQueryable()` in a test double (`.FirstAsync()`/
`.ToListAsync()` require `IAsyncQueryProvider`, which only the former has) — not the "hidden
global dependency" failure mode Rahien's *Repository is the new Singleton* critique describes
for Singletons/Service Locators. A composable `IQueryable` doesn't hide what it depends on or
share mutable state across unrelated callers.

- **Reference**: Ayende Rahien, *Repository is the new Singleton*
  (https://ayende.com/blog/3955/repository-is-the-new-singleton) — the critique this pattern is
  usually raised against, and why it doesn't actually apply here (see above).
- **In this repo**: `IReportingRepository.Query<TDto>()`/`GetByIdAsync<TDto>()`,
  `EndpointRouteBuilderExtensions.MapODataEntitySet<TDto>`,
  `OData/Controllers/ClientDetailsController.cs`/`AccountDetailsController.cs`. See
  `08-reporting-read-models.md`.

### Compensating transaction

*Deep dive: `patterns/resilience-patterns.md`.*

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

*Deep dive: `patterns/repository-and-unit-of-work.md` (covered alongside Repository/Unit of Work).*

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
