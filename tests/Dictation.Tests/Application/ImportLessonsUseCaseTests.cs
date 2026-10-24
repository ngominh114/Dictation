using Dictation.Domain.Lessons;
using Dictation.Tests.Support;

namespace Dictation.Tests.Application;

public sealed class ImportLessonsUseCaseTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ExecuteAsync_LessonsWithAndWithoutSegments_StoresReadyAndFailed()
    {
        var repository = new InMemoryLessonImportRepository();
        var withSegments = TestLessons.Create();
        withSegments.AddSentence("1:10");
        var empty = TestLessons.Create();

        await TestImport.UseCase(repository, TestImport.Pipeline())
            .ExecuteAsync(new StubSource("tatoeba", withSegments, empty), Ct);

        Assert.Equal(LessonStatus.Ready, withSegments.Status);
        Assert.Equal(LessonStatus.Failed, empty.Status);
        Assert.Equal([withSegments, empty], repository.Lessons);
    }

    [Fact]
    public async Task ExecuteAsync_StepThrows_StoresLessonFailedAndSkipsItOnRerun()
    {
        var repository = new InMemoryLessonImportRepository();
        var audio = TestLessons.CreateAudio();
        var failing = new StubStep(_ => true, _ => throw new InvalidOperationException("boom"));
        var useCase = TestImport.UseCase(repository, new StubPipeline(failing));

        await useCase.ExecuteAsync(new StubSource("bbc", audio), Ct);
        await useCase.ExecuteAsync(new StubSource("bbc", audio), Ct);

        Assert.Equal(LessonStatus.Failed, audio.Status);
        Assert.Single(repository.Lessons);
        Assert.Equal(1, failing.Runs);
    }

    [Fact]
    public async Task ExecuteAsync_SourceFetchThrows_StoresNothingAndDoesNotThrow()
    {
        var repository = new InMemoryLessonImportRepository();
        var source = new StubSource("bbc") { Error = new HttpRequestException("BBC is down") };

        await TestImport.UseCase(repository, TestImport.Pipeline()).ExecuteAsync(source, Ct);

        Assert.Empty(repository.Lessons);
    }
}
