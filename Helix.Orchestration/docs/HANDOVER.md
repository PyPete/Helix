# Helix Orchestration — Infrastructure Handover

**Version:** 2.0  
**Scope:** Full architecture, design decisions, technology choices, and conceptual model.

---

## 1. Overview

Helix Orchestration is a distributed lab automation platform coordinating:

- Fumehoods
- Devices and analytical instruments
- Workflows
- Trend data streams
- Experiments

It provides **local execution per fumehood** and **site-level aggregation**, using a capability-based device abstraction and MQTT/NATS streaming for telemetry.

Target domains: process chemistry, analytical science, lab automation.

---

## 2. Core Principles

| Principle | Statement |
|---|---|
| Device-agnostic | Workflows call capability names, not devices or protocols |
| Local execution, global visibility | Each fumehood runs its own agent; the site server aggregates |
| Control vs. data separation | Control: gRPC/REST. Trends: MQTT/NATS. Binary: object storage |
| Experiment-centric | Every action, trend, file, and result is tagged with `ExperimentId` |
| Safety independence | Local agents enforce safety rules without network dependency |
| Authoring vs. execution separation | WSL is declarative authoring input; `RunspaceDefinition` is execution format |
| Campaign isolation | Campaigns are owned by Site Server only; agents execute per-experiment contexts |

---

## 3. Architecture — Top to Bottom

```
Node-Flow UI / ELN / LIMS / Chatbot / ML Optimiser
                    │
                    ▼
┌──────────────────────────────────────────────┐
│ Helix Site Server (ASP.NET)                 │
│ - Workflow Compiler (WSL → RunspaceDefinition)
│ - Campaign Manager                           │
│ - Registry, API, WebSocket Event Hub         │
└──────────────────────────────────────────────┘
                    │ gRPC / REST / WebSocket
                    ▼
┌──────────────────────────────────────────────┐
│ Helix Agent (per FH)                         │
│ - Runspace Execution                          │
│ - ExperimentContext / TestContext             │
│ - Trend + Safety Engine                       │
│ - Device Leasing                              │
└──────────────────────────────────────────────┘
          │                        │
          │                        └─────────────┐
          ▼                                      ▼
┌─────────────────────────┐           ┌─────────────────────────┐
│ Device Adapters         │           │ USB Bridge Agent        │
│ OPC UA / SiLA2 / SDK    │           │ Serial probing + mDNS   │
└─────────────────────────┘           └─────────────────────────┘
          │                                      │
          └──────────────────────┬───────────────┘
                                 ▼
                 ┌────────────────────────────┐
                 │ Physical + Simulated Devices│
                 │ RS232 / OPC UA / SiLA2/REST │
                 └────────────────────────────┘
```

---

## 4. Site Layer — Helix Site Server

**Responsibilities:**

- Registry of sites, fumehoods, agents, devices
- Campaign management (multi-experiment optimisation, DoE, sweeps)
- WSL ingestion and workflow compilation (`WSL` → `RunspaceDefinition`)
- Experiment tracking and status
- MQTT/NATS subscription from all agents
- REST/gRPC API for UI, ELN/LIMS, chatbot, and analytics
- WebSocket event hub for live status and node-flow updates
- Dashboards: device status, fumehood status, experiment history, analytical queues

**Does NOT:**

- Direct device control
- Safety logic
- Per-step workflow execution

**Technology:**

| Component | Choice |
|---|---|
| Runtime | .NET 8 ASP.NET service + background workers |
| Database | PostgreSQL or SQLite (metadata + campaign + experiment registry) |
| Workflow authoring | WSL (JSON declarative contract) |
| Compiler output | `RunspaceDefinition` JSON |
| Telemetry | MQTT/NATS (subscriber) |
| Binary storage | S3 / Blob / MinIO |
| Agent comms | gRPC + REST |
| Realtime events | WebSockets/SignalR |

---

## 5. Fumehood Layer — Helix Agent

**Responsibilities:**

- Hosts device adapters
- Executes compiled `RunspaceDefinition` workflows in Runspaces
- Maintains one `ExperimentContext` or `TestContext` per Runspace
- Enforces local safety rules
- Streams trends via MQTT/NATS
- Handles binary data (chromatograms, spectra, images)
- Performs device leasing (exclusive/shared)
- Supports simulation mode with no physical hardware attached

**Execution model rationale:**  
Runspaces run inside the agent, close to the devices, for low latency, resilience, safety, and deterministic behaviour.

**Technology:**

| Component | Choice |
|---|---|
| Runtime | .NET 8 console app or Windows service |
| Execution abstraction | Helix Runspace (conceptual execution slot) |
| Durability engine | Cleipnir.ResilientFunctions 4.x |
| Messaging | MQTT/NATS client |
| Site comms | gRPC server + REST callbacks |
| Local state | SQLite (cache + state) |
| Drivers | Plugin system — language-agnostic |

---

## 6. Device Layer — Adapters and Capabilities

### Device Adapters

Each device has an adapter translating vendor protocols into capabilities:

| Device | Protocol | Adapter language |
|---|---|---|
| Reactor | OPC UA | C# |
| Pump | Modbus/serial | Python or C# |
| Agilent HPLC | ICF + file-drop | C# + file watcher |
| Camera | SDK / RTSP | Node.js or C# |
| Spectrometer | Vendor SDK | Python |

### Capabilities

Each adapter exposes named verbs:

```
SetTemperature
GetPressure
RunMethod
AcquireSpectrum
CaptureImage
Run2DChromatography
```

### Why this matters

Workflows never reference OPC UA nodes, SiLA 2 features, COM objects, serial commands, or vendor SDKs.  
They only call:

```csharp
await ctx.Capabilities.InvokeAsync("SetTemperature", new { Value = 75 });
```

### Device Integration Layer — `Helix.Orchestration.Devices`

See [`PHASE13-DEVICE-INTEGRATION-LAYER-HANDOVER.md`](PHASE13-DEVICE-INTEGRATION-LAYER-HANDOVER.md) for full architecture, transport hierarchy, fault tolerance design, OPC UA swap path, and test coverage summary.

---

## 7. Data Layer

### 7.1 Trends — Stratified Model

All trends share a common `ITrend` root interface:

```
TrendId, TrendName, TrendKind, Units, ExperimentId, Tags, IsOnline, LastUpdatedAt
```

Data shape varies by `TrendKind`:

| Kind | Shape | Typical sources |
|---|---|---|
| `Scalar` | `double` / `int` / `string` / `bool` | Temperature, pressure, flow, pH |
| `Spectral` | `xData[]` + `yData[]` per scan | MIR, UV/Vis, NIR, Raman spectrometers |
| `DiodeArray` | `wavelengths[]` × timestamped slices (2D) | Online DAD/PDA in HPLC/flow chemistry |
| `Image` | binary storage URI + metadata | Cameras, microscopes |
| `Video` | streaming binary segments | Reaction video capture |
| `AnalyticsResult` | typed result object | Offline peak areas, concentrations |
| `Derived` | computed from ≥1 source trends | Math expressions, Python scripts |

All kinds publish updates as `IObservable<TrendValue>` streams.

**`TrendValue` discriminated union:**

| Case | Contents |
|---|---|
| `Scalar<T>` | typed value (numeric or string) |
| `DoubleArray` | paired `xData[]` + `yData[]` |
| `Spectral` | full `SpectralScan` record |
| `Image` | `ImageFrame` with storage URI |
| `StructuredResult` | named result type + arbitrary object |

**Key interfaces by kind:**

- `IScalarTrend<T>` — backed by `TrendWindow<T>`; `TrendFunctions` operate on it
- `ISpectralTrend` — rolling `SpectralScan` window; `IObservable<SpectralScan>` stream; spectral type: `MIR | UV | NIR | Raman | Fluorescence`
- `IDiodeArrayTrend` — full 2D matrix snapshot; single-λ chromatogram extraction (`IScalarTrend<double>`); single-timepoint spectrum extraction (`SpectralScan`)
- `IImageTrend` — `IObservable<ImageFrame>`; binary payload in S3/MinIO
- `IVideoTrend` — `IObservable<VideoSegment>`; binary payload in S3/MinIO
- `IDerivedTrend` — declares source trend ids and `DerivationKind`

**`ITrendProvider` revised contract:**

- Typed accessors: `GetScalarTrend<T>`, `GetSpectralTrend`, `GetDiodeArrayTrend`, `GetImageTrend`, `GetVideoTrend`, `GetDerivedTrend`
- Discovery: `GetAllTrends()`, `GetTrendsByKind(TrendKind)`, `GetTrendsByExperiment(string)`
- Derived trend registration: `RegisterDerivedTrend(DerivedTrendDefinition)`

**Scalar telemetry** continues to be streamed agent → MQTT/NATS → site + workflows + safety engine, unchanged from v1.

### 7.2 Spectral Trends (MIR / UV online PAT)

- Each scan: `SpectralScan` record — `XData[]` (wavenumbers cm⁻¹ or nm), `YData[]` (absorbance/intensity), `SpectralType`, `SampleId`, `Metadata`
- Rolling window of N recent scans; full stream via `IObservable<SpectralScan>`
- Scan events tagged with `ExperimentId` and `DeviceId`

### 7.3 Diode Array (DAD/PDA) — Online 2D Chromatography

- Continuous `DiodeArraySlice` stream: one absorbance value per wavelength per time point
- Full 2D matrix (`DiodeArrayMatrix`) available as a rolling window snapshot
- Extraction helpers reduce dimensionality for workflow conditions:
  - `ExtractChromatogram(wavelengthNm)` → `IScalarTrend<double>`
  - `GetSpectrumAt(timestamp)` → `SpectralScan`
- Memory budget: a 600-wavelength × 60 s at 1 Hz matrix ≈ 2.8 MB; held in-memory for the rolling window, archived to MinIO for long runs

### 7.4 Binary Data — Images and Video

| Type | Examples |
|---|---|
| Chromatograms | HPLC runs |
| Spectra | UV/Vis, Raman, NMR |
| Images | Cameras, microscopes |
| Video | Reaction capture |
| Raw vendor files | Vendor-specific formats |

Storage: S3 / Blob / MinIO  
MQTT carries: metadata, storage URI, event notifications (not the binary payload)  
Trend layer holds the URI reference + metadata; binary is retrieved on demand

### 7.5 Offline Analytics Results

- Pushed into the trend system by the analytics engine after a run completes (event-driven, not polled)
- Arrive as `AnalyticsResult` trend values; correlated to experiment via `ExperimentId`
- Scalar (double), string, integer, and structured objects all supported via `StructuredResult`

### 7.6 Derived Trends

Three derivation modes:

| Mode | Mechanism | Latency |
|---|---|---|
| `MathExpression` | String expression evaluated against named source values | ≬ 1 ms |
| `PythonScript` | `IPythonTrendEvaluator` — subprocess or Python.NET host | 10–500 ms |
| `CSharpDelegate` | `Func<IReadOnlyDictionary<string, TrendValue>, TrendValue>` | ≬ 1 ms |

Python-derived trends are evaluated asynchronously; results are pushed as `TrendValue` on completion.  
The source dependency graph is maintained by `ITrendProvider`; circular dependencies are rejected at registration time.

### 7.7 Experiment Context

Every data point is tagged:

```
ExperimentId
SiteId
FumehoodId
DeviceId
Timestamp
Method
Operator
```

---

## 8. Workflow Layer — Helix Runtime

### Core concepts

- `WorkflowContext` — single DI surface passed to every workflow
- `ExperimentContext` — ties workflow execution to an experiment record
- Capability invocation via `ICapabilityInvoker`
- Trend monitoring via `ITrendProvider`
- Safety via `ISafetyEngine`

### Workflow abilities

- Call device capabilities
- Wait for scalar trend conditions (stability, slope, thresholds) via `TrendFunctions`
- Read latest spectral scan or DAD slice from online instruments
- Subscribe to derived trend values (math or Python-computed)
- Branch on analytical results (offline or inline)
- Loop, retry, abort
- Trigger safety actions

### Execution location

- Always inside the agent (local)
- Site server optionally coordinates multi-hood workflows

---

## 9. Safety Layer

### Local safety — Agent

- Pressure and temperature limits
- Gas alarms
- Device faults
- Emergency stop

### Global safety — Site

- Aggregated alarms
- Notifications
- Audit trail

**Design constraint:** Safety must never depend on network connectivity.

---

## 10. Communication Protocols

| Purpose | Protocol | Reason |
|---|---|---|
| Device control | gRPC / REST | Strong typing, reliability |
| Trend streaming | MQTT / NATS | Lightweight, real-time |
| Binary data | S3 / Blob / MinIO | Scalable, cheap |
| UI → site | REST / gRPC | Standard |
| Site → agent | gRPC | Fast, typed |
| Agent → site | MQTT / NATS | Pub/sub |

---

## 11. Technology Stack

| Layer | Technology |
|---|---|
| Backend runtime | .NET 8 |
| Durable execution | Cleipnir.ResilientFunctions 4.x |
| API | gRPC, REST |
| Messaging | MQTT / NATS |
| Site database | PostgreSQL |
| Agent local store | SQLite |
| Binary storage | MinIO / S3 |
| Drivers | C#, Python, C++, Node.js |
| UI (any) | React, Angular, MAUI, Qt — talks only to site server |

---

## 12. Naming Model

```
Site
 └─ Fumehood
      └─ Agent
           └─ Device
                └─ Capabilities
```

---

## 13. Workflow Specification Language (WSL)

WSL is a neutral, declarative, JSON-based workflow description format used by Helix.
It defines **what** a workflow should do, not **how** it is executed.
WSL is the contract between the UI, chatbot, ELN, Site Server, Agents, and ML optimiser.

### Purpose

- Provide a language-agnostic, device-agnostic, runtime-agnostic workflow format
- Allow workflows to be generated by UIs, chatbots, ELNs, or ML systems
- Serve as input to the Helix Workflow Compiler, producing executable `RunspaceDefinition`

### Core concepts

1. **Nodes**
   - Represent workflow steps (`SetTemperature`, `WaitForStability`, `Branch`, `Loop`, `ContinuousStage`, `Analytics`)
   - Include: `id`, `type`, `device`, `capability`, `parameters`, optional safety constraints
2. **Edges**
   - Connect node ids for sequential, conditional, parallel, and loopback flow
3. **Modes**
   - `batch` and `continuous`
4. **Device bindings**
   - Logical device names resolved by compiler via Site Server Device Registry
5. **Safety envelopes**
   - Embedded constraints (temperature, pressure, flow, impurity) with compiler-injected rules
6. **Metadata**
   - Name, version, author, ELN reference, tags

### Execution flow

```
WSL (JSON)
   ↓
Workflow Compiler (Site Server)
   ↓
RunspaceDefinition (JSON)
   ↓
Agent Runspace Execution
```

### Minimal WSL example

```json
{
  "version": "1.0",
  "name": "CSTR Stage 1",
  "mode": "continuous",
  "nodes": [
    {
      "id": "setFlow",
      "type": "SetFlowRate",
      "device": "pump01",
      "parameters": { "flowRate": 1.2 }
    },
    {
      "id": "setTemp",
      "type": "SetTemperature",
      "device": "reactor01",
      "parameters": { "temperature": 60 }
    },
    {
      "id": "waitSteady",
      "type": "WaitForSteadyState",
      "device": "reactor01",
      "parameters": { "window": "5m" }
    }
  ],
  "edges": [
    { "from": "setFlow", "to": "setTemp" },
    { "from": "setTemp", "to": "waitSteady" }
  ]
}
```

### LLM generation contract for WSL

- Output must be valid JSON
- Output is declarative intent only (no imperative code)
- `type` must map to a known workflow primitive
- `device` must reference logical device name
- `parameters` must match capability schema
- `edges` must reference valid node ids

---

## 14. Campaign Manager Boundaries

Campaigns live **only** in the Site Server.
Agents do **not** track Campaigns.
Agents track only per-runspace `ExperimentContext`/`TestContext`.

### Why Campaigns are Site-only

Campaigns are cross-experiment, cross-runspace, cross-device, and often cross-agent:

- Multi-experiment optimisation
- Multi-iteration CSTR tuning
- DoE grids
- ML-driven parameter sweeps
- Multi-stage flow chemistry optimisation

This requires central, long-lived, auditable orchestration state.

### Campaign vs. Experiment responsibilities

**Site Server (Campaign Manager):**
- Own campaign state
- Decide next parameters
- Submit experiments
- Collect results and update optimiser
- Decide stop criteria

**Agent:**
- Receive one experiment
- Execute one runspace
- Stream data and safety events
- Return results
- Release leases and forget campaign context

Analogy: campaign manager is the conductor; agent is the musician.

---

## 15. Runspace Definition in Helix

A Runspace is a Helix-level execution container for a single workflow instance.
It is **not** a Cleipnir primitive, **not** an AppDomain, and **not** an AssemblyLoadContext.

### A Runspace contains

- Execution state (current node, branches, loops, waits, timers)
- One `ExperimentContext` or `TestContext`
- Device leases (exclusive/shared ownership + expiry)
- Trend subscriptions and safety envelopes
- Durable continuation points persisted through Cleipnir
- Capability invocation and error-handling state

### Relationship to Cleipnir

- Cleipnir provides durability, resumability, and crash-safe continuation
- Runspace provides Helix workflow semantics: capabilities, trends, safety, leases, context

So: `Runspace` uses Cleipnir; it is not defined by Cleipnir.

---

## 16. Device Leasing Model

Device leasing is owned per Runspace and enforced in the agent.

- **Exclusive lease:** one runspace controls the device
- **Shared lease:** multiple runspaces can read non-conflicting capabilities
- **Lease expiry/heartbeat:** stale leases are reclaimed safely
- **Safety override:** critical safety actions can preempt leases when required

Leases are local execution mechanics; campaign logic remains site-level.

---

## 17. Local Development Platform

Helix can run as a complete local platform for development and test.

### Components

| # | Component | Typical runtime | Purpose |
|---|---|---|---|
| 1 | Local Site Server | ASP.NET service (C#) | Workflow compiler, campaign manager, registry, API, event hub |
| 2 | Local Helix Agent | Console/Windows service (C#) | Runspace execution, contexts, trends, safety, leasing |
| 3 | Simulated Devices | C# / Python / Node.js | Realistic pumps, balances, spectrometers, reactors, autosamplers |
| 4 | USB Bridge Agent | Local service | Serial probing, baud cycling, protocol detection, mDNS |
| 5 | Node-Flow UI | React + Rete.js | Live workflow editing, capability binding, state visualisation |
| 6 | Local Chatbot (optional MCP) | Node.js/Python service | Generate/modify WSL, query devices, explain errors |
| 7 | Local ML Optimisation Service | FastAPI/Flask (Python) | Bayesian optimisation, steady-state detection, CSTR tuning |
| 8 | Local Data Layer | SQLite/PostgreSQL + filesystem | Metadata, trend cache, raw files, registry JSON |

### What can be tested locally

- Real-time trend streaming (sim device → agent → site → node-flow)
- Continuous multi-stage process control (CSTR1 → CSTR2 → CSTR3)
- Device auto-detection (USB bridge + virtual COM ports)
- Workflow execution through runspaces
- Safety events (runaway pressure/temperature)
- ML optimisation feedback loops
- Chatbot tool-calls generating/modifying WSL
- Live node-flow updates during execution

---

## 18. Core Layer — Implementation Status

**Project:** `Helix.Orchestration.Core` (.NET 10)  
**Build:** passing  
**Tests:** 31/31 passing

### Subsystems scaffolded

| Subsystem | Key types |
|---|---|
| WorkflowEngine | `IWorkflow`, `WorkflowContext`, `WorkflowRegistry` |
| Execution | `IWorkflowExecutor`, `WorkflowExecutor`, primitives (`Step`, `Branch`, `Loop`, `WaitFor`, `RetryPolicy`) |
| State | `WorkflowState`, `IWorkflowStateStore`, `WorkflowStateStore` (in-memory) |
| Capabilities | `ICapabilityInvoker`, `CapabilityRegistry`, `CapabilityInvocationResult` |
| Trends | `ITrend`, `TrendKind`, `TrendValue`, `IScalarTrend<T>`, `ISpectralTrend`, `IDiodeArrayTrend`, `IImageTrend`, `IVideoTrend`, `IDerivedTrend`, `ITrendProvider`, `TrendWindow<T>`, `TrendFunctions`, `SpectralScan`, `DiodeArraySlice`, `ImageFrame` (designed; scaffold pending) |
| Experiment | `IExperimentContext`, `ExperimentContext`, `ExperimentLifecycle` |
| Safety | `ISafetyEngine`, `InterlockRule`, `SafetyEngine` |
| WSL contract | JSON schema and validation rules (defined; compiler scaffold pending) |
| Runspace semantics | Concept and boundaries defined; runtime scaffold pending |
| Campaign boundaries | Site-owned orchestration model defined; manager scaffold pending |
| Device leasing | Lease model defined; implementation scaffold pending |

### Packages

| Package | Version | Used for |
|---|---|---|
| `Cleipnir.ResilientFunctions` | 4.0.0 | Durable execution |
| `MQTTnet` | 4.x | MQTT client |
| `NATS.Net` | 2.x | NATS client |
| `xunit` | latest | Tests |

### Cleipnir 4 integration notes

- `WorkflowExecutor` uses `FunctionsRegistry` and `ActionRegistration<string>`
- `Workflow` type from `Cleipnir.ResilientFunctions.CoreRuntime.Invocation`
- `InMemoryFunctionStore` from `Cleipnir.ResilientFunctions.Storage`
- Registrations cached per workflow name; invoked via `flowInstance`/`param`
- Runtime status currently tracked in-memory (`_statuses`, `_cancellations`) — not yet using Cleipnir-native control panel

### Known issues / caveats

- Private NuGet feeds may return 401 — use `dotnet restore --source https://api.nuget.org/v3/index.json`
- Cleipnir 4 API differs materially from 2.x/3.x documentation — inspect package before coding
- WSL schema and compiler are architectural decisions; implementation pending

---

## 19. Recommended Next Steps

| Priority | Action |
|---|---|
| High | Implement Site Server Workflow Compiler (`WSL` → `RunspaceDefinition`) |
| High | Implement Campaign Manager in Site Server (site-owned campaign state) |
| High | Implement Runspace engine abstraction in Agent with `ExperimentContext`/`TestContext` |
| High | Implement device leasing service (exclusive/shared leases + expiry) |
| High | Replace in-memory status tracking with Cleipnir-native control/state queries |
| High | Add PostgreSQL store for Cleipnir (production persistence) |
| High | Implement protocol adapters behind `ICapabilityInvoker` (OPC UA, SiLA 2) |
| Medium | Add MQTT/NATS trend ingestion stream consumers (scalar + spectral envelope) |
| Medium | Implement `IPythonTrendEvaluator` for Python-derived trends |
| Medium | Add USB Bridge Agent scaffold for serial discovery/probing |
| Medium | Add simulated-device harnesses (RS232, OPC UA, SiLA2, REST) |
| Medium | Add API host layer (gRPC/REST) in a separate host project |
| Medium | Expand executor tests: cancellation, retry, failure recovery |
| Low | Add plugin loader for language-agnostic device drivers |

---

## 20. Approach Constraints

- Orchestration logic must be device-agnostic and transport-agnostic
- Safety must be locally enforced; never depend on the network
- Keep core headless and UI-free
- Campaign logic must remain site-owned; agents remain execution-focused
- Validate each subsystem with focused xUnit tests before wiring host/runtime
- Isolate all infrastructure concerns behind interfaces — backends are swappable

---

## 21. Trend Architecture — Design Decisions

Recorded decisions and open questions from the architecture discussion.

### Decisions made

| Decision | Rationale |
|---|---|
| Stratified `ITrend` hierarchy (not a single generic) | Data shapes are fundamentally different; a single `TrendWindow<T>` cannot represent a 2D DAD matrix or a binary image reference |
| `TrendValue` discriminated union | Allows a uniform observable stream (`IObservable<TrendValue>`) across all kinds without losing type information |
| `IScalarTrend<T>` backed by existing `TrendWindow<T>` | Fully additive — existing scalar infrastructure and `TrendFunctions` are preserved unchanged |
| `ITrendProvider` extended, not replaced | `GetTrend<T>` maps to `GetScalarTrend<T>`; expansion is backward-compatible |
| Binary data (image, video) stored externally | S3/MinIO holds payload; trend layer holds URI + metadata only; consistent with existing binary data principle |
| Offline analytics results pushed (event-driven) | Avoids polling; results arrive as `AnalyticsResult` trend values correlated by `ExperimentId` |
| Derived trends declared with explicit source ids | Enables dependency graph tracking; circular dependencies rejected at registration |
| Python evaluation delegated to `IPythonTrendEvaluator` | Isolates Python runtime dependency behind an interface; swappable between Python.NET and subprocess host |

### Open questions (to resolve before implementation)

| # | Question | Options |
|---|---|---|
| 1 | **Spectral window access** | Rolling window of N scans vs. time-range query (e.g. “last 5 minutes”) — time-range is more natural for PAT |
| 2 | **DAD matrix memory budget** | In-memory rolling window up to a threshold, then archive to MinIO — threshold TBD (suggest 50 MB) |
| 3 | **Derived trend evaluation trigger** | (a) eager on every source update, (b) lazy on demand, (c) scheduled cadence — Python scripts may need async/reactive |
| 4 | **`TrendId` vs `TrendName`** | Stable GUID `TrendId` + human-readable `TrendName`, or single string? Single string is readable in workflows; GUID is collision-free across sites |
| 5 | **Spectral MQTT envelope** | Define the MQTT topic and JSON envelope for spectral scan publication (metadata + storage URI vs. inline payload for small scans) |
