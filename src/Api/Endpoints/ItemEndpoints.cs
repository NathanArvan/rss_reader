using Microsoft.EntityFrameworkCore;
using RssReader.Api.Data;
using RssReader.Api.Models;
using System.Text;

namespace RssReader.Api.Endpoints;

/// <summary>
/// Endpoints acting on individual items. Mark-all-as-read (source/category/global)
/// is deliberately out of scope here — it arrives in step 8.
/// </summary>
public static class ItemEndpoints
{
    public static IEndpointRouteBuilder MapItemEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/items");

        // Fetch a single item by id — used by the PWA's detail view on deep-link / refresh.
        group.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            var item = await db.Items.FindAsync(id);
            return item is null ? Results.NotFound() : Results.Ok(ItemResponse.From(item));
        });

        // `read` defaults to true; pass ?read=false to mark an item unread again.
        group.MapPost("/{id:int}/read", async (int id, AppDbContext db, bool? read) =>
        {
            var item = await db.Items.FindAsync(id);
            if (item is null)
            {
                return Results.NotFound();
            }

            item.IsRead = read ?? true;
            await db.SaveChangesAsync();
            return Results.Ok(ItemResponse.From(item));
        });

        group.MapPost("/{id:int}/keep", async (int id, AppDbContext db) =>
        {
            var item = await db.Items.FindAsync(id);
            if (item is null)
            {
                return Results.NotFound();
            }

            item.TriageState = TriageState.Kept;
            await db.SaveChangesAsync();
            return Results.Ok(ItemResponse.From(item));
        });

        group.MapPost("/{id:int}/dismiss", async (int id, AppDbContext db) =>
        {
            var item = await db.Items.FindAsync(id);
            if (item is null)
            {
                return Results.NotFound();
            }

            item.TriageState = TriageState.Dismissed;
            await db.SaveChangesAsync();
            return Results.Ok(ItemResponse.From(item));
        });

        // Phase 2 step 9: paginated cross-source item query.
        // Maps to three views: Inbox (?triage=New), Interested (?triage=Kept), Everything (no triage param).
        group.MapGet("", async (
            AppDbContext db,
            TriageState[]? triage,
            int? sourceId,
            int limit = 50,
            string? cursor = null) =>
        {
            limit = Math.Clamp(limit, 1, 100);

            var query = db.Items.AsQueryable();

            if (triage is { Length: > 0 })
                query = query.Where(i => triage.Contains(i.TriageState));

            if (sourceId.HasValue)
                query = query.Where(i => i.SourceId == sourceId.Value);

            var cp = DecodeCursor(cursor);
            if (cp is { } c)
                query = query.Where(i =>
                    (i.PublishedUtc ?? i.FetchedUtc) < c.Date ||
                    ((i.PublishedUtc ?? i.FetchedUtc) == c.Date && i.Id < c.Id));

            var fetched = await query
                .OrderByDescending(i => i.PublishedUtc ?? i.FetchedUtc)
                .ThenByDescending(i => i.Id)
                .Take(limit + 1)
                .ToListAsync();

            var hasMore = fetched.Count > limit;
            if (hasMore) fetched.RemoveAt(fetched.Count - 1);

            string? nextCursor = hasMore
                ? EncodeCursor(fetched[^1].PublishedUtc ?? fetched[^1].FetchedUtc, fetched[^1].Id)
                : null;

            return Results.Ok(new ItemsPageResponse(
                fetched.Select(ItemResponse.From).ToList(),
                hasMore,
                nextCursor));
        });

        return routes;
    }

    private static string EncodeCursor(DateTime effectiveDate, int id) =>
        Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{effectiveDate.Ticks}|{id}"));

    private static (DateTime Date, int Id)? DecodeCursor(string? cursor)
    {
        if (cursor is null) return null;
        try
        {
            var s = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var pipe = s.IndexOf('|');
            if (pipe < 0) return null;
            return (new DateTime(long.Parse(s[..pipe]), DateTimeKind.Utc), int.Parse(s[(pipe + 1)..]));
        }
        catch { return null; }
    }
}

/// <summary>Response shape for a feed item. No engagement metrics — design doc default.</summary>
public record ItemResponse(
    int Id,
    int SourceId,
    string? Guid,
    string Url,
    string Title,
    string? Author,
    string? Summary,
    DateTime? PublishedUtc,
    DateTime FetchedUtc,
    bool IsRead,
    TriageState TriageState)
{
    // TODO (frontend session): add triageState field + TriageState enum to api.models.ts
    public static ItemResponse From(Item i) => new(
        i.Id, i.SourceId, i.Guid, i.Url, i.Title, i.Author, i.Summary,
        i.PublishedUtc, i.FetchedUtc, i.IsRead, i.TriageState);
}

public record ItemsPageResponse(
    IReadOnlyList<ItemResponse> Items,
    bool HasMore,
    string? NextCursor);
