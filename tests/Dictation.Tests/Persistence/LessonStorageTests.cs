using Dictation.Core.Lessons;
using Dictation.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace Dictation.Tests.Persistence;

public sealed class LessonStorageTests : IAsyncLifetime
{
    private readonly TempSqliteDatabase database = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await using var db = database.CreateContext();
        await db.Database.MigrateAsync(Ct);
    }

    public ValueTask DisposeAsync()
    {
        database.Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task SaveAsync(Lesson lesson)
    {
        await using var db = database.CreateContext();
        db.Lessons.Add(lesson);
        await db.SaveChangesAsync(Ct);
    }

    private async Task<int> CountSegmentsAsync()
    {
        await using var db = database.CreateContext();
        return await db.Lessons.SelectMany(l => l.Segments).CountAsync(Ct);
    }

    [Fact]
    public async Task Lesson_and_segments_round_trip_in_index_order()
    {
        var lesson = TestLessons.Create();
        lesson.AddSegment("11:110", "First sentence.", "https://example.test/110.mp3", 0, null,
            new Attribution("author-a", "CC BY 4.0", "https://example.test/author-a"));
        lesson.AddSegment(null, "Second sentence.", "https://example.test/episode.mp3", 1500, 4200, null);
        await SaveAsync(lesson);

        await using var db = database.CreateContext();
        var reloaded = await db.Lessons.SingleAsync(l => l.Id == lesson.Id, Ct);

        Assert.Equal(lesson.ExternalId, reloaded.ExternalId);
        Assert.Equal(LessonStatus.Ready, reloaded.Status);
        Assert.Equal(DateTimeKind.Utc, reloaded.CreatedAt.Kind);
        Assert.Collection(
            reloaded.Segments,
            first =>
            {
                Assert.Equal(0, first.Index);
                Assert.Equal("First sentence.", first.Text);
                Assert.Null(first.EndMs);
                Assert.Equal("CC BY 4.0", first.Attribution?.License);
            },
            second =>
            {
                Assert.Equal(1, second.Index);
                Assert.Equal((1500, 4200), (second.StartMs, second.EndMs));
                Assert.Null(second.Attribution);
            });
    }

    [Fact]
    public async Task The_same_lesson_cannot_be_imported_twice_from_a_source()
    {
        await SaveAsync(TestLessons.Create("tatoeba", "1,2,3"));
        await SaveAsync(TestLessons.Create("bbc", "1,2,3"));

        await Assert.ThrowsAsync<DbUpdateException>(() => SaveAsync(TestLessons.Create("tatoeba", "1,2,3")));
    }

    [Fact]
    public async Task The_same_sentence_cannot_be_imported_twice_from_a_source()
    {
        var first = TestLessons.Create("tatoeba");
        first.AddSentence("5:50");
        await SaveAsync(first);

        var second = TestLessons.Create("tatoeba");
        second.AddSentence("5:50");

        await Assert.ThrowsAsync<DbUpdateException>(() => SaveAsync(second));
        Assert.Equal(1, await CountSegmentsAsync());
    }

    [Fact]
    public async Task Sentences_without_a_key_never_conflict()
    {
        foreach (var episode in new[] { "ep-1", "ep-2" })
        {
            var lesson = TestLessons.Create("bbc", episode);
            lesson.AddSentence(null);
            lesson.AddSentence(null);
            await SaveAsync(lesson);
        }

        Assert.Equal(4, await CountSegmentsAsync());
    }

    [Fact]
    public void Migrations_match_the_model()
    {
        using var db = database.CreateContext();

        Assert.False(db.Database.HasPendingModelChanges(), "Run 'dotnet ef migrations add' for the model change.");
    }
}
