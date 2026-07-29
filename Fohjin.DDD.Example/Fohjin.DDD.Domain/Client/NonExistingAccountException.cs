namespace Fohjin.DDD.Domain.Client;

public class NonExistingAccountException(string message) : Exception(message)
{
}