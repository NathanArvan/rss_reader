using System.Net;
using System.Net.Http.Json;
using RssReader.Api.Models;

namespace RssReader.Api.Tests;

public class SourceEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public SourceEndpointsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Add_source_then_it_appears_in_list()
    {
        var client = _factory.CreateClient();

        var create = await client.PostAsJsonAsync("/api/sources", new
        {
            feedUrl = $"https://example.com/feed-{Guid.NewGuid():N}.xml",
            title = "Example Blog"
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<SourceDto>();
        Assert.NotNull(created);
        Assert.Equal("Example Blog", created!.Title);

        var list = await client.GetFromJsonAsync<List<SourceDto>>("/api/sources");
        Assert.NotNull(list);
        Assert.Contains(list!, s => s.Id == created.Id);
    }

    [Fact]
    public async Task Add_source_missing_feed_url_is_rejected()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/sources", new { title = "No URL" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Add_duplicate_feed_url_is_conflict()
    {
        var client = _factory.CreateClient();
        var feedUrl = $"https://example.com/dup-{Guid.NewGuid():N}.xml";

        var first = await client.PostAsJsonAsync("/api/sources", new { feedUrl });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/sources", new { feedUrl });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Delete_source_removes_it_and_its_items()
    {
        var client = _factory.CreateClient();

        var create = await client.PostAsJsonAsync("/api/sources", new
        {
            feedUrl = $"https://example.com/del-{Guid.NewGuid():N}.xml",
            title = "To Delete"
        });
        var source = await create.Content.ReadFromJsonAsync<SourceDto>();
        Assert.NotNull(source);

        await _factory.SeedAsync(db =>
        {
            db.Items.Add(new Item
            {
                SourceId = source!.Id,
                Url = "https://example.com/post/1",
                Title = "A post",
                FetchedUtc = DateTime.UtcNow
            });
            return Task.CompletedTask;
        });

        var delete = await client.DeleteAsync($"/api/sources/{source!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var items = await client.GetAsync($"/api/sources/{source.Id}/items");
        Assert.Equal(HttpStatusCode.NotFound, items.StatusCode); // source is gone
    }

    [Fact]
    public async Task Delete_unknown_source_is_not_found()
    {
        var client = _factory.CreateClient();
        var response = await client.DeleteAsync("/api/sources/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record SourceDto(int Id, string FeedUrl, string Title, SourceType Type, string? Category);
}
