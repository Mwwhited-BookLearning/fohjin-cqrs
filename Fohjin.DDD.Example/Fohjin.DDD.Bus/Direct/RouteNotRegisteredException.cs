namespace Fohjin.DDD.Bus.Direct;

public class RouteNotRegisteredException(Type messageType) : Exception(string.Format("No route specified for message '{0}'", messageType.FullName))
{
}