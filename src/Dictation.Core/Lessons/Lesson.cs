namespace Dictation.Core.Lessons;

public sealed class Lesson
{
    private readonly List<Segment> _segments = [];

    /// <summary>
    /// Construction hook used by story 1.1 tests. Public lifecycle factories
    /// (<c>CreateReady</c>, <c>CreateDiscovered</c>, ...) arrive in stories 1.2 / Epic 2.
    /// </summary>
    internal Lesson(
        string source,
        string externalId,
        string title,
        string language,
        string sourceUrl,
        LessonStatus status,
        DateTime createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(sourceUrl);

        Id = Guid.NewGuid();
        Source = source;
        ExternalId = externalId;
        Title = title;
        Language = language;
        SourceUrl = sourceUrl;
        Status = status;
        CreatedAt = ToUtc(createdAt);
    }

    // Used by EF Core materialization.
    private Lesson()
    {
    }

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
    internal Segment AddSegment(
        string? externalKey,
        string text,
        string audioUrl,
        int startMs,
        int? endMs,
        Attribution? attribution)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(audioUrl);

        var index = _segments.Count == 0 ? 0 : _segments.Max(s => s.Index) + 1;
        var segment = new Segment(Source, index, externalKey, text, audioUrl, startMs, endMs, attribution);
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
