# Codebase Structure

**Analysis Date:** 2026-09-05

## Directory Layout

```
Helix/                                      # Solution root
├── Helix.Orchestration.Agent/              # Agent microservice (C#, ASP.NET Core)
│   ├── Adapters/Devices/                   # Device adapter DI registration helpers
│   ├── Api/                                # Minimal API endpoint map files
│   ├── Configuration/                      # Agent config classes
│   ├── Events/                             # RunspaceEvent bus + ExperimentEventChannel
│   ├── Execution/                          # WslExecutionAdapter
│   ├── Experiments/                        # ExperimentManager, persistence worker
│   ├── gRPC/Server/                        # AgentGrpcService (gRPC server impl)
│   ├── HealthChecks/                       # Custom health check implementations
│   ├── Leasing/                            # LeaseExpiryWorker
│   ├── Runspaces/                          # DurableRunspace (Cleipnir-backed)
│   ├── Streaming/                          # TrendAggregator, MqttTrendIngestionService
│   ├── Telemetry/                          # HelixTelemetry (OpenTelemetry)
│   ├── Transport/                          # Transport-level utilities
│   ├── Trends/                             # InMemoryTrendProvider, ScalarTrendNotification
│   ├── AgentService.cs                     # Hosted service: WebSocket hub + broadcast loops
│   ├── Program.cs                          # DI bootstrap, endpoint mapping, startup
│   └── appsettings.json                    # Default configuration
│
├── Helix.Orchestration.SiteServer/         # SiteServer microservice (C#, ASP.NET Core)
│   ├── Agent/                              # Agent proxy client (sim or gRPC)
│   ├── Campaigns/                          # CampaignManager, iteration strategies
│   ├── Contracts/                          # SiteServer-internal DTOs
│   ├── Endpoints/                          # Minimal API endpoint map files
│   ├── Mqtt/                               # MqttTrendBridge hosted service
│   ├── WebSockets/                         # TrendWebSocketManager
│   └── Program.cs                          # DI bootstrap, endpoint mapping
│
├── Helix.Orchestration.Core/               # Domain layer (pure C#, no framework deps)
│   ├── Capabilities/                       # CapabilityRegistry, ICapabilityInvoker, CapabilityDescriptor
│   ├── Devices/                            # DeviceDescriptor, DeviceRegistry
│   ├── Execution/                          # IWorkflowExecutor, WorkflowExecutor, Branch/Loop/Parallel
│   ├── Experiment/                         # ExperimentContext, ExperimentLifecycle, ExperimentPhase
│   ├── Leasing/                            # IDeviceLeasingService, InMemoryDeviceLeasingService
│   ├── Runspace/                           # IRunspaceManager, IRunspace, InMemoryRunspace, RunspaceManager
│   ├── Safety/                             # ISafetyEngine, SafetyEngine, InterlockRule
│   ├── State/                              # IWorkflowStateStore, IGlobalStateStore, WorkflowState
│   ├── TestContext/                        # Test context helpers
│   ├── Trends/                             # ITrendProvider, TrendValue, SpectralValue
│   ├── Trends/Spectral/                    # Spectral trend types
│   ├── WSL/                                # WslCompiler, WslDocument, WslNode, WslEdge, RunspaceDefinition
│   └── WorkflowEngine/                     # IWorkflow, WorkflowRegistry, WorkflowContext
│
├── Helix.Orchestration.Devices/            # Device driver layer (C#)
│   ├── Abstractions/                       # DeviceAdapterBase, IHelixAdapter, IDeviceTransport
│   ├── Drivers/                            # 40+ physical device driver implementations
│   ├── FaultTolerance/                     # Retry/circuit-breaker decorators
│   ├── Hosting/                            # DI extension AddMigratedDeviceAdapters()
│   ├── Interfaces/                         # Device-specific interface contracts (IHplc, IPump, etc.)
│   ├── Model/                              # Shared device model types
│   └── Transports/                         # CDll/, Modbus/, Null/, Opc/, Serial/, Tcp/
│
├── Helix.Orchestration.Infrastructure/     # Persistence layer (C#, Dapper + PostgreSQL)
│   ├── Persistence/                        # PostgresCampaignRepository, PostgresExperimentRepository,
│   │                                       #   PostgresExperimentEventRepository, DbConnectionFactory
│   └── Storage/                            # MinIO / file storage (future)
│
├── Helix.Orchestration.Contracts/          # Shared gRPC protobuf contracts
│   └── Protos/                             # adapter.proto, agent_control.proto
│
├── Helix.Orchestration.Simulation/         # Simulated devices and trends (C#)
│   └── Devices/                            # SimulatorDevice, SimCapabilityInvoker, SimulatedTrendProvider
│
├── Helix.Orchestration.Recorder/           # MQTT-to-MinIO recorder worker (C#)
│   ├── Configuration/                      # Recorder config classes
│   ├── Models/                             # Recording models
│   └── Services/                           # MqttRecorderService
│
├── helix-desktop/                          # Tauri v2 desktop application
│   ├── src/                                # React/TypeScript frontend
│   │   ├── shell/                          # Top-level shell components
│   │   │   ├── HelixShell.tsx              # Composition root — all state, WebSocket, layout
│   │   │   ├── Ribbon.tsx / Ribbon.css     # Tabbed ribbon toolbar
│   │   │   ├── ActivityBar.tsx             # Left icon sidebar
│   │   │   ├── StatusBar.tsx               # Bottom status bar
│   │   │   ├── WorkspaceLayout.tsx         # rc-dock docking layout + WorkspaceLayoutHandle
│   │   │   └── panels/                     # Dockable panel components
│   │   │       ├── DeviceExplorer.tsx      # Device list sidebar panel
│   │   │       ├── TrendExplorer.tsx       # Trend channel browser
│   │   │       ├── ChartPanel.tsx          # Live trend chart panel
│   │   │       ├── LogOutput.tsx           # Log output panel
│   │   │       └── ExperimentTimeline.tsx  # Experiment phase timeline panel
│   │   ├── components/                     # Feature components (docked in main area)
│   │   │   ├── ConnectionConfig.tsx        # Agent connection settings
│   │   │   ├── DeviceManualControl/        # Manual device command UI
│   │   │   ├── DeviceRegistry.tsx          # Device adapter health + capability browser
│   │   │   ├── ExecutionPanel/             # Runspace execution status
│   │   │   ├── SafetyPanel/                # Safety interlock display
│   │   │   ├── TrendCharts/                # Chart rendering components
│   │   │   ├── WorkflowBuilder/            # React Flow workflow graph editor
│   │   │   │   ├── WorkflowBuilder.tsx     # Main editor canvas
│   │   │   │   ├── DevicePalette.tsx       # Node palette (drag source)
│   │   │   │   ├── useHelixFlow.ts         # React Flow state hook
│   │   │   │   ├── nodes/                  # Custom React Flow node types
│   │   │   │   └── edges/                  # Custom React Flow edge types
│   │   │   └── charts/                     # Shared chart sub-components
│   │   ├── hooks/                          # Custom React hooks
│   │   │   ├── useAgentHealth.ts           # Polls GET /health
│   │   │   ├── useAgentWebSocket.ts        # WebSocket connection + event dispatch
│   │   │   ├── useExperiment.ts            # Experiment REST API wrapper
│   │   │   ├── useNodeExecutionState.ts    # Maps runspace events to node highlight states
│   │   │   ├── useTrendRegistry.ts         # Tracks known trend channels
│   │   │   └── useWorkspaceLayouts.ts      # Layout save/load via Tauri store or localStorage
│   │   ├── types/                          # TypeScript type definitions
│   │   │   └── agent.ts                    # All shared agent types (WslDocument, RunspaceEvent, etc.)
│   │   ├── styles/                         # Global and component CSS
│   │   ├── lib/                            # Utility helpers
│   │   └── main.tsx                        # Vite entry point
│   └── src-tauri/                          # Rust Tauri backend
│       ├── src/
│       │   ├── lib.rs                      # Tauri builder, plugin registration
│       │   ├── main.rs                     # Process entry point
│       │   └── commands/                   # Tauri IPC commands (load_config, save_config)
│       └── capabilities/                   # Tauri v2 capability files
│
├── helix-dashboard/                        # Standalone React web dashboard (Vite)
│   └── src/
│       ├── components/                     # Dashboard UI components
│       ├── hooks/                          # Dashboard hooks
│       ├── pages/                          # Route pages
│       ├── themes/                         # Theme CSS
│       └── types/                          # Type definitions
│
├── tests/                                  # All .NET test projects
│   ├── Helix.Orchestration.Agent.Tests/
│   ├── Helix.Orchestration.Core.Tests/
│   ├── Helix.Orchestration.Devices.Tests/
│   ├── Helix.Orchestration.Infrastructure.Tests/
│   ├── Helix.Orchestration.Recorder.Tests/
│   └── Helix.Orchestration.SiteServer.Tests/
│
├── adapters/python/pump_simulator/         # Python pump device simulator
├── deploy/                                 # Deployment scripts (systemd, Windows, preflight)
├── postgres/migrations/                    # SQL migration scripts
├── mosquitto/                              # MQTT broker config
├── docs/                                   # Architecture / developer docs (markdown)
├── docs-site/                              # Docusaurus documentation site
├── docker-compose.yml                      # Local dev services (Mosquitto, Postgres, MinIO, Agent, SiteServer, Recorder)
├── Helix.Orchestration.slnx               # .NET solution file
└── .planning/                              # GSD planning artifacts
    ├── codebase/                           # Architecture docs (this file)
    ├── phases/                             # Phase plan files
    └── sketches/                           # UI/UX design sketches
```

## Directory Purposes

**`Helix.Orchestration.Agent/`:**
- Purpose: Runnable ASP.NET Core application — the execution engine that operators connect to
- Contains: Hosted services, REST endpoints, gRPC server, event bus implementations
- Key files: `Program.cs`, `AgentService.cs`, `Api/RunspaceEndpoints.cs`, `Api/ExperimentEndpoints.cs`

**`Helix.Orchestration.Core/`:**
- Purpose: Framework-agnostic domain model — this is the heart of all business logic
- Contains: Interfaces, domain services, WSL compiler and model, safety engine, capability registry
- Key files: `WSL/WslCompiler.cs`, `Runspace/IRunspaceManager.cs`, `Capabilities/CapabilityRegistry.cs`, `Safety/SafetyEngine.cs`

**`Helix.Orchestration.Devices/`:**
- Purpose: Physical instrument integration — one file per device family
- Contains: 40+ drivers under `Drivers/`, transport implementations under `Transports/`
- Key files: `Abstractions/DeviceAdapterBase.cs`, `Hosting/` DI registration, `Drivers/SimulatorDevice.cs`

**`helix-desktop/src/shell/`:**
- Purpose: Top-level UI shell — all shell-level state lives here (connection, WebSocket, layout)
- Contains: Shell frame components, dockable panel components, workspace layout manager
- Key files: `HelixShell.tsx`, `WorkspaceLayout.tsx`, `Ribbon.tsx`, `panels/`

**`helix-desktop/src/components/`:**
- Purpose: Feature panel components that are docked into the workspace layout
- Contains: WorkflowBuilder graph editor, ExecutionPanel, SafetyPanel, DeviceManualControl, TrendCharts
- Key files: `WorkflowBuilder/WorkflowBuilder.tsx`, `ExecutionPanel/ExecutionPanel.tsx`

**`helix-desktop/src/hooks/`:**
- Purpose: All data-fetching and state-management logic isolated from components
- Contains: WebSocket hook, health hook, experiment hook, trend registry, layout persistence, node execution state
- Key files: `useAgentWebSocket.ts`, `useWorkspaceLayouts.ts`, `useNodeExecutionState.ts`

## Key File Locations

**Entry Points:**
- `Helix.Orchestration.Agent/Program.cs`: Agent service startup and DI wiring
- `Helix.Orchestration.SiteServer/Program.cs`: SiteServer startup and DI wiring
- `helix-desktop/src/main.tsx`: React app entry (Vite)
- `helix-desktop/src-tauri/src/lib.rs`: Tauri backend entry

**Configuration:**
- `Helix.Orchestration.Agent/appsettings.json`: Agent defaults
- `Helix.Orchestration.SiteServer/appsettings.json`: SiteServer defaults
- `docker-compose.yml`: All dev service definitions
- `helix-desktop/src/types/agent.ts`: TypeScript type definitions (also serves as the UI-backend contract)

**Core Logic:**
- `Helix.Orchestration.Core/WSL/WslCompiler.cs`: Workflow compilation pipeline
- `Helix.Orchestration.Core/Capabilities/CapabilityRegistry.cs`: Capability dispatch
- `Helix.Orchestration.Core/Safety/SafetyEngine.cs`: Safety interlock evaluation
- `Helix.Orchestration.Agent/AgentService.cs`: WebSocket broadcast service

**Testing:**
- `tests/Helix.Orchestration.Core.Tests/`: Core domain unit tests
- `tests/Helix.Orchestration.Agent.Tests/Integration/`: Agent integration tests
- `helix-desktop/src/shell/*.test.tsx`: Shell component tests (Vitest)
- `helix-desktop/src/components/**/*.test.tsx`: Component tests (Vitest)

## Naming Conventions

**Files (.NET):**
- `PascalCase.cs` for all C# files
- Interface files: `I{Name}.cs` (e.g. `IRunspaceManager.cs`, `IHelixAdapter.cs`)
- Endpoint files: `{Domain}Endpoints.cs` (e.g. `RunspaceEndpoints.cs`, `ExperimentEndpoints.cs`)
- Repository files: `Postgres{Domain}Repository.cs`

**Files (TypeScript/React):**
- `PascalCase.tsx` for React components (e.g. `WorkflowBuilder.tsx`, `HelixShell.tsx`)
- `camelCase.ts` for hooks prefixed with `use` (e.g. `useAgentWebSocket.ts`)
- `PascalCase.css` for component-scoped styles co-located with the component
- Test files: `{Component}.test.tsx` or `{hook}.test.ts` co-located

**Directories (.NET):**
- `PascalCase` matching the primary concept (e.g. `Runspaces/`, `Capabilities/`, `Transports/`)

**Directories (Frontend):**
- `camelCase` for feature directories under `components/` (e.g. `WorkflowBuilder/`, `ExecutionPanel/`)
- `camelCase` for `hooks/`, `types/`, `lib/`, `styles/`

## Where to Add New Code

**New device driver:**
- Implementation: `Helix.Orchestration.Devices/Drivers/{DeviceName}.cs` extending `DeviceAdapterBase`
- Registration: `Helix.Orchestration.Devices/Hosting/` DI extension method `AddMigratedDeviceAdapters()`
- Interface (if needed): `Helix.Orchestration.Devices/Interfaces/I{DeviceName}.cs`
- Tests: `tests/Helix.Orchestration.Devices.Tests/Drivers/`

**New Agent REST endpoint:**
- Endpoint map: `Helix.Orchestration.Agent/Api/{Domain}Endpoints.cs`
- Register in: `Helix.Orchestration.Agent/Program.cs` — add `app.Map{Domain}Endpoints()`
- Tests: `tests/Helix.Orchestration.Agent.Tests/Api/`

**New domain service in Core:**
- Interface: `Helix.Orchestration.Core/{Domain}/I{ServiceName}.cs`
- Implementation: `Helix.Orchestration.Core/{Domain}/{ServiceName}.cs`
- Tests: `tests/Helix.Orchestration.Core.Tests/{Domain}/`

**New React shell panel (dockable):**
- Component: `helix-desktop/src/shell/panels/{PanelName}.tsx`
- Register tab: Add tab definition in `WorkspaceLayout.tsx` `DEFAULT_LAYOUT` and `buildTabData()` function
- Tests: `helix-desktop/src/shell/panels/{PanelName}.test.tsx`

**New React feature component:**
- Directory: `helix-desktop/src/components/{FeatureName}/{FeatureName}.tsx`
- Add CSS: `helix-desktop/src/components/{FeatureName}/{FeatureName}.css` (or use `styles/`)
- Tests: `helix-desktop/src/components/{FeatureName}/{FeatureName}.test.tsx`

**New custom hook:**
- File: `helix-desktop/src/hooks/use{HookName}.ts`
- Tests: `helix-desktop/src/hooks/use{HookName}.test.ts`

**New shared TypeScript type (agent contract):**
- File: `helix-desktop/src/types/agent.ts` — append to existing file (all agent types are co-located here)

**New PostgreSQL repository:**
- Interface: `Helix.Orchestration.Infrastructure/Persistence/I{Domain}Repository.cs`
- Implementation: `Helix.Orchestration.Infrastructure/Persistence/Postgres{Domain}Repository.cs`
- Register in: `Helix.Orchestration.Infrastructure/Persistence/InfrastructureServiceExtensions.cs`
- Migration SQL: `postgres/migrations/`

## Special Directories

**`.planning/`:**
- Purpose: GSD planning documents — phases, seeds, sketches, codebase maps
- Generated: No (hand-authored and Claude-generated)
- Committed: Yes

**`coverage-results/`:**
- Purpose: Test coverage report outputs from `dotnet test --collect:"XPlat Code Coverage"`
- Generated: Yes
- Committed: No (gitignored)

**`test-results/`:**
- Purpose: TRX and test result XML files from `dotnet test`
- Generated: Yes
- Committed: No

**`helix-desktop/dist/`:**
- Purpose: Vite build output for Tauri to package
- Generated: Yes
- Committed: No

**`helix-dashboard/dist/`:**
- Purpose: Vite build output for the web dashboard
- Generated: Yes
- Committed: No

**`Helix.Orchestration.Agent/logs/`:**
- Purpose: Rolling JSON log files from Serilog
- Generated: Yes
- Committed: No

**`helix-desktop/src-tauri/target/`:**
- Purpose: Rust/Cargo build artifacts
- Generated: Yes
- Committed: No

---

*Structure analysis: 2026-09-05*
