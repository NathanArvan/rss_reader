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
