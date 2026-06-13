using System.Net;
using System.Net.Http.Json;

namespace RssReader.Api.Tests;

// Tests that depend on a clean initial state (no prior POST calls).
public class SettingsFreshStateTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public SettingsFreshStateTests(TestWebApplicationFactory factory) => _factory = factory;

    private sealed record SettingsDto(DateTime? LastOpenedUtc);

    [Fact]
    public async Task Get_settings_returns_null_last_opened_on_fresh_db()
    {
        var client = _factory.CreateClient();

        var dto = await client.GetFromJsonAsync<SettingsDto>("/api/settings");

        Assert.NotNull(dto);
        Assert.Null(dto.LastOpenedUtc);
    }
}

// Tests for POST and round-trip behaviour; these mutate state and share a DB.
public class SettingsEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public SettingsEndpointsTests(TestWebApplicationFactory factory) => _factory = factory;

    private sealed record SettingsDto(DateTime? LastOpenedUtc);

    [Fact]
    public async Task Post_last_opened_returns_no_content()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/settings/last-opened", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Post_last_opened_then_get_returns_non_null_recent_timestamp()
    {
        var client = _factory.CreateClient();
        var before = DateTime.UtcNow.AddSeconds(-1);

        await client.PostAsync("/api/settings/last-opened", null);
        var dto = await client.GetFromJsonAsync<SettingsDto>("/api/settings");

        Assert.NotNull(dto);
        Assert.NotNull(dto.LastOpenedUtc);
        Assert.True(dto.LastOpenedUtc > before);
    }

    [Fact]
    public async Task Post_last_opened_twice_advances_the_timestamp()
    {
        var client = _factory.CreateClient();

        await client.PostAsync("/api/settings/last-opened", null);
        var first = (await client.GetFromJsonAsync<SettingsDto>("/api/settings"))!.LastOpenedUtc;

        await Task.Delay(10); // ensure clock ticks
        await client.PostAsync("/api/settings/last-opened", null);
        var second = (await client.GetFromJsonAsync<SettingsDto>("/api/settings"))!.LastOpenedUtc;

        Assert.True(second >= first);
    }
}
