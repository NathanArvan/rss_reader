namespace RssReader.Api.Models;

/// <summary>
/// A single feed entry belonging to a <see cref="Source"/>. In Phase 1 Step 2
/// items are only created in tests; the real fetcher populates them in step 3.
/// </summary>
public class Item
{
    public int Id { get; set; }

    public int SourceId { get; set; }
    public Source? Source { get; set; }

    /// <summary>
    /// Canonical feed item identifier. Nullable because real-world feeds omit it;
    /// the step-3 fetcher falls back to URL / <see cref="ContentHash"/> for dedup.
    /// </summary>
    public string? Guid { get; set; }

    /// <summary>Hash of the item content, used for dedup when <see cref="Guid"/> is missing. [fwd: 3]</summary>
    public string? ContentHash { get; set; }

    public required string Url { get; set; }
    public required string Title { get; set; }
    public string? Author { get; set; }

    /// <summary>Feed-provided description/excerpt (may contain HTML).</summary>
    public string? Summary { get; set; }

    public DateTime? PublishedUtc { get; set; }
    public DateTime FetchedUtc { get; set; }

    public bool IsRead { get; set; }
}
