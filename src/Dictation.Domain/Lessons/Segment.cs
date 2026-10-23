namespace Dictation.Domain.Lessons;

/// <summary>One sentence of a lesson. <see cref="Index"/> is 0-based and assigned only by <see cref="Lesson"/>.</summary>
public sealed class Segment
{
    internal Segment(
        string source,
        int index,
        string? externalKey,
        string text,
        string audioUrl,
        int startMs,
        int? endMs)
    {
        Source = source;
        Index = index;
        ExternalKey = externalKey;
        Text = text;
        AudioUrl = audioUrl;
        StartMs = startMs;
        EndMs = endMs;
    }

    // Used by EF Core materialization.
    private Segment()
    {
    }

    public int Index { get; private set; }

    public string? ExternalKey { get; private set; }

    public string Text { get; private set; } = null!;

    public string AudioUrl { get; private set; } = null!;

    public int StartMs { get; private set; }

    /// <summary>Exclusive end; null means play to the end of the clip.</summary>
    public int? EndMs { get; private set; }

    /// <summary>
    /// Denormalized copy of the owning lesson's <see cref="Lesson.Source"/>, so that
    /// <c>(Source, ExternalKey)</c> can be uniquely indexed. Set only by <see cref="Lesson"/>.
    /// </summary>
    internal string Source { get; private set; } = null!;
}
