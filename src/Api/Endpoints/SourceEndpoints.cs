using Microsoft.EntityFrameworkCore;
using RssReader.Api.Data;
using RssReader.Api.Models;

namespace RssReader.Api.Endpoints;

/// <summary>
/// CRUD endpoints for feed sources. Item listing for a source lives here too
/// since it hangs off the source route.
/// </summary>
public static class SourceEndpoints
{
    public static IEndpointRouteBuilder MapSourceEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/sources");

        group.MapGet("/", async (AppDbContext db) =>
        {
            var sources = await db.Sources
                .OrderBy(s => s.Title)
                .Select(s => SourceResponse.From(s))
                .ToListAsync();
            return Results.Ok(sources);
        });

        group.MapPost("/", async (CreateSourceRequest request, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.FeedUrl))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["feedUrl"] = ["feedUrl is required."]
                });
            }

            var feedUrl = request.FeedUrl.Trim();

            if (await db.Sources.AnyAsync(s => s.FeedUrl == feedUrl))
            {
                return Results.Conflict(new { message = "A source with this feed URL already exists." });
            }

            var source = new Source
            {
                FeedUrl = feedUrl,
                // Until the fetcher (step 3) fills in the real title, fall back to the URL.
                Title = string.IsNullOrWhiteSpace(request.Title) ? feedUrl : request.Title!.Trim(),
                SiteUrl = request.SiteUrl,
                Type = request.Type ?? SourceType.Rss,
                Category = request.Category,
                AddedUtc = DateTime.UtcNow
            };

            db.Sources.Add(source);
            await db.SaveChangesAsync();

            return Results.Created($"/api/sources/{source.Id}", SourceResponse.From(source));
        });

        group.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
        {
            var source = await db.Sources.FindAsync(id);
            if (source is null)
            {
                return Results.NotFound();
            }

            // Items are removed via cascade delete configured in AppDbContext.
            db.Sources.Remove(source);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapGet("/{id:int}/items", async (
            int id,
            AppDbContext db,
            bool? unreadOnly,
            int? limit,
            DateTime? before) =>
        {
            if (!await db.Sources.AnyAsync(s => s.Id == id))
            {
                return Results.NotFound();
            }

            var query = db.Items.Where(i => i.SourceId == id);

            if (unreadOnly == true)
            {
                query = query.Where(i => !i.IsRead);
            }

            // Explicit paging — the design doc forbids infinite scroll. `before`
            // pages into older items by published date.
            if (before is not null)
            {
                query = query.Where(i => i.PublishedUtc < before);
            }

            var take = Math.Clamp(limit ?? 50, 1, 200);

            var items = await query
                .OrderByDescending(i => i.PublishedUtc)
                .ThenByDescending(i => i.Id)
                .Take(take)
                .Select(i => ItemResponse.From(i))
                .ToListAsync();

            return Results.Ok(items);
        });

        return routes;
    }
}

/// <summary>Request body for adding a source. Only <c>FeedUrl</c> is required.</summary>
public record CreateSourceRequest(
    string FeedUrl,
    string? Title = null,
    string? SiteUrl = null,
    SourceType? Type = null,
    string? Category = null);

/// <summary>Response shape for a source (no unread counts — design doc default).</summary>
public record SourceResponse(
    int Id,
    string FeedUrl,
    string Title,
    string? SiteUrl,
    SourceType Type,
    string? Category,
    DateTime AddedUtc,
    DateTime? LastFetchedUtc)
{
    public static SourceResponse From(Source s) => new(
        s.Id, s.FeedUrl, s.Title, s.SiteUrl, s.Type, s.Category, s.AddedUtc, s.LastFetchedUtc);
}
