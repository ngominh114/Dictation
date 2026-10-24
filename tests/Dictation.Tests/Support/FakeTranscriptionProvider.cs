using Dictation.Application.Import.Abstractions;

namespace Dictation.Tests.Support;

/// <summary>Accepts every submit and returns <paramref name="sentences"/> as the transcript.</summary>
public sealed class FakeTranscriptionProvider(params TranscribedSentence[] sentences)
    : ITranscriptionProvider
{
    public string Key => "fake";

    public List<string> Submitted { get; } = [];

    public Task<string> SubmitAsync(string audioUrl, string language, CancellationToken ct)
    {
        Submitted.Add(audioUrl);
        return Task.FromResult("job-1");
    }

    public Task<IReadOnlyList<TranscribedSentence>> GetSentencesAsync(string jobId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<TranscribedSentence>>(sentences);
}
