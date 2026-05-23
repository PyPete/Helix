# Helix.Orchestration.Core — Phase 1 Handover
## Stratified Trends, Spectral Analytics, Leasing, WSL, Runspace, TestContext

**Phase:** 1 — Core Domain Contracts  
**Status:** Complete  
**Test count:** 56 passing, 0 failing  
**Date completed:** May 2026

---

## What This Phase Delivered

This phase built the foundational domain contracts and in-process implementations for `Helix.Orchestration.Core`. No transport, no UI, no agent host — only the types that everything else will depend on.

Five subsystems were implemented:

| Subsystem | Folder | Summary |
|---|---|---|
| Stratified Trends | `Trends/` | Typed trend hierarchy covering scalar, spectral (1D + 2D), diode array, image, video, derived |
| Spectral Analytics | `Trends/` | MS, MS/MS, NMR (1H and 2D COSY) with 1D scan and 2D matrix payloads |
| Device Leasing | `Leasing/` | Exclusive/shared lease lifecycle with TTL support |
| WSL + RunspaceDefinition | `WSL/` | Declarative workflow authoring graph compiled to execution format |
| Runspace Abstraction | `Runspace/` | Execution slot lifecycle contract + in-memory implementation |
| TestContext | `TestContext/` | Simulation-capable experiment context with injection and fault hooks |

---

## Subsystem Detail

### 1. Stratified Trend Model

**Location:** `Helix.Orchestration.Core/Trends/`

The trend hierarchy is a discriminated model rooted at `ITrend`:

```
ITrend
├── IScalarTrend<T>           — double, int, string, bool scalars
├── ISpectralTrend            — 1D scans (SpectralScan) + 2D matrices (SpectralMatrix)
├── IDiodeArrayTrend          — DAD/PDA chromatography (time × wavelength slices)
├── IImageTrend               — binary storage URI frames
├── IVideoTrend               — streaming segment references
└── IDerivedTrend             — expression/Python-computed from source trends
```

`TrendKind` enum discriminates at runtime. `ITrendProvider` is the single surface for workflow code to access all trend kinds.

**Key types:**

| Type | Role |
|---|---|
| `ITrend` | Root: identity, units, experiment linkage, `IObservable<TrendValue>` stream |
| `TrendKind` | `Scalar, Spectral, DiodeArray, Image, Video, AnalyticsResult, Derived` |
| `TrendValue` | Discriminated union: `ScalarValue<T>`, `SpectralValue`, `SpectralMatrixValue`, `ImageValue`, `VideoValue`, `StructuredResultValue` |
| `ITrendProvider` | Discovery, typed accessors, derived trend registration |
| `TrendWindow<T>` | Rolling scalar buffer for real-time analytics (existing, unchanged) |
| `TrendFunctions` | `Average`, `Slope`, `IsStable`, `Min`, `Max`, `InRange` helpers (existing, unchanged) |

---

### 2. Spectral Analytics

**Location:** `Helix.Orchestration.Core/Trends/`

Spectral data covers both online PAT and offline analytics modes.

**SpectralType enum:**

| Value | Modality |
|---|---|
| `MIR` | Mid-infrared (online PAT) |
| `UV` | UV/Vis (online PAT) |
| `NIR` | Near-infrared (online PAT) |
| `Raman` | Raman spectroscopy (online PAT) |
| `Fluorescence` | Fluorescence spectroscopy |
| `MS` | Mass spectrometry (online or offline) |
| `MSMS` | Tandem MS / MS-MS |
| `NMR1D` | 1H NMR (benchtop or offline) |
| `NMR2D` | 2D NMR (COSY, HSQC, HMBC, NOESY) |

**Data shapes:**

| Type | Structure | When used |
|---|---|---|
| `SpectralScan` | `TrendId, XData[], YData[], SpectralType, SampleId, Timestamp, Metadata` | Any 1D online or offline spectrum |
| `SpectralMatrix` | `TrendId, Axis1[], Axis2[], Intensity[,], SpectralType, Timestamp, Metadata` | MS/MS maps, 2D COSY, stacked spectra |

**NMR-specific metadata convention** (carried in `Metadata` dictionary):

```
"Nucleus"         → "1H", "13C", "31P"
"ExperimentType"  → "COSY", "HSQC", "HMBC", "NOESY"
"SolventName"     → "DMSO-d6", "CDCl3"
"SpectrometerMHz" → 400, 600
"PulseSequence"   → vendor pulse program name
```

`ISpectralTrend` exposes both 1D and 2D access:

```csharp
SpectralScan?          LatestScan     { get; }
IReadOnlyList<SpectralScan> Scans     { get; }
IReadOnlyList<SpectralScan> GetRecentScans(int n);

SpectralMatrix?        LatestMatrix   { get; }
IReadOnlyList<SpectralMatrix> Matrices { get; }
IReadOnlyList<SpectralMatrix> GetRecentMatrices(int n);
```

---

### 3. Device Leasing

**Location:** `Helix.Orchestration.Core/Leasing/`

Workflows acquire leases before using devices. Disposing a lease returns the device to the pool.

```csharp
await using var lease = await leasingService.AcquireAsync(
    deviceId:   "reactor-1",
    holderName: workflowId,
    mode:       LeaseMode.Exclusive,
    ttl:        TimeSpan.FromMinutes(30));
```

**Rules:**

| Situation | Outcome |
|---|---|
| Device free → Exclusive lease | Granted |
| Device has Exclusive → any new request | `DeviceAlreadyLeasedException` |
| Device has Shared(s) → Shared request | Granted |
| Device has Shared(s) → Exclusive request | `DeviceAlreadyLeasedException` |
| Lease TTL expired | Pruned on next acquire; device treated as free |
| Safety/emergency stop | `ForceReleaseAsync` clears all leases on a device |

**Types:**

| Type | Role |
|---|---|
| `LeaseMode` | `Exclusive`, `Shared` |
| `DeviceLease` | Active lease handle; `IAsyncDisposable` |
| `DeviceAlreadyLeasedException` | Conflict exception with device + mode context |
| `IDeviceLeasingService` | Acquire, list active, IsHeldBy, ForceRelease |
| `InMemoryDeviceLeasingService` | Thread-safe in-process implementation |

---

### 4. WSL — Workflow Specification Language

**Location:** `Helix.Orchestration.Core/WSL/`

WSL is the declarative authoring format produced by Node-Flow UI, chatbot, ELN, or ML optimiser. It is a directed graph of nodes and edges compiled into a `RunspaceDefinition` for agent execution.

```
WslDocument (authored) ──Compile()──▶ RunspaceDefinition (executed)
```

**Types:**

| Type | Role |
|---|---|
| `WslNode` | Single step/action with `NodeId`, `NodeType`, `Label`, `Parameters` |
| `WslEdge` | Directed connection; optional boolean `Condition` expression |
| `WslDocument` | Root record: graph + `Compile()` method |
| `WslValidationException` | Structured list of validation errors |
| `RunspaceDefinition` | Compiled, immutable: `DefinitionId`, `Name`, `Nodes`, `Edges`, `Metadata` |

**Validation rules on `Compile()`:**

- `Name` must be non-empty
- Every edge `SourceNodeId` must reference an existing node
- Every edge `TargetNodeId` must reference an existing node

**Example usage:**

```csharp
var doc = new WslDocument
{
    Name = "Amide Coupling Run 1",
    Nodes = [ new WslNode("n1", "Heat to 80°C", "Ramp", ...) ],
    Edges = []
};

RunspaceDefinition def = doc.Compile();  // throws WslValidationException on error
```

---

### 5. Runspace Abstraction

**Location:** `Helix.Orchestration.Core/Runspace/`

A Runspace is the Helix-level execution slot. It is intentionally decoupled from Cleipnir — the durable runtime is an implementation detail of the agent host, not the core contract.

**Lifecycle:**

```
Idle ──StartAsync()──▶ Running ──PauseAsync()──▶ Paused
                          │                        │
                          │◀────ResumeAsync()──────┘
                          │
                   ┌──────┴──────┐
                   ▼             ▼
               Completed      Faulted
                   └─────┬─────┘
                         ▼
                      Disposed
```

**Types:**

| Type | Role |
|---|---|
| `RunspaceStatus` | `Idle, Running, Paused, Completed, Faulted, Disposed` |
| `IRunspace` | Contract: `Start`, `Pause`, `Resume`, `Abort`, `WaitAsync`, `IAsyncDisposable` |
| `InMemoryRunspace` | Delegate-backed in-process runspace for tests and local dev |

`InMemoryRunspace` accepts a `Func<CancellationToken, Task>` body — enough to wire any workflow delegate without importing a full durable runtime.

---

### 6. TestContext

**Location:** `Helix.Orchestration.Core/TestContext/`

`TestContext` is a simulation-capable experiment context. It implements `IExperimentContext` (delegating to `ExperimentContext`) and adds hooks for injecting simulated data and faults.

**Usage in unit tests:**

```csharp
var ctx = new TestContext("exp-001", "SampleExperiment");
ctx.RegisterVirtualDevice("pump-sim-1", "PeristalticPump");
ctx.InjectScalar("temp-01", 25.3);
ctx.SimulateFault("pump-sim-1", new InvalidOperationException("Blocked"));

// Inside the code under test:
ctx.ThrowIfFaulted("pump-sim-1");   // throws InvalidOperationException

var events = ctx.GetEvents();       // ordered event log
```

**Types:**

| Type | Role |
|---|---|
| `ITestContext` | Extends `IExperimentContext` with injection and fault hooks |
| `TestContext` | Implementation; delegates lifecycle to `ExperimentContext` |
| `TestEvent` | Single event record: `Kind`, `Timestamp`, `DeviceOrTrendId`, `Detail` |
| `TestEventKind` | `ScalarInjected, SpectralScanInjected, SpectralMatrixInjected, DeviceRegistered, FaultSimulated, FaultCleared` |

---

## Existing Infrastructure (Pre-Phase 1, Unchanged)

These were already present and were not modified except to extend interfaces forward-compatibly:

| Type | Location | Status |
|---|---|---|
| `WorkflowContext` | `WorkflowEngine/` | Unchanged; holds `Capabilities`, `Trends`, `Safety`, `State`, optional `Experiment` |
| `WorkflowExecutor` | `Execution/` | Unchanged; Cleipnir-backed durable workflow runner |
| `ExperimentContext` | `Experiment/` | Unchanged; lifecycle, metadata, `RecordDataAsync` |
| `CapabilityRegistry` | `Capabilities/` | Unchanged; named capability invoke/register |
| `SafetyEngine` | `Safety/` | Unchanged; interlock rules, emergency stop token |
| `WorkflowStateStore` | `State/` | Unchanged; in-memory workflow state store |
| `TrendWindow<T>` | `Trends/` | Unchanged; rolling scalar buffer |
| `TrendFunctions` | `Trends/` | Unchanged; Average, Slope, IsStable, Min, Max, InRange |

---

## Test Coverage

| Test class | Count | Covers |
|---|---|---|
| `WorkflowExecutorTests` | 2 | Executor lifecycle, trend contract compatibility |
| `TrendValueTests` | 2 | SpectralMatrixValue payload, SpectralType enum coverage |
| `DeviceLeasingTests` | 7 | Exclusive/shared conflict, dispose, TTL expiry, ForceRelease, IsHeldBy |
| `WslDocumentTests` | 6 | Compile success, empty graph, name validation, bad edge source/target, metadata |
| `TestContextTests` | 9 | Lifecycle, scalar/spectral injection, virtual devices, fault simulation, event ordering |
| `CapabilityRegistryTests` | 6 | Register, unregister, invoke, duplicate detection |
| `SafetyEngineTests` | 8 | Interlock rules, emergency stop, reset, monitoring loop |
| `TrendFunctionsTests` | 12 | Average, slope, stability, thresholds, min/max |
| **Total** | **56** | **All passing** |

---

## NuGet / Build Notes

A `nuget.config` was added at solution level to suppress VS IDE NU1301 errors caused by an expired GitLab PAT in the global NuGet config. The config clears all external feeds and routes to `nuget.org` only — all packages used by this solution are available there.

```
Helix.Orchestration/nuget.config   ← clears GitLab, Telerik, Scichart feeds for this solution
```

`dotnet build` and `dotnet test` both succeed cleanly. The GitLab feed errors shown in the VS IDE build output are a credential issue, not a code issue.

---

## What Is Not Yet Implemented

These are known next-phase items, not omissions from this phase:

| Item | Notes |
|---|---|
| Agent host project | `Helix.Orchestration.Agent` — will wire `IRunspace`, leasing, safety, adapters |
| Site Server project | `Helix.Orchestration.SiteServer` — Campaign, WSL ingestion API, experiment registry |
| Cleipnir-backed runspace | Production `IRunspace` implementation wrapping `FunctionsRegistry` |
| MQTT/NATS trend streaming | Transport adapters for `ITrendProvider` live feeds |
| Device adapters | OPC UA, SiLA 2, serial; per-device capability implementations |
| `IPythonTrendEvaluator` | Interface exists; no runtime wired yet |
| Campaign Manager | Site Server responsibility — multi-experiment DoE/sweep orchestration |
| USB Bridge Agent | Serial port discovery, mDNS probing |
| Node-Flow UI | Frontend; not in scope for this library |
| ML Optimiser integration | Consumes `RunspaceDefinition` via Site Server API |
