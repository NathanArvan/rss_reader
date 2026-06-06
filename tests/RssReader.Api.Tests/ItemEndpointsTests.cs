using System.Net;
using System.Net.Http.Json;
using RssReader.Api.Models;

namespace RssReader.Api.Tests;

public class ItemEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ItemEndpointsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Creates a source and seeds it with the given items, returning the source id.</summary>
    private async Task<int> SeedSourceWithItems(params Item[] items)
    {
        var source = new Source
        {
            FeedUrl = $"https://example.com/items-{Guid.NewGuid():N}.xml",
            Title = "Seeded",
            AddedUtc = DateTime.UtcNow
        };

        await _factory.SeedAsync(db =>
        {
            db.Sources.Add(source);
            foreach (var item in items)
            {
                item.Source = source;
                source.Items.Add(item);
            }
            return Task.CompletedTask;
        });

        return source.Id;
    }

    private static Item NewItem(string title, DateTime publishedUtc, bool isRead = false) => new()
    {
        Url = $"https://example.com/{Guid.NewGuid():N}",
        Title = title,
        PublishedUtc = publishedUtc,
        FetchedUtc = DateTime.UtcNow,
        IsRead = isRead
    };

    [Fact]
    public async Task List_items_returns_seeded_items_newest_first()
    {
        var older = NewItem("Older", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var newer = NewItem("Newer", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        var sourceId = await SeedSourceWithItems(older, newer);

        var client = _factory.CreateClient();
        var items = await client.GetFromJsonAsync<List<ItemDto>>($"/api/sources/{sourceId}/items");

        Assert.NotNull(items);
        Assert.Equal(2, items!.Count);
        Assert.Equal("Newer", items[0].Title);
        Assert.Equal("Older", items[1].Title);
    }

    [Fact]
    public async Task List_items_unread_only_filters_read_items()
    {
        var read = NewItem("Read one", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), isRead: true);
        var unread = NewItem("Unread one", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        var sourceId = await SeedSourceWithItems(read, unread);

        var client = _factory.CreateClient();
        var items = await client.GetFromJsonAsync<List<ItemDto>>($"/api/sources/{sourceId}/items?unreadOnly=true");

        Assert.NotNull(items);
        Assert.Single(items!);
        Assert.Equal("Unread one", items![0].Title);
    }

    [Fact]
    public async Task List_items_honors_limit()
    {
        var items = Enumerable.Range(0, 5)
            .Select(i => NewItem($"Item {i}", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i)))
            .ToArray();
        var sourceId = await SeedSourceWithItems(items);

        var client = _factory.CreateClient();
        var page = await client.GetFromJsonAsync<List<ItemDto>>($"/api/sources/{sourceId}/items?limit=2");

        Assert.NotNull(page);
        Assert.Equal(2, page!.Count);
    }

    [Fact]
    public async Task List_items_for_unknown_source_is_not_found()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/sources/999999/items");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Mark_item_read_then_unread()
    {
        var item = NewItem("Toggle me", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        await SeedSourceWithItems(item);

        var client = _factory.CreateClient();

        var read = await client.PostAsync($"/api/items/{item.Id}/read", null);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        var readDto = await read.Content.ReadFromJsonAsync<ItemDto>();
        Assert.True(readDto!.IsRead);

        var unread = await client.PostAsync($"/api/items/{item.Id}/read?read=false", null);
        Assert.Equal(HttpStatusCode.OK, unread.StatusCode);
        var unreadDto = await unread.Content.ReadFromJsonAsync<ItemDto>();
        Assert.False(unreadDto!.IsRead);
    }

    [Fact]
    public async Task Mark_unknown_item_read_is_not_found()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/items/999999/read", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record ItemDto(int Id, int SourceId, string Url, string Title, bool IsRead);
}
