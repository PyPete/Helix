# Testing Strategy

**Analysis Date:** 2026-05-26

## Test Projects

| Project | Type | Location | Covers |
|---------|------|----------|--------|
| `Helix.Orchestration.Core.Tests` | Unit | `Helix.Orchestration/tests/` | Leasing, execution, runspace, safety, state, trends, WSL compiler/document |
| `Helix.Orchestration.Agent.Tests` | Unit | `Helix.Orchestration/tests/` | DurableRunspace, WslExecutionAdapter, LeaseExpiryWorker, InMemory trend providers |
| `Helix.Orchestration.Devices.Tests` | Unit | `Helix.Orchestration/tests/` | Transport abstractions, Modbus/OPC/Serial/TCP/CDll transports, CircuitBreaker, ReconnectPolicy, TransportWatchdog |
| `Helix.Orchestration.SiteServer.Tests` | Integration | `Helix.Orchestration/tests/` | HTTP endpoints (experiments, campaigns, devices, trends), CampaignManager, GrpcAgentProxy, SimAgentProxy, simulation |
| `Helix.Orchestration.Infrastructure.Tests` | Integration (Docker) | `Helix.Orchestration/tests/` | PostgresExperimentRepository, PostgresCampaignRepository |
| `Helix.Orchestration.Recorder.Tests` | Unit | `Helix.Orchestration/tests/` | RecordedEvent parsing, RecorderWriterService flush logic |

## Test Frameworks

**Runner:** xUnit 2.9.x (all projects)

**Mocking:**
- NSubstitute 5.x — used in `Helix.Orchestration.Devices.Tests` for interface fakes (`Substitute.For<IDeviceTransport>()`, `Substitute.For<IOpcSession>()`)
- Moq 4.20.x — used in `Helix.Orchestration.Agent.Tests`, `Helix.Orchestration.SiteServer.Tests`, and `Helix.Orchestration.Recorder.Tests` (`new Mock<IAgentProxy>()`, `.Setup(...)`, `.Verify(...)`)
- Both libraries coexist: NSubstitute preferred for transport/device layer, Moq preferred for service layer

**Assertions:** xUnit built-in (`Assert.*`) — no FluentAssertions

**Coverage:** coverlet.collector 6.x included in most test projects; no enforced coverage threshold configured

**Integration test HTTP:** `Microsoft.AspNetCore.Mvc.Testing` 10.x in `Helix.Orchestration.SiteServer.Tests`

**Integration test database:** `Testcontainers.PostgreSql` 4.x in `Helix.Orchestration.Infrastructure.Tests`

## Run Commands

```bash
# Run all tests in the solution
dotnet test Helix.Orchestration/Helix.Orchestration.slnx -c Release

# Run with TRX results (as CI does)
dotnet test Helix.Orchestration/Helix.Orchestration.slnx --no-build -c Release --logger trx --results-directory test-results

# Run a single test project
dotnet test Helix.Orchestration/tests/Helix.Orchestration.Core.Tests/ -c Release
```

## Test File Organisation

**Location:** Tests live in `Helix.Orchestration/tests/{ProjectName}.Tests/` (separate from source, inside the solution folder). The Recorder tests are outside the main solution under `tests/Helix.Orchestration.Recorder.Tests/`.

**Naming:** `{SubjectClass}Tests.cs` — e.g., `DeviceLeasingTests.cs`, `CircuitBreakerTests.cs`, `ExperimentEndpointsTests.cs`

**Folder structure mirrors source structure:**
```
tests/Helix.Orchestration.Core.Tests/
├── Capabilities/CapabilityRegistryTests.cs
├── Execution/WorkflowExecutorTests.cs
├── Leasing/DeviceLeasingTests.cs
├── Leasing/DeviceLeasingRenewalTests.cs
├── Runspace/RunspaceManagerTests.cs
├── Safety/SafetyEngineTests.cs
├── State/GlobalStateStoreTests.cs
├── State/WorkflowStateExtensionsTests.cs
├── TestContext/TestContextTests.cs
├── Trends/SpectralDataModelTests.cs
├── Trends/TrendFunctionsTests.cs
├── Trends/TrendValueTests.cs
└── WSL/WslCompilerTests.cs, WslDocumentTests.cs
```

## Test Patterns

### Fact (Single scenario)

All test methods are `async Task` returning, even when the code under test is synchronous. `[Fact]` is the dominant attribute.

```csharp
[Fact]
public async Task AcquireExclusive_WhenFree_ReturnsLease()
{
    var svc = new InMemoryDeviceLeasingService();
    await using var lease = await svc.AcquireAsync("device-1", "workflow-A");
    Assert.Equal("device-1", lease.DeviceId);
}
```

### Theory / InlineData (Parameterised)

Used sparingly, primarily for boundary/edge-case tables:

```csharp
[Theory]
[InlineData("helix/site-01/fumehood-02")]                // Too short
[InlineData("invalid/site-01/fumehood-02/exp-123/...")]  // Wrong prefix
public void TryParseFromMqttMessage_InvalidTopic_ReturnsFalse(string topic)
{
    var success = RecordedEvent.TryParseFromMqttMessage(topic, payload, out var result);
    Assert.False(success);
    Assert.Null(result);
}
```

Files using Theory: `TransportAbstractionsTests.cs`, `ReconnectPolicyTests.cs`, `RecordedEventParseTests.cs`

### Test Method Naming

Pattern: `{MethodUnderTest}_{Condition}_{ExpectedOutcome}`

Examples:
- `AcquireExclusive_WhenAlreadyHeld_Throws`
- `ExecuteAsync_TransitionsClosedToOpen_AfterFailureThreshold`
- `ReadHoldingRegistersAsync_WhenIOException_SetsFaulted`
- `PostExperiment_ValidRequest_Returns200WithRunspaceId`

### Arrange / Act / Assert comments

Used inconsistently — present in some test files (`SafetyEngineTests.cs`, `WorkflowExecutorTests.cs`, `RecorderWriterServiceTests.cs`) as `// Arrange`, `// Act`, `// Assert` inline comments. Absent in others (leasing tests, circuit breaker tests) where the flow is self-evident.

### Static factory helpers within test classes

Private static helper methods named `Create{Subject}` or `Build{Subject}` are used to reduce boilerplate:

```csharp
private static InMemoryDeviceLeasingService CreateService() => new();
private static ModbusDeviceTransport CreateTransport(FakeModbusClient client, byte slaveId = 1) { ... }
private static RunspaceDefinition CreateDefinition(string name) => new(...);
private static WslDocument BuildValidDocument() => new() { ... };
```

### Inner stub/fake/spy classes

Preferred over mocking frameworks for complex dependencies that need behaviour (not just interaction verification). Defined as `private sealed class` nested inside the test class:

```csharp
private sealed class StubWorkflowExecutor : IWorkflowExecutor
{
    public string? LastWorkflowName { get; private set; }
    public Task ExecuteAsync(string name, string id, ...) { LastWorkflowName = name; return Task.CompletedTask; }
}
private sealed class BlockingWorkflowExecutor : IWorkflowExecutor
{
    public TaskCompletionSource ExecutionStarted { get; } = new(...);
    public async Task ExecuteAsync(...) { ExecutionStarted.TrySetResult(); await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken); }
}
```

Files: `DurableRunspaceTests.cs`, `RunspaceManagerTests.cs`, `WorkflowExecutorTests.cs`, `ModbusTransportTests.cs`

### NSubstitute (interface substitution)

Used for simple interface fakes where no custom behaviour is needed beyond return value:

```csharp
var transport = Substitute.For<IDeviceTransport>();
transport.Status.Returns(TransportStatus.Connected);
```

Files: `DeviceAdapterBaseTests.cs`, `TransportWatchdogTests.cs`, `OpcTransportTests.cs`

### Moq (mock with setup/verify)

Used when interaction verification (`.Verify(...)`) is required:

```csharp
var agentProxy = new Mock<IAgentProxy>();
agentProxy
    .Setup(a => a.SubmitExperimentAsync(It.IsAny<RunspaceDefinition>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(() => $"runspace-{++counter}");
// ...
agentProxy.Verify(a => a.SubmitExperimentAsync(...), Times.Exactly(2));
```

Files: `CampaignManagerTests.cs`, `GrpcAgentProxyTests.cs`, `RecorderWriterServiceTests.cs`

### xUnit Fixtures

**`IClassFixture<T>`** used for shared setup across tests in one class without re-creating expensive state per test:
- `IClassFixture<HelixWebApplicationFactory>` — injects a single shared `WebApplicationFactory` into all endpoint test classes
- Used in: `ExperimentEndpointsTests`, `CampaignEndpointsTests`, `DeviceEndpointsTests`, `TrendEndpointsTests`

**`ICollectionFixture<T>` + `[Collection]`** used for sharing a fixture across multiple test classes:
- `PostgresFixture` is shared via `PostgresRepositoriesTestsCollection` to avoid spinning up multiple containers

### Integration tests with `WebApplicationFactory`

`HelixWebApplicationFactory` extends `WebApplicationFactory<Program>`, removes the real `MqttTrendBridge` hosted service, and substitutes a `NoOpHostedService` to prevent broker connection during tests:

```csharp
public sealed class HelixWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.AddHostedService<NoOpHostedService>();
        });
    }
}
```

Tests use `factory.CreateClient()` and call HTTP endpoints directly: `await _client.PostAsJsonAsync("/api/experiments", request)`.

### Docker-gated tests

Infrastructure tests that require a live Postgres container are decorated with `[Fact(Skip = "Requires Docker. Run with Docker available.")]` and guarded by `if (!_fixture.IsAvailable) return;`. The `PostgresFixture` swallows startup errors and sets `IsAvailable = false`, so the tests skip cleanly in CI environments without Docker.

## Coverage Observations

**Well covered:**
- Device leasing (acquire, release, expiry, renewal, force-release, shared vs exclusive)
- Circuit breaker state transitions
- WSL document compilation and validation
- Safety engine interlock registration, evaluation, and emergency stop
- Capability registry registration and invocation dispatch
- Runspace lifecycle (create, start, pause, abort, complete)
- HTTP experiment and campaign endpoints (happy path + error cases)
- MQTT topic parsing in the Recorder

**Light coverage:**
- `TrendAggregator`, `MqttTrendIngestionService`, `InMemoryTrendProvider` — logic present but tests for MQTT ingestion are absent from the solution (the `MqttTrendIngestionService` is replaced with a no-op in integration tests)
- `WslExecutionAdapter` — test file exists (`WslExecutionAdapterTests.cs`) but content not fully inspected
- `SiteServer` routing, middleware, and gRPC serialisation beyond the endpoint handler level
- Simulation devices (`SimReactor`, `SimPump`, `SimSpectrometer`) — tested only via `SimReactorTests.cs` and `SimulatedTrendProviderTests.cs`

**Missing:**
- No E2E tests
- No performance or load tests
- No contract tests for the gRPC agent control service
- `Helix.Orchestration.Recorder` background service flush timing relies on `Task.Delay` sleeps in tests which is fragile

## Test Data / Fixtures

Test objects are constructed inline using object initialisers or positional record constructors. No shared fixture factories exist outside of test classes. Data is minimal and purpose-built per test case:

```csharp
var record = new ExperimentRecord(
    RunspaceId: "runspace-1",
    CampaignId: "camp-1",
    ...);
```

No test data files, JSON fixtures, or AutoFixture/Bogus usage detected.

---

*Testing analysis: 2026-05-26*
