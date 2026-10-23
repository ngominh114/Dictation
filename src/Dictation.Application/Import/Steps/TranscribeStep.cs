using Dictation.Application.Pipelines;
using Dictation.Domain.Lessons;

namespace Dictation.Application.Import.Steps;

/// <summary>
/// Transcribes lessons that have source audio but no segments. The guard is real; the work is not
/// implemented yet, so <see cref="RunAsync"/> returns the lesson unchanged.
/// </summary>
public sealed class TranscribeStep : IPipelineStep<Lesson>
{
    public bool AppliesTo(Lesson item) =>
        !item.IsFinished && item.SourceAudioUrl is not null && item.Segments.Count == 0;

    public Task<Lesson> RunAsync(Lesson item, CancellationToken ct) => Task.FromResult(item);
}
