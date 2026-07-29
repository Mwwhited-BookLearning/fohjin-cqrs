# Event Sourcing Infrastructure

The plumbing every aggregate in `01`–`05` sits on top of: how an aggregate raises and
replays events, how it's loaded and saved, and how that's persisted to SQL Server. None of
this changed when this system grew an HTTP front door — it's reached today from
`Fohjin.DDD.WebApi`'s minimal API endpoints (`00-architecture-overview.md`'s data-flow
section) instead of directly from WinForms presenters, but the code in this document is
untouched.

## Components

```plantuml
@startuml
skinparam rectangle {
  BackgroundColor<<Component>> #85bbf0
  FontColor<<Component>> black
  BorderColor black
}
skinparam defaultTextAlignment center
skinparam wrapWidth 200

rectangle "BaseAggregateRoot<T>\n<size:11><<Component>></size>\nApply/RegisterEvent/LoadFromHistory/\nGetChanges, 0-based version counter" <<Component>> as aggRoot
rectangle "EntityList<TEntity,T>\n<size:11><<Component>></size>\nChild-entity collection that\nregisters each add with the aggregate" <<Component>> as entityList
rectangle "DomainRepository<T>\n<size:11><<Component>></size>\nGetByIdAsync/Add,\nchecks identity map first" <<Component>> as repo
rectangle "EventStoreIdentityMap<T>\n<size:11><<Component>></size>\nIn-memory Type -> Guid -> instance cache" <<Component>> as idmap
rectangle "EventStoreUnitOfWork<T>\n<size:11><<Component>></size>\nCommitAsync/RollbackAsync,\nsnapshot load, tracks dirty aggregates" <<Component>> as uow
rectangle "DomainEventStorage<T>\n<size:11><<Component>></size>\nEF Core reads/writes,\nconcurrency check, transactions" <<Component>> as storage
database "DomainEventStoreDbContext\n<size:11><<EF Core, SQL Server>></size>" <<Component>> as dbcontext

aggRoot o-- entityList
repo --> idmap : checks first
repo --> uow : delegates on miss
uow --> storage : Save/GetSnapShot/GetEvents
uow --> aggRoot : SetMemento / LoadFromHistory
storage --> dbcontext
@enduml
```

## `BaseAggregateRoot<TDomainEvent>`

```plantuml
@startuml
skinparam classAttributeIconSize 0
hide circle

class BaseAggregateRoot<T> {
  + Id : Guid
  + Version : int
  + EventVersion : int
  - _registeredEvents : Dictionary<Type, Action<T>>
  - _appliedEvents : List<T>
  - _childEventProviders : List<IEntityEventProvider<T>>
  # RegisterEvent<TEvent>(Action<TEvent>)
  # Apply<TEvent>(TEvent)
  + LoadFromHistory(IEnumerable<T>)
  + GetChanges() : IEnumerable<T>
  + Clear()
  + UpdateVersion(int)
  - GetNewEventVersion() : int
}
note right of BaseAggregateRoot::GetNewEventVersion
  return EventVersion++
  (post-increment: first event
  gets version 0, not 1)
end note

class EntityList<TEntity, T> {
  + Add(TEntity) : void
}
note right of EntityList::Add
  registers the child with the
  aggregate (RegisterChildEventProvider)
  BEFORE calling List<T>.Add
end note

BaseAggregateRoot "1" o-- "*" EntityList
@enduml
```

`Apply<TEvent>` does three things in order: stamp `AggregateId`/`Version` on the event,
dispatch it to the registered handler (mutating state), and append it to `_appliedEvents`
(the "changes since last commit" buffer that `GetChanges()` returns).

Child entities (e.g. `BankCard` under `Client`) don't keep their own version counter —
they call back into the parent aggregate's `EventVersion` via a `Func<int>` hooked up when
`EntityList.Add` registers them, so parent and child events share one monotonic sequence.

## Repository / unit-of-work chain

```plantuml
@startuml
skinparam classAttributeIconSize 0
hide circle

interface "IDomainRepository<T>" as IRepo {
  + GetByIdAsync<TAggregate>(Guid) : Task<TAggregate?>
  + Add<TAggregate>(TAggregate)
}
class "DomainRepository<T>" as Repo
interface "IEventStoreUnitOfWork<T>" as IUow {
  + GetByIdAsync<TAggregate>(Guid)
  + Add<TAggregate>(TAggregate)
  + CommitAsync()
  + RollbackAsync()
}
class "EventStoreUnitOfWork<T>" as Uow
interface "IDomainEventStorage<T>" as IStorage
class "DomainEventStorage<T>" as Storage

Repo ..|> IRepo
Uow ..|> IUow
Storage ..|> IStorage
Repo --> IUow : delegates
Repo --> "EventStoreIdentityMap<T>" : checks first
Uow --> IStorage
Uow --> "IBus" : Publish(GetChanges())\nper aggregate on commit
@enduml
```

`DomainRepository.GetByIdAsync` checks the identity map first (so a second `GetByIdAsync`
for the same aggregate within one unit of work returns the same in-memory instance)
before falling through to `EventStoreUnitOfWork`, which actually loads from storage.

## Entity relations — event store tables

```plantuml
@startuml
entity EventProviderEntity {
  * EventProviderId : Guid <<PK>>
  --
  Type : string
  Version : int
}
entity EventRecordEntity {
  * Id : Guid <<PK>>
  --
  EventProviderId : Guid
  Event : byte[]
  Version : int
}
entity SnapShotEntity {
  * EventProviderId : Guid <<PK>>
  --
  SnapShot : byte[]
  Version : int
}

EventProviderEntity ||--o{ EventRecordEntity : "EventProviderId (logical,\nno DB FK constraint)"
EventProviderEntity ||--o| SnapShotEntity : "EventProviderId (logical,\nno DB FK constraint)"
@enduml
```

One `EventProviderEntity` row per aggregate/entity stream. Relationships are enforced only
by matching `EventProviderId` values — there's no `HasOne`/`WithMany` in the EF Core model
and no foreign-key constraint in the migrations.

> **Gap found while documenting this**: the snapshot machinery
> (`SaveShapShotAsync`/`GetEventCountSinceLastSnapShotAsync`) exists and works, but nothing
> in the production commit path (`EventStoreUnitOfWork.CommitAsync`) ever calls it
> automatically. It's only invoked from repository tests, whose names imply an intended
> "snapshot every 10 events" policy that was never wired up outside tests.

## Sequence: load with snapshot

```plantuml
@startuml
participant "DomainRepository" as Repo
participant "EventStoreUnitOfWork" as Uow
participant "DomainEventStorage" as Storage
participant "Aggregate\n(e.g. Client)" as Agg

Repo -> Repo : IdentityMap.GetById(id) -> miss
Repo -> Uow : GetByIdAsync<T>(id)
Uow -> Agg : new T()
Uow -> Storage : GetSnapShotAsync(id)
Storage --> Uow : SnapShot (or null)
Uow -> Agg : SetMemento(snapshot.Memento)
Uow -> Storage : GetEventsSinceLastSnapShotAsync(id)
Storage --> Uow : events where Version >= snapshotVersion
Uow -> Agg : LoadFromHistory(events)
Agg -> Agg : replay each event via\nregistered handler,\nVersion = EventVersion = last event's Version
Uow -> Uow : RegisterForTracking(aggregate)
Uow --> Repo : aggregate
@enduml
```

If no snapshot exists, `GetEventsSinceLastSnapShotAsync` returns nothing and the flow falls
back to `GetAllEventsAsync(id)` — full replay from event 0.

## Sequence: commit (save)

```plantuml
@startuml
participant "Command Handler" as Handler
participant "Aggregate" as Agg
participant "EventStoreUnitOfWork" as Uow
participant "DomainEventStorage" as Storage
participant "IBus" as Bus

Handler -> Agg : domain method call\n(Apply raises event(s))
Handler -> Uow : (via repository.Add or\nprior GetByIdAsync) RegisterForTracking
Handler -> Uow : CommitAsync()
Uow -> Storage : BeginTransactionAsync()
loop each tracked aggregate
    Uow -> Storage : SaveAsync(aggregate)
    Storage -> Storage : concurrency check:\nstoredVersion != aggregate.Version\n&& aggregate.Version > 0\n-> throw ConcurrencyViolationException
    Storage -> Storage : insert one EventRecordEntity\nper change
    Storage -> Agg : UpdateVersion(newVersion)
    Uow -> Bus : Publish(aggregate.GetChanges())\n(just enqueues - see 07-messaging-bus.md)
    Uow -> Agg : Clear()
end
Uow -> Bus : CommitAsync()\n(fire-and-forget: hands queued messages\nto the post-commit queue and returns\nwithout waiting for dispatch)
Uow -> Storage : CommitAsync() (DB transaction commit)
@enduml
```

`Bus.CommitAsync()` returning does **not** mean event handlers have run yet — see
`07-messaging-bus.md` for the fire-and-forget dispatch this hands off to, which is also
why `Fohjin.DDD.WebApi`'s command endpoints return `202 Accepted` rather than `200 OK`
with a result.
