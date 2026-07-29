using Fohjin.DDD.CommandHandlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fohjin.DDD.MessageRouting;

public static class ServiceCollectionExtensions
{
    public static T AddMessageRoutingServices<T>(this T service) where T : IServiceCollection
    {
        service.TryAddSingleton<ICommandHandlerHelper, CommandHandlerHelper>();
        return service;
    }
}