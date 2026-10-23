using System.Net.Http.Json;
using Dictation.Application.Import;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dictation.Infrastructure.Transcription.AssemblyAi;

/// <summary>AssemblyAI transcription adapter (key <c>assemblyai</c>).</summary>
public sealed class AssemblyAiTranscriptionProvider : ITranscriptionProvider
{
    public const string ProviderKey = "assemblyai";

    private const string DefaultBaseUrl = "https://api.assemblyai.com";

    // Canonical language -> AssemblyAI language_code.
    private static readonly IReadOnlyDictionary<string, string> LanguageCodes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = "en" };

    private readonly HttpClient _http;
    private readonly AssemblyAiOptions _options;
    private readonly ILogger<AssemblyAiTranscriptionProvider> _logger;

    public AssemblyAiTranscriptionProvider(
        HttpClient http,
        IOptions<AssemblyAiOptions> options,
        ILogger<AssemblyAiTranscriptionProvider> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Key => ProviderKey;

    public async Task<string> SubmitAsync(string audioUrl, string language, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(audioUrl);

        if (language is null || !LanguageCodes.TryGetValue(language.Trim(), out var languageCode))
        {
            throw new ImportException($"AssemblyAI does not support language '{language}'.");
        }

        using var request = CreateRequest(HttpMethod.Post, "/v2/transcript");
        request.Content = JsonContent.Create(new AssemblyAiSubmitRequestDto
        {
            AudioUrl = audioUrl,
            LanguageCode = languageCode,
        });

        var transcript = await SendAsync<AssemblyAiTranscriptDto>(request, ct).ConfigureAwait(false);
        ThrowIfError(transcript);

        if (string.IsNullOrWhiteSpace(transcript.Id))
        {
            throw new ImportException("AssemblyAI returned no transcript id.");
        }

        _logger.LogInformation("AssemblyAI job {JobId} submitted", transcript.Id);
        return transcript.Id;
    }

    public async Task<bool> IsCompletedAsync(string jobId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);

        using var request = CreateRequest(HttpMethod.Get, $"/v2/transcript/{Uri.EscapeDataString(jobId)}");
        var transcript = await SendAsync<AssemblyAiTranscriptDto>(request, ct).ConfigureAwait(false);
        ThrowIfError(transcript);

        return transcript.Status?.Trim().ToLowerInvariant() switch
        {
            "queued" or "processing" => false,
            "completed" => true,
            var other => throw new ImportException($"AssemblyAI returned unknown status '{other}'."),
        };
    }

    public async Task<IReadOnlyList<TranscribedSentence>> GetSentencesAsync(string jobId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);

        using var request = CreateRequest(HttpMethod.Get, $"/v2/transcript/{Uri.EscapeDataString(jobId)}/sentences");
        var response = await SendAsync<AssemblyAiSentencesResponseDto>(request, ct).ConfigureAwait(false);

        var sentences = (response.Sentences ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s.Text))
            .Select(s => new TranscribedSentence(s.Text!.Trim(), ToMs(s.Start), ToMs(s.End)))
            .OrderBy(s => s.StartMs)
            .ToList();

        if (sentences.Count == 0)
        {
            throw new ImportException($"AssemblyAI job {jobId} returned no sentences.");
        }

        // Clamp overlaps so EndMs[i] <= StartMs[i+1].
        for (var i = 0; i < sentences.Count - 1; i++)
        {
            var nextStart = sentences[i + 1].StartMs;
            if (sentences[i].EndMs > nextStart)
            {
                sentences[i] = sentences[i] with { EndMs = Math.Max(sentences[i].StartMs, nextStart) };
            }
        }

        return sentences.AsReadOnly();
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, ResolveBaseUrl() + path);
        request.Headers.TryAddWithoutValidation("authorization", _options.ApiKey ?? "");
        return request;
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken ct)
        where T : class
    {
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new ImportException(
                $"AssemblyAI returned unexpected status {(int)response.StatusCode} ({response.StatusCode}).");
        }

        var body = await response.Content.ReadFromJsonAsync<T>(ct).ConfigureAwait(false);
        return body ?? throw new ImportException("AssemblyAI returned an empty response.");
    }

    private static void ThrowIfError(AssemblyAiTranscriptDto transcript)
    {
        if (string.Equals(transcript.Status?.Trim(), "error", StringComparison.OrdinalIgnoreCase))
        {
            throw new ImportException(
                string.IsNullOrWhiteSpace(transcript.Error) ? "AssemblyAI reported an error." : transcript.Error);
        }
    }

    private static int ToMs(long value) => (int)Math.Clamp(value, 0, int.MaxValue);

    private string ResolveBaseUrl()
    {
        if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            return _options.BaseUrl.Trim().TrimEnd('/');
        }

        if (_http.BaseAddress is not null)
        {
            return _http.BaseAddress.GetLeftPart(UriPartial.Authority);
        }

        return DefaultBaseUrl;
    }
}
