namespace Fohjin.DDD.EventStore.SQLite.Entities;

public class EventRecordEntity
{
    public Guid Id { get; set; }
    public Guid EventProviderId { get; set; }
    public byte[] Event { get; set; } = Array.Empty<byte>();
    public int Version { get; set; }
}
