using Dictation.Application.Lessons;
using MediatR;

namespace Dictation.Api.Endpoints;

/// <summary>The lesson endpoints the UI calls (web-contract.md). Each one sends a query through the mediator.</summary>
public static class LessonEndpoints
{
    public static IEndpointRouteBuilder MapLessonEndpoints(this IEndpointRouteBuilder app)
    {
        var lessons = app.MapGroup("/api/lessons");
        lessons.MapGet("/", ListAsync);
        lessons.MapGet("/{id:guid}", GetAsync);
        return app;
    }

    internal static async Task<IResult> ListAsync(
        ISender sender,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = GetLessonsQuery.DefaultPageSize)
    {
        // Out-of-range paging falls back to the defaults instead of failing.
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize is < 1 or > GetLessonsQuery.MaxPageSize)
        {
            pageSize = GetLessonsQuery.DefaultPageSize;
        }

        var response = await sender.Send(new GetLessonsQuery(page, pageSize), cancellationToken).ConfigureAwait(false);

        return Results.Ok(response);
    }

    internal static async Task<IResult> GetAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GetLessonQuery(id), cancellationToken).ConfigureAwait(false);

        return response is null
            ? Results.NotFound()
            : Results.Ok(response);
    }
}
