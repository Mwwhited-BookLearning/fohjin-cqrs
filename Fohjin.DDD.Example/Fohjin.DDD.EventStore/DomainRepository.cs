using Fohjin.DDD.EventStore.Storage;
using Fohjin.DDD.EventStore.Storage.Memento;
using Microsoft.Extensions.Logging;

namespace Fohjin.DDD.EventStore;

public class DomainRepository<TDomainEvent>(
    IEventStoreUnitOfWork<TDomainEvent> eventStoreUnitOfWork,
    IIdentityMap<TDomainEvent> identityMap,
    ILogger<DomainRepository<TDomainEvent>> log
        ) : IDomainRepository<TDomainEvent> where TDomainEvent : IDomainEvent
{
    private readonly IEventStoreUnitOfWork<TDomainEvent> _eventStoreUnitOfWork = eventStoreUnitOfWork;
    private readonly IIdentityMap<TDomainEvent> _identityMap = identityMap;
    private readonly ILogger _log = log;

    public async Task<TAggregate?> GetByIdAsync<TAggregate>(Guid id)
        where TAggregate : class, IOriginator, IEventProvider<TDomainEvent>, new()
    {
        _log.LogInformation($"{nameof(GetByIdAsync)}({{{nameof(id)}}})", id);
        return RegisterForTracking(_identityMap.GetById<TAggregate>(id)) ?? await _eventStoreUnitOfWork.GetByIdAsync<TAggregate>(id);
    }

    public void Add<TAggregate>(TAggregate aggregateRoot) where TAggregate : class, IOriginator, IEventProvider<TDomainEvent>, new()
    {
        _log.LogInformation($"{nameof(Add)}({{{nameof(aggregateRoot)}}})", aggregateRoot);
        _eventStoreUnitOfWork.Add(aggregateRoot);
    }

    private TAggregate? RegisterForTracking<TAggregate>(TAggregate? aggregateRoot)
        where TAggregate : class, IOriginator, IEventProvider<TDomainEvent>, new()
    {
        if (aggregateRoot == null)
            return aggregateRoot;

        _eventStoreUnitOfWork.RegisterForTracking(aggregateRoot);
        return aggregateRoot;
    }
}
