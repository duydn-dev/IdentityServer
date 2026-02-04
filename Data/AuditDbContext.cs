using IdentityServerHost.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerHost.Data;

public class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options) { }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(200);
            e.Property(x => x.UserId).HasMaxLength(450);
            e.Property(x => x.EntityType).HasMaxLength(100);
            e.Property(x => x.EntityId).HasMaxLength(200);
            e.Property(x => x.IpAddress).HasMaxLength(50);
            e.Property(x => x.UserAgent).HasMaxLength(500);
            e.HasIndex(x => x.Timestamp);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.Action);
        });
    }
}
