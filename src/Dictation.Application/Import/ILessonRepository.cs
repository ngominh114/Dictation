using Dictation.Domain.Lessons;

namespace Dictation.Application.Import;

public interface ILessonRepository
{
    /// <summary>Lesson <c>ExternalId</c>s plus segment <c>ExternalKey</c>s stored for <paramref name="source"/>.</summary>
    Task<IReadOnlySet<string>> GetKnownKeysAsync(string source, CancellationToken ct);

    /// <summary>Whether a lesson, <c>Ready</c> or <c>Failed</c>, is already stored for the item.</summary>
    Task<bool> ExistsAsync(string source, string externalId, CancellationToken ct);

    /// <summary>Adds and saves a lesson; returns false on a unique-index violation.</summary>
    Task<bool> TryAddAsync(Lesson lesson, CancellationToken ct);
}
