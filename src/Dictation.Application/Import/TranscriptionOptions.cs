namespace Dictation.Application.Import;

public sealed class TranscriptionOptions
{
    public const string SectionName = "Transcription";

    public int PollIntervalSeconds { get; set; } = 5;
}
