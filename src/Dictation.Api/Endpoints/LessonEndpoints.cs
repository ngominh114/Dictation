using Dictation.Application.Lessons;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Dictation.Api.Endpoints;

/// <summary>The lesson endpoints the UI calls (web-contract.md). Each one sends a query through the mediator.</summary>
public static class LessonEndpoints
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public static IEndpointRouteBuilder MapLessonEndpoints(this IEndpointRouteBuilder app)
    {
        var lessons = app.MapGroup("/api/lessons");
        lessons.MapGet("/", ListAsync);
        lessons.MapGet("/{id:guid}", GetAsync);
        return app;
    }

    private static async Task<Results<Ok<LessonPage>, ProblemHttpResult>> ListAsync(
        ISender sender,
        CancellationToken ct,
        int page = 1,
        int pageSize = DefaultPageSize)
    {
        if (page < 1)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, detail: "page must be 1 or more.");
        }

        if (pageSize is < 1 or > MaxPageSize)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"pageSize must be between 1 and {MaxPageSize}.");
        }

        return TypedResults.Ok(await sender.Send(new GetLessonsQuery(page, pageSize), ct));
    }

    private static async Task<Results<Ok<LessonDetail>, ProblemHttpResult>> GetAsync(
        Guid id,
        ISender sender,
        CancellationToken ct) =>
        await sender.Send(new GetLessonQuery(id), ct) is { } lesson ? TypedResults.Ok(lesson) : LessonNotFound();

    private static ProblemHttpResult LessonNotFound() =>
        TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, detail: "Lesson not found.");
}
