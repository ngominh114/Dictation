namespace Dictation.Domain.Lessons;

public sealed class Lesson
{
    private readonly List<Segment> _segments = [];

    private Lesson(
        string source,
        string externalId,
        string title,
        string language,
        string sourceUrl,
        string? sourceAudioUrl,
        DateTime createdAt)
    {
        Id = Guid.NewGuid();
        Source = source;
        ExternalId = externalId;
        Title = title;
        Language = language;
        SourceUrl = sourceUrl;
        SourceAudioUrl = sourceAudioUrl;
        Status = LessonStatus.Discovered;
        CreatedAt = ToUtc(createdAt);
    }

    // Used by EF Core materialization.
    private Lesson()
    {
    }

    /// <summary>Creates a newly discovered lesson (status <see cref="LessonStatus.Discovered"/>).</summary>
    public static Lesson CreateDiscovered(
        string source,
        string externalId,
        string title,
        string language,
        string sourceUrl,
        string? sourceAudioUrl,
        DateTime createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(sourceUrl);

        return new Lesson(
            source,
            externalId,
            title,
            language,
            sourceUrl,
            string.IsNullOrWhiteSpace(sourceAudioUrl) ? null : sourceAudioUrl,
            createdAt);
    }

    /// <summary>True when the lesson is <see cref="LessonStatus.Ready"/> or <see cref="LessonStatus.Failed"/>.</summary>
    public bool IsFinished => Status is LessonStatus.Ready or LessonStatus.Failed;

    /// <summary>Marks the lesson <see cref="LessonStatus.Ready"/>. Throws <see cref="InvalidOperationException"/> without segments.</summary>
    public void Complete()
    {
        if (_segments.Count == 0)
        {
            throw new InvalidOperationException("A lesson cannot be completed without segments.");
        }

        Status = LessonStatus.Ready;
    }

    /// <summary>Records the transcription job and marks the lesson <see cref="LessonStatus.Transcribing"/>.</summary>
    public void StartTranscription(string provider, string jobId, DateTime startedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);

        TranscriptionProvider = provider;
        TranscriptionJobId = jobId;
        TranscriptionStartedAt = ToUtc(startedAt);
        Status = LessonStatus.Transcribing;
    }

    /// <summary>Marks the lesson <see cref="LessonStatus.Failed"/>.</summary>
    public void Fail() => Status = LessonStatus.Failed;

    public Guid Id { get; private set; }

    public string Source { get; private set; } = null!;

    public string ExternalId { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    public string Language { get; private set; } = null!;

    public string SourceUrl { get; private set; } = null!;

    public string? SourceAudioUrl { get; private set; }

    public LessonStatus Status { get; private set; }

    public string? TranscriptionProvider { get; private set; }

    public string? TranscriptionJobId { get; private set; }

    public DateTime? TranscriptionStartedAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    /// <summary>Segments in ascending <see cref="Segment.Index"/> order.</summary>
    public IReadOnlyList<Segment> Segments => _segments.OrderBy(s => s.Index).ToList().AsReadOnly();

    /// <summary>
    /// Appends a segment, assigning the next 0-based <see cref="Segment.Index"/> and copying
    /// this lesson's <see cref="Source"/> onto the segment's Source column.
    /// </summary>
    public Segment AddSegment(
        string? externalKey,
        string text,
        string audioUrl,
        int startMs,
        int? endMs)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(audioUrl);

        var index = _segments.Count == 0 ? 0 : _segments.Max(s => s.Index) + 1;
        var segment = new Segment(Source, index, externalKey, text, audioUrl, startMs, endMs);
        _segments.Add(segment);
        return segment;
    }

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
