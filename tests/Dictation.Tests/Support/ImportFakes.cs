using Dictation.Application.Import;
using Dictation.Application.Pipelines;
using Dictation.Domain.Lessons;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Dictation.Tests.Support;

/// <summary>In-memory <see cref="ILessonRepository"/>.</summary>
public sealed class InMemoryLessonRepository : ILessonRepository
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

/// <summary>A source whose items are ready-made lessons, so a test controls exactly what gets imported.</summary>
public sealed class StubSource(string key, params Lesson[] lessons) : IContentSource
{
    public string Key => key;

    public IReadOnlyCollection<string> SupportedLanguages { get; } = ["en"];

    public Task<IReadOnlyList<SourceItem>> FetchAsync(string language, IReadOnlySet<string> knownKeys, int max, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<SourceItem>>(lessons.Select(l => new LessonItem(l)).ToList());

    public Lesson ToLesson(SourceItem item) => ((LessonItem)item).Lesson;

    private sealed record LessonItem(Lesson Lesson) : SourceItem(Lesson.ExternalId);
}

/// <summary>A pipeline over the given steps, in order.</summary>
public sealed class StubPipeline(params IPipelineStep<Lesson>[] stepList) : Pipeline<Lesson>
{
    protected override void ConfigureSteps(List<IPipelineStep<Lesson>> steps) => steps.AddRange(stepList);
}

public static class TestImport
{
    /// <summary>The real lesson pipeline over <paramref name="provider"/> (a fake by default), polling without delay.</summary>
    public static LessonPipeline Pipeline(ITranscriptionProvider? provider = null) =>
        new(provider ?? new FakeTranscriptionProvider(), Options.Create(new TranscriptionOptions { PollIntervalSeconds = 0 }));

    public static ImportLessonsUseCase UseCase(InMemoryLessonRepository repository, Pipeline<Lesson> pipeline) =>
        new(
            repository,
            pipeline,
            Options.Create(new ImportOptions { Enabled = true, Languages = ["en"], MaxPerSource = 5 }),
            NullLogger<ImportLessonsUseCase>.Instance);
}
