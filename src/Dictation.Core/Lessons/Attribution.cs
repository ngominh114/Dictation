namespace Dictation.Core.Lessons;

/// <summary>Clip attribution (author, licence, link). Stored as an EF owned type (3 columns).</summary>
public sealed class Attribution
{
    public Attribution(string author, string license, string url)
    {
        ArgumentNullException.ThrowIfNull(author);
        ArgumentNullException.ThrowIfNull(license);
        ArgumentNullException.ThrowIfNull(url);

        Author = author;
        License = license;
        Url = url;
    }

    // Used by EF Core materialization.
    private Attribution()
    {
    }

    public string Author { get; private set; } = null!;

    public string License { get; private set; } = null!;

    public string Url { get; private set; } = null!;
}
