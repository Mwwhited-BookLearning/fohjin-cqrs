namespace Fohjin.DDD.Domain.Client;

public class NonExistingClientException(string message) : Exception(message)
{
}