using Dictation.Application.Import;
using Dictation.Application.Import.Abstractions;
using Dictation.Application.Import.Configurations;
using Dictation.Application.Pipelines;
using Dictation.Domain.Lessons;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Dictation.Tests.Support;

/// <summary>In-memory <see cref="ILessonImportRepository"/>.</summary>
public sealed class InMemoryLessonImportRepository : ILessonImportRepository
{
    public List<Lesson> Lessons { get; } = [];

    public Task<IReadOnlySet<string>> GetKnownKeysAsync(string source, CancellationToken ct) =>
        Task.FromResult<IReadOnlySet<string>>(Lessons
            .Where(l => l.Source == source)
            .SelectMany(l => l.Segments.Select(s => s.ExternalKey).Append(l.ExternalId))
            .OfType<string>()
            .ToHashSet());

    public Task<bool> ExistsAsync(string source, string externalId, CancellationToken ct) =>
        Task.FromResult(Lessons.Any(l => l.Source == source && l.ExternalId == externalId));

    public Task<bool> TryAddAsync(Lesson lesson, CancellationToken ct)
    {
        Lessons.Add(lesson);
        return Task.FromResult(true);
    }
}

public sealed class StubStep(Func<Lesson, bool> appliesTo, Func<Lesson, Lesson>? run = null)
    : IPipelineStep<Lesson>
{
    public int Runs { get; private set; }

    public bool AppliesTo(Lesson item) => appliesTo(item);

    public Task<Lesson> RunAsync(Lesson item, CancellationToken ct)
    {
        Runs++;
        return Task.FromResult(run is null ? item : run(item));
    }
}

/// <summary>
/// A source that returns the given lessons, or throws <see cref="Error"/> when set, so a test controls exactly what
/// gets imported.
/// </summary>
public sealed class StubSource(string key, params Lesson[] lessons) : IContentSource
{
    public string Key => key;

    public Exception? Error { get; init; }

    public IReadOnlyCollection<string> SupportedLanguages { get; } = ["en"];

    public Task<IReadOnlyList<Lesson>> FetchAsync(string language, IReadOnlySet<string> knownKeys, int max, CancellationToken ct) =>
        Error is null
            ? Task.FromResult<IReadOnlyList<Lesson>>(lessons)
            : Task.FromException<IReadOnlyList<Lesson>>(Error);
}

/// <summary>A pipeline over the given steps, in order.</summary>
public sealed class StubPipeline(params IPipelineStep<Lesson>[] stepList) : Pipeline<Lesson>
{
    protected override void ConfigureSteps(List<IPipelineStep<Lesson>> steps) => steps.AddRange(stepList);
}

public static class TestImport
{
    /// <summary>The real lesson pipeline over <paramref name="provider"/> (a fake by default).</summary>
    public static LessonPipeline Pipeline(ITranscriptionProvider? provider = null) =>
        new(provider ?? new FakeTranscriptionProvider());

    public static ImportLessonsUseCase UseCase(InMemoryLessonImportRepository repository, Pipeline<Lesson> pipeline) =>
        new(
            repository,
            pipeline,
            Options.Create(new ImportOptions { Enabled = true, Languages = ["en"], MaxPerSource = 5 }),
            NullLogger<ImportLessonsUseCase>.Instance);
}
