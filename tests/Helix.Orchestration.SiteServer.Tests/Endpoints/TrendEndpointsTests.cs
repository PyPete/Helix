using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Helix.Orchestration.SiteServer.Tests.Endpoints;

public class TrendEndpointsTests : IClassFixture<HelixWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TrendEndpointsTests(HelixWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTrends_Returns200()
    {
        var response = await _client.GetAsync("/api/trends");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTrends_ReturnsJsonArray()
    {
        var response = await _client.GetAsync("/api/trends");
        response.EnsureSuccessStatusCode();

        var trends = await response.Content.ReadFromJsonAsync<TrendDto[]>();
        Assert.NotNull(trends);
    }

    [Fact]
    public async Task GetTrendHistory_UnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/trends/does.not.exist/history");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record TrendDto(
        string TrendId,
        string TrendName,
        string TrendKind,
        string? Units,
        string? ExperimentId,
        bool IsOnline,
        DateTimeOffset LastUpdatedAt);
}
