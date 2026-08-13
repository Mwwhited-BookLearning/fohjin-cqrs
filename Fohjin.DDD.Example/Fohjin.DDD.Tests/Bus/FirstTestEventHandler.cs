using Fohjin.DDD.EventHandlers;
using Fohjin.DDD.EventStore;

namespace Fohjin.DDD.Tests.Bus;

public class FirstTestEventHandler : IEventHandler<TestEvent>
{
    public List<Guid> Ids;
    public readonly SemaphoreSlim Signal = new(0);

    // Opt-in, defaults false - see FirstTestCommandHandler.ShouldThrow for why this is a
    // property on the existing fixture rather than a second discoverable IEventHandler<TestEvent>
    // type (All_domain_events_must_have_a_handler.cs would sweep one up and fail on it).
    public bool ShouldThrow { get; set; }

    public FirstTestEventHandler()
    {
        Ids = [];
    }

    public Task ExecuteAsync(TestEvent @event)
    {
        Ids.Add(@event.Id);
        Signal.Release();
        if (ShouldThrow)
            throw new InvalidOperationException("boom");
        return Task.CompletedTask;
    }

    public Task ExecuteAsync(IDomainEvent @event) =>
        ExecuteAsync((TestEvent)@event);
}
