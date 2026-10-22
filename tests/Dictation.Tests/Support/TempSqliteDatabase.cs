using Dictation.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dictation.Tests.Support;

/// <summary>A unique temporary SQLite file per test, deleted on dispose.</summary>
public sealed class TempSqliteDatabase : IDisposable
{
    private readonly string filePath = Path.Combine(Path.GetTempPath(), $"dictation-test-{Guid.NewGuid():N}.db");

    public string ConnectionString => $"Data Source={filePath}";

    public DictationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<DictationDbContext>().UseSqlite(ConnectionString).Options);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var path in new[] { filePath, filePath + "-wal", filePath + "-shm" })
        {
            File.Delete(path);
        }
    }
}
