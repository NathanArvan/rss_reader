using Microsoft.EntityFrameworkCore;
using RssReader.Api.Data;
using RssReader.Api.Models;

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

        return routes;
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
