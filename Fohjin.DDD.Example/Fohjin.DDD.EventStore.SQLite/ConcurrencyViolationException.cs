namespace Fohjin.DDD.EventStore.SQLite;

public class ConcurrencyViolationException(string? message) : Exception(message)
{
}