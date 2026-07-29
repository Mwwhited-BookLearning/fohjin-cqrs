using System.Collections.Concurrent;

namespace Fohjin.DDD.EventStore.Aggregate;

public abstract class BaseEntity<TDomainEvent> : IEntityEventProvider<TDomainEvent> where TDomainEvent : IDomainEvent
{
    public Guid Id { get; set; }
    private readonly ConcurrentDictionary<Type, Action<TDomainEvent>> _events = new();
    private readonly ConcurrentBag<TDomainEvent> _appliedEvents = new();
    private Func<int>? _versionProvider;

    protected void RegisterEvent<TEvent>(Action<TEvent> eventHandler) where TEvent : class, TDomainEvent
    {
        _events.TryAdd(typeof(TEvent), theEvent => eventHandler((TEvent)theEvent));
    }

    protected void Apply<TEvent>(TEvent domainEvent) where TEvent : class, TDomainEvent
    {
        domainEvent.Version = _versionProvider?.Invoke() ?? -1;
        Apply(domainEvent.GetType(), domainEvent);
        // See BaseAggregateRoot<T>.Apply's comment - same fix, same reason: stamping AggregateId
        // from the pre-handler Id would silently record Guid.Empty for any entity whose Id gets
        // assigned by its own creation event's handler rather than beforehand.
        domainEvent.AggregateId = Id;
        _appliedEvents.Add(domainEvent);
    }

    void IEntityEventProvider<TDomainEvent>.LoadFromHistory(IEnumerable<TDomainEvent> domainEvents)
    {
        foreach (var domainEvent in domainEvents)
        {
            Apply(domainEvent.GetType(), domainEvent);
        }
    }

    public void HookUpVersionProvider(Func<int> versionProvider) => _versionProvider = versionProvider;
    IEnumerable<TDomainEvent> IEntityEventProvider<TDomainEvent>.GetChanges() => _appliedEvents;
    void IEntityEventProvider<TDomainEvent>.Clear() => _appliedEvents.Clear();

    private void Apply(Type eventType, TDomainEvent domainEvent)
    {
        if (!_events.TryGetValue(eventType, out var handler))
            throw new UnregisteredDomainEventException($"The requested domain event '{eventType.FullName}' is not registered in '{GetType().FullName}'");

        handler(domainEvent);
    }
}