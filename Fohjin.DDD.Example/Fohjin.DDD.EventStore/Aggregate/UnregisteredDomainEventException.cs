namespace Fohjin.DDD.EventStore.Aggregate;

public class UnregisteredDomainEventException(string message) : Exception(message)
{
}