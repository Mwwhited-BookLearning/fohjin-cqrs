namespace Fohjin.DDD.Services;

public class UnknownAccountException(string message) : Exception(message)
{
}