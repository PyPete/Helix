using Helix.Orchestration.Core.WSL;
using Xunit;

namespace Helix.Orchestration.Core.Tests.WSL;

public sealed class WslDocumentTests
{
    private static WslDocument BuildValid() => new()
    {
        Name = "TestWorkflow",
        Nodes =
        [
            new WslNode("n1", "Start", "Hold", new Dictionary<string, object?> { ["duration"] = 60 }),
            new WslNode("n2", "Ramp Up", "Ramp", new Dictionary<string, object?> { ["target"] = 80.0 }),
            new WslNode("n3", "End", "Terminate", new Dictionary<string, object?>())
        ],
        Edges =
        [
            new WslEdge("e1", "n1", "n2"),
            new WslEdge("e2", "n2", "n3")
        ]
    };

    [Fact]
    public void Compile_ValidDocument_ReturnsRunspaceDefinition()
    {
        var doc = BuildValid();

        var def = doc.Compile();

        Assert.Equal(doc.DocumentId, def.DefinitionId);
        Assert.Equal("TestWorkflow", def.Name);
        Assert.Equal(3, def.Nodes.Count);
        Assert.Equal(2, def.Edges.Count);
    }

    [Fact]
    public void Compile_NoName_ThrowsValidationException()
    {
        var doc = BuildValid() with { Name = "" };

        var ex = Assert.Throws<WslValidationException>(() => doc.Compile());
        Assert.Contains(ex.Errors, e => e.Contains("Name"));
    }

    [Fact]
    public void Compile_EdgeWithMissingSource_ThrowsValidationException()
    {
        var doc = new WslDocument
        {
            Name = "Bad",
            Nodes = [new WslNode("n1", "Only", "Hold", new Dictionary<string, object?>())],
            Edges = [new WslEdge("e1", "missing", "n1")]
        };

        var ex = Assert.Throws<WslValidationException>(() => doc.Compile());
        Assert.Contains(ex.Errors, e => e.Contains("missing"));
    }

    [Fact]
    public void Compile_EdgeWithMissingTarget_ThrowsValidationException()
    {
        var doc = new WslDocument
        {
            Name = "Bad",
            Nodes = [new WslNode("n1", "Only", "Hold", new Dictionary<string, object?>())],
            Edges = [new WslEdge("e1", "n1", "ghost")]
        };

        var ex = Assert.Throws<WslValidationException>(() => doc.Compile());
        Assert.Contains(ex.Errors, e => e.Contains("ghost"));
    }

    [Fact]
    public void Compile_MetadataContainsSchemaVersion()
    {
        var doc = BuildValid();

        var def = doc.Compile();

        Assert.True(def.Metadata.ContainsKey("schemaVersion"));
        Assert.Equal("1.0", def.Metadata["schemaVersion"]);
    }

    [Fact]
    public void Compile_EmptyGraph_IsValid()
    {
        var doc = new WslDocument { Name = "Empty" };

        var def = doc.Compile();

        Assert.Empty(def.Nodes);
        Assert.Empty(def.Edges);
    }
}
