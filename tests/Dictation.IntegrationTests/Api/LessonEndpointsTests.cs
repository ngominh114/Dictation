using System.Net;
using System.Text.Json;
using Dictation.Domain.Lessons;
using Dictation.IntegrationTests.Support;
using Dictation.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Dictation.IntegrationTests.Api;

/// <summary>
/// The lesson endpoints over real HTTP, on the real app and database. Import is off, so nothing calls an
/// external source.
/// </summary>
public sealed class LessonEndpointsTests : SqliteDatabase
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Dictation"] = ConnectionString,
                ["Import:Enabled"] = "false",
            })));
        _client = _factory.CreateClient();
    }

    public override async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await base.DisposeAsync();
    }

    [Fact]
    public async Task GetLessons_ReadyAndUnfinishedLessonsStored_ReturnsOnlyReadyInCamelCase()
    {
        var ready = await AddAsync(ReadyLesson());
        await AddAsync(TestLessons.Create());

        using var body = await GetJsonAsync("/api/lessons");

        var item = Assert.Single(body.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(ready.Id, item.GetProperty("id").GetGuid());
        Assert.Equal(1, item.GetProperty("segmentCount").GetInt32());
        Assert.Equal(1, body.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task GetLessons_OutOfRangePaging_FallsBackToDefaults()
    {
        using var body = await GetJsonAsync("/api/lessons?page=0&pageSize=500");

        Assert.Equal(1, body.RootElement.GetProperty("page").GetInt32());
        Assert.Equal(20, body.RootElement.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task GetLesson_ReadyLesson_ReturnsSegmentsInOrderWithoutNullFields()
    {
        var lesson = ReadyLesson();
        lesson.AddSegment("2:22", "Second.", "https://example.test/2.mp3", startMs: 0, endMs: 1500);
        await AddAsync(lesson);

        using var body = await GetJsonAsync($"/api/lessons/{lesson.Id}");

        var segments = body.RootElement.GetProperty("segments").EnumerateArray().ToList();
        Assert.Equal([0, 1], segments.Select(s => s.GetProperty("index").GetInt32()));
        Assert.False(segments[0].TryGetProperty("endMs", out _));
        Assert.Equal(1500, segments[1].GetProperty("endMs").GetInt32());
    }

    [Fact]
    public async Task GetLesson_MissingNotReadyOrInvalidId_ReturnsNotFoundProblemDetails()
    {
        var failed = TestLessons.Create();
        failed.Fail();
        await AddAsync(failed);

        foreach (var url in new[] { $"/api/lessons/{Guid.NewGuid()}", $"/api/lessons/{failed.Id}", "/api/lessons/not-a-guid" })
        {
            using var response = await _client.GetAsync(url, Ct);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }
    }

    private static Lesson ReadyLesson()
    {
        var lesson = TestLessons.Create();
        lesson.AddSentence(Guid.NewGuid().ToString("N"));
        lesson.Complete();
        return lesson;
    }

    private async Task<Lesson> AddAsync(Lesson lesson)
    {
        await using var db = CreateContext();
        Assert.True(await db.TryAddAsync(lesson, Ct));
        return lesson;
    }

    private async Task<JsonDocument> GetJsonAsync(string url)
    {
        using var response = await _client.GetAsync(url, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
    }
}
