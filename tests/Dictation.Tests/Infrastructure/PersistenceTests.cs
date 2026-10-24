using Dictation.Infrastructure.Persistence;
using Dictation.Tests.Support;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dictation.Tests.Infrastructure;

/// <summary>Runs against an in-memory SQLite database, so the real unique indexes apply.</summary>
public sealed class PersistenceTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private DictationDbContext _db = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await _connection.OpenAsync();
        _db = new DictationDbContext(new DbContextOptionsBuilder<DictationDbContext>().UseSqlite(_connection).Options);
        await _db.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task TryAddAsync_SameSourceAndExternalId_ReturnsFalse()
    {
        Assert.True(await _db.TryAddAsync(TestLessons.Create(externalId: "1,3"), Ct));

        Assert.False(await _db.TryAddAsync(TestLessons.Create(externalId: "1,3"), Ct));
        Assert.Equal(1, await _db.Lessons.CountAsync(Ct));
    }

    [Fact]
    public async Task TryAddAsync_SegmentKeyAlreadyStoredForSource_ReturnsFalse()
    {
        var first = TestLessons.Create(externalId: "1,3");
        first.AddSentence("1:11");
        var overlapping = TestLessons.Create(externalId: "1,5");
        overlapping.AddSentence("1:11");
        await _db.TryAddAsync(first, Ct);

        Assert.False(await _db.TryAddAsync(overlapping, Ct));
    }

    [Fact]
    public async Task GetKnownKeysAsync_LessonsFromSeveralSources_ReturnsIdsAndSegmentKeysOfThatSourceOnly()
    {
        var tatoeba = TestLessons.Create("tatoeba", externalId: "1,3");
        tatoeba.AddSentence("1:11");
        await _db.TryAddAsync(tatoeba, Ct);
        await _db.TryAddAsync(TestLessons.Create("bbc", externalId: "ep-1"), Ct);

        var keys = await _db.GetKnownKeysAsync("tatoeba", Ct);

        Assert.Equal(["1,3", "1:11"], keys.Order());
    }

    [Fact]
    public async Task GetPageAsync_ReadyAndUnfinishedLessons_ReturnsOnlyReady()
    {
        var ready = TestLessons.Create();
        ready.AddSentence("1:10");
        ready.Complete();
        await _db.TryAddAsync(ready, Ct);
        await _db.TryAddAsync(TestLessons.Create(), Ct);

        var page = await new LessonRepository(_db).GetPageAsync(page: 1, pageSize: 20, Ct);

        var summary = Assert.Single(page.Items);
        Assert.Equal((ready.Id, 1), (summary.Id, summary.SegmentCount));
        Assert.Equal(1, page.TotalCount);
    }
}
