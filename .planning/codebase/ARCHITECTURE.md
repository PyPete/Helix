---
last_mapped_commit: 25fb557bd430bea160370af41b8deb877170076b
last_mapped_at: 2026-10-29
---
<!-- refreshed: 2026-10-29 -->
# Architecture

**Analysis Date:** 2026-10-29

## System Overview

Helix is a distributed laboratory-orchestration system. The site-wide control plane accepts REST requests, compiles workflow specifications, manages campaigns, and calls a per-fumehood Agent. The Agent owns execution and device capability dispatch. MQTT distributes trend data, while PostgreSQL holds orchestration and durable-function data and MinIO receives recorder batches.

```text
┌───────────────────────────────────────────────────────────────────────────┐
│ Client applications                                                        │
│ `helix-dashboard/src`, `helix-desktop/src`, `helix-desktop/src-tauri`    │
└──────────────┬────────────────────────────────────────────────────────────┘
               │ REST / WebSocket
               ▼
┌───────────────────────────────────────────────────────────────────────────┐
│ Site control plane: `Helix.Orchestration.SiteServer`                       │
│ Endpoints → CampaignManager / WslCompiler → IAgentProxy                    │
└─────────────────────────┬────────────────────┬────────────────────────────┘
                          │ gRPC               │ MQTT trends
                          ▼                    ▼
┌───────────────────────────────────┐  ┌───────────────────────────────────┐
│ Fumehood Agent                     │  │ MQTT / Recorder                   │
│ `Helix.Orchestration.Agent`        │  │ `Helix.Orchestration.Recorder`    │
│ gRPC + REST → runspace → registry  │  │ bounded channel → MinIO            │
└───────────────┬───────────────────┘  └───────────────────────────────────┘
                ▼
┌───────────────────────────────────────────────────────────────────────────┐
│ Domain and adapters                                                        │
│ `Core` interfaces/models → `Devices` adapters/transports → lab hardware   │
│ PostgreSQL: `Infrastructure/Persistence`; contracts: `Contracts/Protos`   │
└───────────────────────────────────────────────────────────────────────────┘
```

## Component Responsibilities

| Component | Responsibility | File |
|-----------|----------------|------|
| Site Server | Hosts the control-plane REST API, agent proxy seam, MQTT trend bridge, WebSocket fan-out, and campaign services. | `Helix.Orchestration.SiteServer/Program.cs` |
| Agent | Hosts fumehood-local REST, gRPC, WebSocket, workflow/runspace services, adapters, leasing, and event workers. | `Helix.Orchestration.Agent/Program.cs` |
| Core | Defines domain models, workflow/WSL processing, capability, safety, leasing, runspace, state, and trend abstractions. | `Helix.Orchestration.Core/` |
| Contracts | Shares generated gRPC client/server contracts from protobuf schemas. | `Helix.Orchestration.Contracts/Protos/agent_control.proto` |
| Devices | Implements device abstractions, transport implementations, device drivers, and fault-tolerance primitives. | `Helix.Orchestration.Devices/` |
| Infrastructure | Implements PostgreSQL repositories and MinIO storage sink behind interfaces. | `Helix.Orchestration.Infrastructure/` |
| Simulation | Supplies simulated instruments, trend provider, and capability invoker for development/testing. | `Helix.Orchestration.Simulation/SimulationServiceExtensions.cs` |
| Recorder | Runs a background MQTT-to-object-storage recording pipeline. | `Helix.Orchestration.Recorder/Program.cs` |

## Pattern Overview

**Overall:** Two-tier distributed clean/ports-and-adapters architecture using ASP.NET Core composition roots and dependency injection.

**Key Characteristics:**
- Keep domain contracts and interfaces in `Helix.Orchestration.Core`; bind concrete infrastructure in host `Program.cs` files or service-registration extensions.
- Cross-tier experiment control goes through `IAgentProxy`; select `GrpcAgentProxy` or `SimAgentProxy` during Site Server registration in `Helix.Orchestration.SiteServer/Agent/AgentProxyServiceExtensions.cs`.
- Device workflows invoke logical, namespaced capabilities through `ICapabilityInvoker`/`CapabilityRegistry`, not hardware protocols directly.
- Use protobuf for Agent control-plane gRPC (`Helix.Orchestration.Contracts/Protos/agent_control.proto`) and MQTT for streaming trend events.

## Layers

**Presentation and client layer:**
- Purpose: provide browser dashboard and installed desktop user interfaces.
- Location: `helix-dashboard/src/` and `helix-desktop/src/`.
- Contains: React routes/pages, hooks, visual components, workflow builder, desktop shell.
- Depends on: Site Server REST/WebSocket endpoints and Agent REST/WebSocket endpoints.
- Used by: laboratory operators.

**Application host layer:**
- Purpose: expose HTTP/gRPC transport boundaries and compose runtime dependencies.
- Location: `Helix.Orchestration.Agent/`, `Helix.Orchestration.SiteServer/`, and `Helix.Orchestration.Recorder/`.
- Contains: minimal endpoint modules, hosted workers, proxies, event streaming, DI setup.
- Depends on: Core contracts plus Infrastructure, Devices, Simulation, and Contracts as needed.
- Used by: clients, Docker services, and system service definitions in `deploy/`.

**Domain/workflow layer:**
- Purpose: define orchestration behavior independently of transport and hardware implementation.
- Location: `Helix.Orchestration.Core/`.
- Contains: WSL compiler, workflow engine, execution primitives, capability/safety/leasing/state/trend abstractions and models.
- Depends on: BCL and durable workflow library abstractions; should not take dependencies on host endpoint projects.
- Used by: Agent, Site Server, Simulation, and tests.

**Integration layer:**
- Purpose: adapt device protocols and persistent services to Core-facing ports.
- Location: `Helix.Orchestration.Devices/`, `Helix.Orchestration.Infrastructure/`, and `Helix.Orchestration.Simulation/`.
- Contains: driver classes, serial/TCP/Modbus/OPC transports, PostgreSQL repositories, MinIO sink, and simulators.
- Depends on: external protocol/client packages and Core models/interfaces.
- Used by: application host DI composition.

## Data Flow

### Primary Experiment Submission Path

1. A Site Server endpoint in `Helix.Orchestration.SiteServer/Endpoints/ExperimentEndpoints.cs` accepts workflow/experiment input.
2. `IWslCompiler` implemented by `Helix.Orchestration.Core/WSL/WslCompiler.cs` validates graph, bindings, and variable references, then emits a `RunspaceDefinition` (`WslCompiler.cs:26-77`).
3. `IAgentProxy` serializes the definition and calls `SubmitExperiment` over gRPC in `Helix.Orchestration.SiteServer/Agent/GrpcAgentProxy.cs:10-22`.
4. `AgentGrpcService` deserializes it and creates an Agent runspace through `IRunspaceManager` in `Helix.Orchestration.Agent/gRPC/Server/AgentGrpcService.cs:21-33`.
5. The Agent's runspace factory traverses execution edges, substitutes state variables, and invokes device capabilities through `CapabilityRegistry` in `Helix.Orchestration.Agent/Program.cs:108-235`.
6. `CapabilityRegistry` resolves a namespaced adapter bridge; the driver uses its selected transport in `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs`.

### Trend Streaming and Recording Path

1. Agent trend ingestion/publishing is hosted by `Helix.Orchestration.Agent/Trends/MqttTrendIngestionService.cs`.
2. `MqttTrendBridge` in `Helix.Orchestration.SiteServer/Mqtt/MqttTrendBridge.cs` subscribes and passes trends to `TrendWebSocketManager`.
3. Site Server accepts browser trend WebSockets at `/api/trends/ws` in `Helix.Orchestration.SiteServer/Program.cs:77-92`; dashboard hook `helix-dashboard/src/hooks/useTrendWebSocket.ts` consumes them.
4. Recorder service `Helix.Orchestration.Recorder/Services/MqttRecorderService.cs` sends MQTT events through a bounded `Channel<RecordedEvent>` to `RecorderWriterService`, which writes batches through `IObjectStorageSink`.

### Campaign Path

1. Campaign REST handlers in `Helix.Orchestration.SiteServer/Endpoints/CampaignEndpoints.cs` call `ICampaignManager`.
2. `CampaignManager` builds iterations via `ICampaignIterationStrategy`, stores records via `ICampaignRepository`, and submits each generated definition through `IAgentProxy` (`Helix.Orchestration.SiteServer/Campaigns/CampaignManager.cs:16-136`).
3. PostgreSQL repository implementations in `Helix.Orchestration.Infrastructure/Persistence/` persist campaign, experiment, and event records.

**State Management:**
- Agent-local process state uses singleton registries/services (`CapabilityRegistry`, `DeviceRegistry`, in-memory leasing/trends/global state) registered in `Helix.Orchestration.Agent/Program.cs`.
- Campaigns maintain an in-memory concurrent cache backed by PostgreSQL in `Helix.Orchestration.SiteServer/Campaigns/CampaignManager.cs`.
- `WorkflowExecutor` retains run status and cancellation token maps in concurrent dictionaries while Cleipnir's PostgreSQL function store provides durable-function backing (`Helix.Orchestration.Core/Execution/WorkflowExecutor.cs`).

## Key Abstractions

**Workflow Specification Language (WSL):**
- Purpose: declarative graph format for authored workflows and compiler input.
- Examples: `Helix.Orchestration.Core/WSL/WslDocument.cs`, `WslNode.cs`, `WslEdge.cs`, `WslCompiler.cs`.
- Pattern: validate then transform a graph (safety injection, loop expansion, branch flattening, parallel fork/join) into `RunspaceDefinition`.

**Agent proxy:**
- Purpose: hide local simulation versus remote Agent control behind one Site Server dependency.
- Examples: `Helix.Orchestration.SiteServer/Agent/IAgentProxy.cs`, `GrpcAgentProxy.cs`, `SimAgentProxy.cs`.
- Pattern: port with production gRPC and development simulation adapters.

**Capability registry:**
- Purpose: map a workflow capability key such as `device.Verb` to an invoker.
- Examples: `Helix.Orchestration.Core/Capabilities/CapabilityRegistry.cs`, `Helix.Orchestration.Agent/Adapters/Devices/HelixAdapterCapabilityBridge.cs`.
- Pattern: thread-safe registry and adapter bridge; device registration belongs in `DeviceAdapterExtensions.cs`.

**Runspaces and workflows:**
- Purpose: represent, start, pause, resume, abort, and observe a submitted execution.
- Examples: `Helix.Orchestration.Core/Runspace/IRunspace.cs`, `RunspaceManager.cs`, `Helix.Orchestration.Agent/Runspaces/DurableRunspace.cs`.
- Pattern: runspace state machine protected by `SemaphoreSlim`; execution may be backed by `IWorkflowExecutor`/Cleipnir or the Agent's registered in-memory graph traversal factory.

## Entry Points

**Agent web host:**
- Location: `Helix.Orchestration.Agent/Program.cs`.
- Triggers: Docker `helix-agent`, systemd `deploy/systemd/helix-agent.service`, or local `dotnet run`.
- Responsibilities: register device adapters and application services; expose REST endpoint groups, gRPC `AgentGrpcService`, `/health`, and `/ws`.

**Site Server web host:**
- Location: `Helix.Orchestration.SiteServer/Program.cs`.
- Triggers: Docker `helix-siteserver` or local `dotnet run`.
- Responsibilities: register proxy/compiler/campaign/MQTT services; map REST API groups and `/api/trends/ws`.

**Recorder worker:**
- Location: `Helix.Orchestration.Recorder/Program.cs`.
- Triggers: Docker `helix-recorder` or systemd `deploy/systemd/helix-recorder.service`.
- Responsibilities: configure bounded event channel, MQTT consumer, MinIO sink, and writer worker.

**Dashboard browser entry:**
- Location: `helix-dashboard/src/main.tsx`.
- Triggers: Vite-built SPA load.
- Responsibilities: configure SciChart, React root, and router; routes in `helix-dashboard/src/App.tsx` call the Site Server.

**Desktop application entry:**
- Location: `helix-desktop/src/main.tsx` and `helix-desktop/src-tauri/src/main.rs`.
- Triggers: Tauri desktop startup.
- Responsibilities: render React `HelixShell`; Rust Tauri library registers local config commands in `helix-desktop/src-tauri/src/lib.rs`.

## Architectural Constraints

- **Threading:** ASP.NET Core and hosted workers run asynchronous tasks; shared state uses `ConcurrentDictionary`, `Channel`, and `SemaphoreSlim` rather than unsafe mutable collections (`CampaignManager.cs`, `WorkflowExecutor.cs`, `DurableRunspace.cs`).
- **Global state:** agent singleton state includes `CapabilityRegistry`, `DeviceRegistry`, `InMemoryDeviceLeasingService`, `InMemoryTrendProvider`, `InMemoryGlobalStateStore`, and runspace manager registrations in `Helix.Orchestration.Agent/Program.cs`; CampaignManager holds `_campaigns` cache in `CampaignManager.cs`.
- **Transport boundary:** do not reference Agent implementation types from Site Server endpoint/campaign code; use `IAgentProxy` and protobuf contracts.
- **Hardware boundary:** do not call serial/TCP/OPC APIs from Core workflows; expose an `ICapabilityInvoker` and add bridges/transports in `Devices` and Agent composition.
- **Circular imports:** no project-reference cycles are represented in `Helix.Orchestration.slnx`; preserve inward project dependencies.

## Anti-Patterns

### Bypassing the capability port

**What happens:** a workflow, endpoint, or Site Server class directly constructs a driver or transport.
**Why it's wrong:** it binds orchestration to a physical protocol and defeats simulated Agent selection.
**Do this instead:** add a device adapter and register its namespaced bridge in `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs`, then invoke it through `ICapabilityInvoker`.

### Putting composition into Core

**What happens:** Core models or workflow services take dependencies on ASP.NET, gRPC, PostgreSQL, or device SDKs.
**Why it's wrong:** it breaks the domain's testable, reusable inward boundary.
**Do this instead:** define the port in `Helix.Orchestration.Core/` and bind the concrete service in `Program.cs` or `InfrastructureServiceExtensions.cs`.

## Error Handling

**Strategy:** use explicit result models for expected capability failures and exceptions for invalid operations, transport failures, and boundary failures; host code logs fatal/unexpected failures through Serilog where configured.

**Patterns:**
- `CapabilityRegistry.InvokeAsync` returns `CapabilityInvocationResult.Failed` for unregistered or throwing invokers (`Helix.Orchestration.Core/Capabilities/CapabilityRegistry.cs:43-60`).
- gRPC endpoint methods translate failures to acknowledgement payloads (`Helix.Orchestration.Agent/gRPC/Server/AgentGrpcService.cs`).
- Runspace execution transitions to faulted/suspended statuses on exception/cancellation (`Helix.Orchestration.Agent/Runspaces/DurableRunspace.cs`).

## Cross-Cutting Concerns

**Logging:** Agent and Recorder configure Serilog JSON console output; Agent also uses rolling files in `Helix.Orchestration.Agent/Program.cs`.
**Validation:** `WslCompiler` validates schema, graph, parameters, device bindings, and variable scopes before workflow compilation in `Helix.Orchestration.Core/WSL/WslCompiler.cs`.
**Authentication:** Not detected in application endpoint setup; endpoint groups in Agent and Site Server are mapped without auth middleware in their `Program.cs` files.

---

*Architecture analysis: 2026-10-29*
