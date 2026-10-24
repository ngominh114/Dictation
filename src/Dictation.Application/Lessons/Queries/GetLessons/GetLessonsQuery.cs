using MediatR;

namespace Dictation.Application.Lessons;

/// <summary>A page of <c>Ready</c> lessons, newest first. <see cref="Page"/> is 1-based.</summary>
public sealed record GetLessonsQuery(int Page, int PageSize) : IRequest<GetLessonsResponse>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

/// <summary>One page of <c>Ready</c> lessons with the total count.</summary>
public sealed record GetLessonsResponse(IReadOnlyList<LessonSummary> Items, int Page, int PageSize, int TotalCount);

public sealed record LessonSummary(Guid Id, string Title, string Source, string Language, int SegmentCount, DateTime CreatedAt);
