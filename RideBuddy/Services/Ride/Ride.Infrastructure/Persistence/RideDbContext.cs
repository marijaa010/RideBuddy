using Microsoft.EntityFrameworkCore;
using Ride.Domain.Entities;

namespace Ride.Infrastructure.Persistence;

/// <summary>
/// Entity Framework DbContext for the Ride bounded context.
/// </summary>
public class RideDbContext : DbContext
{
    public RideDbContext(DbContextOptions<RideDbContext> options) : base(options) { }

    public DbSet<RideEntity> Rides => Set<RideEntity>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RideDbContext).Assembly);
    }
}
