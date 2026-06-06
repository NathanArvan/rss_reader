using Microsoft.EntityFrameworkCore;
using RssReader.Api.Data;

namespace RssReader.Api.Services;

public sealed class FeedFetchingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FeedFetchingService> _logger;
    private readonly TimeSpan _interval;

    public FeedFetchingService(
        IServiceScopeFactory scopeFactory,
        ILogger<FeedFetchingService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = TimeSpan.FromMinutes(
            configuration.GetValue("FeedFetching:IntervalMinutes", 60));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Feed fetching service started. Interval: {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FetchAllAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Unhandled error in feed fetching cycle");
            }

            try { await Task.Delay(_interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }

        _logger.LogInformation("Feed fetching service stopped.");
    }

    private async Task FetchAllAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var fetcher = scope.ServiceProvider.GetRequiredService<IFeedFetcher>();

        var sources = await db.Sources.ToListAsync(ct);
        _logger.LogInformation("Polling {Count} source(s)", sources.Count);

        foreach (var source in sources)
        {
            if (ct.IsCancellationRequested) break;
            await fetcher.FetchAsync(source, ct);
        }
    }
}
