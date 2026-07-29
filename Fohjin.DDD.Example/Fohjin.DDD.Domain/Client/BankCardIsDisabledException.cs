namespace Fohjin.DDD.Domain.Client;

public class BankCardIsDisabledException(string message) : Exception(message)
{
}