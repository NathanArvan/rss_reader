using RssReader.Api.Data;
using RssReader.Api.Models;

namespace RssReader.Api.Endpoints;

public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/settings");

        // Returns the app-wide last-opened timestamp; null until the client first advances it.
        group.MapGet("", async (AppDbContext db) =>
        {
            var settings = await db.AppSettings.FindAsync(1);
            return Results.Ok(new AppSettingsResponse(settings?.LastOpenedUtc));
        });

        // Advances last-opened to now. Client calls this when the user intentionally
        // starts a reading session — never auto-called on app open.
        group.MapPost("/last-opened", async (AppDbContext db) =>
        {
            var settings = await db.AppSettings.FindAsync(1);
            if (settings is not null)
            {
                settings.LastOpenedUtc = DateTime.UtcNow;
            }
            else
            {
                // Defensive: HasData seeds Id=1, but handle missing row gracefully.
                db.AppSettings.Add(new AppSettings { Id = 1, LastOpenedUtc = DateTime.UtcNow });
            }
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return routes;
    }
}

public record AppSettingsResponse(DateTime? LastOpenedUtc);
