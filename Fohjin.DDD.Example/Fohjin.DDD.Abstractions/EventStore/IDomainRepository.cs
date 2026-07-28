using Fohjin.DDD.EventStore.Storage.Memento;

namespace Fohjin.DDD.EventStore;

public interface IDomainRepository<TDomainEvent> where TDomainEvent : IDomainEvent
{
    Task<TAggregate?> GetByIdAsync<TAggregate>(Guid id)
        where TAggregate : class, IOriginator, IEventProvider<TDomainEvent>, new();

    void Add<TAggregate>(TAggregate aggregateRoot)
        where TAggregate : class, IOriginator, IEventProvider<TDomainEvent>, new();
}
