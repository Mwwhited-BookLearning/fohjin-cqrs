using Fohjin.DDD.EventHandlers;
using Fohjin.DDD.EventStore;

namespace Fohjin.DDD.Tests.Bus;

public class FirstTestEventHandler : IEventHandler<TestEvent>
{
    public List<Guid> Ids;
    public readonly SemaphoreSlim Signal = new(0);

    public FirstTestEventHandler()
    {
        Ids = [];
    }

    public Task ExecuteAsync(TestEvent @event)
    {
        Ids.Add(@event.Id);
        Signal.Release();
        return Task.CompletedTask;
    }

    public Task ExecuteAsync(IDomainEvent @event) =>
        ExecuteAsync((TestEvent)@event);
}
