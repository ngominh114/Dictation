using Dictation.Domain.Lessons;
using Dictation.Tests.Support;

namespace Dictation.Tests.Domain;

public sealed class LessonTests
{
    [Fact]
    public void AddSegment_SeveralSegments_AssignsSequentialZeroBasedIndexes()
    {
        var lesson = TestLessons.Create();

        lesson.AddSentence("1:10");
        lesson.AddSentence("2:20");
        lesson.AddSentence(null);

        Assert.Equal([0, 1, 2], lesson.Segments.Select(s => s.Index));
    }

    [Fact]
    public void CreateDiscovered_BlankAudioUrl_StartsDiscoveredWithNoAudio()
    {
        var lesson = TestLessons.Create(sourceAudioUrl: "  ");

        Assert.Equal(LessonStatus.Discovered, lesson.Status);
        Assert.Null(lesson.SourceAudioUrl);
    }

    [Fact]
    public void Complete_NoSegments_ThrowsAndKeepsStatus()
    {
        var lesson = TestLessons.Create();

        Assert.Throws<InvalidOperationException>(lesson.Complete);
        Assert.Equal(LessonStatus.Discovered, lesson.Status);
    }

    [Fact]
    public void StartTranscription_UnspecifiedKindTime_RecordsJobInUtcAndMarksTranscribing()
    {
        var lesson = TestLessons.CreateAudio();
        var startedAt = new DateTime(2026, 10, 22, 9, 0, 0, DateTimeKind.Unspecified);

        lesson.StartTranscription("assemblyai", "job-1", startedAt);

        Assert.Equal(LessonStatus.Transcribing, lesson.Status);
        Assert.Equal(("assemblyai", "job-1"), (lesson.TranscriptionProvider, lesson.TranscriptionJobId));
        Assert.Equal(DateTimeKind.Utc, lesson.TranscriptionStartedAt?.Kind);
    }
}
