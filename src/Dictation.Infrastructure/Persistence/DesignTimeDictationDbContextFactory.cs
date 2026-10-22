using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dictation.Infrastructure.Persistence;

/// <summary>Used only by <c>dotnet ef</c> to create migrations.</summary>
internal sealed class DesignTimeDictationDbContextFactory : IDesignTimeDbContextFactory<DictationDbContext>
{
    public DictationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DictationDbContext>()
            .UseSqlite(PersistenceServiceCollectionExtensions.DefaultConnectionString)
            .Options;
        return new DictationDbContext(options);
    }
}
