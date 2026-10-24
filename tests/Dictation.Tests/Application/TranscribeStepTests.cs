using Dictation.Application.Import;
using Dictation.Application.Import.Abstractions;
using Dictation.Application.Import.Steps;
using Dictation.Domain.Lessons;
using Dictation.Tests.Support;

namespace Dictation.Tests.Application;

public sealed class TranscribeStepTests
{
    [Fact]
    public async Task RunAsync_AudioLesson_AddsSegmentsAndEndsReady()
    {
        var provider = new FakeTranscriptionProvider(
            new TranscribedSentence("Hi Phil.", 250, 900),
            new TranscribedSentence("Hi Georgie.", 900, 1800));
        var lesson = TestLessons.CreateAudio();

        await TestImport.Pipeline(provider).RunAsync(lesson, TestContext.Current.CancellationToken);

        Assert.Equal([lesson.SourceAudioUrl!], provider.Submitted);
        Assert.Equal(LessonStatus.Ready, lesson.Status);
        Assert.Equal(("fake", "job-1"), (lesson.TranscriptionProvider, lesson.TranscriptionJobId));
        Assert.Equal(
            [("Hi Phil.", 250, (int?)900, lesson.SourceAudioUrl), ("Hi Georgie.", 900, 1800, lesson.SourceAudioUrl)],
            lesson.Segments.Select(s => (s.Text, s.StartMs, s.EndMs, (string?)s.AudioUrl)));
    }

    [Fact]
    public void AppliesTo_AudioLessonWithoutSegments_ReturnsTrueOtherwiseFalse()
    {
        var step = new TranscribeStep(new FakeTranscriptionProvider());
        var withSegments = TestLessons.CreateAudio();
        withSegments.AddSentence("1:10");

        Assert.True(step.AppliesTo(TestLessons.CreateAudio()));
        Assert.False(step.AppliesTo(TestLessons.Create()));
        Assert.False(step.AppliesTo(withSegments));
    }

    [Fact]
    public async Task RunAsync_ProviderThrows_PropagatesAndAddsNoSegments()
    {
        var provider = new FakeTranscriptionProvider { Error = new ImportException("transcription failed") };
        var lesson = TestLessons.CreateAudio();

        await Assert.ThrowsAsync<ImportException>(
            () => new TranscribeStep(provider).RunAsync(lesson, TestContext.Current.CancellationToken));

        Assert.Empty(lesson.Segments);
    }
}
