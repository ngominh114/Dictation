namespace Dictation.Infrastructure.ContentSources.Tatoeba;

public sealed class TatoebaOptions
{
    public const string SectionName = "Sources:Tatoeba";

    public string BaseUrl { get; set; } = "";

    public int GroupSize { get; set; }

    public string? WordCount { get; set; }

    public int MaxPages { get; set; }
}
