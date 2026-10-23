namespace Dictation.Infrastructure.Transcription.AssemblyAi;

public sealed class AssemblyAiOptions
{
    public const string SectionName = "Transcription:AssemblyAi";

    public string BaseUrl { get; set; } = "";

    public string ApiKey { get; set; } = "";
}
