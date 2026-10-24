using Dictation.Domain.Lessons;

namespace Dictation.Application.Import.Abstractions;

/// <summary>One item as a source returns it, before mapping. Adapters derive their own item types from it.</summary>
public abstract record SourceItem(string ExternalId);

public interface IContentSource
{
    /// <summary>Constant lowercase key, e.g. <c>tatoeba</c>.</summary>
    string Key { get; }

    IReadOnlyCollection<string> SupportedLanguages { get; }

    /// <summary>Fetches up to <paramref name="max"/> new items, skipping <paramref name="knownKeys"/>.</summary>
    Task<IReadOnlyList<SourceItem>> FetchAsync(
        string language,
        IReadOnlySet<string> knownKeys,
        int max,
        CancellationToken ct);

    /// <summary>Maps one fetched item to a new <c>Discovered</c> lesson (adapter: their format to ours).</summary>
    Lesson ToLesson(SourceItem item);
}
