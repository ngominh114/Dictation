using Dictation.Api.Endpoints;
using Dictation.Application.Lessons;
using Dictation.Tests.Support;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Dictation.Tests.Api;

public sealed class LessonEndpointsTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ListAsync_OutOfRangePaging_FallsBackToDefaults()
    {
        var response = new GetLessonsResponse([], 1, GetLessonsQuery.DefaultPageSize, 0);
        var sender = new StubSender(response);

        var result = await LessonEndpoints.ListAsync(sender, Ct, page: 0, pageSize: GetLessonsQuery.MaxPageSize + 1);

        Assert.Equal([new GetLessonsQuery(1, GetLessonsQuery.DefaultPageSize)], sender.Sent);
        Assert.Same(response, Assert.IsType<Ok<GetLessonsResponse>>(result).Value);
    }

    [Fact]
    public async Task GetAsync_LessonNotFound_ReturnsNotFound()
    {
        var result = await LessonEndpoints.GetAsync(Guid.NewGuid(), new StubSender(null), Ct);

        Assert.IsType<NotFound>(result);
    }
}
