namespace Fohjin.DDD.EventStore;

// Bound from an "EventStore" appsettings.json section (optional - AddEventStoreServices()
// registers a working default so nothing has to configure this to get snapshotting at all).
// See EventStoreUnitOfWork<TDomainEvent>.CommitAsync() for where SnapshotFrequency is used.
public sealed class EventStoreOptions
{
    public const string SectionName = "EventStore";

    // "Every 10 events" matches the cadence the pre-existing repository tests already assumed
    // in their names (Fohjin.DDD.Tests/Domain/Repositories/*RepositoryTest.cs) before this was
    // ever actually wired up - see docs/06-event-sourcing-infrastructure.md.
    public int SnapshotFrequency { get; set; } = 10;
}
