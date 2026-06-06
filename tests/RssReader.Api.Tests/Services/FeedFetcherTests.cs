using System.Net;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RssReader.Api.Data;
using RssReader.Api.Models;
using RssReader.Api.Services;

namespace RssReader.Api.Tests.Services;

public class FeedFetcherTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly FakeHttpHandler _handler;
    private readonly FeedFetcher _fetcher;

    public FeedFetcherTests()
    {
        _conn = new SqliteConnection("Data Source=:memory:");
        _conn.Open();
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_conn)
            .Options;
        _db = new AppDbContext(opts);
        _db.Database.EnsureCreated();

        _handler = new FakeHttpHandler();
        _fetcher = new FeedFetcher(_db, new HttpClient(_handler), NullLogger<FeedFetcher>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }

    // ── tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task FetchAsync_saves_items_from_feed()
    {
        _handler.SetOk(TwoItemFeed);
        var source = SeedSource();

        await _fetcher.FetchAsync(source, CancellationToken.None);

        var items = await _db.Items.Where(i => i.SourceId == source.Id).ToListAsync();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, i => i.Guid == "guid-1" && i.Title == "Item One");
        Assert.Contains(items, i => i.Guid == "guid-2" && i.Title == "Item Two");
    }

    // ── deduplication ─────────────────────────────────────────────────────────

    [Fact]
    public async Task FetchAsync_does_not_create_duplicate_items_by_guid()
    {
        _handler.SetOk(TwoItemFeed);
        var source = SeedSource();

        await _fetcher.FetchAsync(source, CancellationToken.None);
        await _fetcher.FetchAsync(source, CancellationToken.None); // second fetch same feed

        var count = await _db.Items.CountAsync(i => i.SourceId == source.Id);
        Assert.Equal(2, count); // still 2, not 4
    }

    [Fact]
    public async Task FetchAsync_does_not_create_duplicate_items_by_url_when_no_guid()
    {
        _handler.SetOk(NoGuidFeed);
        var source = SeedSource();

        await _fetcher.FetchAsync(source, CancellationToken.None);
        await _fetcher.FetchAsync(source, CancellationToken.None);

        var items = await _db.Items.Where(i => i.SourceId == source.Id).ToListAsync();
        Assert.Single(items);
        Assert.Null(items[0].Guid);
        Assert.Equal("https://example.com/no-guid-1", items[0].Url);
    }

    // ── conditional GET ───────────────────────────────────────────────────────

    [Fact]
    public async Task FetchAsync_stores_etag_from_response()
    {
        _handler.SetOk(TwoItemFeed, etag: "\"abc123\"");
        var source = SeedSource();

        await _fetcher.FetchAsync(source, CancellationToken.None);

        Assert.Equal("\"abc123\"", source.ETag);
    }

    [Fact]
    public async Task FetchAsync_sends_if_none_match_when_etag_is_stored()
    {
        var source = SeedSource();
        source.ETag = "\"abc123\"";
        await _db.SaveChangesAsync();

        _handler.SetOk(TwoItemFeed, etag: "\"abc123\"");
        await _fetcher.FetchAsync(source, CancellationToken.None);

        var lastRequest = _handler.LastRequest!;
        Assert.True(lastRequest.Headers.Contains("If-None-Match"));
        Assert.Equal("\"abc123\"", lastRequest.Headers.GetValues("If-None-Match").First());
    }

    [Fact]
    public async Task FetchAsync_handles_304_not_modified()
    {
        var source = SeedSource();
        source.ETag = "\"abc123\"";
        await _db.SaveChangesAsync();

        _handler.SetStatus(HttpStatusCode.NotModified);
        var before = DateTime.UtcNow.AddSeconds(-1);

        await _fetcher.FetchAsync(source, CancellationToken.None);

        Assert.NotNull(source.LastFetchedUtc);
        Assert.True(source.LastFetchedUtc >= before);
        Assert.Equal(0, await _db.Items.CountAsync(i => i.SourceId == source.Id));
    }

    // ── error handling ────────────────────────────────────────────────────────

    [Fact]
    public async Task FetchAsync_sets_LastError_on_network_failure()
    {
        var throwingFetcher = new FeedFetcher(
            _db,
            new HttpClient(new ThrowingHttpHandler("Connection refused")),
            NullLogger<FeedFetcher>.Instance);
        var source = SeedSource();

        await throwingFetcher.FetchAsync(source, CancellationToken.None);

        var updated = await _db.Sources.FindAsync(source.Id);
        Assert.NotNull(updated!.LastError);
        Assert.Contains("Connection refused", updated.LastError);
        Assert.NotNull(updated.LastErrorUtc);
    }

    [Fact]
    public async Task FetchAsync_sets_LastError_on_http_error_status()
    {
        _handler.SetStatus(HttpStatusCode.ServiceUnavailable);
        var source = SeedSource();

        await _fetcher.FetchAsync(source, CancellationToken.None);

        Assert.Equal("HTTP 503", source.LastError);
        Assert.NotNull(source.LastErrorUtc);
    }

    [Fact]
    public async Task FetchAsync_clears_LastError_after_successful_fetch()
    {
        var source = SeedSource();
        source.LastError = "HTTP 503";
        source.LastErrorUtc = DateTime.UtcNow.AddHours(-1);
        await _db.SaveChangesAsync();

        _handler.SetOk(TwoItemFeed);
        await _fetcher.FetchAsync(source, CancellationToken.None);

        Assert.Null(source.LastError);
        Assert.Null(source.LastErrorUtc);
    }

    [Fact]
    public async Task FetchAsync_updates_title_from_feed_when_source_used_url_as_title()
    {
        _handler.SetOk(TwoItemFeed); // feed title is "Test Feed"
        var source = SeedSource(feedUrl: "https://example.com/feed.xml");
        // SeedSource sets Title = FeedUrl when no title supplied (mirrors POST endpoint behaviour)
        Assert.Equal("https://example.com/feed.xml", source.Title);

        await _fetcher.FetchAsync(source, CancellationToken.None);

        var updated = await _db.Sources.FindAsync(source.Id);
        Assert.Equal("Test Feed", updated!.Title);
    }

    [Fact]
    public async Task FetchAsync_sets_LastFetchedUtc_on_success()
    {
        _handler.SetOk(TwoItemFeed);
        var source = SeedSource();
        Assert.Null(source.LastFetchedUtc);

        var before = DateTime.UtcNow.AddSeconds(-1);
        await _fetcher.FetchAsync(source, CancellationToken.None);

        Assert.NotNull(source.LastFetchedUtc);
        Assert.True(source.LastFetchedUtc >= before);
    }

    // ── fixtures ──────────────────────────────────────────────────────────────

    internal Source SeedSource(string? feedUrl = null, string? title = null)
    {
        feedUrl ??= "https://example.com/feed.xml";
        var source = new Source
        {
            FeedUrl = feedUrl,
            Title = title ?? feedUrl,   // no user-supplied title → URL used as placeholder
            AddedUtc = DateTime.UtcNow
        };
        _db.Sources.Add(source);
        _db.SaveChanges();
        return source;
    }

    internal const string TwoItemFeed = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
          <channel>
            <title>Test Feed</title>
            <link>https://example.com</link>
            <description>A test feed</description>
            <item>
              <title>Item One</title>
              <link>https://example.com/1</link>
              <guid>guid-1</guid>
              <pubDate>Fri, 01 Jan 2021 12:00:00 +0000</pubDate>
              <description>First item description</description>
            </item>
            <item>
              <title>Item Two</title>
              <link>https://example.com/2</link>
              <guid>guid-2</guid>
              <pubDate>Sat, 02 Jan 2021 12:00:00 +0000</pubDate>
              <description>Second item description</description>
            </item>
          </channel>
        </rss>
        """;

    internal const string NoGuidFeed = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
          <channel>
            <title>No-GUID Feed</title>
            <link>https://example.com</link>
            <description>Feed without GUIDs</description>
            <item>
              <title>Unguidded Item</title>
              <link>https://example.com/no-guid-1</link>
              <pubDate>Fri, 01 Jan 2021 12:00:00 +0000</pubDate>
              <description>No guid here</description>
            </item>
          </channel>
        </rss>
        """;
}

// ── test doubles ──────────────────────────────────────────────────────────────

internal sealed class FakeHttpHandler : HttpMessageHandler
{
    private Func<HttpRequestMessage, HttpResponseMessage> _responder =
        _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(FeedFetcherTests.TwoItemFeed, Encoding.UTF8, "application/rss+xml")
        };

    public List<HttpRequestMessage> Requests { get; } = new();
    public HttpRequestMessage? LastRequest => Requests.LastOrDefault();

    public void SetOk(string content, string? etag = null)
    {
        _responder = _ =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/rss+xml")
            };
            if (etag is not null)
                resp.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue(etag);
            return resp;
        };
    }

    public void SetStatus(HttpStatusCode status)
    {
        _responder = _ => new HttpResponseMessage(status);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Requests.Add(request);
        return Task.FromResult(_responder(request));
    }
}

internal sealed class ThrowingHttpHandler : HttpMessageHandler
{
    private readonly string _message;
    public ThrowingHttpHandler(string message) => _message = message;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => throw new HttpRequestException(_message);
}
