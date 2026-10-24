using Dictation.Application.Lessons.Abstractions;
using MediatR;

namespace Dictation.Application.Lessons;

/// <summary>Reads one <c>Ready</c> lesson; null when it is missing or not <c>Ready</c>.</summary>
public sealed class GetLessonQueryHandler(ILessonRepository lessons) : IRequestHandler<GetLessonQuery, GetLessonResponse?>
{
    public Task<GetLessonResponse?> Handle(GetLessonQuery query, CancellationToken ct) =>
        lessons.GetAsync(query.Id, ct);
}
