using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace User.Infrastructure.Persistence;

/// <summary>
/// Entity Framework DbContext for the User bounded context.
/// Extends IdentityDbContext to include ASP.NET Core Identity tables.
/// </summary>
public class UserDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserDbContext).Assembly);
    }
}
