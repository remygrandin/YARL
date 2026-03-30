using Microsoft.EntityFrameworkCore;
using YARL.Domain.Models;

namespace YARL.Infrastructure.Persistence;

public class YarlDbContext : DbContext
{
    public DbSet<RomSource> RomSources => Set<RomSource>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<RomFile> RomFiles => Set<RomFile>();
    public DbSet<GameVersion> GameVersions => Set<GameVersion>();

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

        builder.Entity<Game>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired().HasMaxLength(500);
            e.Property(x => x.RawTitle).HasMaxLength(500);
            e.Property(x => x.PlatformId).IsRequired().HasMaxLength(50);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Region).HasMaxLength(100);
            e.HasIndex(x => new { x.PlatformId, x.Status });
            e.HasIndex(x => x.LastPlayedAt);
            e.HasOne<RomSource>().WithMany().HasForeignKey(x => x.SourceId);
            e.Property(x => x.ScrapeStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Description).HasMaxLength(4000);
            e.Property(x => x.Genre).HasMaxLength(200);
            e.Property(x => x.Developer).HasMaxLength(200);
            e.Property(x => x.Publisher).HasMaxLength(200);
            e.Property(x => x.CoverArtPath).HasMaxLength(1024);
            e.Property(x => x.ScraperMatchId).HasMaxLength(100);
            e.Property(x => x.ScraperSource).HasMaxLength(20);
            e.Property(x => x.TotalPlayTime).HasConversion<long>().HasDefaultValue(TimeSpan.Zero);
        });

        builder.Entity<GameVersion>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Region).IsRequired().HasMaxLength(20);
            e.Property(x => x.LocalizedTitle).HasMaxLength(500);
            e.HasOne(x => x.Game).WithMany(g => g.Versions).HasForeignKey(x => x.GameId);
            e.HasOne<RomFile>().WithMany().HasForeignKey(x => x.RomFileId).IsRequired(false);
            e.HasIndex(x => new { x.GameId, x.Region });
        });

        builder.Entity<RomFile>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FilePath).IsRequired().HasMaxLength(2048);
            e.HasIndex(x => x.FilePath).IsUnique();
            e.HasOne(x => x.Game).WithMany(g => g.RomFiles).HasForeignKey(x => x.GameId);
            e.HasOne<RomSource>().WithMany().HasForeignKey(x => x.SourceId);
        });
    }
}
