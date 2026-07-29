using Fohjin.DDD.EventStore;

namespace Fohjin.DDD.Tests.Bus;

public class NullUnitOfWork : IUnitOfWork
{
    public Task CommitAsync() => Task.CompletedTask;

    public Task RollbackAsync() => Task.CompletedTask;
}
