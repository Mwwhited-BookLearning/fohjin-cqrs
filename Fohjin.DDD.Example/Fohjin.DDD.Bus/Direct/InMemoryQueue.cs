using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Fohjin.DDD.Bus.Direct;

public class InMemoryQueue(
    ILogger<InMemoryQueue> log
        ) : IQueue
{
    private static int _seed;
    private readonly int _id = _seed++;

    private readonly ConcurrentQueue<object> _itemQueue = new();
    private readonly ConcurrentQueue<Func<object, Task>> _listenerQueue = new();

    private readonly ILogger _log = log;

    public async Task PutAsync(object item)
    {
        _log.LogInformation($"PutAsync({{id}})> {{{nameof(item)}}}", _id, item);
        if (!_listenerQueue.Any())
        {
            _itemQueue.Enqueue(item);
            return;
        }

        if (_listenerQueue.TryDequeue(out var listener))
            await listener(item);
    }

    public async Task PopAsync(Func<object, Task> popAction)
    {
        _log.LogInformation($"PopAsync({{id}})> {{{nameof(popAction)}}}", _id, popAction);
        if (!_itemQueue.Any())
        {
            _listenerQueue.Enqueue(popAction);
            return;
        }

        if (_itemQueue.TryDequeue(out var item))
            await popAction(item);
    }
}