namespace Fohjin.DDD.EventStore.Storage;

public interface ISnapShotStorage<TDomainEvent> where TDomainEvent : IDomainEvent
{
    Task<ISnapShot?> GetSnapShotAsync(Guid entityId);
    Task SaveShapShotAsync(IEventProvider<TDomainEvent> entity);
}
