using System.Web;
using Dictation.Domain.Lessons;
using Dictation.Infrastructure.Sources.Tatoeba;
using Dictation.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Dictation.Tests.Adapters;

/// <summary>Tatoeba adapter on recorded-shape responses in <c>Fixtures/tatoeba/</c> (adapter-rules, Tatoeba section).</summary>
public sealed class TatoebaContentSourceTests
{
    private static readonly IReadOnlySet<string> NoKnownKeys = new HashSet<string>();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static TatoebaContentSource Create(FixtureHttpHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://api.tatoeba.org") },
            Options.Create(new TatoebaOptions { BaseUrl = "https://api.tatoeba.org" }),
            NullLogger<TatoebaContentSource>.Instance);

    /// <summary>Fetches items and maps each to a lesson, as the use case does.</summary>
    private static async Task<List<Lesson>> DiscoverAsync(TatoebaContentSource source, IReadOnlySet<string> knownKeys) =>
        (await source.FetchAsync("en", knownKeys, 5, Ct)).Select(source.ToLesson).ToList();

    private static string[] Keys(Lesson lesson) => lesson.Segments.Select(s => s.ExternalKey!).ToArray();

    [Fact]
    public async Task First_start_groups_sentences_into_five_lessons_of_ten_clips()
    {
        var handler = new FixtureHttpHandler("tatoeba", "sentences-60.json");

        var lessons = await DiscoverAsync(Create(handler), NoKnownKeys);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/v1/sentences", request.AbsolutePath);
        var query = HttpUtility.ParseQueryString(request.Query);
        Assert.Equal("eng", query["lang"]);
        Assert.Equal("created", query["sort"]);
        Assert.Equal("yes", query["has_audio"]);
        Assert.Equal("audios", query["include"]);
        Assert.Equal("none", query["showtrans"]);
        Assert.Equal("100", query["limit"]);
        Assert.Null(query["word_count"]);

        Assert.Equal(5, lessons.Count);
        Assert.All(lessons, l => Assert.Equal(10, l.Segments.Count));

        var first = lessons[0];
        Assert.Equal("tatoeba", first.Source);
        Assert.Equal(string.Join(',', Enumerable.Range(14000001, 10)), first.ExternalId);
        Assert.Equal("Tatoeba #14000001–#14000010", first.Title);
        Assert.Equal("https://tatoeba.org/en/sentences/show/14000001", first.SourceUrl);
        Assert.Equal("en", first.Language);
        Assert.Null(first.SourceAudioUrl);
        Assert.Equal(LessonStatus.Discovered, first.Status);

        var segment = first.Segments[0];
        Assert.Equal(0, segment.Index);
        Assert.Equal("14000001:1200001", segment.ExternalKey);
        Assert.Equal("We left before noon.", segment.Text);
        Assert.Equal("https://api.tatoeba.org/v1/audios/1200001/file", segment.AudioUrl);
        Assert.Equal(0, segment.StartMs);
        Assert.Null(segment.EndMs);

        Assert.Equal("Tatoeba #14000041–#14000050", lessons[4].Title);
    }

    [Fact]
    public async Task Known_keys_are_excluded_before_grouping()
    {
        var known = Enumerable.Range(0, 5).Select(i => $"{14000001 + i}:{1200001 + i}").ToHashSet();

        var lessons = await DiscoverAsync(Create(new FixtureHttpHandler("tatoeba", "sentences-60.json")), known);

        Assert.Equal(5, lessons.Count);
        Assert.Equal("14000006:1200006", lessons[0].Segments[0].ExternalKey);
        Assert.DoesNotContain(lessons.SelectMany(Keys), known.Contains);
    }
}
