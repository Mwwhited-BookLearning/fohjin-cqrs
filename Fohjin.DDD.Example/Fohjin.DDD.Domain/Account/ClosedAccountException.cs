namespace Fohjin.DDD.Domain.Account;

public class ClosedAccountException(string message) : Exception(message)
{
}