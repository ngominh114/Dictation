using Dictation.Domain.Lessons;

namespace Dictation.Tests.Support;

public static class TestLessons
{
    public static readonly DateTime CreatedAt = new(2026, 10, 22, 8, 30, 15, DateTimeKind.Utc);

    public static Lesson Create(string source = "tatoeba", string? externalId = null, string? sourceAudioUrl = null) =>
        Lesson.CreateDiscovered(
            source,
            externalId ?? Guid.NewGuid().ToString("N"),
            title: "Lesson title",
            language: "en",
            sourceUrl: "https://example.test/lesson",
            sourceAudioUrl,
            CreatedAt);

    /// <summary>A lesson like BBC's: one audio file to transcribe, no segments yet.</summary>
    public static Lesson CreateAudio(string source = "bbc") =>
        Create(source, sourceAudioUrl: "https://example.test/episode_download.mp3");

    public static Segment AddSentence(this Lesson lesson, string? externalKey) =>
        lesson.AddSegment(externalKey, "A sentence.", "https://example.test/audio.mp3", startMs: 0, endMs: null);
}
