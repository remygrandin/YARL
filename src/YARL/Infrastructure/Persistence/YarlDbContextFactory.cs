using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using YARL.Infrastructure.Config;

namespace YARL.Infrastructure.Persistence;

public class YarlDbContextFactory : IDesignTimeDbContextFactory<YarlDbContext>
{
    public YarlDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<YarlDbContext>();
        opts.UseSqlite($"Data Source={AppPaths.DatabasePath}");
        return new YarlDbContext(opts.Options);
    }
}
