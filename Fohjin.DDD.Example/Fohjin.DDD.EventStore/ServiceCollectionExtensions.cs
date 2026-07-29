using Fohjin.DDD.EventStore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Fohjin.DDD.EventStore;

public static class ServiceCollectionExtensions
{
    public static T AddEventStoreServices<T>(this T service) where T : IServiceCollection
    {
        // A working default (EventStoreOptions.SnapshotFrequency = 10) even if nothing ever
        // binds an "EventStore" config section - Fohjin.DDD.WebApi.Program.cs still hooks
        // Configure<EventStoreOptions>() up to appsettings.json for anyone who wants a
        // different cadence, same Options<T> pattern the rest of this codebase now uses.
        service.AddOptions<EventStoreOptions>();

        service.TryAddSingleton(typeof(IDomainRepository<>), typeof(DomainRepository<>));
        service.TryAddSingleton(typeof(IEventStoreUnitOfWork<>), typeof(EventStoreUnitOfWork<>));
        service.TryAddSingleton(typeof(IIdentityMap<>), typeof(EventStoreIdentityMap<>));

        service.TryAddSingleton<IUnitOfWork>(sp => sp.GetRequiredService<IEventStoreUnitOfWork<IDomainEvent>>());
        return service;
    }
}