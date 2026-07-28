namespace Fohjin.DDD.EventStore.SQLite.Entities;

public class EventProviderEntity
{
    public Guid EventProviderId { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Version { get; set; }
}
