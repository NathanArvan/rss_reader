namespace RssReader.Api.Models;

/// <summary>
/// A subscribed feed. Created when the user adds a feed URL; later steps fill in
/// the fetch-bookkeeping fields. Columns marked "[fwd: stepN]" are unused in
/// Phase 1 Step 2 but included now so the fetcher (step 3) and categories
/// (step 6) don't require a schema migration.
/// </summary>
public class Source
{
    public int Id { get; set; }

    /// <summary>The feed URL we poll. Unique across sources.</summary>
    public required string FeedUrl { get; set; }

    /// <summary>Display title; user-supplied on add, later filled/overwritten from the feed.</summary>
    public required string Title { get; set; }

    /// <summary>Link to the human-facing site, when known.</summary>
    public string? SiteUrl { get; set; }

    public SourceType Type { get; set; } = SourceType.Rss;

    /// <summary>User-defined category. Free-form string for now — no category table yet. [fwd: 6]</summary>
    public string? Category { get; set; }

    public DateTime AddedUtc { get; set; }

    public DateTime? LastFetchedUtc { get; set; } // [fwd: 3]

    public string? ETag { get; set; } // [fwd: 3] conditional-GET caching
    public string? LastModified { get; set; } // [fwd: 3] conditional-GET caching

    public string? LastError { get; set; } // [fwd: 3] fetch failure / backoff
    public DateTime? LastErrorUtc { get; set; } // [fwd: 3]

    public ICollection<Item> Items { get; set; } = new List<Item>();
}
