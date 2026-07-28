using Fohjin.DDD.EventStore;

namespace Fohjin.DDD.Bus
{
    public interface IBus : IUnitOfWork
    {
        void Publish(object message);
        void Publish(IEnumerable<object> messages);
        IObservable<IDomainEvent> Events { get; }
    }
}
