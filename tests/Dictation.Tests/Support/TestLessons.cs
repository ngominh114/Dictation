using Dictation.Core.Lessons;

namespace Dictation.Tests.Support;

public static class TestLessons
{
    public static Lesson Create(string source = "tatoeba", string? externalId = null) =>
        new(
            source,
            externalId ?? Guid.NewGuid().ToString("N"),
            title: "Lesson title",
            language: "en",
            sourceUrl: "https://example.test/lesson",
            LessonStatus.Ready,
            createdAt: new DateTime(2026, 10, 22, 8, 30, 15, DateTimeKind.Utc));

    public static Segment AddSentence(this Lesson lesson, string? externalKey) =>
        lesson.AddSegment(externalKey, "A sentence.", "https://example.test/audio.mp3", startMs: 0, endMs: null, attribution: null);
}
