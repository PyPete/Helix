using System.Text.Json;
using Helix.Orchestration.Core.ScientificSetup;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Integration;

/// <summary>Tracer contract guard. The PowerShell verifier owns the live host lifecycle.</summary>
public sealed class ScientificEndToEndTests
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
}
