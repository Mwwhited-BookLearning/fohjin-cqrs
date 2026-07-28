using Fohjin.DDD.EventStore;

namespace Test.Fohjin.DDD.Bus;

public class NullUnitOfWork : IUnitOfWork
{
    public Task CommitAsync() => Task.CompletedTask;

    public Task RollbackAsync() => Task.CompletedTask;
}
