using Dictation.Application.Import.Steps;
using Dictation.Domain.Lessons;
using Dictation.Tests.Support;

namespace Dictation.Tests.Application;

public sealed class FinishStepTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task RunAsync_LessonHasSegments_MarksReady()
    {
        var lesson = TestLessons.Create();
        lesson.AddSentence("1:10");

        await new FinishStep().RunAsync(lesson, Ct);

        Assert.Equal(LessonStatus.Ready, lesson.Status);
    }

    [Fact]
    public async Task RunAsync_LessonHasNoSegments_MarksFailed()
    {
        var lesson = TestLessons.Create();

        await new FinishStep().RunAsync(lesson, Ct);

        Assert.Equal(LessonStatus.Failed, lesson.Status);
    }
}
