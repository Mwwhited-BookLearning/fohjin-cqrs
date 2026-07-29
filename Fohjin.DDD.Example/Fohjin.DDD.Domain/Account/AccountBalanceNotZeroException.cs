namespace Fohjin.DDD.Domain.Account;

public class AccountBalanceNotZeroException(string message) : Exception(message)
{
}