using Fohjin.DDD.EventStore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Fohjin.DDD.Bus.Direct;

public class DirectBus : IBus
{
    private IRouteMessages? _routeMessages;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _log;

    private readonly ConcurrentQueue<object> _preCommitQueue = new();
    private readonly IQueue _postCommitQueue;
    private readonly Subject<IDomainEvent> _events = new();

    public DirectBus(
        IServiceProvider serviceProvider,
        IQueue postCommitQueue,
        ILogger<DirectBus> log
        )
    {
        _serviceProvider = serviceProvider;
        _log = log;
        _postCommitQueue = postCommitQueue;
        _ = _postCommitQueue.PopAsync(DoPublishAsync);
    }

    public IObservable<IDomainEvent> Events => _events.AsObservable();

    public void Publish(object message)
    {
        _log.LogInformation($"{nameof(Publish)}: {{{nameof(message)}}}", message);
        _preCommitQueue.Enqueue(message);
    }

    public void Publish(IEnumerable<object> messages)
    {
        _log.LogInformation($"{nameof(Publish)}: {{{nameof(messages)}}}", messages);
        foreach (var message in messages)
            _preCommitQueue.Enqueue(message);
    }

    public Task CommitAsync()
    {
        _log.LogInformation($"{nameof(CommitAsync)}");

        // Fire-and-forget: hand each message to the post-commit queue without waiting for it
        // to be dispatched. Command handling and event-stream delivery both happen detached
        // from this call, so callers stop blocking on the outcome of what they published.
        while (_preCommitQueue.TryDequeue(out var @obj))
        {
            _ = _postCommitQueue.PutAsync(@obj);
        }

        return Task.CompletedTask;
    }

    public void Rollback()
    {
        _log.LogInformation($"{nameof(Rollback)}");
            _preCommitQueue.Clear();
    }

    private async Task DoPublishAsync(object message)
    {
        _log.LogInformation($"{nameof(DoPublishAsync)}: {{{nameof(message)}}}", message);
        try
        {
            if (message is IDomainEvent domainEvent)
            {
                _events.OnNext(domainEvent);
            }
            else
            {
                _routeMessages ??= _serviceProvider.GetRequiredService<IRouteMessages>();
                await _routeMessages.RouteAsync(message);
            }
        }
        catch (Exception ex)
        {
            // Nothing awaits this method anymore now that CommitAsync is fire-and-forget, so an
            // unhandled exception here would otherwise be lost as an unobserved task exception.
            _log.LogError(ex, $"{nameof(DoPublishAsync)}-Failed: {{type}}: {{{nameof(message)}}}", message.GetType(), message);
        }
        finally
        {
            await _postCommitQueue.PopAsync(DoPublishAsync);
        }
    }
}
