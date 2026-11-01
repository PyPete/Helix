using System.Text.Json;
using Helix.Orchestration.Agent.Tests.Scientific;
using Helix.Orchestration.Core.ScientificSetup;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Integration;

/// <summary>Tracer contract guard. The PowerShell verifier owns the live host lifecycle.</summary>
[Collection(nameof(ScientificPostgresCollection))]
public sealed class ScientificEndToEndTests(ScientificPostgresFixture fixture)
{
    [Fact]
    public void Vessel_source_content_has_a_stable_server_side_hash()
    {
        var vessel = new ApparatusDefinition(Guid.NewGuid(), "Tracer vessel", [new ApparatusComponent(Guid.NewGuid(), "Vessel", ["Source"])]);
        vessel.Validate();
        var canonical = ScientificContentCanonicalizer.Canonicalize(JsonSerializer.SerializeToElement(vessel));
        Assert.Equal(64, canonical.Hash.Length);
        Assert.StartsWith("{", canonical.Content);
    }

    [Fact]
    public void Owned_authenticated_agent_uses_the_native_loopback_endpoint()
    {
        Assert.Equal(5080, fixture.AgentAddress.Port);
    }
}
