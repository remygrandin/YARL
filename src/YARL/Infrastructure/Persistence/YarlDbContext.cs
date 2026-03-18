using Microsoft.EntityFrameworkCore;
using YARL.Domain.Models;

namespace YARL.Infrastructure.Persistence;

public class YarlDbContext : DbContext
{
    public DbSet<RomSource> RomSources => Set<RomSource>();

    public YarlDbContext(DbContextOptions<YarlDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<RomSource>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Path).IsRequired().HasMaxLength(1024);
            e.Property(x => x.SourceType).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Label).HasMaxLength(200);
            e.Property(x => x.IsEnabled).HasDefaultValue(true);
        });
    }
}
