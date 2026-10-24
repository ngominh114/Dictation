using Dictation.Application.Import.Steps;
using Dictation.Application.Pipelines;
using Dictation.Domain.Lessons;

namespace Dictation.Application.Import;

/// <summary>The lesson import pipeline: transcribe, then finish.</summary>
public sealed class LessonPipeline : Pipeline<Lesson>
{
    private readonly ITranscriptionProvider _provider;

    public LessonPipeline(ITranscriptionProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    protected override void ConfigureSteps(List<IPipelineStep<Lesson>> steps)
    {
        steps.Add(new TranscribeStep(_provider));
        steps.Add(new FinishStep());
    }
}
