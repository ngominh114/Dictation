using Dictation.Application.Lessons;
using Dictation.Application.Lessons.Abstractions;
using Dictation.Domain.Lessons;
using Microsoft.EntityFrameworkCore;

namespace Dictation.Infrastructure.Persistence;

/// <summary>Reads <c>Ready</c> lessons from SQLite for the lesson queries.</summary>
public sealed class LessonRepository(DictationDbContext db) : ILessonRepository
{
    private IQueryable<Lesson> Ready => db.Lessons.AsNoTracking().Where(l => l.Status == LessonStatus.Ready);

    public async Task<GetLessonsResponse> GetPageAsync(int page, int pageSize, CancellationToken ct)
    {
        var totalCount = await Ready.CountAsync(ct);
        var items = await Ready
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LessonSummary(l.Id, l.Title, l.Source, l.Language, l.Segments.Count, l.CreatedAt))
            .ToListAsync(ct);

        return new GetLessonsResponse(items, page, pageSize, totalCount);
    }

    public async Task<GetLessonResponse?> GetAsync(Guid id, CancellationToken ct)
    {
        var lesson = await Ready.FirstOrDefaultAsync(l => l.Id == id, ct);

        return lesson is null
            ? null
            : new GetLessonResponse(
                lesson.Id,
                lesson.Title,
                lesson.Source,
                lesson.Language,
                lesson.SourceUrl,
                lesson.Segments.Select(s => new LessonSegment(s.Index, s.Text, s.AudioUrl, s.StartMs, s.EndMs)).ToList());
    }
}
