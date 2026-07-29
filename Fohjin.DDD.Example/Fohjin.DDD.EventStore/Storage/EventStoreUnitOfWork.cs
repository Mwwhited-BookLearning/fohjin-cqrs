using Fohjin.DDD.Bus;
using Fohjin.DDD.EventStore.Storage.Memento;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fohjin.DDD.EventStore.Storage;

public class EventStoreUnitOfWork<TDomainEvent>(
    IDomainEventStorage<TDomainEvent> domainEventStorage,
    IIdentityMap<TDomainEvent> identityMap,
    IBus bus,
    IOptions<EventStoreOptions> eventStoreOptions,
    ILogger<EventStoreUnitOfWork<TDomainEvent>> log
        ) : IEventStoreUnitOfWork<TDomainEvent> where TDomainEvent : IDomainEvent
{
    private static int _seed;
    private readonly int _id = _seed++;

    private readonly IDomainEventStorage<TDomainEvent> _domainEventStorage = domainEventStorage;
    private readonly IIdentityMap<TDomainEvent> _identityMap = identityMap;
    private readonly IBus _bus = bus;
    private readonly int _snapshotFrequency = eventStoreOptions.Value.SnapshotFrequency;
    private readonly List<IEventProvider<TDomainEvent>> _eventProviders = new ();
    private readonly ILogger _log = log;

    public async Task<TAggregate?> GetByIdAsync<TAggregate>(Guid id) where TAggregate : class, IOriginator, IEventProvider<TDomainEvent>, new()
    {
        _log.LogInformation($"{nameof(GetByIdAsync)}({{{nameof(id)}}})", id);
        var aggregateRoot = new TAggregate();

        await LoadSnapShotIfExistsAsync(id, aggregateRoot);
        await LoadRemainingHistoryEventsAsync(id, aggregateRoot);
        RegisterForTracking(aggregateRoot);

        return aggregateRoot;
    }

    public void Add<TAggregate>(TAggregate aggregateRoot) where TAggregate : class, IOriginator, IEventProvider<TDomainEvent>, new()
    {
        _log.LogInformation($"{nameof(Add)}({{{nameof(aggregateRoot)}}})", aggregateRoot);
        RegisterForTracking(aggregateRoot);
    }

    public void RegisterForTracking<TAggregate>(TAggregate aggregateRoot) where TAggregate : class, IOriginator, IEventProvider<TDomainEvent>, new()
    {
        _log.LogInformation($"{nameof(RegisterForTracking)}({{{nameof(aggregateRoot)}}})", aggregateRoot);
        _eventProviders.Add(aggregateRoot);
        _identityMap.Add(aggregateRoot);
    }

    public async Task CommitAsync()
    {
        _log.LogInformation($"{nameof(CommitAsync)}");
        await _domainEventStorage.BeginTransactionAsync();

        foreach (var eventProvider in _eventProviders)
        {
            await _domainEventStorage.SaveAsync(eventProvider);

            // The read path (LoadSnapShotIfExistsAsync/LoadRemainingHistoryEventsAsync above)
            // has always honored a snapshot if one exists - this is the write side that used to
            // be missing entirely (docs/06-event-sourcing-infrastructure.md,
            // docs/patterns/event-sourcing.md): nothing ever called SaveShapShotAsync outside of
            // test fixtures, so a snapshot never existed in the production commit path no matter
            // how many events an aggregate accumulated. GetEventCountSinceLastSnapShotAsync is
            // exactly the "should I snapshot now" predicate the storage layer already implements
            // for this - SaveAsync above has already persisted this event and advanced
            // eventProvider.Version, so the count below reflects it.
            if (await _domainEventStorage.GetEventCountSinceLastSnapShotAsync(eventProvider.Id) >= _snapshotFrequency)
                await _domainEventStorage.SaveShapShotAsync(eventProvider);

            _bus.Publish(eventProvider.GetChanges().Select(x => (object)x));
            eventProvider.Clear();
        }
        _eventProviders.Clear();

        await _bus.CommitAsync();
        await _domainEventStorage.CommitAsync();
    }

    public async Task RollbackAsync()
    {
        _log.LogInformation($"{nameof(RollbackAsync)}");
        _bus.Rollback();
        await _domainEventStorage.RollbackAsync();
        foreach (var eventProvider in _eventProviders)
        {
            _identityMap.Remove(eventProvider.GetType(), eventProvider.Id);
        }
        _eventProviders.Clear();
    }

    private async Task LoadSnapShotIfExistsAsync(Guid id, IOriginator aggregateRoot)
    {
        _log.LogInformation($"{nameof(LoadSnapShotIfExistsAsync)}({{{nameof(id)}}}, {{{nameof(aggregateRoot)}}})", id, aggregateRoot);
        var snapShot = await _domainEventStorage.GetSnapShotAsync(id);
        if (snapShot == null)
            return;

        aggregateRoot.SetMemento(snapShot.Memento);
    }

    private async Task LoadRemainingHistoryEventsAsync(Guid id, IEventProvider<TDomainEvent> aggregateRoot)
    {
        _log.LogInformation($"{nameof(LoadRemainingHistoryEventsAsync)}({{{nameof(id)}}}, {{{nameof(aggregateRoot)}}})", id, aggregateRoot);
        var events = await _domainEventStorage.GetEventsSinceLastSnapShotAsync(id);
        if (events.Any())
        {
            aggregateRoot.LoadFromHistory(events);
            return;
        }

        aggregateRoot.LoadFromHistory(await _domainEventStorage.GetAllEventsAsync(id));
    }
}
