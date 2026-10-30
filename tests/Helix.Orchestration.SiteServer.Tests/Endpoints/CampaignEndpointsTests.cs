using System.Net;
using System.Net.Http.Json;
using Helix.Orchestration.SiteServer.Contracts;
using Xunit;

namespace Helix.Orchestration.SiteServer.Tests.Endpoints;

public class CampaignEndpointsTests : IClassFixture<HelixWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CampaignEndpointsTests(HelixWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateCampaign_ValidRequest_Returns201()
    {
        var request = new CreateCampaignRequest
        {
            Name = "Campaign API 1",
            Description = "test",
            ParameterSets = new()
            {
                new Dictionary<string, object?> { ["temp"] = 45 }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/campaigns/", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<CampaignResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.CampaignId));
        Assert.Equal("Draft", payload.Status);
    }

    [Fact]
    public async Task CreateCampaign_MissingName_Returns400()
    {
        var request = new CreateCampaignRequest
        {
            Name = "",
            ParameterSets = new()
        };

        var response = await _client.PostAsJsonAsync("/api/campaigns/", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCampaigns_Returns200()
    {
        var response = await _client.GetAsync("/api/campaigns/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task StartCampaign_UnknownId_Returns404()
    {
        var response = await _client.PostAsync($"/api/campaigns/{Guid.NewGuid():N}/start", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCampaign_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/campaigns/{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
