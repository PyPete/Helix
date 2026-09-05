<!-- refreshed: 2026-09-05 -->
# Architecture

**Analysis Date:** 2026-09-05

## System Overview

```text
┌──────────────────────────────────────────────────────────────────────┐
│                    helix-desktop  (Tauri v2 + React/Vite)            │
│  HelixShell → Ribbon | ActivityBar | WorkspaceLayout | StatusBar     │
│  `helix-desktop/src/shell/`   `helix-desktop/src/components/`        │
└──────────┬───────────────────────┬───────────────────────────────────┘
           │  REST + WebSocket      │  REST + WebSocket
           ▼                        ▼
┌──────────────────────┐  ┌────────────────────────────────────────────┐
│   Helix.Orchestration│  │   Helix.Orchestration.SiteServer           │
│   .Agent  (port 5080)│  │   (port 5090)                              │
│   ASP.NET Core +gRPC │  │   ASP.NET Core minimal API                 │
│   `Helix.Orchestration│  │   `Helix.Orchestration.SiteServer/`       │
│   .Agent/`            │  └───────────┬────────────────────────────────┘
└──────────┬───────────┘              │
           │                          │
           ▼                          ▼
┌──────────────────────────────────────────────────────────────────────┐
│              Helix.Orchestration.Core  (domain layer)                │
│  Capabilities | Runspace | WSL/WslCompiler | Execution | Safety      │
│  Leasing | State | Trends | Experiment | WorkflowEngine              │
│  `Helix.Orchestration.Core/`                                         │
└────────────────────┬──────────────────────────────────────────────────┘
                     │
          ┌──────────┴────────────┐
          ▼                       ▼
┌─────────────────┐   ┌──────────────────────────────┐
│  Helix.Orch.    │   │  Helix.Orchestration.         │
│  Devices        │   │  Infrastructure               │
│  (device layer) │   │  (persistence layer)          │
│  `Helix.Orch.   │   │  `Helix.Orchestration.        │
│  Devices/`      │   │  Infrastructure/`             │
└────────┬────────┘   └──────────────┬────────────────┘
         │                           │
         ▼                           ▼
┌────────────────┐  ┌──────────────────────────────────┐
│  Physical      │  │  PostgreSQL (experiments,        │
│  Devices       │  │  campaigns, workflow state)      │
│  (Serial/OPC/  │  │  + MinIO (recorder storage)      │
│  Modbus/TCP)   │  │  + MQTT (trend ingestion)        │
└────────────────┘  └──────────────────────────────────┘
```

## Component Responsibilities

| Component | Responsibility | Key Files |
|-----------|----------------|-----------|
| `helix-desktop` | Tauri/React operator UI shell | `helix-desktop/src/shell/HelixShell.tsx` |
| `Helix.Orchestration.Agent` | Runspace execution, WebSocket hub, gRPC server | `Helix.Orchestration.Agent/Program.cs`, `AgentService.cs` |
| `Helix.Orchestration.SiteServer` | Multi-agent coordination, campaign management, MQTT bridge | `Helix.Orchestration.SiteServer/Program.cs` |
| `Helix.Orchestration.Core` | Domain model: WSL compiler, runspace, capabilities, safety | `Helix.Orchestration.Core/` |
| `Helix.Orchestration.Devices` | Device driver adapters, transport layer, fault tolerance | `Helix.Orchestration.Devices/Drivers/`, `Transports/` |
| `Helix.Orchestration.Infrastructure` | PostgreSQL persistence for experiments, campaigns, events | `Helix.Orchestration.Infrastructure/Persistence/` |
| `Helix.Orchestration.Contracts` | Shared gRPC protobuf definitions | `Helix.Orchestration.Contracts/Protos/` |
| `Helix.Orchestration.Simulation` | Simulated device and trend providers for dev/test | `Helix.Orchestration.Simulation/` |
| `Helix.Orchestration.Recorder` | MQTT subscriber that records trend data to MinIO | `Helix.Orchestration.Recorder/` |
| `helix-dashboard` | Standalone React web dashboard (separate from desktop app) | `helix-dashboard/src/` |
| `adapters/python/pump_simulator` | Python pump device simulator | `adapters/python/pump_simulator/` |

## Pattern Overview

**Overall:** Layered microservice architecture — a desktop UI (Tauri) communicates with a local Agent service over REST/WebSocket/gRPC; a SiteServer coordinates across multiple agents. The backend uses a vertical slice / ports-and-adapters pattern within each .NET project.

**Key Characteristics:**
- Domain model in `Core` is independent of transport and persistence — no ASP.NET or EF Core references in `Helix.Orchestration.Core`
- Device drivers in `Devices` implement adapters over physical transports (Serial, OPC, Modbus, TCP, CDll); Core depends on abstractions only
- WSL (Workflow Specification Language) is a JSON graph format compiled by `WslCompiler` into an executable `RunspaceDefinition`
- Agent is both a gRPC server (`adapter.proto`, `agent_control.proto`) and an HTTP/WebSocket server; clients can use either
- Real-time data flows from devices → MQTT → `MqttTrendIngestionService` → `TrendAggregator` → `AgentService` WebSocket broadcast → `useAgentWebSocket` hook in UI

## Layers

**Desktop UI Layer:**
- Purpose: Operator shell — workflow authoring, execution control, live trend charts, device inspection
- Location: `helix-desktop/src/`
- Contains: React components, custom hooks, Tauri commands, TypeScript types
- Depends on: Agent REST API, Agent WebSocket `/ws`, Tauri plugin store for config persistence
- Used by: End operators

**Agent Service Layer:**
- Purpose: Executes workflows (runspaces), manages device leasing, streams events over WebSocket
- Location: `Helix.Orchestration.Agent/`
- Contains: Minimal API endpoints, hosted services, gRPC server, event bus, MQTT trend ingestion
- Depends on: `Core`, `Devices`, `Infrastructure`, `Contracts`
- Used by: `helix-desktop`, `Helix.Orchestration.SiteServer`

**SiteServer Layer:**
- Purpose: Lab-wide coordination — campaign management (multi-experiment batches), multi-agent proxy, MQTT-over-WebSocket bridge for dashboard clients
- Location: `Helix.Orchestration.SiteServer/`
- Contains: Minimal API endpoints, campaign services, MQTT bridge, WebSocket manager
- Depends on: `Core`, `Infrastructure`, `Simulation`
- Used by: `helix-dashboard`, multi-agent deployments

**Domain Core Layer:**
- Purpose: Business logic — workflow compilation, runspace lifecycle, capabilities, safety interlocks, leasing, state, trends
- Location: `Helix.Orchestration.Core/`
- Contains: Interfaces, domain services, WSL model, workflow engine, safety engine
- Depends on: Nothing outside the project (pure domain)
- Used by: `Agent`, `SiteServer`, `Devices`

**Device Layer:**
- Purpose: Wraps physical instrument protocols behind `IHelixAdapter` / `DeviceAdapterBase`
- Location: `Helix.Orchestration.Devices/`
- Contains: 40+ device drivers, transport implementations (Serial, OPC-UA, Modbus, TCP, CDll, Null), fault tolerance decorators
- Depends on: `Core` (capability model, transport abstractions)
- Used by: `Agent`

**Infrastructure Layer:**
- Purpose: PostgreSQL repositories for experiments, campaigns, and experiment events; connection factory
- Location: `Helix.Orchestration.Infrastructure/`
- Contains: Dapper-based repositories, `InfrastructureServiceExtensions`
- Depends on: `Core` (domain model)
- Used by: `Agent`, `SiteServer`

## Data Flow

### Workflow Execution (Primary Path)

1. Operator authors workflow in `WorkflowBuilder` React component (`helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx`)
2. `WslDocument` exported as JSON, stored in `HelixShell` state (`helix-desktop/src/shell/HelixShell.tsx:37`)
3. Operator clicks Run → `handleRun` POSTs to `POST /api/runspaces/start` with workflow JSON (`HelixShell.tsx:59`)
4. Agent `RunspaceEndpoints` receives request, calls `IWslCompiler.Compile()` → `WslCompiler.cs` validates and transforms the graph into a `RunspaceDefinition`
5. `IRunspaceManager.CreateRunspaceAsync()` spawns an `InMemoryRunspace`, BFS-walks nodes, invokes `CapabilityRegistry.InvokeAsync()` per step (`Program.cs:104–210`)
6. Each capability maps to a device driver method via `CapabilityRegistry` populated by `AddMigratedDeviceAdapters()`
7. Step lifecycle events (`StepStartedEvent`, `StepCompletedEvent`, `StepFaultedEvent`) are published to `IRunspaceEventEmitter` → `RunspaceEventEmitter`
8. `AgentService.BroadcastLoopAsync()` dequeues events and sends JSON frames to all connected WebSocket clients (`AgentService.cs`)
9. `useAgentWebSocket` hook on the UI receives frames, updates React state, re-renders `ExecutionPanel` and `WorkspaceLayout` (`helix-desktop/src/hooks/useAgentWebSocket.ts`)

### Live Trend Data Flow

1. Physical device publishes trend via serial/OPC/Modbus transport → `IHelixAdapter` driver (`Helix.Orchestration.Devices/Drivers/`)
2. Driver pushes reading to MQTT broker (localhost:1883) under topic namespace
3. `MqttTrendIngestionService` hosted service in Agent subscribes to MQTT, feeds values to `TrendAggregator.ProcessAsync()`
4. `TrendAggregator` pushes scalar values to `InMemoryTrendProvider` and a `Channel<ScalarTrendNotification>`
5. `AgentService.TrendForwardLoopAsync()` reads the channel, broadcasts `{"type":"trend-value",...}` JSON frames to WebSocket clients
6. `useAgentWebSocket` hook buffers last 200 values in state, `TrendCharts` components re-render

### Experiment Lifecycle

1. POST to `/api/experiments/{id}/start` handled by `ExperimentEndpoints` in Agent
2. `IExperimentManager.StartAsync()` transitions experiment to `Running` phase via `ExperimentLifecycle`
3. Lifecycle events published to `IExperimentEventChannel`
4. `ExperimentEventPersistenceWorker` persists events to PostgreSQL via `IExperimentEventRepository`
5. `AgentService` also broadcasts experiment events over WebSocket
6. UI `useAgentWebSocket` dispatches to `experimentEvents` state; `ExperimentTimeline` panel renders timeline

## Key Abstractions

**IHelixAdapter / DeviceAdapterBase:**
- Purpose: Abstract interface every device driver implements; exposes capabilities as named methods
- Examples: `Helix.Orchestration.Devices/Abstractions/DeviceAdapterBase.cs`, `Helix.Orchestration.Devices/Drivers/SimulatorDevice.cs`
- Pattern: Template method — base class handles transport lifecycle, fault tolerance; subclass implements device-specific commands

**CapabilityRegistry / ICapabilityInvoker:**
- Purpose: Runtime registry mapping capability keys (e.g. `simulator.set-flow-rate`) to async invocation delegates
- Examples: `Helix.Orchestration.Core/Capabilities/CapabilityRegistry.cs`, `Helix.Orchestration.Core/Capabilities/ICapabilityInvoker.cs`
- Pattern: Service locator for capabilities; populated at startup by `AddMigratedDeviceAdapters()`

**WslDocument / RunspaceDefinition:**
- Purpose: `WslDocument` is the authored JSON workflow graph (nodes + edges); `RunspaceDefinition` is the compiled, validated, executable form
- Examples: `Helix.Orchestration.Core/WSL/WslDocument.cs`, `Helix.Orchestration.Core/WSL/RunspaceDefinition.cs`
- Pattern: Two-phase compilation with diagnostic reporting; compiler applies 4 graph transforms (safety injection, loop expansion, branch flattening, parallel fork-join)

**IRunspaceManager / IRunspace:**
- Purpose: Factory and lifecycle for a single workflow execution unit
- Examples: `Helix.Orchestration.Core/Runspace/IRunspaceManager.cs`, `Helix.Orchestration.Core/Runspace/IRunspace.cs`
- Pattern: Factory returns `InMemoryRunspace` wrapping an async BFS execution body; Durable variant uses Cleipnir for fault-tolerant replay

**IDeviceTransport:**
- Purpose: Protocol abstraction below device drivers — Serial, OPC-UA, Modbus, TCP, CDll, Null all implement this
- Examples: `Helix.Orchestration.Devices/Abstractions/IDeviceTransport.cs`, transports in `Helix.Orchestration.Devices/Transports/`
- Pattern: Strategy — driver selects transport at construction; `TransportFaultException` signals recoverable faults

**WorkspaceLayoutHandle:**
- Purpose: Imperative ref-based handle exposed by `WorkspaceLayout` component to allow `HelixShell` to drive the rc-dock layout (save, load, open panel, activate tab)
- Examples: `helix-desktop/src/shell/WorkspaceLayout.tsx`
- Pattern: `useImperativeHandle` — avoids prop-drilling for layout commands; shell holds `useRef<WorkspaceLayoutHandle>`

## Entry Points

**helix-desktop (Tauri App):**
- Location: `helix-desktop/src/main.tsx` → `App.tsx` → `helix-desktop/src/shell/HelixShell.tsx`
- Triggers: Tauri launches Vite-built frontend in webview
- Responsibilities: Render shell, manage WebSocket connection to Agent, forward user actions to Agent REST API

**Helix.Orchestration.Agent:**
- Location: `Helix.Orchestration.Agent/Program.cs`
- Triggers: `dotnet run` or Docker container start
- Responsibilities: Build DI container, initialize PostgreSQL workflow store, map API endpoints, start hosted services (AgentService, LeaseExpiryWorker, ExperimentEventPersistenceWorker, MqttTrendIngestionService)

**Helix.Orchestration.SiteServer:**
- Location: `Helix.Orchestration.SiteServer/Program.cs`
- Triggers: `dotnet run` or Docker container start
- Responsibilities: Build DI container, register simulation/campaign/MQTT bridge, map API endpoints

**Tauri Backend:**
- Location: `helix-desktop/src-tauri/src/lib.rs`
- Triggers: Tauri runtime initialization
- Responsibilities: Register `load_config` / `save_config` commands, initialize `tauri-plugin-store` for layout persistence

## Architectural Constraints

- **Threading:** Agent uses `async/await` throughout on the ASP.NET Core thread pool; `AgentService` runs background loops (`BroadcastLoopAsync`, `TrendForwardLoopAsync`, `SafetyPollLoopAsync`, `DeviceHealthPollLoopAsync`) as fire-and-forget tasks from `StartAsync`
- **Global state:** `AgentService._clients` (ConcurrentDictionary) and `AgentService._prevViolations` (HashSet) are mutable singletons in `Helix.Orchestration.Agent/AgentService.cs`; `InMemoryTrendProvider` and `InMemoryGlobalStateStore` are singletons
- **Circular imports:** None detected — dependency graph flows strictly: `Agent` → `Core`, `Devices` → `Core`, `Infrastructure` → `Core`; `Contracts` is a leaf
- **CORS:** Agent allows only `http://localhost:5173`, `tauri://localhost`, `https://tauri.localhost` (configured in `Program.cs:270-278`)
- **rc-dock trap:** Never set `background` on `.dock-box`, `.dock-hbox`, `.dock-vbox` — `.dock-fbox`/`.dock-mbox` are opaque overlays that hide all panels (documented in project memory)

## Anti-Patterns

### Inline BFS execution in DI factory

**What happens:** The entire BFS runspace execution body (200+ lines) is inlined as a lambda inside the `IRunspaceManager` DI factory registration in `Helix.Orchestration.Agent/Program.cs` (lines 89–211).
**Why it's wrong:** Logic is untestable in isolation; `Program.cs` becomes a god file; the lambda captures many DI services making dependency tracking opaque.
**Do this instead:** Extract the BFS body to a dedicated `RunspaceExecutionStrategy` or extend `RunspaceManager` in `Helix.Orchestration.Core/Runspace/RunspaceManager.cs`.

### Direct `fetch()` in shell component

**What happens:** `HelixShell.tsx` issues `fetch()` calls to agent REST endpoints directly (lines 59–70).
**Why it's wrong:** Bypasses any centralized error handling, auth, or retry logic; couples the shell to URL construction.
**Do this instead:** Extract an `AgentClient` service/hook (e.g. `useAgentClient`) that encapsulates all REST calls, mirroring the `useAgentWebSocket` pattern.

## Error Handling

**Strategy:** Exceptions are caught at the step execution level in the BFS loop; `StepFaultedEvent` is emitted and loop exits. Transport-level faults use `TransportFaultException`. Top-level unhandled exceptions in `Program.cs` are caught by `try/finally` and logged via Serilog.

**Patterns:**
- Device driver faults: `TransportFaultException` thrown from `IDeviceTransport` implementations, caught by fault tolerance decorators in `Helix.Orchestration.Devices/FaultTolerance/`
- Runspace step faults: `OperationCanceledException` re-thrown (cancelled); other exceptions logged and converted to `StepFaultedEvent`
- WSL compilation errors: `CompilationResult.Failed(diagnostics)` returned (no exceptions thrown); caller checks `result.Success`

## Cross-Cutting Concerns

**Logging:** Serilog with `RenderedCompactJsonFormatter` — console + rolling file (`logs/helix-agent-.log`); configured in `Helix.Orchestration.Agent/Program.cs:32-36` and structured via `ILogger<T>` injection throughout
**Validation:** WSL documents validated by `WslCompiler.Compile()` before execution; device capability parameters validated per `CapabilityDescriptor.Parameters` schema
**Authentication:** None — Agent and SiteServer are assumed to run on a trusted local network; CORS origin whitelist is the only access control

---

*Architecture analysis: 2026-09-05*
