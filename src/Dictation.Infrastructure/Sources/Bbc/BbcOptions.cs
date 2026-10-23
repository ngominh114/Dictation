namespace Dictation.Infrastructure.Sources.Bbc;

public sealed class BbcOptions
{
    public const string SectionName = "Sources:Bbc";

    public string FeedUrl { get; set; } = "";

    public string PageBaseUrl { get; set; } = "";
}
