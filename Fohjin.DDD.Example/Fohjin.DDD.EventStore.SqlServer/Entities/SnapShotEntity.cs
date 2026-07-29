namespace Fohjin.DDD.EventStore.SqlServer.Entities;

public class SnapShotEntity
{
    public Guid EventProviderId { get; set; }
    public byte[] SnapShot { get; set; } = [];
    public int Version { get; set; }
}
