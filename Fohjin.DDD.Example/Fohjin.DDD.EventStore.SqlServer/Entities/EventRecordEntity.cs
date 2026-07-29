namespace Fohjin.DDD.EventStore.SqlServer.Entities;

public class EventRecordEntity
{
    public Guid Id { get; set; }
    public Guid EventProviderId { get; set; }
    public byte[] Event { get; set; } = [];
    public int Version { get; set; }
}
