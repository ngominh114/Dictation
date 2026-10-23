using System.Globalization;
using System.Net.Http.Json;
using Dictation.Application.Import;
using Dictation.Domain.Lessons;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dictation.Infrastructure.Sources.Tatoeba;

/// <summary>Tatoeba source adapter (key <c>tatoeba</c>).</summary>
public sealed class TatoebaContentSource : IContentSource
{
    public const string SourceKey = "tatoeba";

    private const string DefaultBaseUrl = "https://api.tatoeba.org";
    private const int DefaultGroupSize = 10;
    private const int DefaultMaxPages = 20;

    // Canonical language -> Tatoeba ISO 639-3 code.
    private static readonly IReadOnlyDictionary<string, string> LanguageCodes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = "eng" };

    private readonly HttpClient _http;
    private readonly TatoebaOptions _options;
    private readonly ILogger<TatoebaContentSource> _logger;

    public TatoebaContentSource(
        HttpClient http,
        IOptions<TatoebaOptions> options,
        ILogger<TatoebaContentSource> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Key => SourceKey;

    public IReadOnlyCollection<string> SupportedLanguages { get; } = ["en"];

    public async Task<IReadOnlyList<SourceItem>> FetchAsync(
        string language,
        IReadOnlySet<string> knownKeys,
        int max,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(knownKeys);

        if (max <= 0 || language is null || !LanguageCodes.TryGetValue(language.Trim(), out var tatoebaLang))
        {
            return [];
        }

        var groupSize = _options.GroupSize > 0 ? _options.GroupSize : DefaultGroupSize;
        var maxPages = _options.MaxPages > 0 ? _options.MaxPages : DefaultMaxPages;
        var needed = max * groupSize;
        var baseUrl = ResolveBaseUrl();

        var picked = new List<PickedSentence>(needed);
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        string? url = BuildFirstUrl(baseUrl, tatoebaLang);

        for (var page = 0; page < maxPages && url is not null && picked.Count < needed; page++)
        {
            var response = await GetPageAsync(url, ct).ConfigureAwait(false);

            foreach (var sentence in response.Data ?? [])
            {
                if (picked.Count >= needed)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(sentence.Text))
                {
                    continue;
                }

                // Personal use only: clip licences are not checked; take the first clip in API order.
                var clip = sentence.Audios?.FirstOrDefault();
                if (clip is null)
                {
                    continue;
                }

                var key = string.Create(CultureInfo.InvariantCulture, $"{sentence.Id}:{clip.Id}");
                if (knownKeys.Contains(key) || !seenKeys.Add(key))
                {
                    continue;
                }

                picked.Add(new PickedSentence(sentence.Id, clip.Id, sentence.Text.Trim(), key));
            }

            url = response.Paging is { HasNext: not false, Next: { Length: > 0 } next } ? next : null;
        }

        var groups = new List<SourceItem>(max);

        // A trailing partial group is not created.
        for (var start = 0; start + groupSize <= picked.Count && groups.Count < max; start += groupSize)
        {
            var group = picked.GetRange(start, groupSize);
            var externalId = string.Join(",", group.Select(s => s.SentenceId.ToString(CultureInfo.InvariantCulture)));
            if (!knownKeys.Contains(externalId))
            {
                groups.Add(new TatoebaGroup(externalId, baseUrl, group));
            }
        }

        _logger.LogInformation(
            "Tatoeba returned {SentenceCount} new sentence(s), grouped into {GroupCount} item(s)",
            picked.Count,
            groups.Count);

        return groups;
    }

    public Lesson ToLesson(SourceItem item)
    {
        if (item is not TatoebaGroup group)
        {
            throw new ArgumentException($"Not a Tatoeba item: {item.GetType().Name}.", nameof(item));
        }

        var first = group.Sentences[0].SentenceId.ToString(CultureInfo.InvariantCulture);
        var last = group.Sentences[^1].SentenceId.ToString(CultureInfo.InvariantCulture);
        var lesson = Lesson.CreateDiscovered(
            SourceKey,
            group.ExternalId,
            $"Tatoeba #{first}–#{last}",
            "en",
            $"https://tatoeba.org/en/sentences/show/{first}",
            sourceAudioUrl: null,
            DateTime.UtcNow);

        foreach (var sentence in group.Sentences)
        {
            lesson.AddSegment(
                sentence.ExternalKey,
                sentence.Text,
                string.Create(CultureInfo.InvariantCulture, $"{group.BaseUrl}/v1/audios/{sentence.AudioId}/file"),
                startMs: 0,
                endMs: null);
        }

        return lesson;
    }

    private async Task<TatoebaSentencesResponseDto> GetPageAsync(string url, CancellationToken ct)
    {
        using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new ImportException(
                $"Tatoeba returned unexpected status {(int)response.StatusCode} ({response.StatusCode}).");
        }

        var body = await response.Content.ReadFromJsonAsync<TatoebaSentencesResponseDto>(ct).ConfigureAwait(false);
        return body ?? throw new ImportException("Tatoeba returned an empty response.");
    }

    private string BuildFirstUrl(string baseUrl, string tatoebaLang)
    {
        var url = $"{baseUrl}/v1/sentences?lang={Uri.EscapeDataString(tatoebaLang)}"
                  + "&sort=created&has_audio=yes&include=audios&showtrans=none&limit=100";
        if (!string.IsNullOrWhiteSpace(_options.WordCount))
        {
            url += $"&word_count={Uri.EscapeDataString(_options.WordCount.Trim())}";
        }

        return url;
    }

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

    private sealed record PickedSentence(long SentenceId, long AudioId, string Text, string ExternalKey);

    private sealed record TatoebaGroup(string ExternalId, string BaseUrl, IReadOnlyList<PickedSentence> Sentences)
        : SourceItem(ExternalId);
}
