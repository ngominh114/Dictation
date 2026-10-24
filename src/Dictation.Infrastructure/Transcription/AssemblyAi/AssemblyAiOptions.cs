namespace Dictation.Infrastructure.Transcription.AssemblyAi;

public sealed class AssemblyAiOptions
{
    public const string SectionName = "Transcription:AssemblyAi";

    public string BaseUrl { get; set; } = "";

    public string ApiKey { get; set; } = "";

    /// <summary>Seconds between status checks while a job is queued or processing (AssemblyAI allows 60 a minute).</summary>
    public int PollIntervalSeconds { get; set; } = 5;
}
