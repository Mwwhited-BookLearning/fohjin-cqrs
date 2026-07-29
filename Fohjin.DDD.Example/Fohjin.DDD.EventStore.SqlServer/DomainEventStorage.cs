using Fohjin.DDD.Common;
using Fohjin.DDD.EventStore.SqlServer.Entities;
using Fohjin.DDD.EventStore.Storage;
using Fohjin.DDD.EventStore.Storage.Memento;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Fohjin.DDD.EventStore.SqlServer;

public static class DomainEventStorageConfig
{
    // Aspire's WithReference(sqlDb) on a project resource injects the connection string as
    // ConnectionStrings__eventstoredb, which ASP.NET Core's config system maps to
    // ConnectionStrings:eventstoredb - using that same key outside Aspire (docker-compose .env,
    // plain appsettings.json) keeps one connection-string wiring convention everywhere.
    public const string ConnectionStringName = "eventstoredb";
    public const string ConnectionStringConfigKey = $"ConnectionStrings:{ConnectionStringName}";
}

public class DomainEventStorage<TDomainEvent>(IDbContextFactory<DomainEventStoreDbContext> dbContextFactory, IExtendedFormatter formatter) : IDomainEventStorage<TDomainEvent> where TDomainEvent : IDomainEvent
{
    private readonly IDbContextFactory<DomainEventStoreDbContext> _dbContextFactory = dbContextFactory;
    private readonly IExtendedFormatter _formatter = formatter;
    private DomainEventStoreDbContext? _transactionalContext;
    private IDbContextTransaction? _transaction;

    public Task<IEnumerable<TDomainEvent>> GetAllEventsAsync(Guid eventProviderId) =>
        WithContextAsync(async context =>
        {
            var records = await context.Events
                .Where(e => e.EventProviderId == eventProviderId)
                .OrderBy(e => e.Version)
                .ToListAsync();

            return (IEnumerable<TDomainEvent>)[.. records.Select(r => Deserialize<TDomainEvent>(r.Event))];
        });

    public Task<IEnumerable<TDomainEvent>> GetEventsSinceLastSnapShotAsync(Guid eventProviderId) =>
        WithContextAsync(async context =>
        {
            var snapShotVersion = await GetSnapShotVersionAsync(context, eventProviderId);

            var records = await context.Events
                .Where(e => e.EventProviderId == eventProviderId && e.Version >= snapShotVersion)
                .OrderBy(e => e.Version)
                .ToListAsync();

            return (IEnumerable<TDomainEvent>)[.. records.Select(r => Deserialize<TDomainEvent>(r.Event))];
        });

    public Task<int> GetEventCountSinceLastSnapShotAsync(Guid eventProviderId) =>
        WithContextAsync(async context =>
        {
            var snapShotVersion = await GetSnapShotVersionAsync(context, eventProviderId);

            return await context.Events
                .Where(e => e.EventProviderId == eventProviderId && e.Version >= snapShotVersion)
                .CountAsync();
        });

    public Task SaveAsync(IEventProvider<TDomainEvent> eventProvider) =>
        WithContextAsync(async context =>
        {
            if (_transactionalContext == null)
                throw new Exception("Operation is not running within a transaction");

            var providerEntity = await GetOrCreateEventProviderEntityAsync(context, eventProvider);

            if (providerEntity.Version != eventProvider.Version && eventProvider.Version > 0)
                throw new ConcurrencyViolationException($"version not correct: {providerEntity.Version} != {eventProvider.Version} ({eventProvider.GetType()})");

            foreach (var domainEvent in eventProvider.GetChanges())
            {
                context.Events.Add(new EventRecordEntity
                {
                    Id = domainEvent.Id,
                    EventProviderId = eventProvider.Id,
                    Event = Serialize(domainEvent),
                    Version = domainEvent.Version,
                });
            }

            eventProvider.UpdateVersion(eventProvider.Version + eventProvider.GetChanges().Count());
            providerEntity.Version = eventProvider.Version;

            await context.SaveChangesAsync();
            return true;
        });

    public Task<ISnapShot?> GetSnapShotAsync(Guid entityId) =>
        WithContextAsync(async context =>
        {
            var entity = await context.SnapShots
                .Where(s => s.EventProviderId == entityId && s.Version != -1)
                .FirstOrDefaultAsync();

            return entity == null ? null : Deserialize<ISnapShot>(entity.SnapShot);
        });

    public Task SaveShapShotAsync(IEventProvider<TDomainEvent> entity) =>
        StoreSnapShotAsync(new SnapShot(entity.Id, entity.Version, ((IOriginator)entity).CreateMemento()));

    public async Task BeginTransactionAsync()
    {
        _transactionalContext = await _dbContextFactory.CreateDbContextAsync();
        _transaction = await _transactionalContext.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        if (_transaction == null || _transactionalContext == null)
            throw new Exception("Operation is not running within a transaction");

        await _transaction.CommitAsync();
        await _transaction.DisposeAsync();
        await _transactionalContext.DisposeAsync();
        _transaction = null;
        _transactionalContext = null;
    }

    public async Task RollbackAsync()
    {
        if (_transaction == null || _transactionalContext == null)
            throw new Exception("Operation is not running within a transaction");

        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _transactionalContext.DisposeAsync();
        _transaction = null;
        _transactionalContext = null;
    }

    private async Task<int> GetSnapShotVersionAsync(DomainEventStoreDbContext context, Guid eventProviderId)
    {
        var snapShotEntity = await context.SnapShots
            .Where(s => s.EventProviderId == eventProviderId && s.Version != -1)
            .FirstOrDefaultAsync();

        return snapShotEntity?.Version ?? -1;
    }

    private static async Task<EventProviderEntity> GetOrCreateEventProviderEntityAsync(DomainEventStoreDbContext context, IEventProvider<TDomainEvent> eventProvider)
    {
        var entity = await context.EventProviders.FindAsync(eventProvider.Id);
        if (entity != null)
            return entity;

        entity = new EventProviderEntity
        {
            EventProviderId = eventProvider.Id,
            Type = eventProvider.GetType().FullName ?? eventProvider.GetType().Name,
            Version = 0,
        };
        context.EventProviders.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    private async Task StoreSnapShotAsync(ISnapShot snapShot) =>
        await WithContextAsync(async context =>
        {
            var existing = await context.SnapShots.FindAsync(snapShot.EventProviderId);
            var bytes = Serialize(snapShot);

            if (existing == null)
            {
                context.SnapShots.Add(new SnapShotEntity
                {
                    EventProviderId = snapShot.EventProviderId,
                    SnapShot = bytes,
                    Version = snapShot.Version,
                });
            }
            else
            {
                existing.SnapShot = bytes;
                existing.Version = snapShot.Version;
            }

            await context.SaveChangesAsync();
            return true;
        });

    private async Task<T> WithContextAsync<T>(Func<DomainEventStoreDbContext, Task<T>> action)
    {
        if (_transactionalContext != null)
            return await action(_transactionalContext);

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await action(context);
    }

    private byte[] Serialize<T>(T theObject)
    {
        using var memoryStream = new MemoryStream();
        _formatter.Serialize(memoryStream, theObject);
        return memoryStream.ToArray();
    }

    private TType Deserialize<TType>(byte[] bytes)
    {
        using var memoryStream = new MemoryStream(bytes);
        return _formatter.Deserialize<TType>(memoryStream);
    }
}
