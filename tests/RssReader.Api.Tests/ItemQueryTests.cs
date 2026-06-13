using System.Net.Http.Json;
using RssReader.Api.Models;

namespace RssReader.Api.Tests;

public class ItemQueryTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ItemQueryTests(TestWebApplicationFactory factory) => _factory = factory;

    private async Task<int> SeedSourceWithItems(
        params (string title, TriageState state, DateTime? publishedUtc, DateTime? fetchedUtc)[] specs)
    {
        var source = new Source
        {
            FeedUrl = $"https://example.com/query-{Guid.NewGuid():N}.xml",
            Title = "Query Test Source",
            AddedUtc = DateTime.UtcNow
        };

        await _factory.SeedAsync(db =>
        {
            db.Sources.Add(source);
            foreach (var (title, state, publishedUtc, fetchedUtc) in specs)
            {
                db.Items.Add(new Item
                {
                    Url = $"https://example.com/{Guid.NewGuid():N}",
                    Title = title,
                    FetchedUtc = fetchedUtc ?? DateTime.UtcNow,
                    PublishedUtc = publishedUtc,
                    Source = source,
                    TriageState = state
                });
            }
            return Task.CompletedTask;
        });

        return source.Id;
    }

    private sealed record ItemsPage(List<ItemSummary> Items, bool HasMore, string? NextCursor);
    private sealed record ItemSummary(int Id, int SourceId, string Title, DateTime? PublishedUtc, DateTime FetchedUtc);

    private static DateTime D(int day) => new(2026, 1, day, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Query_no_filter_returns_all_items_for_source()
    {
        var sourceId = await SeedSourceWithItems(
            ("New", TriageState.New, D(1), null),
            ("Kept", TriageState.Kept, D(2), null),
            ("Dismissed", TriageState.Dismissed, D(3), null));
        var client = _factory.CreateClient();

        var page = await client.GetFromJsonAsync<ItemsPage>($"/api/items?sourceId={sourceId}");

        Assert.NotNull(page);
        Assert.Equal(3, page.Items.Count);
        Assert.False(page.HasMore);
        Assert.Null(page.NextCursor);
    }

    [Fact]
    public async Task Query_triage_New_returns_only_new_items()
    {
        var sourceId = await SeedSourceWithItems(
            ("New", TriageState.New, D(1), null),
            ("Kept", TriageState.Kept, D(2), null));
        var client = _factory.CreateClient();

        var page = await client.GetFromJsonAsync<ItemsPage>($"/api/items?triage=New&sourceId={sourceId}");

        Assert.NotNull(page);
        Assert.Single(page.Items);
        Assert.Equal("New", page.Items[0].Title);
    }

    [Fact]
    public async Task Query_triage_Kept_returns_only_kept_items()
    {
        var sourceId = await SeedSourceWithItems(
            ("New", TriageState.New, D(1), null),
            ("Kept", TriageState.Kept, D(2), null),
            ("Dismissed", TriageState.Dismissed, D(3), null));
        var client = _factory.CreateClient();

        var page = await client.GetFromJsonAsync<ItemsPage>($"/api/items?triage=Kept&sourceId={sourceId}");

        Assert.NotNull(page);
        Assert.Single(page.Items);
        Assert.Equal("Kept", page.Items[0].Title);
    }

    [Fact]
    public async Task Query_multi_triage_returns_union_of_states()
    {
        var sourceId = await SeedSourceWithItems(
            ("New", TriageState.New, D(1), null),
            ("Kept", TriageState.Kept, D(2), null),
            ("Dismissed", TriageState.Dismissed, D(3), null));
        var client = _factory.CreateClient();

        var page = await client.GetFromJsonAsync<ItemsPage>($"/api/items?triage=New&triage=Kept&sourceId={sourceId}");

        Assert.NotNull(page);
        Assert.Equal(2, page.Items.Count);
        var titles = page.Items.Select(i => i.Title).ToHashSet();
        Assert.Contains("New", titles);
        Assert.Contains("Kept", titles);
    }

    [Fact]
    public async Task Query_returns_items_newest_first()
    {
        var sourceId = await SeedSourceWithItems(
            ("Older", TriageState.New, D(1), null),
            ("Newest", TriageState.New, D(3), null),
            ("Middle", TriageState.New, D(2), null));
        var client = _factory.CreateClient();

        var page = await client.GetFromJsonAsync<ItemsPage>($"/api/items?sourceId={sourceId}");

        Assert.NotNull(page);
        Assert.Equal(3, page.Items.Count);
        Assert.Equal("Newest", page.Items[0].Title);
        Assert.Equal("Middle", page.Items[1].Title);
        Assert.Equal("Older", page.Items[2].Title);
    }

    [Fact]
    public async Task Pagination_first_page_sets_hasMore_true_and_returns_cursor()
    {
        var sourceId = await SeedSourceWithItems(
            ("A", TriageState.New, D(1), null),
            ("B", TriageState.New, D(2), null),
            ("C", TriageState.New, D(3), null));
        var client = _factory.CreateClient();

        var page = await client.GetFromJsonAsync<ItemsPage>($"/api/items?sourceId={sourceId}&limit=2");

        Assert.NotNull(page);
        Assert.Equal(2, page.Items.Count);
        Assert.True(page.HasMore);
        Assert.NotNull(page.NextCursor);
    }

    [Fact]
    public async Task Pagination_cursor_yields_remaining_items_with_no_duplicates()
    {
        var sourceId = await SeedSourceWithItems(
            ("A", TriageState.New, D(1), null),
            ("B", TriageState.New, D(2), null),
            ("C", TriageState.New, D(3), null));
        var client = _factory.CreateClient();

        var page1 = await client.GetFromJsonAsync<ItemsPage>($"/api/items?sourceId={sourceId}&limit=2");
        Assert.True(page1!.HasMore);

        var page2 = await client.GetFromJsonAsync<ItemsPage>(
            $"/api/items?sourceId={sourceId}&limit=2&cursor={page1.NextCursor}");

        Assert.NotNull(page2);
        Assert.Single(page2.Items);
        Assert.False(page2.HasMore);
        Assert.Null(page2.NextCursor);

        var page1Ids = page1.Items.Select(i => i.Id).ToHashSet();
        Assert.DoesNotContain(page2.Items[0].Id, page1Ids);
    }

    [Fact]
    public async Task Pagination_when_all_fit_on_one_page_hasMore_is_false()
    {
        var sourceId = await SeedSourceWithItems(
            ("A", TriageState.New, D(1), null),
            ("B", TriageState.New, D(2), null));
        var client = _factory.CreateClient();

        var page = await client.GetFromJsonAsync<ItemsPage>($"/api/items?sourceId={sourceId}&limit=50");

        Assert.NotNull(page);
        Assert.Equal(2, page.Items.Count);
        Assert.False(page.HasMore);
        Assert.Null(page.NextCursor);
    }

    [Fact]
    public async Task Ordering_null_published_date_falls_back_to_fetched_utc()
    {
        // Item with null PublishedUtc but recent FetchedUtc should sort above
        // an item with an older explicit PublishedUtc.
        var now = new DateTime(2026, 6, 13, 12, 0, 0, DateTimeKind.Utc);
        var sourceId = await SeedSourceWithItems(
            ("No Date", TriageState.New, null, now),
            ("Old Date", TriageState.New, now.AddDays(-5), now.AddDays(-5)));
        var client = _factory.CreateClient();

        var page = await client.GetFromJsonAsync<ItemsPage>($"/api/items?sourceId={sourceId}");

        Assert.NotNull(page);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal("No Date", page.Items[0].Title);
    }

    [Fact]
    public async Task Ordering_duplicate_published_date_stable_via_id_no_duplicates_across_pages()
    {
        var sameDate = D(5);
        var sourceId = await SeedSourceWithItems(
            ("Alpha", TriageState.New, sameDate, null),
            ("Beta", TriageState.New, sameDate, null));
        var client = _factory.CreateClient();

        var page1 = await client.GetFromJsonAsync<ItemsPage>($"/api/items?sourceId={sourceId}&limit=1");
        Assert.True(page1!.HasMore);

        var page2 = await client.GetFromJsonAsync<ItemsPage>(
            $"/api/items?sourceId={sourceId}&limit=1&cursor={page1.NextCursor}");

        var page1Ids = page1.Items.Select(i => i.Id).ToHashSet();
        var page2Ids = page2!.Items.Select(i => i.Id).ToHashSet();
        Assert.Empty(page1Ids.Intersect(page2Ids));
        Assert.False(page2.HasMore);
    }
}
