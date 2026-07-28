namespace Fohjin.DDD.EventStore;

public interface ITransactional
{
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
