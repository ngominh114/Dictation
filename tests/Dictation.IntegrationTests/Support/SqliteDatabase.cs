using Dictation.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dictation.IntegrationTests.Support;

/// <summary>
/// A fresh SQLite file with the real migrations applied, deleted on dispose. Each test class instance gets its own,
/// so tests never share data.
/// </summary>
public abstract class SqliteDatabase : IAsyncLifetime
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"dictation-tests-{Guid.NewGuid():N}.db");

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected string ConnectionString => $"Data Source={_path}";

    /// <summary>A new context on the database; the caller disposes it.</summary>
    protected DictationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<DictationDbContext>().UseSqlite(ConnectionString).Options);

    public virtual async ValueTask InitializeAsync()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync(Ct);
    }

    public virtual ValueTask DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        // The app switches the file to WAL mode, which adds the -wal and -shm files.
        foreach (var file in new[] { _path, $"{_path}-wal", $"{_path}-shm" })
        {
            File.Delete(file);
        }

        return ValueTask.CompletedTask;
    }
}
