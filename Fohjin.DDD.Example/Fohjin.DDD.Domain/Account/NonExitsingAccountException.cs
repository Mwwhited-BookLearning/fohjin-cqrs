namespace Fohjin.DDD.Domain.Account;

public class NonExitsingAccountException(string message) : Exception(message)
{
}