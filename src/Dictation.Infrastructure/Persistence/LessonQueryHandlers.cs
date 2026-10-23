using Dictation.Application.Lessons;
using Dictation.Domain.Lessons;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dictation.Infrastructure.Persistence;

/// <summary>Handles the lesson queries against SQLite. Only <c>Ready</c> lessons are visible.</summary>
public sealed class LessonQueryHandlers(DictationDbContext db) :
    IRequestHandler<GetLessonsQuery, LessonPage>,
    IRequestHandler<GetLessonQuery, LessonDetail?>
{
    private IQueryable<Lesson> Ready => db.Lessons.AsNoTracking().Where(l => l.Status == LessonStatus.Ready);

    public async Task<LessonPage> Handle(GetLessonsQuery query, CancellationToken ct)
    {
        var totalCount = await Ready.CountAsync(ct);
        var items = await Ready
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(l => new LessonSummary(l.Id, l.Title, l.Source, l.Language, l.Segments.Count, l.CreatedAt))
            .ToListAsync(ct);

        return new LessonPage(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<LessonDetail?> Handle(GetLessonQuery query, CancellationToken ct)
    {
        var lesson = await Ready.FirstOrDefaultAsync(l => l.Id == query.Id, ct);

        return lesson is null
            ? null
            : new LessonDetail(
                lesson.Id,
                lesson.Title,
                lesson.Source,
                lesson.Language,
                lesson.SourceUrl,
                lesson.Segments.Select(s => new LessonSegment(s.Index, s.Text, s.AudioUrl, s.StartMs, s.EndMs)).ToList());
    }
}
