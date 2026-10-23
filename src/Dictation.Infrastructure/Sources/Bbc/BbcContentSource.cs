using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Dictation.Application.Import;
using Dictation.Domain.Lessons;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dictation.Infrastructure.Sources.Bbc;

/// <summary>BBC 6 Minute English source adapter (key <c>bbc</c>).</summary>
public sealed partial class BbcContentSource : IContentSource
{
    public const string SourceKey = "bbc";

    /// <summary>Browser-like User-Agent; some fetchers are blocked on bbc.co.uk.</summary>
    public const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36";

    private const string DefaultFeedUrl = "https://podcasts.files.bbci.co.uk/p02pc9tn.rss";
    private const string DefaultPageBaseUrl = "https://www.bbc.co.uk/learningenglish/english/features";
    private const string SeriesSlug = "6-minute-english";

    private readonly HttpClient _http;
    private readonly BbcOptions _options;
    private readonly ILogger<BbcContentSource> _logger;

    public BbcContentSource(HttpClient http, IOptions<BbcOptions> options, ILogger<BbcContentSource> logger)
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

        if (max <= 0 || language is null || !SupportedLanguages.Contains(language.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            return [];
        }

        var feedUrl = string.IsNullOrWhiteSpace(_options.FeedUrl) ? DefaultFeedUrl : _options.FeedUrl.Trim();
        var feed = await GetStringAsync(feedUrl, ct).ConfigureAwait(false)
            ?? throw new ImportException("BBC feed could not be downloaded.");

        var candidates = ParseFeed(feed)
            .Where(e => !knownKeys.Contains(e.Guid))
            .OrderByDescending(e => e.PublishedAt)
            .ToList();

        var items = new List<SourceItem>(max);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        string? indexPage = null;
        var indexLoaded = false;

        foreach (var entry in candidates)
        {
            if (items.Count >= max)
            {
                break;
            }

            if (!seen.Add(entry.Guid))
            {
                continue;
            }

            var datedUrl = BuildDatedPageUrl(entry.AirDate);
            var pageUrl = datedUrl;
            var page = await GetStringAsync(datedUrl, ct).ConfigureAwait(false);

            if (page is null)
            {
                if (!indexLoaded)
                {
                    indexPage = await GetStringAsync(IndexPageUrl, ct).ConfigureAwait(false);
                    indexLoaded = true;
                }

                var fallbackUrl = indexPage is null ? null : FindIndexLink(indexPage, entry.AirDate);
                if (fallbackUrl is not null && !string.Equals(fallbackUrl, datedUrl, StringComparison.OrdinalIgnoreCase))
                {
                    page = await GetStringAsync(fallbackUrl, ct).ConfigureAwait(false);
                    pageUrl = fallbackUrl;
                }
            }

            if (page is null)
            {
                _logger.LogWarning(
                    "BBC episode page not found for {Source}/{ExternalId}; skipping it for this run",
                    SourceKey,
                    entry.Guid);
                continue;
            }

            var audioUrl = AudioUrlRegex().Match(page) is { Success: true } match
                ? WebUtility.HtmlDecode(match.Value)
                : null;

            items.Add(new BbcEpisode(entry.Guid, entry.Title, pageUrl, audioUrl));
        }

        _logger.LogInformation("BBC returned {Count} new episode(s)", items.Count);
        return items;
    }

    public Lesson ToLesson(SourceItem item)
    {
        if (item is not BbcEpisode episode)
        {
            throw new ArgumentException($"Not a BBC item: {item.GetType().Name}.", nameof(item));
        }

        return Lesson.CreateDiscovered(
            SourceKey,
            episode.ExternalId,
            episode.Title,
            "en",
            episode.PageUrl,
            episode.AudioUrl,
            DateTime.UtcNow);
    }

    private string PageBaseUrl =>
        (string.IsNullOrWhiteSpace(_options.PageBaseUrl) ? DefaultPageBaseUrl : _options.PageBaseUrl.Trim()).TrimEnd('/');

    private string IndexPageUrl => $"{PageBaseUrl}/{SeriesSlug}";

    private string BuildDatedPageUrl(DateOnly date)
    {
        var episode = date.ToString("yyMMdd", CultureInfo.InvariantCulture);
        return date.Year >= 2022
            ? string.Create(CultureInfo.InvariantCulture, $"{PageBaseUrl}/{SeriesSlug}_{date.Year:D4}/ep-{episode}")
            : $"{PageBaseUrl}/{SeriesSlug}/ep-{episode}";
    }

    private string? FindIndexLink(string indexPage, DateOnly date)
    {
        var episode = date.ToString("yyMMdd", CultureInfo.InvariantCulture);
        foreach (Match match in HrefRegex().Matches(indexPage))
        {
            var href = WebUtility.HtmlDecode(match.Groups["href"].Value).Trim();
            var path = href.Split('?', '#')[0].TrimEnd('/');
            if (!path.EndsWith($"/ep-{episode}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (Uri.TryCreate(new Uri(IndexPageUrl), href, out var absolute)
                && absolute.Scheme is "http" or "https")
            {
                return absolute.GetLeftPart(UriPartial.Path).TrimEnd('/');
            }
        }

        return null;
    }

    private IEnumerable<FeedEntry> ParseFeed(string xml)
    {
        var document = XDocument.Parse(xml);
        foreach (var element in document.Descendants().Where(e => e.Name.LocalName == "item"))
        {
            var guid = element.Elements().FirstOrDefault(e => e.Name.LocalName == "guid")?.Value.Trim();
            var title = element.Elements().FirstOrDefault(e => e.Name.LocalName == "title")?.Value.Trim();
            var pubDate = element.Elements().FirstOrDefault(e => e.Name.LocalName == "pubDate")?.Value;

            if (string.IsNullOrEmpty(guid))
            {
                continue;
            }

            if (!TryParseRfc822(pubDate, out var publishedAt))
            {
                _logger.LogWarning("BBC feed item {ExternalId} has no valid pubDate; skipping it", guid);
                continue;
            }

            yield return new FeedEntry(guid, title ?? "", publishedAt);
        }
    }

    private static bool TryParseRfc822(string? value, out DateTimeOffset publishedAt)
    {
        publishedAt = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        // "+0000" is not understood by .NET parsing; turn it into "+00:00".
        var normalized = NumericOffsetRegex().Replace(value.Trim(), " $1:$2");
        if (DateTimeOffset.TryParse(normalized, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed))
        {
            publishedAt = parsed;
            return true;
        }

        return false;
    }

    /// <summary>GETs <paramref name="url"/> as text; null when the server answers with a non-success status.</summary>
    private async Task<string?> GetStringAsync(string url, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogInformation("BBC returned {StatusCode} for {Url}", (int)response.StatusCode, url);
            return null;
        }

        return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    }

    [GeneratedRegex(@"https://downloads\.bbc\.co\.uk/[^""'\s<>]*?_download\.mp3", RegexOptions.IgnoreCase)]
    private static partial Regex AudioUrlRegex();

    [GeneratedRegex(@"href\s*=\s*[""'](?<href>[^""']*ep-\d{6}[^""']*)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex HrefRegex();

    [GeneratedRegex(@"\s([+-]\d{2})(\d{2})$")]
    private static partial Regex NumericOffsetRegex();

    private sealed record FeedEntry(string Guid, string Title, DateTimeOffset PublishedAt)
    {
        /// <summary>Air date as published (in the feed's own offset).</summary>
        public DateOnly AirDate => DateOnly.FromDateTime(PublishedAt.DateTime);
    }

    private sealed record BbcEpisode(string ExternalId, string Title, string PageUrl, string? AudioUrl)
        : SourceItem(ExternalId);
}
