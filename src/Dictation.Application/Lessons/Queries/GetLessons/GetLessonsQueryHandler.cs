using Dictation.Application.Lessons.Abstractions;
using MediatR;

namespace Dictation.Application.Lessons;

/// <summary>Reads one page of <c>Ready</c> lessons.</summary>
public sealed class GetLessonsQueryHandler(ILessonRepository lessons) : IRequestHandler<GetLessonsQuery, GetLessonsResponse>
{
    public Task<GetLessonsResponse> Handle(GetLessonsQuery query, CancellationToken ct) =>
        lessons.GetPageAsync(query.Page, query.PageSize, ct);
}
