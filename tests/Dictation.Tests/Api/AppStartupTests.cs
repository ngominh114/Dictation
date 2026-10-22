using System.Net;
using Dictation.Tests.Support;

namespace Dictation.Tests.Api;

public sealed class AppStartupTests : IDisposable
{
    private readonly TempSqliteDatabase database = new();

    public void Dispose() => database.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task First_start_creates_the_database_and_serves_the_home_page()
    {
        await using var factory = new DictationApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var db = database.CreateContext();
        Assert.Empty(db.Lessons);
    }

    [Fact]
    public async Task Unknown_api_route_returns_404_problem_details()
    {
        await using var factory = new DictationApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/does-not-exist", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
