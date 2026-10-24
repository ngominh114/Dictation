using Dictation.Domain.Lessons;

namespace Dictation.Application.Import.Abstractions;

public interface IContentSource
{
    /// <summary>Constant lowercase key, e.g. <c>tatoeba</c>.</summary>
    string Key { get; }

    IReadOnlyCollection<string> SupportedLanguages { get; }

    /// <summary>
    /// Fetches up to <paramref name="max"/> new <c>Discovered</c> lessons, skipping <paramref name="knownKeys"/>
    /// (adapter: their format to ours).
    /// </summary>
    Task<IReadOnlyList<Lesson>> FetchAsync(
        string language,
        IReadOnlySet<string> knownKeys,
        int max,
        CancellationToken ct);
}
