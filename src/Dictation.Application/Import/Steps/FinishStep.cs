using Dictation.Application.Pipelines;
using Dictation.Domain.Lessons;

namespace Dictation.Application.Import.Steps;

/// <summary>Marks a lesson <c>Ready</c> when it has segments, otherwise <c>Failed</c>.</summary>
public sealed class FinishStep : IPipelineStep<Lesson>
{
    public bool AppliesTo(Lesson item) => !item.IsFinished;

    public Task<Lesson> RunAsync(Lesson item, CancellationToken ct)
    {
        if (item.Segments.Count > 0)
        {
            item.Complete();
        }
        else
        {
            item.Fail();
        }

        return Task.FromResult(item);
    }
}
