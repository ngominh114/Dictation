using Dictation.Application.Pipelines;
using Dictation.Domain.Lessons;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dictation.Application.Import;

/// <summary>
/// The import business for one source: fetch its new items, then import each one. An item already stored,
/// <c>Ready</c> or <c>Failed</c>, is skipped, so re-runs are idempotent and a failure is final.
/// </summary>
public sealed class ImportLessonsUseCase
{
    private readonly ILessonRepository _repository;
    private readonly Pipeline<Lesson> _pipeline;
    private readonly IOptions<ImportOptions> _options;
    private readonly ILogger<ImportLessonsUseCase> _logger;

    public ImportLessonsUseCase(
        ILessonRepository repository,
        Pipeline<Lesson> pipeline,
        IOptions<ImportOptions> options,
        ILogger<ImportLessonsUseCase> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(IContentSource source, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(source);

        var options = _options.Value;
        var languages = (options.Languages ?? [])
            .Select(l => l.Trim())
            .Where(l => source.SupportedLanguages.Contains(l, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var language in languages)
        {
            IReadOnlyList<SourceItem> items;
            try
            {
                var knownKeys = await _repository.GetKnownKeysAsync(source.Key, ct).ConfigureAwait(false);
                items = await source.FetchAsync(language, knownKeys, options.MaxPerSource, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Fetching from {Source} failed; skipping it for this run", source.Key);
                return;
            }

            foreach (var item in items)
            {
                await ImportItemAsync(source, item, ct).ConfigureAwait(false);
            }
        }
    }

    private async Task ImportItemAsync(IContentSource source, SourceItem item, CancellationToken ct)
    {
        if (await _repository.ExistsAsync(source.Key, item.ExternalId, ct).ConfigureAwait(false))
        {
            return;                                                             // idempotent re-runs
        }

        Lesson? lesson = null;
        try
        {
            lesson = source.ToLesson(item);                                     // adapter: their format to ours
            lesson = await _pipeline.RunAsync(lesson, ct).ConfigureAwait(false); // business steps
            await _repository.TryAddAsync(lesson, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Log identifiers only, never payload content.
            _logger.LogWarning(ex, "Import failed for {Source}/{ExternalId}", source.Key, item.ExternalId);
            if (lesson is not null)
            {
                lesson.Fail();
                await _repository.TryAddAsync(lesson, ct).ConfigureAwait(false);
            }
        }
    }
}
