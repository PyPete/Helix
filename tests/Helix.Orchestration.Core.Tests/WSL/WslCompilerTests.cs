using Helix.Orchestration.Core.Devices;
using Helix.Orchestration.Core.Safety;
using Helix.Orchestration.Core.WSL;
using Xunit;

namespace Helix.Orchestration.Core.Tests.WSL;

// Manual test double for ISafetyEngine — no mock framework required
file sealed class FakeSafetyEngine : ISafetyEngine
{
    private readonly List<InterlockRule> _violations;
    public bool RegisterInterlockCalled { get; private set; }

    public FakeSafetyEngine(IEnumerable<InterlockRule>? violations = null)
    {
        _violations = violations?.ToList() ?? [];
    }

    public void RegisterInterlock(InterlockRule rule) => RegisterInterlockCalled = true;
    public void UnregisterInterlock(string ruleId) { }
    public Task<bool> EvaluateInterlocksAsync() => Task.FromResult(true);
    public Task<IEnumerable<InterlockRule>> GetViolationsAsync()
        => Task.FromResult<IEnumerable<InterlockRule>>(_violations);
    public Task TriggerEmergencyStopAsync(string reason) => Task.CompletedTask;
    public bool IsEmergencyStopActive => false;
    public Task ResetEmergencyStopAsync() => Task.CompletedTask;
    public CancellationToken EmergencyStopToken => CancellationToken.None;
}

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

    // ────────────────────────────────────────────────────────────────────────
    // Existing baseline tests
    // ────────────────────────────────────────────────────────────────────────

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
            DocumentId = "doc-orphan-src",
            Name = "TestWorkflow",
            SchemaVersion = "1.0",
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
            DocumentId = "doc-orphan-tgt",
            Name = "TestWorkflow",
            SchemaVersion = "1.0",
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
            DocumentId = "doc-selfloop",
            Name = "TestWorkflow",
            SchemaVersion = "1.0",
            Nodes = [new WslNode("n1", "Loop", "Hold", new Dictionary<string, object?> { ["entryPoint"] = true })],
            Edges = [new WslEdge("e1", "n1", "n1")]
        };
        var registry = BuildDeviceRegistry();

        var result = compiler.Compile(doc, registry);

        Assert.True(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning && d.Code == "WSL013");
    }

    [Fact]
    public void Compile_UnreachableNode_ReturnsWarning()
    {
        var compiler = new WslCompiler();
        var doc = new WslDocument
        {
            DocumentId = "doc-unreachable",
            Name = "TestWorkflow",
            SchemaVersion = "1.0",
            Nodes =
            [
                new WslNode("n1", "Start", "Start", new Dictionary<string, object?> { ["entryPoint"] = true }),
                new WslNode("n2", "Orphan", "Hold", new Dictionary<string, object?>())
            ],
            Edges = []
        };
        var registry = BuildDeviceRegistry();

        var result = compiler.Compile(doc, registry);

        Assert.True(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning && d.Code == "WSL014" && d.NodeId == "n2");
    }

    [Fact]
    public void Compile_NodeWithEmptyNodeId_ReturnsError()
    {
        var compiler = new WslCompiler();
        var doc = new WslDocument
        {
            DocumentId = "doc-empty-id",
            Name = "TestWorkflow",
            SchemaVersion = "1.0",
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
            DocumentId = "doc-empty-type",
            Name = "TestWorkflow",
            SchemaVersion = "1.0",
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
        Assert.True(result.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error) >= 4);
    }

    // ────────────────────────────────────────────────────────────────────────
    // New TDD tests: loop expansion, branch flattening, safety injection, parallel
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Compile_LoopNode_ExpandsBodyToNIterations()
    {
        // Arrange: Start -> Loop(iterations=3) -> Body -> End
        var compiler = new WslCompiler();
        var doc = new WslDocument
        {
            DocumentId = "loop-doc",
            Name = "LoopWorkflow",
            SchemaVersion = "1.0",
            Nodes =
            [
                new WslNode("start", "Start", "Start", new Dictionary<string, object?> { ["entryPoint"] = true }),
                new WslNode("loop1", "Loop", "Loop", new Dictionary<string, object?> { ["iterations"] = 3 }),
                new WslNode("body1", "Body Step", "Hold", new Dictionary<string, object?>()),
                new WslNode("end", "End", "Terminate", new Dictionary<string, object?>())
            ],
            Edges =
            [
                new WslEdge("e1", "start", "loop1"),
                new WslEdge("e2", "loop1", "body1"),
                new WslEdge("e3", "body1", "end")
            ]
        };
        var registry = new DeviceRegistry();

        // Act
        var result = compiler.Compile(doc, registry);

        // Assert: loop node is absent, body node appears 3 times
        Assert.True(result.Success, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        Assert.NotNull(result.Definition);
        Assert.DoesNotContain(result.Definition.Nodes, n => n.NodeType.Equals("Loop", StringComparison.OrdinalIgnoreCase));
        var bodyNodes = result.Definition.Nodes.Where(n => n.NodeId.StartsWith("body1")).ToList();
        Assert.Equal(3, bodyNodes.Count);
    }

    [Fact]
    public void Compile_BranchNode_ProducesConditionalTrueFalseEdges()
    {
        // Arrange: Start -> Branch -> TruePath / FalsePath
        var compiler = new WslCompiler();
        var doc = new WslDocument
        {
            DocumentId = "branch-doc",
            Name = "BranchWorkflow",
            SchemaVersion = "1.0",
            Nodes =
            [
                new WslNode("start", "Start", "Start", new Dictionary<string, object?> { ["entryPoint"] = true }),
                new WslNode("branch1", "Decision", "Branch", new Dictionary<string, object?>()),
                new WslNode("true-path", "True Path", "Hold", new Dictionary<string, object?>()),
                new WslNode("false-path", "False Path", "Hold", new Dictionary<string, object?>())
            ],
            Edges =
            [
                new WslEdge("e1", "start", "branch1"),
                new WslEdge("e2", "branch1", "true-path"),
                new WslEdge("e3", "branch1", "false-path")
            ]
        };
        var registry = new DeviceRegistry();

        // Act
        var result = compiler.Compile(doc, registry);

        // Assert: edges from branch point labeled "true" and "false"
        Assert.True(result.Success, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        Assert.NotNull(result.Definition);

        // Collect edges that originate from the branch node or any ConditionalGateway it produces
        var branchAndGatewayNodeIds = result.Definition.Nodes
            .Where(n => n.NodeId == "branch1" ||
                        n.NodeType.Equals("ConditionalGateway", StringComparison.OrdinalIgnoreCase))
            .Select(n => n.NodeId)
            .ToHashSet();

        var outgoingEdges = result.Definition.Edges
            .Where(e => branchAndGatewayNodeIds.Contains(e.SourceNodeId))
            .ToList();

        Assert.Contains(outgoingEdges, e => e.Condition != null && e.Condition.Equals("true", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(outgoingEdges, e => e.Condition != null && e.Condition.Equals("false", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Compile_WithSafetyViolation_InjectsSafetyGuardNode()
    {
        // Arrange: valid document + safety engine reporting one violation
        var compiler = new WslCompiler();
        var doc = BuildValidDocument();
        var registry = BuildDeviceRegistry();
        var violation = new InterlockRule(() => Task.FromResult(false))
        {
            RuleId = "PRESSURE-001",
            Description = "Reactor pressure exceeds threshold",
            Severity = InterlockSeverity.Critical
        };
        var safetyEngine = new FakeSafetyEngine([violation]);

        // Act
        var result = compiler.Compile(doc, registry, safetyEngine);

        // Assert: a SafetyGuard node was injected
        Assert.True(result.Success, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        Assert.NotNull(result.Definition);
        var guardNodes = result.Definition.Nodes.Where(n => n.NodeType == "SafetyGuard").ToList();
        Assert.Single(guardNodes);
        Assert.Equal("PRESSURE-001", guardNodes[0].Parameters["ruleId"]?.ToString());
        Assert.Contains(result.Diagnostics, d => d.Code == "WSL050");
    }

    [Fact]
    public void Compile_SafetyInjection_DoesNotCallRegisterInterlock()
    {
        // Arrange
        var compiler = new WslCompiler();
        var doc = BuildValidDocument();
        var registry = BuildDeviceRegistry();
        var safetyEngine = new FakeSafetyEngine([]);

        // Act
        compiler.Compile(doc, registry, safetyEngine);

        // Assert: RegisterInterlock must NOT have been called (read-only access)
        Assert.False(safetyEngine.RegisterInterlockCalled);
    }

    [Fact]
    public void Compile_ParallelNode_ProducesForkJoinStructure()
    {
        // Arrange: Start -> Parallel(branch-a, branch-b) -> End
        var compiler = new WslCompiler();
        var doc = new WslDocument
        {
            DocumentId = "parallel-doc",
            Name = "ParallelWorkflow",
            SchemaVersion = "1.0",
            Nodes =
            [
                new WslNode("start", "Start", "Start", new Dictionary<string, object?> { ["entryPoint"] = true }),
                new WslNode("par1", "Parallel Step", "Parallel", new Dictionary<string, object?>()),
                new WslNode("branch-a", "Branch A", "Hold", new Dictionary<string, object?>()),
                new WslNode("branch-b", "Branch B", "Hold", new Dictionary<string, object?>()),
                new WslNode("end", "End", "Terminate", new Dictionary<string, object?>())
            ],
            Edges =
            [
                new WslEdge("e1", "start", "par1"),
                new WslEdge("e2", "par1", "branch-a"),
                new WslEdge("e3", "par1", "branch-b"),
                new WslEdge("e4", "branch-a", "end"),
                new WslEdge("e5", "branch-b", "end")
            ]
        };
        var registry = new DeviceRegistry();

        // Act
        var result = compiler.Compile(doc, registry);

        // Assert: ParallelFork and ParallelJoin nodes exist; original Parallel node is gone
        Assert.True(result.Success, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        Assert.NotNull(result.Definition);
        Assert.Contains(result.Definition.Nodes, n => n.NodeType == "ParallelFork");
        Assert.Contains(result.Definition.Nodes, n => n.NodeType == "ParallelJoin");
        Assert.DoesNotContain(result.Definition.Nodes, n => n.NodeType.Equals("Parallel", StringComparison.OrdinalIgnoreCase));
    }
}
