
namespace Fohjin.DDD.EventStore;

public interface IUnitOfWork
{
    Task CommitAsync();
    Task RollbackAsync();
}
