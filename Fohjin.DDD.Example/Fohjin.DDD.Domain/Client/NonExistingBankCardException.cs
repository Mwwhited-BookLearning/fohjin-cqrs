namespace Fohjin.DDD.Domain.Client;

public class NonExistingBankCardException(string message) : Exception(message)
{
}