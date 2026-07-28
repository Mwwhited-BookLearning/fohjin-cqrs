namespace Fohjin.DDD.EventStore.Storage;

public interface IDomainEventStorage<TDomainEvent> : ISnapShotStorage<TDomainEvent>, ITransactional where TDomainEvent : IDomainEvent
{
    Task<IEnumerable<TDomainEvent>> GetAllEventsAsync(Guid eventProviderId);
    Task<IEnumerable<TDomainEvent>> GetEventsSinceLastSnapShotAsync(Guid eventProviderId);
    Task<int> GetEventCountSinceLastSnapShotAsync(Guid eventProviderId);
    Task SaveAsync(IEventProvider<TDomainEvent> eventProvider);
}
