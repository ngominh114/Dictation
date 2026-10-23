using Dictation.Application.Import.Steps;
using Dictation.Application.Pipelines;
using Dictation.Domain.Lessons;
using Microsoft.Extensions.Options;

namespace Dictation.Application.Import;

/// <summary>The lesson import pipeline: transcribe, then finish.</summary>
public sealed class LessonPipeline : Pipeline<Lesson>
{
    private readonly ITranscriptionProvider _provider;
    private readonly TranscriptionOptions _options;

    public LessonPipeline(ITranscriptionProvider provider, IOptions<TranscriptionOptions> options)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    protected override void ConfigureSteps(List<IPipelineStep<Lesson>> steps)
    {
        steps.Add(new TranscribeStep(_provider, _options));
        steps.Add(new FinishStep());
    }
}
