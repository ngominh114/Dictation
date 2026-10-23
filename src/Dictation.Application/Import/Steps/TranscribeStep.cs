using Dictation.Application.Pipelines;
using Dictation.Domain.Lessons;

namespace Dictation.Application.Import.Steps;

/// <summary>
/// Transcribes lessons that have source audio but no segments: submits the audio, polls until done, then adds
/// one segment per sentence.
/// </summary>
public sealed class TranscribeStep : IPipelineStep<Lesson>
{
    private readonly ITranscriptionProvider _provider;
    private readonly TimeSpan _pollInterval;

    public TranscribeStep(ITranscriptionProvider provider, TranscriptionOptions options)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        ArgumentNullException.ThrowIfNull(options);
        _pollInterval = TimeSpan.FromSeconds(Math.Max(0, options.PollIntervalSeconds));
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

        // No deadline: poll until the provider reports completion (or throws on a provider error).
        while (!await _provider.IsCompletedAsync(jobId, ct).ConfigureAwait(false))
        {
            await Task.Delay(_pollInterval, ct).ConfigureAwait(false);
        }

        var sentences = await _provider.GetSentencesAsync(jobId, ct).ConfigureAwait(false);
        foreach (var sentence in sentences)
        {
            item.AddSegment(null, sentence.Text, audioUrl, sentence.StartMs, sentence.EndMs);
        }

        return item;
    }
}
