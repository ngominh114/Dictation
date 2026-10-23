using Dictation.Application.Import.Steps;
using Dictation.Application.Pipelines;
using Dictation.Domain.Lessons;

namespace Dictation.Application.Import;

/// <summary>The lesson import pipeline: transcribe, then finish.</summary>
public sealed class LessonPipeline : Pipeline<Lesson>
{
    protected override void ConfigureSteps(List<IPipelineStep<Lesson>> steps)
    {
        steps.Add(new TranscribeStep());
        steps.Add(new FinishStep());
    }
}
