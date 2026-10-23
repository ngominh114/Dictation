namespace Dictation.Application.Import;

/// <summary>One transcribed sentence, times in integer milliseconds (<see cref="EndMs"/> exclusive).</summary>
public sealed record TranscribedSentence(string Text, int StartMs, int EndMs);

public interface ITranscriptionProvider
{
    /// <summary>Constant lowercase key, e.g. <c>assemblyai</c>.</summary>
    string Key { get; }

    /// <summary>Submits <paramref name="audioUrl"/> for transcription and returns the provider's job id.</summary>
    Task<string> SubmitAsync(string audioUrl, string language, CancellationToken ct);

    /// <summary>Whether the job is done. Throws <see cref="ImportException"/> when the provider reports an error.</summary>
    Task<bool> IsCompletedAsync(string jobId, CancellationToken ct);

    /// <summary>The job's sentences in order: non-empty text, no overlaps. Throws <see cref="ImportException"/> when none remain.</summary>
    Task<IReadOnlyList<TranscribedSentence>> GetSentencesAsync(string jobId, CancellationToken ct);
}
