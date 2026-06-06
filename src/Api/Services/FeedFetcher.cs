using System.Net;
using CodeHollow.FeedReader;
using Microsoft.EntityFrameworkCore;
using RssReader.Api.Data;
using RssReader.Api.Models;

namespace RssReader.Api.Services;

public interface IFeedFetcher
{
    Task FetchAsync(Source source, CancellationToken ct);
}

public class FeedFetcher : IFeedFetcher
{
    private readonly AppDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly ILogger<FeedFetcher> _logger;

    public FeedFetcher(AppDbContext db, HttpClient httpClient, ILogger<FeedFetcher> logger)
    {
        _db = db;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task FetchAsync(Source source, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, source.FeedUrl);

        if (source.ETag is not null)
            request.Headers.TryAddWithoutValidation("If-None-Match", source.ETag);
        if (source.LastModified is not null)
            request.Headers.TryAddWithoutValidation("If-Modified-Since", source.LastModified);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            source.LastError = ex.Message;
            source.LastErrorUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            _logger.LogWarning(ex, "Network error fetching {FeedUrl}", source.FeedUrl);
            return;
        }

        if (response.StatusCode == HttpStatusCode.NotModified)
        {
            source.LastFetchedUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            _logger.LogDebug("{FeedUrl} returned 304 Not Modified", source.FeedUrl);
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            source.LastError = $"HTTP {(int)response.StatusCode}";
            source.LastErrorUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            _logger.LogWarning("HTTP {Status} fetching {FeedUrl}", (int)response.StatusCode, source.FeedUrl);
            return;
        }

        var content = await response.Content.ReadAsStringAsync(ct);

        Feed feed;
        try
        {
            feed = FeedReader.ReadFromString(content);
        }
        catch (Exception ex)
        {
            source.LastError = $"Parse error: {ex.Message}";
            source.LastErrorUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            _logger.LogWarning(ex, "Parse error for {FeedUrl}", source.FeedUrl);
            return;
        }

        // Update title if it is still the URL placeholder set by the POST endpoint.
        if (source.Title == source.FeedUrl && !string.IsNullOrWhiteSpace(feed.Title))
            source.Title = feed.Title.Trim();

        var newItems = 0;
        foreach (var feedItem in feed.Items)
        {
            var guid = string.IsNullOrEmpty(feedItem.Id) ? null : feedItem.Id;
            var url = feedItem.Link ?? string.Empty;

            var exists = guid is not null
                ? await _db.Items.AnyAsync(i => i.SourceId == source.Id && i.Guid == guid, ct)
                : await _db.Items.AnyAsync(i => i.SourceId == source.Id && i.Url == url, ct);

            if (exists) continue;

            _db.Items.Add(new Item
            {
                SourceId = source.Id,
                Guid = guid,
                Url = url,
                Title = feedItem.Title ?? "(no title)",
                Author = feedItem.Author,
                Summary = feedItem.Description,
                PublishedUtc = feedItem.PublishingDate?.ToUniversalTime(),
                FetchedUtc = DateTime.UtcNow,
                IsRead = false
            });
            newItems++;
        }

        source.LastFetchedUtc = DateTime.UtcNow;
        source.LastError = null;
        source.LastErrorUtc = null;

        var etag = response.Headers.ETag?.Tag;
        if (etag is not null) source.ETag = etag;

        var lastModified = response.Content.Headers.LastModified;
        if (lastModified is not null)
            source.LastModified = lastModified.Value.UtcDateTime.ToString("R");

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Fetched {NewItems} new item(s) from {FeedUrl}", newItems, source.FeedUrl);
    }
}
