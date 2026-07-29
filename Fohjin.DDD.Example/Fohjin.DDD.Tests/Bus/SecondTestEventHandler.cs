using Fohjin.DDD.EventHandlers;
using Fohjin.DDD.EventStore;

namespace Fohjin.DDD.Tests.Bus;

public class SecondTestEventHandler : IEventHandler<TestEvent>
{
    public List<Guid> Ids;
    public readonly SemaphoreSlim Signal = new(0);

    public SecondTestEventHandler()
    {
        Ids = [];
    }

    public Task ExecuteAsync(TestEvent command)
    {
        Ids.Add(command.Id);
        Signal.Release();
        return Task.CompletedTask;
    }

    public async Task ExecuteAsync(IDomainEvent @event)
    {
        if (@event is TestEvent evnt)
            await ExecuteAsync(evnt);
    }
}
