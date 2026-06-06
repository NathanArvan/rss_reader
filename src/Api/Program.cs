using Microsoft.EntityFrameworkCore;
using RssReader.Api.Data;
using RssReader.Api.Endpoints;
using RssReader.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHttpClient<FeedFetcher>(c =>
{
    c.DefaultRequestHeaders.UserAgent.ParseAdd("RssReader/1.0");
    c.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddTransient<IFeedFetcher>(sp => sp.GetRequiredService<FeedFetcher>());
builder.Services.AddHostedService<FeedFetchingService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Apply any pending migrations on startup so the SQLite file is created automatically.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Health/plumbing check: proves the API is up and can reach the database.
app.MapGet("/api/ping", async (AppDbContext db) =>
{
    var dbConnected = await db.Database.CanConnectAsync();
    return Results.Ok(new
    {
        status = "ok",
        dbConnected,
        utc = DateTime.UtcNow
    });
});

// Feed source and item endpoints (Phase 1, Step 2).
app.MapSourceEndpoints();
app.MapItemEndpoints();

// Serve the Angular PWA (single deployable). In production the built app lives in
// wwwroot; client-side routes fall through to index.html.
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();

// Exposed so the test project (WebApplicationFactory) can reference the entry point.
public partial class Program { }
