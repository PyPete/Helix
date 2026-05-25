using Helix.Orchestration.Core.Devices;
using Helix.Orchestration.Core.WSL;
using Xunit;

namespace Helix.Orchestration.Core.Tests.WSL;

public sealed class WslCompilerTests
{
    private static WslDocument BuildValidDocument() => new()
    {
        DocumentId = "test-doc-1",
        Name = "TestWorkflow",
        SchemaVersion = "1.0",
        Nodes =
        [
            new WslNode("n1", "Start", "Start", new Dictionary<string, object?> { ["entryPoint"] = true }),
            new WslNode("n2", "Ramp Up", "Ramp", new Dictionary<string, object?> { ["target"] = 80.0, ["device"] = "reactor1" }),
            new WslNode("n3", "End", "Terminate", new Dictionary<string, object?>())
        ],
        Edges =
        [
            new WslEdge("e1", "n1", "n2"),
            new WslEdge("e2", "n2", "n3")
        ]
    };

    private static DeviceRegistry BuildDeviceRegistry()
    {
        var registry = new DeviceRegistry();
        registry.Register(new DeviceDescriptor(
            "reactor-001",
            "reactor1",
            "reactor",
            ["SetTemperature", "GetPressure"],
            new Dictionary<string, object?> { ["location"] = "FH-A" }));
        return registry;
    }

    [Fact]
    public void Compile_ValidDocument_ReturnsSuccess()
    {
        var compiler = new WslCompiler();
        var doc = BuildValidDocument();
        var registry = BuildDeviceRegistry();

        var result = compiler.Compile(doc, registry);

        Assert.True(result.Success);
        Assert.NotNull(result.Definition);
        Assert.Equal("test-doc-1", result.Definition.DefinitionId);
        Assert.Equal("TestWorkflow", result.Definition.Name);
        Assert.Equal(3, result.Definition.Nodes.Count);
        Assert.Equal(2, result.Definition.Edges.Count);
        Assert.Contains(result.Definition.Metadata, kvp => kvp.Key == "compiledAt");
    }

    [Fact]
    public void Compile_EmptyDocumentId_ReturnsError()
    {
        var compiler = new WslCompiler();
        var doc = BuildValidDocument() with { DocumentId = "" };
        var registry = BuildDeviceRegistry();

        var result = compiler.Compile(doc, registry);

        Assert.False(result.Success);
        Assert.Null(result.Definition);
        Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error && d.Code == "WSL001");
    }

    [Fact]
    public void Compile_EmptyName_ReturnsError()
    {
        var compiler = new WslCompiler();
        var doc = BuildValidDocument() with { Name = "" };
        var registry = BuildDeviceRegistry();

        var result = compiler.Compile(doc, registry);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error && d.Code == "WSL002");
    }

    [Fact]
    public void Compile_UnsupportedSchemaVersion_ReturnsError()
    {
        var compiler = new WslCompiler();
        var doc = BuildValidDocument() with { SchemaVersion = "99.0" };
        var registry = BuildDeviceRegistry();

        var result = compiler.Compile(doc, registry);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error && d.Code == "WSL003");
    }

    [Fact]
    public void Compile_DuplicateNodeIds_ReturnsError()
    {
        var compiler = new WslCompiler();
        var doc = BuildValidDocument() with
        {
            Nodes =
            [
                new WslNode("n1", "Start", "Start", new Dictionary<string, object?>()),
                new WslNode("n1", "Duplicate", "Hold", new Dictionary<string, object?>())
            ]
        };
        var registry = BuildDeviceRegistry();

        var result = compiler.Compile(doc, registry);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error && d.Code == "WSL010");
    }

    [Fact]
    public void Compile_OrphanSourceNode_ReturnsError()
    {
        var compiler = new WslCompiler();
        var doc = new WslDocument
        {
            Name = "TestWorkflow",
            Nodes = [new WslNode("n1", "Only", "Hold", new Dictionary<string, object?>())],
            Edges = [new WslEdge("e1", "missing", "n1")]
        };
        var registry = BuildDeviceRegistry();

        var result = compiler.Compile(doc, registry);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error && d.Code == "WSL011");
    }

    [Fact]
    public void Compile_OrphanTargetNode_ReturnsError()
    {
        var compiler = new WslCompiler();
        var doc = new WslDocument
        {
            Name = "TestWorkflow",
            Nodes = [new WslNode("n1", "Only", "Hold", new Dictionary<string, object?>())],
            Edges = [new WslEdge("e1", "n1", "ghost")]
        };
        var registry = BuildDeviceRegistry();

        var result = compiler.Compile(doc, registry);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error && d.Code == "WSL012");
    }

    [Fact]
    public void Compile_SelfLoop_ReturnsWarning()
    {
        var compiler = new WslCompiler();
        var doc = new WslDocument
        {
            Name = "TestWorkflow",
            Nodes = [new WslNode("n1", "Loop", "Hold", new Dictionary<string, object?> { ["entryPoint"] = true })],
            Edges = [new WslEdge("e1", "n1", "n1")]
        };
        var registry = BuildDeviceRegistry();

        var result = compiler.Compile(doc, registry);

        // Should succeed but with a warning
        Assert.True(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning && d.Code == "WSL013");
    }

    [Fact]
    public void Compile_UnreachableNode_ReturnsWarning()
    {
        var compiler = new WslCompiler();
        var doc = new WslDocument
        {
            Name = "TestWorkflow",
            Nodes =
            [
                new WslNode("n1", "Start", "Start", new Dictionary<string, object?> { ["entryPoint"] = true }),
                new WslNode("n2", "Orphan", "Hold", new Dictionary<string, object?>())
            ],
            Edges = []
        };
        var registry = BuildDeviceRegistry();

        var result = compiler.Compile(doc, registry);

        // Should succeed but with a warning for unreachable node
        Assert.True(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning && d.Code == "WSL014" && d.NodeId == "n2");
    }

    [Fact]
    public void Compile_NodeWithEmptyNodeId_ReturnsError()
    {
        var compiler = new WslCompiler();
        var doc = new WslDocument
        {
            Name = "TestWorkflow",
            Nodes = [new WslNode("", "NoId", "Hold", new Dictionary<string, object?>())],
            Edges = []
        };
        var registry = BuildDeviceRegistry();

        var result = compiler.Compile(doc, registry);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error && d.Code == "WSL020");
    }

    [Fact]
    public void Compile_NodeWithEmptyNodeType_ReturnsError()
    {
        var compiler = new WslCompiler();
        var doc = new WslDocument
        {
            Name = "TestWorkflow",
            Nodes = [new WslNode("n1", "NoType", "", new Dictionary<string, object?>())],
            Edges = []
        };
        var registry = BuildDeviceRegistry();

        var result = compiler.Compile(doc, registry);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error && d.Code == "WSL021");
    }

    [Fact]
    public void Compile_UnknownDeviceAlias_ReturnsError()
    {
        var compiler = new WslCompiler();
        var doc = BuildValidDocument() with
        {
            Nodes =
            [
                new WslNode("n1", "Start", "Start", new Dictionary<string, object?> { ["device"] = "unknownDevice" })
            ]
        };
        var registry = BuildDeviceRegistry();

        var result = compiler.Compile(doc, registry);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error && d.Code == "WSL030");
    }

    [Fact]
    public void Compile_ValidDeviceAlias_Succeeds()
    {
        var compiler = new WslCompiler();
        var doc = BuildValidDocument();
        var registry = BuildDeviceRegistry();

        var result = compiler.Compile(doc, registry);

        Assert.True(result.Success);
        // Should have no device-binding errors
        Assert.DoesNotContain(result.Diagnostics, d => d.Code == "WSL030");
    }

    [Fact]
    public void Compile_MultipleErrors_ReturnsAllDiagnostics()
    {
        var compiler = new WslCompiler();
        var doc = new WslDocument
        {
            DocumentId = "",
            Name = "",
            SchemaVersion = "99.0",
            Nodes = [new WslNode("", "", "", new Dictionary<string, object?>())],
            Edges = []
        };
        var registry = BuildDeviceRegistry();

        var result = compiler.Compile(doc, registry);

        Assert.False(result.Success);
        // Should have multiple error diagnostics
        Assert.True(result.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error) >= 4);
    }
}
