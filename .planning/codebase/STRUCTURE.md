# Directory Structure

## Top-Level Layout

```
Helix/                                        repo root
|-- .github/
|   |-- workflows/ci.yml                      GitHub Actions CI
|   |-- copilot-instructions.md
|   `-- CICD.md / PIPELINE_VERIFICATION.md
|-- .planning/codebase/                       codebase map (this folder)
|-- adapters/python/pump_simulator/           Python pump adapter (Windows service)
|-- docs/                                     root-level docs (mirrors Helix.Orchestration/docs)
|-- docker-compose.yml                        local infra: MQTT, Postgres, MinIO
|-- Helix.Orchestration/                      main solution folder
|   |-- Helix.Orchestration.slnx              solution file
|   |-- Helix.Orchestration.Core/
|   |-- Helix.Orchestration.Contracts/
|   |-- Helix.Orchestration.Devices/
|   |-- Helix.Orchestration.Agent/
|   |-- Helix.Orchestration.SiteServer/
|   |-- Helix.Orchestration.Simulation/
|   |-- Helix.Orchestration.Infrastructure/   inside solution
|   |-- Helix.Orchestration.Recorder/         inside solution
|   |-- tests/
|   `-- docs/                                 phase handover docs
|-- Helix.Orchestration.Contracts/            also at root
|-- Helix.Orchestration.Devices/              also at root (post-refactor location)
|-- Helix.Orchestration.Infrastructure/       also at root (untracked, in-progress)
|-- Helix.Orchestration.Recorder/             also at root (untracked, in-progress)
`-- tests/                                    root-level test runner (untracked)
```

Note: Several projects exist in both `Helix.Orchestration/` and the repo root. This is an in-progress layout migration -- the root-level copies appear to be the target locations.

## Project-by-Project Breakdown

### Helix.Orchestration.Core
- **Purpose**: Pure domain library -- workflow engine, trends, safety, leasing, WSL contract, experiment lifecycle
- **Target**: .NET 10
- **Key namespaces**:
  - `Capabilities/` -- `ICapabilityInvoker`, `CapabilityRegistry`, `CapabilityInvocationResult`
  - `Execution/` -- `WorkflowExecutor`, `WorkflowStep`, `Condition`, `Branch`, `Loop`, `ParallelBlock`, `Delay`, `WaitFor`, `RetryPolicy`
  - `Experiment/` -- `IExperimentContext`, `ExperimentContext`, `ExperimentLifecycle`
  - `Leasing/` -- `IDeviceLeasingService`, `InMemoryDeviceLeasingService`, `DeviceLease`, `LeaseMode`
  - `Runspace/` -- `IRunspace`, `IRunspaceManager`, `RunspaceManager`, `RunspaceStatus`
  - `Safety/` -- `ISafetyEngine`, `SafetyEngine`, `InterlockRule`
  - `State/` -- `IWorkflowStateStore`, `WorkflowStateStore`, `WorkflowState`, `IGlobalStateStore`
  - `Trends/` -- all `ITrend*` interfaces, `TrendWindow<T>`, `TrendFunctions`, `InMemoryScalarTrend`, `DerivedTrendDefinition`, typed value types
  - `WSL/` -- `WslDocument`, `WslNode`, `WslEdge`, `RunspaceDefinition`, `IWslCompiler`, `CompilationResult`
  - `WorkflowEngine/` -- `IWorkflow`, `WorkflowRegistry`, `WorkflowContext`
  - `Devices/` -- `DeviceRegistry`, `DeviceDescriptor`
- **Dependencies**: None (pure domain)

### Helix.Orchestration.Contracts
- **Purpose**: gRPC protobuf definitions and generated stubs
- **Key files**:
  - `Protos/agent_control.proto` -- runspace start/cancel/status RPCs
  - `Protos/adapter.proto` -- capability invocation RPCs
- **Dependencies**: Google.Protobuf, Grpc.Net.Client

### Helix.Orchestration.Devices
- **Purpose**: Device abstraction -- transport interfaces, device model interfaces, device result types
- **Key namespaces**:
  - `Transport/` -- `IModbusTransport`, `ITcpTransport`, `IOpcTransport`, `ISerialTransport`
  - `Interfaces/` -- `IDevice`, `IReactionPlatform`, `IHplc`, `ISetPointDevice`, `IVapourtec`
  - `Model/` -- `DeviceResult`, `DeviceMeasurement`, `DeviceDetails`, `DeviceKind`, `VapourtecStatus`
- **Note**: Base classes (`DeviceBase`, `ModbusDeviceBase`, etc.) and driver implementations (`VapourtecR4`, `KnauerAzura`, `SystagOpcUaClient`) were deleted in the current dev branch -- in-progress refactor to composition-based model
- **Dependencies**: Helix.Orchestration.Core

### Helix.Orchestration.Agent
- **Purpose**: Fumehood agent executable -- hosts device adapters, executes runspaces, exposes gRPC server
- **Key namespaces**:
  - `Adapters/` -- `IAdapterDiscovery`, `AdapterDiscoveryService`, `AdapterProxy`, `AdapterRegistry`, `AdapterHealthMonitor`
  - `Adapters/Devices/` -- `DeviceAdapterExtensions`, `DeviceOperationParameters`
  - `Execution/` -- `WslExecutionAdapter`
  - `gRPC/` -- `AgentControlService`, `GrpcServerHost`
  - `Leasing/` -- `LeaseExpiryWorker` (hosted service)
  - `Runspaces/` -- `DurableRunspace`, `RunspaceExecutionContext`, `DefinitionCompiler`, `RunspaceManager`
  - `Streaming/` -- `MqttTrendPublisher`, `TrendAggregator`, `TrendBuffer`
  - `Telemetry/` -- `HelixTelemetry`
  - `Configuration/` -- `AgentConfiguration`, `DeviceRegistry`, `agent-config.json`, `device-registry.json`
- **Entry point**: `Program.cs`
- **Dependencies**: Core, Contracts, Devices, Infrastructure

### Helix.Orchestration.SiteServer
- **Purpose**: Site-wide server -- REST API, campaign manager, simulation, WSL compilation, agent proxy
- **Key components** (from phase handover docs):
  - REST endpoints grouped by resource (`/api/campaigns`, `/api/runspaces`, etc.)
  - `CampaignManager` -- sequential iteration strategy, in-memory lifecycle
  - `IAgentProxy` / `SimAgentProxy` / `GrpcAgentProxy`
  - `SimulatedTrendProvider`, `SimReactor`, `SimPump`, `SimSpectrometer`
  - MQTT trend bridge publishing to technology-agnostic topics
- **Dependencies**: Core, Contracts, Infrastructure

### Helix.Orchestration.Simulation
- **Purpose**: Simulated device implementations for hardware-free development and testing
- **Dependencies**: Core, Devices

### Helix.Orchestration.Infrastructure
- **Purpose**: PostgreSQL persistence (EF Core), MinIO object storage
- **Key files**:
  - `Persistence/InfrastructureServiceExtensions.cs` -- DI registration
  - `Storage/IObjectStorageSink.cs`, `MinioObjectStorageSink.cs`, `MinioOptions.cs`
- **Dependencies**: Core, EF Core, Npgsql, Minio SDK

### Helix.Orchestration.Recorder
- **Purpose**: Standalone Worker Service -- captures MQTT events, batches to JSONL, flushes to object storage
- **Key files**:
  - `Services/MqttRecorderService.cs` -- MQTT subscriber, publishes to bounded channel
  - `Services/RecorderWriterService.cs` -- drains channel, writes JSONL batches
  - `Models/RecordedEvent.cs` -- event envelope
  - `Configuration/RecorderOptions.cs` -- flush policy (count/time), topic config
  - `Program.cs` -- Worker Service host
- **Dependencies**: Infrastructure (for `IObjectStorageSink`), MQTTnet

## Test Projects

| Project | Covers |
|---------|--------|
| `Helix.Orchestration.Core.Tests` | Capabilities, leasing, trends, safety, WSL, workflow execution |
| `Helix.Orchestration.Agent.Tests` | DurableRunspace, LeaseExpiryWorker, WslExecutionAdapter |
| `Helix.Orchestration.SiteServer.Tests` | REST endpoints, campaign manager, agent proxy, MQTT bridge |
| `Helix.Orchestration.Infrastructure.Tests` | PostgreSQL repositories (container-backed integration tests) |
| `Helix.Orchestration.Devices.Tests` | Transport and device driver tests (hardware-free via test seams) |
| `Helix.Orchestration.Recorder.Tests` | MQTT parsing, JSONL writer logic |

All test projects are under `Helix.Orchestration/tests/`.

## Configuration Files

| File | Purpose |
|------|---------|
| `docker-compose.yml` (root) | Local infra: Mosquitto MQTT (1883/9001), PostgreSQL (5432), MinIO (9000/9001) |
| `Helix.Orchestration.Agent/Configuration/agent-config.json` | Agent runtime configuration |
| `Helix.Orchestration.Agent/Configuration/device-registry.json` | Device descriptor registry |
| `Helix.Orchestration.Recorder/appsettings.json` | Recorder Worker Service config |
| `.github/workflows/ci.yml` | GitHub Actions CI pipeline |

## Adapters

```
adapters/python/pump_simulator/
|-- pump_adapter.py             Python pump adapter (gRPC server)
|-- install-windows-service.ps1
`-- pump-adapter.service        systemd unit (Linux)
```

## Where to Add New Code

| Task | Location |
|------|----------|
| New domain concept / interface | `Helix.Orchestration.Core/` in the appropriate subdomain folder |
| New device transport | `Helix.Orchestration.Devices/Transport/` |
| New device interface | `Helix.Orchestration.Devices/Interfaces/` |
| New device driver/adapter | `Helix.Orchestration.Agent/Adapters/Devices/` |
| New gRPC service | `Helix.Orchestration.Contracts/Protos/` + `Helix.Orchestration.Agent/gRPC/` |
| New REST endpoint (Site Server) | `Helix.Orchestration.SiteServer/` endpoint group |
| New infrastructure repository | `Helix.Orchestration.Infrastructure/Persistence/` |
| New simulated device | `Helix.Orchestration.Simulation/` |
| New phase handover doc | `Helix.Orchestration/docs/PHASE{N}-{TOPIC}-HANDOVER.md` |
