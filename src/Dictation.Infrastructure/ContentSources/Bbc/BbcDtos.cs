namespace Dictation.Infrastructure.ContentSources.Bbc;

/// <summary>One <c>&lt;item&gt;</c> of the BBC RSS feed, as sent.</summary>
internal sealed record BbcFeedItemDto(string Guid, string Title, DateTimeOffset PublishedAt)
{
    /// <summary>Air date as published (in the feed's own offset).</summary>
    public DateOnly AirDate => DateOnly.FromDateTime(PublishedAt.DateTime);
}
