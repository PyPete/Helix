using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Helix.Orchestration.SiteServer.Contracts;
using Xunit;

namespace Helix.Orchestration.SiteServer.Tests.Endpoints;

/// <summary>
/// Integration tests for experiment lifecycle endpoints using the in-memory test server.
/// MqttTrendBridge is replaced with a no-op hosted service to avoid broker dependency.
/// </summary>
public class ExperimentEndpointsTests : IClassFixture<HelixWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ExperimentEndpointsTests(HelixWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static string MinimalWslJson(string name = "Test Experiment") =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            documentId = Guid.NewGuid().ToString("N"),
            name,
            schemaVersion = "1.0",
            nodes = Array.Empty<object>(),
            edges = Array.Empty<object>()
        });

    [Fact]
    public async Task PostExperiment_ValidRequest_Returns200WithRunspaceId()
    {
        var request = new SubmitExperimentRequest
        {
            WslSource = MinimalWslJson(),
            ExperimentId = "integration-test-1",
            ExperimentName = "Integration Test"
        };

        var response = await _client.PostAsJsonAsync("/api/experiments", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RunspaceIdResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body?.RunspaceId));
    }

    [Fact]
    public async Task GetExperimentStatus_AfterSubmit_Returns200()
    {
        var request = new SubmitExperimentRequest
        {
            WslSource = MinimalWslJson(),
            ExperimentName = "Status Test"
        };

        var postResponse = await _client.PostAsJsonAsync("/api/experiments", request);
        postResponse.EnsureSuccessStatusCode();

        var posted = await postResponse.Content.ReadFromJsonAsync<RunspaceIdResponse>();
        Assert.NotNull(posted);

        var getResponse = await _client.GetAsync($"/api/experiments/{posted!.RunspaceId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var status = await getResponse.Content.ReadFromJsonAsync<ExperimentStatusResponse>();
        Assert.Equal(posted.RunspaceId, status?.RunspaceId);
        Assert.NotNull(status?.Status);
    }

    [Fact]
    public async Task GetExperimentStatus_UnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/experiments/nonexistent-id-xyz");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteExperiment_AfterSubmit_Returns204()
    {
        var request = new SubmitExperimentRequest
        {
            WslSource = MinimalWslJson(),
            ExperimentName = "Abort Test"
        };

        var postResponse = await _client.PostAsJsonAsync("/api/experiments", request);
        var posted = await postResponse.Content.ReadFromJsonAsync<RunspaceIdResponse>();
        Assert.NotNull(posted);

        var deleteResponse = await _client.DeleteAsync($"/api/experiments/{posted!.RunspaceId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteExperiment_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync("/api/experiments/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record RunspaceIdResponse(string RunspaceId);
}
