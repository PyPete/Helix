using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Helix.Orchestration.Agent.Tests.Scientific;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Integration;

[Collection(nameof(ScientificPostgresCollection))]
public sealed class SamplesTests(ScientificPostgresFixture fixture)
{
    [Fact]
    public async Task Empty_sample_history_remains_empty_until_an_explicit_confirmation()
    {
        using var client = fixture.CreateAuthenticatedClient();

        var initial = await client.GetAsync("/api/samples");

        Assert.Equal(HttpStatusCode.OK, initial.StatusCode);
        Assert.Equal("[]", await initial.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Explicit_confirmation_creates_one_sample_and_replays_the_original_operation()
    {
        using var client = fixture.CreateAuthenticatedClient();
        var operationId = Guid.NewGuid();
        var request = new
        {
            operationId,
            location = "Fumehood collector",
            occurredAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            label = "Collected vial"
        };

        var first = await client.PostAsJsonAsync("/api/samples/confirmations", request);
        var replay = await client.PostAsJsonAsync("/api/samples/confirmations", request);

        Assert.True(first.StatusCode == HttpStatusCode.Created, await first.Content.ReadAsStringAsync());
        Assert.True(replay.StatusCode == HttpStatusCode.OK, await replay.Content.ReadAsStringAsync());
        using var firstReceipt = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        using var replayReceipt = JsonDocument.Parse(await replay.Content.ReadAsStringAsync());
        Assert.Equal(
            firstReceipt.RootElement.GetProperty("sample").GetProperty("identity").GetProperty("sampleId").GetGuid(),
            replayReceipt.RootElement.GetProperty("sample").GetProperty("identity").GetProperty("sampleId").GetGuid());
    }

    [Fact]
    public async Task Confirmation_does_not_infer_or_cross_correlate_adjacent_planned_activity()
    {
        using var client = fixture.CreateAuthenticatedClient();
        var request = new
        {
            operationId = Guid.NewGuid(),
            location = "Fumehood collector",
            occurredAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            label = "Adjacent evidence remains independent"
        };

        var response = await client.PostAsJsonAsync("/api/samples/confirmations", request);

        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var samples = await client.GetFromJsonAsync<List<SampleSummary>>("/api/samples");
        Assert.Single(samples!);
    }

    private sealed record SampleSummary(Guid SampleId);
}
