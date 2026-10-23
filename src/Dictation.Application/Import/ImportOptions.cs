namespace Dictation.Application.Import;

public sealed class ImportOptions
{
    public const string SectionName = "Import";

    public bool Enabled { get; set; }

    public List<string> Languages { get; set; } = [];

    public int MaxPerSource { get; set; }
}
