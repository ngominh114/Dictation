using System.Net;
using Dictation.Application.Import;
using Dictation.Infrastructure.ContentSources.Bbc;
using Dictation.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Dictation.Tests.Infrastructure;

public sealed class BbcContentSourceTests
{
    private const string FeedUrl = "https://bbc.test/feed.rss";
    private const string EpisodePageUrl = "https://bbc.test/features/6-minute-english_2025/ep-251016";
    private const string AudioUrl = "https://downloads.bbc.co.uk/learningenglish/features/6min/251016_6min_english_download.mp3";

    private const string Feed = """
        <rss><channel>
          <item><guid>ep-new</guid><title>New episode</title><pubDate>Thu, 16 Oct 2025 13:00:00 +0000</pubDate></item>
          <item><guid>ep-old</guid><title>Old episode</title><pubDate>Thu, 09 Oct 2025 13:00:00 +0000</pubDate></item>
        </channel></rss>
        """;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task FetchAsync_NewEpisode_ReturnsLessonWithEpisodeAudio()
    {
        var source = CreateSource(url => url switch
        {
            FeedUrl => (HttpStatusCode.OK, Feed),
            EpisodePageUrl => (HttpStatusCode.OK, $"<a href=\"{AudioUrl}\">Download</a>"),
            _ => (HttpStatusCode.NotFound, ""),
        });

        var lessons = await source.FetchAsync("en", new HashSet<string> { "ep-old" }, max: 5, Ct);

        var lesson = Assert.Single(lessons);
        Assert.Equal(("ep-new", "New episode"), (lesson.ExternalId, lesson.Title));
        Assert.Equal((EpisodePageUrl, AudioUrl), (lesson.SourceUrl, lesson.SourceAudioUrl));
        Assert.Empty(lesson.Segments);
    }

    [Fact]
    public async Task FetchAsync_EpisodePageNotFound_SkipsEpisode()
    {
        var source = CreateSource(url => url == FeedUrl ? (HttpStatusCode.OK, Feed) : (HttpStatusCode.NotFound, ""));

        var lessons = await source.FetchAsync("en", new HashSet<string>(), max: 5, Ct);

        Assert.Empty(lessons);
    }

    [Fact]
    public async Task FetchAsync_DatedPageMissing_UsesEpisodeLinkFromIndexPage()
    {
        const string indexPageUrl = "https://bbc.test/features/6-minute-english";
        const string linkedPageUrl = "https://bbc.test/features/6-minute-english/ep-251016";
        var source = CreateSource(url => url switch
        {
            FeedUrl => (HttpStatusCode.OK, Feed),
            indexPageUrl => (HttpStatusCode.OK, $"<a href=\"{linkedPageUrl}?utm=1\">New episode</a>"),
            linkedPageUrl => (HttpStatusCode.OK, $"<a href=\"{AudioUrl}\">Download</a>"),
            _ => (HttpStatusCode.NotFound, ""),
        });

        var lessons = await source.FetchAsync("en", new HashSet<string> { "ep-old" }, max: 5, Ct);

        var lesson = Assert.Single(lessons);
        Assert.Equal((linkedPageUrl, AudioUrl), (lesson.SourceUrl, lesson.SourceAudioUrl));
    }

    [Fact]
    public async Task FetchAsync_FeedUnavailable_ThrowsImportException()
    {
        var source = CreateSource(_ => (HttpStatusCode.ServiceUnavailable, ""));

        await Assert.ThrowsAsync<ImportException>(() => source.FetchAsync("en", new HashSet<string>(), max: 5, Ct));
    }

    private static BbcContentSource CreateSource(Func<string, (HttpStatusCode, string)> respond) =>
        new(
            new StubHttpHandler(respond).CreateClient(),
            Options.Create(new BbcOptions { FeedUrl = FeedUrl, PageBaseUrl = "https://bbc.test/features" }),
            NullLogger<BbcContentSource>.Instance);
}
