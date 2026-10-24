using System.Net;
using Dictation.Application.Import;
using Dictation.Infrastructure.ContentSources.Tatoeba;
using Dictation.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Dictation.Tests.Infrastructure;

public sealed class TatoebaContentSourceTests
{
    private const string Page = """
        {
          "data": [
            { "id": 1, "text": " One. ", "audios": [{ "id": 11 }] },
            { "id": 2, "text": "No audio.", "audios": [] },
            { "id": 3, "text": "Three.", "audios": [{ "id": 33 }] },
            { "id": 4, "text": "Already imported.", "audios": [{ "id": 44 }] },
            { "id": 5, "text": "Left over.", "audios": [{ "id": 55 }] }
          ],
          "paging": { "has_next": false }
        }
        """;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task FetchAsync_NewSentencesWithAudio_GroupsThemIntoFullLessons()
    {
        var source = CreateSource(_ => (HttpStatusCode.OK, Page));

        var lessons = await source.FetchAsync("en", new HashSet<string> { "4:44" }, max: 5, Ct);

        var lesson = Assert.Single(lessons);
        Assert.Equal("1,3", lesson.ExternalId);
        Assert.Equal(
            [("1:11", "One.", "https://tatoeba.test/v1/audios/11/file"), ("3:33", "Three.", "https://tatoeba.test/v1/audios/33/file")],
            lesson.Segments.Select(s => (s.ExternalKey, s.Text, s.AudioUrl)));
    }

    [Fact]
    public async Task FetchAsync_ApiReturnsError_ThrowsImportException()
    {
        var source = CreateSource(_ => (HttpStatusCode.InternalServerError, ""));

        await Assert.ThrowsAsync<ImportException>(() => source.FetchAsync("en", new HashSet<string>(), max: 5, Ct));
    }

    private static TatoebaContentSource CreateSource(Func<string, (HttpStatusCode, string)> respond) =>
        new(
            new StubHttpHandler(respond).CreateClient(),
            Options.Create(new TatoebaOptions { BaseUrl = "https://tatoeba.test", GroupSize = 2 }),
            NullLogger<TatoebaContentSource>.Instance);
}
