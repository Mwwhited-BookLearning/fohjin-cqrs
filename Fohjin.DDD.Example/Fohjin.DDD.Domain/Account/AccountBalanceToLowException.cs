namespace Fohjin.DDD.Domain.Account;

public class AccountBalanceToLowException(string message) : Exception(message)
{
}