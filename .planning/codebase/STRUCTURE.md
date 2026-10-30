---
last_mapped_commit: 25fb557bd430bea160370af41b8deb877170076b
last_mapped_at: 2026-10-29
---
# Codebase Structure

**Analysis Date:** 2026-10-29

## Directory Layout

```text
Helix/
├── Helix.Orchestration.Core/           # Domain models, workflow, WSL, safety, state
├── Helix.Orchestration.Contracts/      # Shared protobuf/gRPC contracts
├── Helix.Orchestration.Devices/        # Device adapters, drivers, and transports
├── Helix.Orchestration.Infrastructure/ # PostgreSQL and object-storage implementations
├── Helix.Orchestration.Agent/          # Per-fumehood ASP.NET Core host
├── Helix.Orchestration.SiteServer/     # Site control-plane ASP.NET Core host
├── Helix.Orchestration.Recorder/       # MQTT recording worker host
├── Helix.Orchestration.Simulation/     # Hardware-free implementations
├── tests/                              # Project-aligned .NET test projects
├── helix-dashboard/                    # Vite/React browser dashboard
├── helix-desktop/                      # Vite/React + Tauri desktop application
├── adapters/python/pump_simulator/     # Python adapter/service example
├── postgres/migrations/                # PostgreSQL initialization SQL
├── mosquitto/                          # MQTT broker configuration
├── deploy/                             # systemd, Windows, and preflight scripts
├── docs-site/                          # Docusaurus documentation site
├── docs/                               # Engineering handover and project documents
├── docker-compose.yml                  # Local multi-service topology
└── Helix.Orchestration.slnx            # .NET solution manifest
```

## Directory Purposes

**`Helix.Orchestration.Core/`:**
- Purpose: own framework-independent orchestration domain contracts and behavior.
- Contains: focused bounded-context directories such as `Capabilities/`, `Execution/`, `Experiment/`, `Leasing/`, `Runspace/`, `Safety/`, `State/`, `Trends/`, `WSL/`, and `WorkflowEngine/`.
- Key files: `WSL/WslCompiler.cs`, `Capabilities/CapabilityRegistry.cs`, `Execution/WorkflowExecutor.cs`, `WorkflowEngine/WorkflowContext.cs`.

**`Helix.Orchestration.Contracts/`:**
- Purpose: define cross-process Agent control protocol.
- Contains: `.proto` schemas compiled by the project.
- Key files: `Protos/agent_control.proto`, `Protos/adapter.proto`.

**`Helix.Orchestration.Devices/`:**
- Purpose: isolate laboratory hardware-specific code.
- Contains: `Abstractions/`, `Drivers/`, `Transport/` contracts, `Transports/` implementations, `Interfaces/`, `Model/`, and `FaultTolerance/`.
- Key files: `Abstractions/DeviceAdapterBase.cs`, `Transport/ISerialTransport.cs`, `Transports/Serial/SerialDeviceTransport.cs`.

**`Helix.Orchestration.Agent/`:**
- Purpose: run a fumehood-local orchestration service.
- Contains: `Api/` minimal endpoint groups, `gRPC/Server/`, `Adapters/Devices/` registrations/bridges, `Runspaces/`, `Experiments/`, `Events/`, `Trends/`, `Transport/`, and background worker folders.
- Key files: `Program.cs`, `gRPC/Server/AgentGrpcService.cs`, `Adapters/Devices/DeviceAdapterExtensions.cs`.

**`Helix.Orchestration.SiteServer/`:**
- Purpose: site-wide control-plane API and Agent coordination.
- Contains: `Endpoints/`, `Agent/` proxy implementations, `Campaigns/`, `Contracts/` API DTOs, `Mqtt/`, and `WebSockets/`.
- Key files: `Program.cs`, `Agent/IAgentProxy.cs`, `Campaigns/CampaignManager.cs`.

**`Helix.Orchestration.Infrastructure/`:**
- Purpose: concrete external persistence and storage adapters.
- Contains: `Persistence/` repository interfaces/implementations and `Storage/` MinIO adapter/options.
- Key files: `Persistence/InfrastructureServiceExtensions.cs`, `Persistence/PostgresExperimentRepository.cs`, `Storage/MinioObjectStorageSink.cs`.

**`Helix.Orchestration.Recorder/`:**
- Purpose: independently record MQTT events to object storage.
- Contains: `Configuration/`, `Models/`, and `Services/`.
- Key files: `Program.cs`, `Services/MqttRecorderService.cs`, `Services/RecorderWriterService.cs`.

**`Helix.Orchestration.Simulation/`:**
- Purpose: supply substitutes for real lab hardware.
- Contains: simulated devices in `Devices/`, trend provider, capability invoker, and DI registration.
- Key files: `SimulationServiceExtensions.cs`, `SimCapabilityInvoker.cs`, `SimulatedTrendProvider.cs`.

**`tests/`:**
- Purpose: maintain separate test assemblies mirroring production project ownership.
- Contains: `Helix.Orchestration.<Project>.Tests/` and behavior-oriented subfolders such as `Api/`, `Execution/`, `Drivers/`, `Persistence/`, and `Endpoints/`.
- Key files: `tests/Helix.Orchestration.SiteServer.Tests/HelixWebApplicationFactory.cs` and each `*.Tests.csproj`.

**`helix-dashboard/`:**
- Purpose: browser dashboard for the Site Server.
- Contains: route `pages/`, common `components/`, service hooks, app types, test setup, themes, Vite configuration.
- Key files: `src/main.tsx`, `src/App.tsx`, `src/hooks/useTrendWebSocket.ts`.

**`helix-desktop/`:**
- Purpose: Tauri-packaged local operator interface.
- Contains: React `components/`, `hooks/`, `shell/`, `lib/`, and TypeScript `types/`; Rust host lies in `src-tauri/`.
- Key files: `src/main.tsx`, `src/shell/HelixShell.tsx`, `src-tauri/src/lib.rs`, `src-tauri/src/commands/connection.rs`.

## Key File Locations

**Entry Points:**
- `Helix.Orchestration.Agent/Program.cs`: Agent HTTP/gRPC host and dependency composition.
- `Helix.Orchestration.SiteServer/Program.cs`: Site Server REST/WebSocket host and dependency composition.
- `Helix.Orchestration.Recorder/Program.cs`: recorder generic host.
- `helix-dashboard/src/main.tsx`: dashboard React startup.
- `helix-desktop/src/main.tsx`: desktop React startup.
- `helix-desktop/src-tauri/src/main.rs`: Tauri executable startup.

**Configuration:**
- `Helix.Orchestration.slnx`: solution project list.
- `docker-compose.yml`: local broker, database, object storage, and service wiring.
- `Helix.Orchestration.Agent/appsettings.json`: Agent settings shape.
- `Helix.Orchestration.SiteServer/appsettings.json`: Site Server settings shape.
- `Helix.Orchestration.Recorder/appsettings.json`: recorder settings shape.
- `helix-dashboard/vite.config.ts` and `helix-desktop/vite.config.ts`: frontend build setup.
- `helix-desktop/src-tauri/tauri.conf.json`: desktop packaging/runtime configuration.

**Core Logic:**
- `Helix.Orchestration.Core/WSL/`: workflow specification model and compiler.
- `Helix.Orchestration.Core/Execution/`: durable workflow execution contracts/implementation.
- `Helix.Orchestration.Core/Capabilities/`: named capability invocation model.
- `Helix.Orchestration.Agent/Adapters/Devices/`: device composition and capability bridges.
- `Helix.Orchestration.SiteServer/Campaigns/`: campaign orchestration.

**Testing:**
- `tests/Helix.Orchestration.Core.Tests/`: Core behavior tests.
- `tests/Helix.Orchestration.Agent.Tests/`: Agent endpoint, execution, gRPC, and worker tests.
- `tests/Helix.Orchestration.Devices.Tests/`: drivers/transports/fault-tolerance tests.
- `tests/Helix.Orchestration.SiteServer.Tests/`: proxy, endpoint, campaign, and simulation tests.
- `helix-dashboard/src/**/*.test.tsx` and `helix-desktop/src/**/*.test.tsx`: frontend tests co-located with components/hooks.

## Naming Conventions

**Files:**
- Use PascalCase singular C# filenames matching their public type: `CapabilityRegistry.cs`, `CampaignManager.cs`, `GrpcAgentProxy.cs`.
- Prefix abstractions with `I`: `IAgentProxy.cs`, `ICampaignRepository.cs`, `ITrendProvider.cs`.
- Name service-registration extension classes `*ServiceExtensions.cs` or `*Extensions.cs`: `InfrastructureServiceExtensions.cs`, `DeviceAdapterExtensions.cs`.
- Name endpoint modules by resource plus `Endpoints`: `ExperimentEndpoints.cs`, `CampaignEndpoints.cs`.
- Use React PascalCase component/page files and camelCase hook/helper files: `DeviceRegistry.tsx`, `TrendsPage.tsx`, `useTrendWebSocket.ts`, `workflowLibrary.ts`.
- Co-locate TypeScript tests under the source tree with `.test.ts` or `.test.tsx`; place C# tests in a matching test-project folder with `Tests.cs` suffix.

**Directories:**
- Organize C# code by domain responsibility rather than generic technical type; e.g. `Core/Leasing/`, `SiteServer/Campaigns/`, and `Agent/Events/`.
- Put concrete protocol implementations below `Devices/Transports/<Protocol>/` and hardware drivers directly under `Devices/Drivers/`.
- Use lowercase hyphenated directory names for web applications: `helix-dashboard/` and `helix-desktop/`.

## Where to Add New Code

**New workflow-domain feature:**
- Primary contracts/models: `Helix.Orchestration.Core/<DomainArea>/`.
- Runtime binding: `Helix.Orchestration.Agent/Program.cs` or `Helix.Orchestration.SiteServer/Program.cs`.
- Tests: `tests/Helix.Orchestration.Core.Tests/<DomainArea>/` plus host-specific test project when transport behavior is involved.

**New REST API resource:**
- Site-wide API implementation: `Helix.Orchestration.SiteServer/Endpoints/<Resource>Endpoints.cs`.
- Agent-local API implementation: `Helix.Orchestration.Agent/Api/<Resource>Endpoints.cs`.
- DTOs for Site Server: `Helix.Orchestration.SiteServer/Contracts/<Resource>Contracts.cs`.
- Tests: `tests/Helix.Orchestration.SiteServer.Tests/Endpoints/` or `tests/Helix.Orchestration.Agent.Tests/Api/`.

**New device integration:**
- Driver: `Helix.Orchestration.Devices/Drivers/<DeviceName>.cs`.
- Protocol abstraction or transport: `Helix.Orchestration.Devices/Transport/` or `Helix.Orchestration.Devices/Transports/<Protocol>/`.
- Agent capability bridge/registration: `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs`.
- Tests: `tests/Helix.Orchestration.Devices.Tests/Drivers/<DeviceName>Tests.cs` and transport tests as applicable.

**New persistence backend or repository:**
- Port: `Helix.Orchestration.Infrastructure/Persistence/I<Resource>Repository.cs` when it is infrastructure-owned; domain-facing contract must remain in Core if consumers require it.
- PostgreSQL implementation: `Helix.Orchestration.Infrastructure/Persistence/Postgres<Resource>Repository.cs`.
- Registration: `Helix.Orchestration.Infrastructure/Persistence/InfrastructureServiceExtensions.cs`.
- Schema initialization: `postgres/migrations/<NNN>_<name>.sql`.

**New UI feature:**
- Dashboard route/view: `helix-dashboard/src/pages/`; reusable UI: `helix-dashboard/src/components/`; integration hook: `helix-dashboard/src/hooks/`.
- Desktop workflow/control component: `helix-desktop/src/components/`; application shell/panels: `helix-desktop/src/shell/`; connection state: `helix-desktop/src/hooks/` or `src/types/`.
- Add a co-located `.test.tsx` when adding a React component/hook.

**Utilities:**
- C# shared domain helpers live with their domain in `Helix.Orchestration.Core/`, not a catch-all utility folder.
- Dashboard shared helpers live in `helix-dashboard/src/hooks/` or `src/types/` according to whether they implement behavior or contracts.
- Desktop helpers live in `helix-desktop/src/lib/`.

## Special Directories

**`postgres/migrations/`:**
- Purpose: bootstrap schema SQL mounted by local PostgreSQL container.
- Generated: No.
- Committed: Yes.

**`deploy/`:**
- Purpose: OS service installation and environment preflight scripts.
- Generated: No.
- Committed: Yes.

**`docs-site/build/`, `helix-dashboard/dist/`, `helix-desktop/dist/`:**
- Purpose: frontend generated build output.
- Generated: Yes.
- Committed: No; source lives in the corresponding `src/` directory.

**`coverage-results/`, `test-results/`, `test-results-hang-test/`:**
- Purpose: generated test and coverage artifacts.
- Generated: Yes.
- Committed: No.

**`helix-desktop/src-tauri/target/`:**
- Purpose: Cargo build artifacts.
- Generated: Yes.
- Committed: No.

---

*Structure analysis: 2026-10-29*
