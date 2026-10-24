namespace Dictation.Application.Lessons.Abstractions;

/// <summary>Reads <c>Ready</c> lessons for the lesson queries. Lessons in any other status are never returned.</summary>
public interface ILessonRepository
{
    /// <summary>One page of <c>Ready</c> lessons, newest first, with the total count. <paramref name="page"/> is 1-based.</summary>
    Task<GetLessonsResponse> GetPageAsync(int page, int pageSize, CancellationToken ct);

    /// <summary>The <c>Ready</c> lesson with its segments in order, or null when it is missing or not <c>Ready</c>.</summary>
    Task<GetLessonResponse?> GetAsync(Guid id, CancellationToken ct);
}
