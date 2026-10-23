using Dictation.Application.Import;
using Dictation.Domain.Lessons;
using Dictation.Tests.Support;

namespace Dictation.Tests.Application;

public sealed class TranscribeStepTests
{
    [Fact]
    public async Task Audio_lesson_is_transcribed_into_segments_and_ends_ready()
    {
        var provider = new FakeTranscriptionProvider(
            pendingPolls: 1,
            new TranscribedSentence("Hi Phil.", 250, 900),
            new TranscribedSentence("Hi Georgie.", 900, 1800));
        var lesson = TestLessons.CreateAudio();

        await TestImport.Pipeline(provider).RunAsync(lesson, TestContext.Current.CancellationToken);

        Assert.Equal([lesson.SourceAudioUrl!], provider.Submitted);
        Assert.Equal(2, provider.Polls);
        Assert.Equal(LessonStatus.Ready, lesson.Status);
        Assert.Equal(("fake", "job-1"), (lesson.TranscriptionProvider, lesson.TranscriptionJobId));
        Assert.Equal(
            [("Hi Phil.", 250, (int?)900, lesson.SourceAudioUrl), ("Hi Georgie.", 900, 1800, lesson.SourceAudioUrl)],
            lesson.Segments.Select(s => (s.Text, s.StartMs, s.EndMs, (string?)s.AudioUrl)));
    }
}
