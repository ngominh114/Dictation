using MediatR;

namespace Dictation.Application.Lessons;

/// <summary>One <c>Ready</c> lesson with its segments; null when it is missing or not <c>Ready</c>.</summary>
public sealed record GetLessonQuery(Guid Id) : IRequest<GetLessonResponse?>;

/// <summary>A <c>Ready</c> lesson with its segments in order.</summary>
public sealed record GetLessonResponse(
    Guid Id,
    string Title,
    string Source,
    string Language,
    string SourceUrl,
    IReadOnlyList<LessonSegment> Segments);

public sealed record LessonSegment(int Index, string Text, string AudioUrl, int StartMs, int? EndMs);
