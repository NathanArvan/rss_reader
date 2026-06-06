using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RssReader.Api.Tests;

public class PingEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PingEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Ping_returns_ok_and_reports_db_connected()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PingResponse>();
        Assert.NotNull(body);
        Assert.Equal("ok", body!.Status);
        Assert.True(body.DbConnected);
    }

    private sealed record PingResponse(string Status, bool DbConnected, DateTime Utc);
}
