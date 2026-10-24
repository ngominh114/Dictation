using System.Net;
using Dictation.Application.Import;
using Dictation.Application.Import.Abstractions;
using Dictation.Infrastructure.Transcription.AssemblyAi;
using Dictation.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Dictation.Tests.Infrastructure;

public sealed class AssemblyAiTranscriptionProviderTests
{
    private const string TranscriptUrl = "https://aai.test/v2/transcript/job-1";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetSentencesAsync_CompletedJob_ReturnsOrderedSentencesWithoutOverlaps()
    {
        var provider = CreateProvider(url => url switch
        {
            TranscriptUrl => (HttpStatusCode.OK, """{ "id": "job-1", "status": "completed" }"""),
            TranscriptUrl + "/sentences" => (HttpStatusCode.OK, """
                {
                  "sentences": [
                    { "text": "Hi Georgie.", "start": 900, "end": 1800 },
                    { "text": " ", "start": 1800, "end": 1900 },
                    { "text": " Hi Phil. ", "start": 250, "end": 1000 }
                  ]
                }
                """),
            _ => (HttpStatusCode.NotFound, ""),
        });

        var sentences = await provider.GetSentencesAsync("job-1", Ct);

        Assert.Equal([new TranscribedSentence("Hi Phil.", 250, 900), new TranscribedSentence("Hi Georgie.", 900, 1800)], sentences);
    }

    [Fact]
    public async Task GetSentencesAsync_JobReportsError_ThrowsImportExceptionWithProviderMessage()
    {
        var provider = CreateProvider(_ =>
            (HttpStatusCode.OK, """{ "id": "job-1", "status": "error", "error": "Audio file is too short." }"""));

        var ex = await Assert.ThrowsAsync<ImportException>(() => provider.GetSentencesAsync("job-1", Ct));

        Assert.Equal("Audio file is too short.", ex.Message);
    }

    [Fact]
    public async Task GetSentencesAsync_UnknownJobStatus_ThrowsInsteadOfPollingForever()
    {
        var provider = CreateProvider(_ => (HttpStatusCode.OK, """{ "id": "job-1", "status": "paused" }"""));

        await Assert.ThrowsAsync<ImportException>(() => provider.GetSentencesAsync("job-1", Ct));
    }

    [Fact]
    public async Task GetSentencesAsync_CompletedJobWithNoSentences_ThrowsImportException()
    {
        var provider = CreateProvider(url => url == TranscriptUrl
            ? (HttpStatusCode.OK, """{ "id": "job-1", "status": "completed" }""")
            : (HttpStatusCode.OK, """{ "sentences": [] }"""));

        await Assert.ThrowsAsync<ImportException>(() => provider.GetSentencesAsync("job-1", Ct));
    }

    [Fact]
    public async Task SubmitAsync_ProviderRejectsRequest_ThrowsImportException()
    {
        var provider = CreateProvider(_ => (HttpStatusCode.Unauthorized, """{ "error": "Invalid API key" }"""));

        await Assert.ThrowsAsync<ImportException>(
            () => provider.SubmitAsync("https://example.test/episode_download.mp3", "en", Ct));
    }

    private static AssemblyAiTranscriptionProvider CreateProvider(Func<string, (HttpStatusCode, string)> respond) =>
        new(
            new StubHttpHandler(respond).CreateClient(),
            Options.Create(new AssemblyAiOptions { BaseUrl = "https://aai.test", ApiKey = "test-key" }),
            NullLogger<AssemblyAiTranscriptionProvider>.Instance);
}
