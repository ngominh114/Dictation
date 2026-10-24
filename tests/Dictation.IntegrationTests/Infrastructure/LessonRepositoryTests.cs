using Dictation.Domain.Lessons;
using Dictation.Infrastructure.Persistence;
using Dictation.IntegrationTests.Support;
using Dictation.Tests.Support;

namespace Dictation.IntegrationTests.Infrastructure;

public sealed class LessonRepositoryTests : SqliteDatabase
{
    [Fact]
    public async Task GetPageAsync_ReadyAndUnfinishedLessons_ReturnsReadyOnlyNewestFirstPaged()
    {
        var oldest = await AddAsync(ReadyLesson(daysOld: 3));
        var middle = await AddAsync(ReadyLesson(daysOld: 2));
        var newest = await AddAsync(ReadyLesson(daysOld: 1));
        await AddAsync(TestLessons.Create());

        await using var db = CreateContext();
        var repository = new LessonRepository(db);
        var first = await repository.GetPageAsync(page: 1, pageSize: 2, Ct);
        var second = await repository.GetPageAsync(page: 2, pageSize: 2, Ct);

        Assert.Equal([newest.Id, middle.Id], first.Items.Select(i => i.Id));
        Assert.Equal([oldest.Id], second.Items.Select(i => i.Id));
        Assert.Equal((3, 1), (first.TotalCount, first.Items[0].SegmentCount));
    }

    [Fact]
    public async Task GetAsync_ReadyLesson_ReturnsItWithSegmentsInOrder()
    {
        var lesson = ReadyLesson(daysOld: 1);
        lesson.AddSentence("2:22");
        await AddAsync(lesson);

        await using var db = CreateContext();
        var stored = await new LessonRepository(db).GetAsync(lesson.Id, Ct);

        Assert.NotNull(stored);
        Assert.Equal([0, 1], stored.Segments.Select(s => s.Index));
    }

    [Fact]
    public async Task GetAsync_LessonNotReady_ReturnsNull()
    {
        var failed = TestLessons.Create();
        failed.Fail();
        await AddAsync(failed);

        await using var db = CreateContext();

        Assert.Null(await new LessonRepository(db).GetAsync(failed.Id, Ct));
    }

    private static Lesson ReadyLesson(int daysOld)
    {
        var lesson = Lesson.CreateDiscovered(
            "tatoeba",
            Guid.NewGuid().ToString("N"),
            "Lesson title",
            "en",
            "https://example.test/lesson",
            sourceAudioUrl: null,
            TestLessons.CreatedAt.AddDays(-daysOld));
        lesson.AddSentence(Guid.NewGuid().ToString("N"));
        lesson.Complete();
        return lesson;
    }

    private async Task<Lesson> AddAsync(Lesson lesson)
    {
        await using var db = CreateContext();
        Assert.True(await db.TryAddAsync(lesson, Ct));
        return lesson;
    }
}
