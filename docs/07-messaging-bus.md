# Messaging / Bus

How a published command reaches exactly one handler, and how a published domain event
reaches every independently-subscribed handler. Both travel through the same `IBus`, but
split onto two different paths inside `DirectBus`.

> **Naming collision worth knowing about**: there are two unrelated interfaces both named
> `IUnitOfWork`. `Fohjin.DDD.Bus.IUnitOfWork` (`CommitAsync` + sync `Rollback`) is what
> `IBus` extends. `Fohjin.DDD.EventStore.IUnitOfWork` (`CommitAsync` + async
> `RollbackAsync`) is a completely different type, injected into `TransactionHandler` as
> the event-store session (resolved via `IEventStoreUnitOfWork<IDomainEvent>`, see
> `06-event-sourcing-infrastructure.md`). Don't assume "unit of work" means "the bus" when
> reading handler constructors.

## Components

```plantuml
@startuml
skinparam rectangle {
  BackgroundColor<<Component>> #85bbf0
  FontColor<<Component>> black
  BorderColor black
}
skinparam defaultTextAlignment center
skinparam wrapWidth 220

rectangle "DirectBus\n<size:11><<Component>></size>\nTwo-stage queue; branches\nIDomainEvent vs everything else" <<Component>> as bus
rectangle "InMemoryQueue\n<size:11><<Component>></size>\nSingle-consumer async\nput/pop rendezvous" <<Component>> as queue
rectangle "Subject<IDomainEvent>\n<size:11><<Component>></size>\nRx.NET hot observable" <<Component>> as subject
rectangle "MessageRouter\n<size:11><<Component>></size>\nCommands only" <<Component>> as router
rectangle "CommandHandlerHelper\n<size:11><<Component>></size>\nMatches ICommandHandler<T>\nby message runtime type" <<Component>> as helper
rectangle "TransactionHandler<TCmd,THandler>\n<size:11><<Component>></size>\nExecutes handler, then\ncommits/rolls back the event store" <<Component>> as txn
rectangle "EventSubscriptionBootstrapper\n<size:11><<Component>></size>\nOne Rx subscription per\nIEventHandler, wired at startup" <<Component>> as subscriber

bus --> queue : Publish/CommitAsync (fire-and-forget)
bus --> subject : OnNext (if IDomainEvent)
bus --> router : RouteAsync (otherwise)
router --> helper
helper --> txn : resolves ITransactionHandler<,>\n(open generic) via IServiceProvider
subscriber --> subject : subscribes at startup,\nonce per event handler
@enduml
```

## `DirectBus` — the two-stage queue

```plantuml
@startuml
skinparam classAttributeIconSize 0
hide circle

class DirectBus {
  - _preCommitQueue : ConcurrentQueue<object>
  - _postCommitQueue : IQueue
  - _events : Subject<IDomainEvent>
  + Publish(object)
  + Publish(IEnumerable<object>)
  + CommitAsync() : Task
  + Rollback()
  + Events : IObservable<IDomainEvent>
  - DoPublishAsync(object) : Task
}
note bottom of DirectBus
  Publish() only enqueues to _preCommitQueue - nothing
  dispatches until CommitAsync() moves items to
  _postCommitQueue. CommitAsync() doesn't await the
  moves either: it returns as soon as the queue is
  drained, dispatch happens fully detached.
end note
@enduml
```

`DoPublishAsync` is registered once at construction (`_postCommitQueue.PopAsync(DoPublishAsync)`)
and re-registers itself in a `finally` block after every message — a self-sustaining
single-consumer loop, one message processed at a time, in submission order. Its try/catch
logs and swallows exceptions, because nothing awaits this method anymore now that
`CommitAsync` is fire-and-forget — an unhandled exception here would otherwise become an
unobserved task exception.

## Sequence: command dispatch

```plantuml
@startuml
participant "Caller" as Caller
participant "DirectBus" as Bus
participant "InMemoryQueue" as Queue
participant "MessageRouter" as Router
participant "CommandHandlerHelper" as Helper
participant "TransactionHandler" as Txn
participant "ICommandHandler<T>" as Handler
participant "EventStore.IUnitOfWork" as Uow

Caller -> Bus : Publish(command)
Bus -> Bus : enqueue to _preCommitQueue
Caller -> Bus : CommitAsync()
Bus -> Queue : PutAsync(command) (fire-and-forget)
Bus --> Caller : returns immediately
Queue -> Bus : DoPublishAsync(command)
Bus -> Bus : not IDomainEvent
Bus -> Router : RouteAsync(command)
Router -> Helper : RouteAsync(command)
Helper -> Helper : find ICommandHandler<T>\nassignable to command's type
Helper -> Txn : resolve ITransactionHandler<T,H>\n(GetRequiredService, open generic)
Helper -> Txn : ExecuteAsync(command, handler)
Txn -> Handler : ExecuteAsync(command)
alt success
    Txn -> Uow : CommitAsync()
else exception
    Txn -> Uow : RollbackAsync()
    Txn -> Txn : rethrow
    note right of Txn : caught + logged by\nDirectBus.DoPublishAsync,\nnever reaches original caller
end
Bus -> Queue : (finally) PopAsync(DoPublishAsync)\nre-registers for next message
@enduml
```

## Sequence: domain event fan-out (Rx)

```plantuml
@startuml
participant "Caller" as Caller
participant "DirectBus" as Bus
participant "Subject<IDomainEvent>" as Subject
participant "EventHandler A" as HandlerA
participant "EventHandler B" as HandlerB

note over Subject : At startup, EventSubscriptionBootstrapper\nsubscribed one filter per registered\nIEventHandler: Events.Select(e=>(object)e).OfType<TEvent>()

Caller -> Bus : Publish(domainEvent)
Caller -> Bus : CommitAsync()
Bus -> Bus : DoPublishAsync sees IDomainEvent
Bus -> Subject : OnNext(domainEvent)
Subject ->> HandlerA : matching subscription fires\n(try/catch per subscription)
Subject ->> HandlerB : matching subscription fires\n(independent try/catch)
note right of HandlerA : HandlerA throwing does not\nstop HandlerB, and neither\nbubbles back to DirectBus
@enduml
```

Each event handler gets its **own** independent Rx subscription over the same shared
`bus.Events` stream, filtered by `OfType<TEvent>()`. N handlers subscribed to the same
event type means N independent invocations per publish, each with its own try/catch — one
handler's failure never blocks or is seen by another (see `EventSubscriptionBootstrapper`
in `Fohjin.DDD.Configuration`).

The Vue frontend has its own client-side mirror of exactly this shape:
`Fohjin.DDD.WebUI/src/events/eventBus.ts` is one shared `GET /api/events` (SSE) connection
for the whole browser session, with N independent filtered subscribers (one per screen that
wants live refresh) instead of each screen opening its own connection or polling — see
`09-winforms-ui.md`'s Vue section for the client-side details.

## Startup wiring

This bus, and every command/event handler it dispatches to, now lives inside
`Fohjin.DDD.WebApi`'s process rather than the WinForms exe's (see
`00-architecture-overview.md`) — `Fohjin.DDD.WebApi/Program.cs`:
`app.Services.BootStrapApplicationAsync()` (event-store/reporting database migrations)
runs first, then `app.Services.SubscribeEventHandlers()` — every `IEventHandler`
registered in DI gets its reflection-derived `IEventHandler<TEvent>` inspected once, and
one Rx subscription created, *before* the API starts accepting requests.
