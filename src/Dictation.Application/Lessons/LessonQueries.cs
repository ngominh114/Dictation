using MediatR;

namespace Dictation.Application.Lessons;

/// <summary>A page of <c>Ready</c> lessons, newest first.</summary>
public sealed record GetLessonsQuery(int Page, int PageSize) : IRequest<LessonPage>;

/// <summary>One <c>Ready</c> lesson with its segments; null when missing or not <c>Ready</c>.</summary>
public sealed record GetLessonQuery(Guid Id) : IRequest<LessonDetail?>;

public sealed record LessonPage(IReadOnlyList<LessonSummary> Items, int Page, int PageSize, int TotalCount);

public sealed record LessonSummary(Guid Id, string Title, string Source, string Language, int SegmentCount, DateTime CreatedAt);

public sealed record LessonDetail(
    Guid Id,
    string Title,
    string Source,
    string Language,
    string SourceUrl,
    IReadOnlyList<LessonSegment> Segments);

public sealed record LessonSegment(int Index, string Text, string AudioUrl, int StartMs, int? EndMs);
