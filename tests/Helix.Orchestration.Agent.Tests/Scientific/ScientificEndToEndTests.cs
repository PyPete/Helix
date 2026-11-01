using System.Net;
using System.Net.Http.Json;
using Helix.Orchestration.Agent.Api;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Scientific;

[Collection(nameof(ScientificPostgresCollection))]
public sealed class ScientificEndToEndTests(ScientificPostgresFixture fixture)
{
    [Fact]
    public async Task Production_authenticated_scientific_trace_is_durable_idempotent_and_never_dispatches_hardware()
    {
        var operationId = Guid.NewGuid();
        const string actorSid = "S-1-5-21-scientific-tracer";
        ScientificOperationResponse created;
        using (var client = fixture.CreateAuthenticatedClient(actorSid))
        {
            var anonymous = new HttpClient { BaseAddress = fixture.AgentAddress };
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/scientific-setup/apparatus/")).StatusCode);

            var request = new { operationId, name = "Tracer reactor", description = "Production Bearer path" };
            var response = await client.PostAsJsonAsync("/api/scientific-setup/apparatus/", request);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            created = (await response.Content.ReadFromJsonAsync<ScientificOperationResponse>())!;

            var replay = await client.PostAsJsonAsync("/api/scientific-setup/apparatus/", request);
            Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
            var replayed = (await replay.Content.ReadFromJsonAsync<ScientificOperationResponse>())!;
            Assert.Equal(created.ApparatusId, replayed.ApparatusId);
            Assert.Equal(operationId, replayed.OperationId);

            var conflict = await client.PostAsJsonAsync("/api/scientific-setup/apparatus/", new { operationId, name = "Conflicting tracer reactor", description = "different" });
            Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        }

        Assert.Equal(1, await fixture.OwnerOperationCountAsync(operationId));
        Assert.Equal(1, await fixture.OwnerApparatusCountAsync(created.ApparatusId));
        await fixture.RestartAgentAsync();
        using (var restartedClient = fixture.CreateAuthenticatedClient(actorSid))
        {
            var readback = await restartedClient.GetFromJsonAsync<ScientificApparatusResponse>($"/api/scientific-setup/apparatus/{created.ApparatusId}");
            Assert.NotNull(readback);
            Assert.Equal(created.ApparatusId, readback.ApparatusId);
            Assert.Equal("Tracer reactor", readback.Name);
        }

        await fixture.AssertRuntimeMutationIsForbiddenAsync(created.ApparatusId);
        await fixture.AssertRuntimeRevisionMutationIsForbiddenAsync(created.ApparatusId);
        Assert.Equal(1, await fixture.OwnerApparatusCountAsync(created.ApparatusId));
        await fixture.AssertNoHardwareOrCapabilityDispatchAsync();
    }
}
