using Dictation.Domain.Lessons;
using Dictation.IntegrationTests.Support;
using Dictation.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace Dictation.IntegrationTests.Infrastructure;

public sealed class DictationDbContextTests : SqliteDatabase
{
    [Fact]
    public async Task Migrations_FreshDatabase_MatchCurrentModel()
    {
        await using var db = CreateContext();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync(Ct));
        Assert.False(db.Database.HasPendingModelChanges(), "The model changed without a migration: run dotnet ef migrations add.");
    }

    [Fact]
    public async Task TryAddAsync_LessonWithSegments_RoundTripsAllFields()
    {
        var lesson = TestLessons.CreateAudio();
        lesson.StartTranscription("assemblyai", "job-1", new DateTime(2026, 10, 22, 9, 0, 0, DateTimeKind.Unspecified));
        lesson.AddSegment("ep:0", "Hi Phil.", lesson.SourceAudioUrl!, startMs: 250, endMs: 900);
        lesson.AddSegment(null, "Bye.", lesson.SourceAudioUrl!, startMs: 900, endMs: null);
        lesson.Complete();
        await using (var db = CreateContext())
        {
            Assert.True(await db.TryAddAsync(lesson, Ct));
        }

        await using var reader = CreateContext();
        var stored = await reader.Lessons.SingleAsync(Ct);

        Assert.Equal(
            (lesson.Id, "bbc", lesson.ExternalId, LessonStatus.Ready, lesson.SourceAudioUrl),
            (stored.Id, stored.Source, stored.ExternalId, stored.Status, stored.SourceAudioUrl));
        Assert.Equal(("assemblyai", "job-1"), (stored.TranscriptionProvider, stored.TranscriptionJobId));
        Assert.Equal((TestLessons.CreatedAt, DateTimeKind.Utc), (stored.CreatedAt, stored.CreatedAt.Kind));
        Assert.Equal(DateTimeKind.Utc, stored.TranscriptionStartedAt?.Kind);
        Assert.Equal(
            [(0, "ep:0", "Hi Phil.", 250, (int?)900), (1, null, "Bye.", 900, null)],
            stored.Segments.Select(s => (s.Index, s.ExternalKey, s.Text, s.StartMs, s.EndMs)));
    }

    [Fact]
    public async Task TryAddAsync_DuplicateLessonOrSegmentKey_ReturnsFalseAndContextStaysUsable()
    {
        await using var db = CreateContext();
        Assert.True(await db.TryAddAsync(TatoebaLesson("1,3", "1:11"), Ct));

        Assert.False(await db.TryAddAsync(TatoebaLesson("1,3", "3:33"), Ct));
        Assert.False(await db.TryAddAsync(TatoebaLesson("1,5", "1:11"), Ct));
        Assert.True(await db.TryAddAsync(TatoebaLesson("5,7", "5:55"), Ct));

        await using var reader = CreateContext();
        Assert.Equal(["1,3", "5,7"], await reader.Lessons.Select(l => l.ExternalId).Order().ToListAsync(Ct));
    }

    [Fact]
    public async Task TryAddAsync_SameLessonFromTwoContextsConcurrently_OnlyOneStored()
    {
        await using var first = CreateContext();
        await using var second = CreateContext();

        var added = await Task.WhenAll(
            first.TryAddAsync(TatoebaLesson("1,3", "1:11"), Ct),
            second.TryAddAsync(TatoebaLesson("1,3", "1:11"), Ct));

        Assert.Equal([false, true], added.Order());
        await using var reader = CreateContext();
        Assert.Equal(1, await reader.Lessons.CountAsync(Ct));
    }

    [Fact]
    public async Task GetKnownKeysAsync_SeveralSources_ReturnsIdsAndSegmentKeysOfThatSourceOnly()
    {
        await using var db = CreateContext();
        await db.TryAddAsync(TatoebaLesson("1,3", "1:11"), Ct);
        await db.TryAddAsync(TestLessons.Create("bbc", externalId: "ep-1"), Ct);

        var keys = await db.GetKnownKeysAsync("tatoeba", Ct);

        Assert.Equal(["1,3", "1:11"], keys.Order());
    }

    private static Lesson TatoebaLesson(string externalId, string segmentKey)
    {
        var lesson = TestLessons.Create("tatoeba", externalId);
        lesson.AddSentence(segmentKey);
        return lesson;
    }
}
