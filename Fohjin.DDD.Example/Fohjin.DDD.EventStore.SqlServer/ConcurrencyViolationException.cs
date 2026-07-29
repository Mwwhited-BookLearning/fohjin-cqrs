namespace Fohjin.DDD.EventStore.SqlServer;

public class ConcurrencyViolationException(string? message) : Exception(message)
{
}