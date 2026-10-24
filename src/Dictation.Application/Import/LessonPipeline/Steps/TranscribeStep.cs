using Dictation.Application.Import.Abstractions;
using Dictation.Application.Pipelines;
using Dictation.Domain.Lessons;

namespace Dictation.Application.Import.Steps;

/// <summary>
/// Transcribes lessons that have source audio but no segments: submits the audio, waits for the provider's
/// sentences, then adds one segment per sentence.
/// </summary>
public sealed class TranscribeStep : IPipelineStep<Lesson>
{
    private readonly ITranscriptionProvider _provider;

    public TranscribeStep(ITranscriptionProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public bool AppliesTo(Lesson item) =>
        !item.IsFinished && item.SourceAudioUrl is not null && item.Segments.Count == 0;

    public async Task<Lesson> RunAsync(Lesson item, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(item);

        var audioUrl = item.SourceAudioUrl
            ?? throw new InvalidOperationException("The lesson has no source audio to transcribe.");

        var jobId = await _provider.SubmitAsync(audioUrl, item.Language, ct).ConfigureAwait(false);
        item.StartTranscription(_provider.Key, jobId, DateTime.UtcNow);

        var sentences = await _provider.GetSentencesAsync(jobId, ct).ConfigureAwait(false);
        foreach (var sentence in sentences)
        {
            item.AddSegment(null, sentence.Text, audioUrl, sentence.StartMs, sentence.EndMs);
        }

        return item;
    }
}
