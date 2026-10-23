using Dictation.Application.Import;

namespace Dictation.Tests.Support;

/// <summary>Reports the job pending <paramref name="pendingPolls"/> times, then completed with <paramref name="sentences"/>.</summary>
public sealed class FakeTranscriptionProvider(int pendingPolls = 0, params TranscribedSentence[] sentences)
    : ITranscriptionProvider
{
    public string Key => "fake";

    public List<string> Submitted { get; } = [];

    public int Polls { get; private set; }

    public Task<string> SubmitAsync(string audioUrl, string language, CancellationToken ct)
    {
        Submitted.Add(audioUrl);
        return Task.FromResult("job-1");
    }

    public Task<bool> IsCompletedAsync(string jobId, CancellationToken ct) => Task.FromResult(++Polls > pendingPolls);

    public Task<IReadOnlyList<TranscribedSentence>> GetSentencesAsync(string jobId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<TranscribedSentence>>(sentences);
}
