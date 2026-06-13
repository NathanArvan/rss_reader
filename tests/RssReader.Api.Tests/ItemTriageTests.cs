using System.Net;
using System.Net.Http.Json;
using RssReader.Api.Models;

namespace RssReader.Api.Tests;

public class ItemTriageTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ItemTriageTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(int sourceId, int itemId)> SeedOneItem(TriageState initialState = TriageState.New)
    {
        var source = new Source
        {
            FeedUrl = $"https://example.com/triage-{Guid.NewGuid():N}.xml",
            Title = "Triage Source",
            AddedUtc = DateTime.UtcNow
        };
        var item = new Item
        {
            Url = $"https://example.com/{Guid.NewGuid():N}",
            Title = "Test Item",
            FetchedUtc = DateTime.UtcNow,
            TriageState = initialState
        };

        await _factory.SeedAsync(db =>
        {
            db.Sources.Add(source);
            item.Source = source;
            db.Items.Add(item);
            return Task.CompletedTask;
        });

        return (source.Id, item.Id);
    }

    private async Task<int> SeedSourceWithItems(params (string title, TriageState state)[] specs)
    {
        var source = new Source
        {
            FeedUrl = $"https://example.com/triage-{Guid.NewGuid():N}.xml",
            Title = "Bulk Triage Source",
            AddedUtc = DateTime.UtcNow
        };

        await _factory.SeedAsync(db =>
        {
            db.Sources.Add(source);
            foreach (var (title, state) in specs)
            {
                db.Items.Add(new Item
                {
                    Url = $"https://example.com/{Guid.NewGuid():N}",
                    Title = title,
                    FetchedUtc = DateTime.UtcNow,
                    Source = source,
                    TriageState = state
                });
            }
            return Task.CompletedTask;
        });

        return source.Id;
    }

    private sealed record ItemTriageDto(int Id, int SourceId, string Url, string Title, bool IsRead, TriageState TriageState);
    private sealed record DismissAllResponse(int Count);

    [Fact]
    public async Task Keep_new_item_sets_triage_state_to_Kept()
    {
        var (_, itemId) = await SeedOneItem(TriageState.New);
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"/api/items/{itemId}/keep", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ItemTriageDto>();
        Assert.Equal(TriageState.Kept, dto!.TriageState);
    }

    [Fact]
    public async Task Dismiss_new_item_sets_triage_state_to_Dismissed()
    {
        var (_, itemId) = await SeedOneItem(TriageState.New);
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"/api/items/{itemId}/dismiss", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ItemTriageDto>();
        Assert.Equal(TriageState.Dismissed, dto!.TriageState);
    }

    [Fact]
    public async Task Keep_dismissed_item_sets_triage_state_to_Kept()
    {
        var (_, itemId) = await SeedOneItem(TriageState.Dismissed);
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"/api/items/{itemId}/keep", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ItemTriageDto>();
        Assert.Equal(TriageState.Kept, dto!.TriageState);
    }

    [Fact]
    public async Task Dismiss_kept_item_sets_triage_state_to_Dismissed()
    {
        var (_, itemId) = await SeedOneItem(TriageState.Kept);
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"/api/items/{itemId}/dismiss", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ItemTriageDto>();
        Assert.Equal(TriageState.Dismissed, dto!.TriageState);
    }

    [Fact]
    public async Task Keep_unknown_item_is_not_found()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/items/999999/keep", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Dismiss_unknown_item_is_not_found()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/items/999999/dismiss", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Dismiss_all_sets_all_new_items_to_Dismissed()
    {
        var sourceId = await SeedSourceWithItems(
            ("Item A", TriageState.New),
            ("Item B", TriageState.New));
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"/api/sources/{sourceId}/items/dismiss-all", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await client.GetFromJsonAsync<List<ItemTriageDto>>($"/api/sources/{sourceId}/items");
        Assert.All(items!, i => Assert.Equal(TriageState.Dismissed, i.TriageState));
    }

    [Fact]
    public async Task Dismiss_all_does_not_change_kept_items()
    {
        var sourceId = await SeedSourceWithItems(
            ("New Item", TriageState.New),
            ("Kept Item", TriageState.Kept));
        var client = _factory.CreateClient();

        await client.PostAsync($"/api/sources/{sourceId}/items/dismiss-all", null);

        var items = await client.GetFromJsonAsync<List<ItemTriageDto>>($"/api/sources/{sourceId}/items");
        var kept = items!.Single(i => i.Title == "Kept Item");
        Assert.Equal(TriageState.Kept, kept.TriageState);
    }

    [Fact]
    public async Task Dismiss_all_does_not_affect_other_sources()
    {
        var targetSourceId = await SeedSourceWithItems(("Target New", TriageState.New));
        var otherSourceId = await SeedSourceWithItems(("Other New", TriageState.New));
        var client = _factory.CreateClient();

        await client.PostAsync($"/api/sources/{targetSourceId}/items/dismiss-all", null);

        var otherItems = await client.GetFromJsonAsync<List<ItemTriageDto>>($"/api/sources/{otherSourceId}/items");
        Assert.All(otherItems!, i => Assert.Equal(TriageState.New, i.TriageState));
    }

    [Fact]
    public async Task Dismiss_all_returns_count_of_dismissed_items()
    {
        var sourceId = await SeedSourceWithItems(
            ("New 1", TriageState.New),
            ("New 2", TriageState.New),
            ("Kept 1", TriageState.Kept));
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"/api/sources/{sourceId}/items/dismiss-all", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<DismissAllResponse>();
        Assert.Equal(2, result!.Count);
    }

    [Fact]
    public async Task Dismiss_all_on_unknown_source_is_not_found()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/sources/999999/items/dismiss-all", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
