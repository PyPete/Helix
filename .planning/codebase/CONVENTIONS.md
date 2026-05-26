# Coding Conventions

**Analysis Date:** 2026-05-26

## Naming Conventions

**Classes:**
- PascalCase throughout
- Concrete service/component classes are almost always `sealed`: `public sealed class DeviceLease`, `public sealed class CapabilityRegistry`, `public sealed class DurableRunspace`
- Non-sealed classes are the exception (used for `TransportFaultException`, `Worker`, `MinioObjectStorageSink`)
- Exception classes extend `Exception` and are `sealed`: `DeviceAlreadyLeasedException`, `CircuitBreakerOpenException`

**Interfaces:**
- `I`-prefix: `IDeviceLeasingService`, `IWorkflowExecutor`, `IRunspaceManager`, `ICapabilityInvoker`
- One interface per file, named to describe the capability not the implementation

**Methods:**
- PascalCase for all public and private methods
- Async methods always use `Async` suffix: `AcquireAsync`, `ExecuteAsync`, `RenewAsync`, `ForceReleaseAsync`
- Factory/static helper methods use verb-noun: `CapabilityInvocationResult.Succeeded(...)`, `CapabilityInvocationResult.Failed(...)`

**Fields and Properties:**
- Private instance fields: `_camelCase` prefix with underscore: `_leasingService`, `_workflowExecutor`, `_leases`, `_lock`
- Public properties: PascalCase: `LeaseId`, `HolderName`, `ExpiresAt`
- Static readonly fields (telemetry, singletons): PascalCase: `HelixTelemetry.TrendsPublished`, `EmptyObservable<T>.Instance`

**Enums:**
- PascalCase type and member names: `LeaseMode.Exclusive`, `LeaseMode.Shared`, `RunspaceStatus.Idle`, `CircuitBreakerState.Open`

**Records:**
- `public sealed record` for immutable data/value objects: `DeviceDescriptor`, `CapabilityInvocationResult`, `TrendValue`, `Spectrum1D`, `WorkflowState`
- Positional syntax used for simple records: `public sealed record DiodeArraySlice(DateTimeOffset Timestamp, double[] Intensities)`
- Property-initialiser syntax used when XML doc comments are needed per-property

## Namespaces

- File-scoped namespace declarations (C# 10+): `namespace Helix.Orchestration.Agent.Execution;`
- Namespace mirrors directory structure under the assembly root: `Helix.Orchestration.Agent/Execution/WslExecutionAdapter.cs` → `namespace Helix.Orchestration.Agent.Execution`
- One type per file (enforced by structure)

## Code Organisation

- Feature folder layout inside each project: `Leasing/`, `Execution/`, `Runspace/`, `Trends/`, `Safety/`, `State/`
- Extension methods for DI registration placed in dedicated `*Extensions.cs` files in the same folder: `DeviceAdapterExtensions.cs`, `InfrastructureServiceExtensions.cs`
- `static` extension classes named `{Feature}Extensions` or `{Service}Extensions`

## Error Handling

**Guard clauses:**
Two styles are used, sometimes within the same class:
- Newer .NET 7+ style: `ArgumentNullException.ThrowIfNull(document)`, `ArgumentException.ThrowIfNullOrWhiteSpace(holderName)` — preferred in method bodies
- Inline null-coalescing throw in constructors: `_leasingService = leasingService ?? throw new ArgumentNullException(nameof(leasingService))`

**Domain exceptions:**
- Custom sealed exception classes for domain violations: `DeviceAlreadyLeasedException`, `CircuitBreakerOpenException`, `TransportFaultException`
- Exceptions carry context properties: `DeviceAlreadyLeasedException.DeviceId`, `.RequestedMode`

**Result pattern:**
- `CapabilityInvocationResult` is a sealed record with `Success`, `ErrorMessage`, `Exception`, and `ReturnValue` — used for capability invocations instead of throwing
- `static CapabilityInvocationResult.Succeeded(...)` and `.Failed(...)` factory methods

**Exception propagation:**
- Exceptions are caught selectively; `OperationCanceledException` is handled distinctly from general `Exception` in `DurableRunspace.ExecuteAsync`
- Catch-and-rethrow used in `WslExecutionAdapter.ExecuteAsync` to ensure lease cleanup before propagation

## Logging

**Framework:** Serilog with `Microsoft.Extensions.Logging.ILogger<T>` abstraction

**Setup:** Serilog configured at bootstrap in `Program.cs` via `builder.Host.UseSerilog(...)`, writing compact JSON to console and rolling daily file (`logs/helix-agent-.log`)

**Injection pattern:** `ILogger<T>` injected via constructor or primary constructor:
```csharp
public sealed class AgentService(ILogger<AgentService> logger) : IHostedService
```

**Structured logging:** Message templates with named placeholders used throughout:
```csharp
logger.LogInformation("Purged {PurgedLeaseCount} expired leases", purged);
logger.LogWarning("Ignoring message with unrecognized topic structure: {Topic}", topic);
```

**Test logging:** `NullLogger<T>.Instance` and `NullLogger.Instance` used in tests — no log output asserted

## Async Patterns

- All async methods return `Task` or `Task<T>`; `ValueTask`/`ValueTask<T>` used only for `IAsyncDisposable.DisposeAsync()` implementations
- `.ConfigureAwait(false)` used on await calls in library/service code that does not need a synchronisation context: `WslExecutionAdapter.ExecuteAsync`
- `CancellationToken` is the last parameter in all async signatures with a default of `default`
- `await using` used for `IAsyncDisposable` resources (leases in tests and production)
- Top-level `await app.RunAsync()` in `Program.cs` (minimal hosting model)

## Dependency Injection

**Registration style:** `Microsoft.Extensions.DependencyInjection` throughout

**Lifetimes:**
- `AddSingleton` for stateful long-lived services: `InMemoryDeviceLeasingService`, `RunspaceManager`, `TrendAggregator`, `CapabilityRegistry`
- `AddScoped` for per-request repositories: `PostgresExperimentRepository`, `PostgresCampaignRepository`
- `AddTransient` for transports: `SerialTransport`, `ModbusTransport`, `TcpTransport`, `OpcTransport`
- `AddHostedService` for background workers: `AgentService`, `LeaseExpiryWorker`, `MqttTrendIngestionService`

**Extension methods:** DI wiring collected into feature extension methods on `IServiceCollection`:
```csharp
services.AddMigratedDeviceAdapters();   // DeviceAdapterExtensions.cs
services.AddHelixInfrastructure(config); // InfrastructureServiceExtensions.cs
```

**Interface aliasing:** Where a concrete type needs both direct and interface resolution, both registrations are made explicitly:
```csharp
builder.Services.AddSingleton<InMemoryDeviceLeasingService>();
builder.Services.AddSingleton<IDeviceLeasingService>(sp => sp.GetRequiredService<InMemoryDeviceLeasingService>());
```

## Thread Safety

- Shared mutable state protected with `lock` on a dedicated `private readonly object _lock = new()` field
- `ConcurrentDictionary<K,V>` used for read-heavy, low-contention registries
- `SemaphoreSlim` used as an async gate in `DurableRunspace` for state transitions

## XML Documentation

- `/// <summary>` on all public types and public/internal members
- `/// <inheritdoc/>` used on interface implementation members to avoid duplication
- `/// <param name="...">` and `/// <exception cref="...">` used where relevant

---

*Convention analysis: 2026-05-26*
