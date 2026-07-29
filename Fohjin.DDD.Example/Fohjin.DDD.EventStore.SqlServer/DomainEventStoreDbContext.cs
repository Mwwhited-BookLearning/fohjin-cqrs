using Fohjin.DDD.EventStore.SqlServer.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fohjin.DDD.EventStore.SqlServer;

public class DomainEventStoreDbContext(DbContextOptions<DomainEventStoreDbContext> options) : DbContext(options)
{
    public DbSet<EventProviderEntity> EventProviders => Set<EventProviderEntity>();
    public DbSet<EventRecordEntity> Events => Set<EventRecordEntity>();
    public DbSet<SnapShotEntity> SnapShots => Set<SnapShotEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventProviderEntity>(entity =>
        {
            entity.ToTable("EventProviders");
            entity.HasKey(x => x.EventProviderId);
            entity.Property(x => x.Type).IsRequired();
            entity.Property(x => x.Version).IsRequired();
        });

        modelBuilder.Entity<EventRecordEntity>(entity =>
        {
            entity.ToTable("Events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Event).IsRequired();
            entity.Property(x => x.Version).IsRequired();
            entity.HasIndex(x => new { x.EventProviderId, x.Version });
        });

        modelBuilder.Entity<SnapShotEntity>(entity =>
        {
            entity.ToTable("SnapShots");
            entity.HasKey(x => x.EventProviderId);
            entity.Property(x => x.SnapShot).IsRequired();
            entity.Property(x => x.Version).IsRequired();
        });
    }
}
