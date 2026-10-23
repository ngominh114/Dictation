using Dictation.Application.Import.Steps;
using Dictation.Domain.Lessons;
using Dictation.Tests.Support;

namespace Dictation.Tests.Application;

/// <summary>Matrix rows "Fits every source" and "Step failure" (AD-3, AD-5, AD-9).</summary>
public sealed class ImportLessonsUseCaseTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Fits_every_source_lesson_with_segments_becomes_ready_and_lesson_with_nothing_fails()
    {
        var repository = new InMemoryLessonRepository();
        var withSegments = TestLessons.Create();
        withSegments.AddSentence("1:10");
        var empty = TestLessons.Create();

        await TestImport.UseCase(repository, new TranscribeStep(), new FinishStep())
            .ExecuteAsync(new StubSource("tatoeba", withSegments, empty), Ct);

        Assert.Equal(LessonStatus.Ready, withSegments.Status);
        Assert.Equal(LessonStatus.Failed, empty.Status);
        Assert.Equal([withSegments, empty], repository.Lessons);
    }

    [Fact]
    public async Task Step_failure_stores_the_lesson_as_failed_and_later_runs_skip_it()
    {
        var repository = new InMemoryLessonRepository();
        var audio = TestLessons.CreateAudio();
        var transcribe = new StubStep(_ => true, _ => throw new InvalidOperationException("boom"));
        var useCase = TestImport.UseCase(repository, transcribe, new StubStep(_ => false));

        await useCase.ExecuteAsync(new StubSource("bbc", audio), Ct);
        await useCase.ExecuteAsync(new StubSource("bbc", audio), Ct);

        Assert.Equal(LessonStatus.Failed, audio.Status);
        Assert.Single(repository.Lessons);
        Assert.Equal(1, transcribe.Runs);
    }
}
