using Helix.Orchestration.Core.Devices;
using Helix.Orchestration.Core.Safety;
using Helix.Orchestration.Core.WSL;
using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Core.WorkflowEngine;
using Xunit;

namespace Helix.Orchestration.Core.Tests.WSL;

// Manual test double for ISafetyEngine â€” no mock framework required
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
            new WslNode("n2", "Set temperature", "Action", new Dictionary<string, object?> { ["capability"] = "SetTemperature", ["temperature"] = 80m, ["device"] = "simulator" }),
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
            "simulator",
            "simulator",
            "simulator",
            ["SetFlow", "GetFlow", "SetTemperature", "GetTemperature", "SetMode", "GetMode"],
            new Dictionary<string, object?> { ["location"] = "FH-A" }));
        return registry;
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // Existing baseline tests
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // New TDD tests: loop expansion, branch flattening, safety injection, parallel
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public void Compile_LoopNode_IsRejectedBecauseItsSemanticsAreNotEnforced()
    {
        var doc = BuildValidDocument() with
        {
            Nodes =
            [
                new WslNode("start", "Start", "Start", new Dictionary<string, object?> { ["entryPoint"] = true }),
                new WslNode("loop", "Loop", "Loop", new Dictionary<string, object?>()),
                new WslNode("end", "End", "Terminate", new Dictionary<string, object?>())
            ]
        };

        var result = new WslCompiler().Compile(doc, BuildDeviceRegistry());

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "WSL060");
    }

    [Fact]
    public void Compile_BranchNode_IsRejectedBecauseItsGuardIsNotEnforced()
    {
        var doc = BuildValidDocument() with
        {
            Nodes =
            [
                new WslNode("start", "Start", "Start", new Dictionary<string, object?> { ["entryPoint"] = true }),
                new WslNode("branch", "Branch", "Branch", new Dictionary<string, object?>()),
                new WslNode("end", "End", "Terminate", new Dictionary<string, object?>())
            ]
        };

        var result = new WslCompiler().Compile(doc, BuildDeviceRegistry());

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "WSL060");
    }

    [Fact]
    public void Compile_ExplicitSafetyGuard_IsRejectedBecauseTheExecutorCannotEnforceIt()
    {
        var doc = BuildValidDocument() with
        {
            Nodes =
            [
                new WslNode("start", "Start", "Start", new Dictionary<string, object?> { ["entryPoint"] = true }),
                new WslNode("guard", "Guard", "SafetyGuard", new Dictionary<string, object?>()),
                new WslNode("end", "End", "Terminate", new Dictionary<string, object?>())
            ]
        };

        var result = new WslCompiler().Compile(doc, BuildDeviceRegistry(), new FakeSafetyEngine());

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "WSL060");
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
    public void Compile_ParallelNode_IsRejectedBecauseTheExecutorIsSequential()
    {
        var doc = BuildValidDocument() with
        {
            Nodes =
            [
                new WslNode("start", "Start", "Start", new Dictionary<string, object?> { ["entryPoint"] = true }),
                new WslNode("parallel", "Parallel", "Parallel", new Dictionary<string, object?>()),
                new WslNode("end", "End", "Terminate", new Dictionary<string, object?>())
            ]
        };

        var result = new WslCompiler().Compile(doc, BuildDeviceRegistry());

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "WSL060");
    }
}

public sealed class CapabilityReadExtensionsTests
{
    [Fact]
    public async Task ReadObservationAsync_UsesOnlyTheCanonicalGetterAllowlist()
    {
        var observations = new CountingObservations(new SimulatorObservation(
            "simulator", "flowRate", Guid.NewGuid(), "1.5", "mL/min", 1, DateTimeOffset.UtcNow));

        var known = await observations.ReadObservationAsync("GetFlow");
        var rejected = await observations.ReadObservationAsync("SetFlow");

        Assert.Equal(ObservationReadStatus.Known, known.Status);
        Assert.Equal(1.5m, Assert.IsType<decimal>(known.Value));
        Assert.Equal(ObservationReadStatus.Invalid, rejected.Status);
        Assert.Equal(1, observations.ReadCount);
        Assert.Equal("flowRate", observations.LastStateKey);
    }

    [Fact]
    public async Task ReadObservationAsync_StaleObservationRemainsUnknown()
    {
        var observations = new CountingObservations(new SimulatorObservation(
            "simulator", "temperature", Guid.NewGuid(), "25", "°C", 1, DateTimeOffset.UtcNow.AddSeconds(-3)));

        var result = await observations.ReadObservationAsync("GetTemperature");

        Assert.Equal(ObservationReadStatus.Unknown, result.Status);
        Assert.Null(result.Value);
        Assert.Equal("observation.stale", result.Code);
    }

    private sealed class CountingObservations(SimulatorObservation observation) : ISimulatorObservation
    {
        private readonly SimulatorObservation _observation = observation;
        public int ReadCount { get; private set; }
        public string? LastStateKey { get; private set; }

        public Task<SimulatorObservation?> GetAsync(string deviceId, string stateKey, CancellationToken cancellationToken = default)
        {
            ReadCount++;
            LastStateKey = stateKey;
            return Task.FromResult<SimulatorObservation?>(_observation);
        }
    }
}
public sealed class WorkflowCompilerAdmissionTests
{
    [Fact]
    public void CompileWithFeedback_ReturnsEveryFreshCheckWithoutGrantingDispatchAuthority()
    {
        var compiler = new WslCompiler();
        var document = new WslDocument
        {
            DocumentId = "proposal",
            Name = "Proposal",
            SchemaVersion = "1.0",
            Nodes =
            [
                new WslNode("start", "Start", "Start", new Dictionary<string, object?> { ["entryPoint"] = true }),
                new WslNode("flow", "Set flow", "Action", new Dictionary<string, object?>
                {
                    ["device"] = "simulator", ["capability"] = "SetFlow", ["flowRate"] = 1.5m
                }),
                new WslNode("end", "End", "Terminate", new Dictionary<string, object?>())
            ],
            Edges = [new WslEdge("start-flow", "start", "flow"), new WslEdge("flow-end", "flow", "end")]
        };
        var registry = new DeviceRegistry();
        registry.Register(new DeviceDescriptor(
            "simulator", "simulator", "simulator",
            ["SetFlow", "GetFlow", "SetTemperature", "GetTemperature", "SetMode", "GetMode"],
            new Dictionary<string, object?>()));
        var passed = new Dictionary<string, CommandCheckStatus>
        {
            ["capability"] = CommandCheckStatus.Passed,
            ["safety"] = CommandCheckStatus.Passed,
            ["lease"] = CommandCheckStatus.Passed,
            ["resource"] = CommandCheckStatus.Passed,
            ["budget"] = CommandCheckStatus.Passed,
            ["termination"] = CommandCheckStatus.Passed,
            ["health"] = CommandCheckStatus.Passed
        };

        var allowed = compiler.CompileWithFeedback(
            document, registry, new CommandAdmissionEvaluator(), new CommandAdmissionContext(passed, "revision-a"));
        var changed = compiler.CompileWithFeedback(
            document, registry, new CommandAdmissionEvaluator(),
            new CommandAdmissionContext(new Dictionary<string, CommandCheckStatus>(passed)
            {
                ["safety"] = CommandCheckStatus.Unknown
            }, "revision-b"));

        Assert.True(allowed.Compilation.Success);
        Assert.Equal(CommandAdmissionState.Allowed, allowed.Admission.State);
        Assert.Equal(7, allowed.Admission.Checks.Count);
        Assert.False(allowed.IsDispatchAuthorization);
        Assert.Equal(CommandAdmissionState.Held, changed.Admission.State);
        Assert.False(changed.IsDispatchAuthorization);
    }
}
public sealed class CanonicalCapabilityCompilerTests
{
    [Fact]
    public void Compile_LegacyCapabilityAndArbitraryParameter_AreRejected()
    {
        var document = new WslDocument
        {
            DocumentId = "legacy-proposal",
            Name = "Legacy proposal",
            SchemaVersion = "1.0",
            Nodes =
            [
                new WslNode("start", "Start", "Start", new Dictionary<string, object?> { ["entryPoint"] = true }),
                new WslNode("action", "Legacy", "Action", new Dictionary<string, object?>
                {
                    ["device"] = "simulator", ["capability"] = "SetPumpMode", ["mode"] = "Purge", ["unsafe"] = true
                }),
                new WslNode("end", "End", "Terminate", new Dictionary<string, object?>())
            ],
            Edges = [new WslEdge("start-action", "start", "action"), new WslEdge("action-end", "action", "end")]
        };
        var registry = new DeviceRegistry();
        registry.Register(new DeviceDescriptor("simulator", "simulator", "simulator", ["SetMode"], new Dictionary<string, object?>()));

        var result = new WslCompiler().Compile(document, registry);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "WSL061");
    }
}