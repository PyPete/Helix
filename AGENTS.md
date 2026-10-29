<!-- GSD:project-start source:PROJECT.md -->

## Project

**Helix Vision Mark II**

Helix Vision Mark II is a local-first scientific workstation for a scientist operating a fumehood. It connects a live digital representation of laboratory apparatus with device capabilities, durable experiment execution, samples and analytical results, so a scientist can progress safely from experiment design to adaptive scientific campaigns.

The desktop UI helps construct and observe the workflow, while the local Helix Agent remains the durable authority for physical execution, safety, and recovery.

**Core Value:** Reliably complete a traceable and safe scientific feedback loop from physical apparatus execution through sample collection and reviewed analytical evidence to the next validated experiment.

### Constraints

- **Execution authority**: The local Helix Agent alone may address drivers or execute physical commands — UI and LLM outputs are untrusted proposals, not device authority.
- **Safety**: Every device command must pass deterministic capability, safety, lease, resource, budget, and termination validation — autonomous campaigns are bounded by an approved policy.
- **Autonomy**: A scientist approves the campaign policy up front — the Agent may execute validated iterations within that policy without approving every run.
- **Reliability**: Campaign waits, external-result correlation, and decisions must survive UI closure and Agent restart — recovery reconciles physical state rather than blindly replaying commands.
- **Scientific integrity**: Samples, results, calculations, decisions, and approvals require stable identities, immutable revisions, provenance, units/quality validation, and idempotent processing.
- **Hardware rollout**: Simulated devices are the initial execution target — real-device integration follows a proven end-to-end slice.
- **Analysis MVP**: Structured CSV import plus review is the first analytical-result path — preserve the source artifact and quarantine ambiguous or invalid results.

<!-- GSD:project-end -->

<!-- GSD:stack-start source:codebase/STACK.md -->

## Technology Stack

## Languages

- C# / .NET 10 (`net10.0`) powers the orchestration services and class libraries in `Helix.Orchestration.*/*.csproj`.
- TypeScript powers the Vite web applications in `helix-dashboard/`, `helix-desktop/`, and the Docusaurus site in `docs-site/`.
- Rust 1.77+ supports the Tauri desktop host in `helix-desktop/src-tauri/Cargo.toml`.
- Protocol Buffers defines gRPC contracts in `Helix.Orchestration.Contracts/`.
- SQL is embedded in the PostgreSQL repositories under `Helix.Orchestration.Infrastructure/Persistence/`.

## Runtime

- .NET 10 SDK/runtime builds and runs the services listed by `Helix.Orchestration.slnx`.
- Node.js 20+ is explicitly required by `docs-site/package.json`; it also runs the Vite applications.
- Rust/Cargo is required to build the Tauri native wrapper in `helix-desktop/src-tauri/`.
- NuGet restores .NET dependencies declared by each `*.csproj`; no `packages.lock.json` is detected.
- npm manages each JavaScript workspace; lockfiles are present at `helix-dashboard/package-lock.json`, `helix-desktop/package-lock.json`, and `docs-site/package-lock.json`.

## Frameworks

- ASP.NET Core minimal APIs / hosted services provide HTTP, gRPC, and WebSocket endpoints in `Helix.Orchestration.Agent/Program.cs` and `Helix.Orchestration.SiteServer/Program.cs`.
- React powers the dashboard (`helix-dashboard/package.json`, React 19) and desktop renderer (`helix-desktop/package.json`, React 18).
- Tauri 2 combines the Rust host and React renderer for `helix-desktop/`.
- Docusaurus 3 generates documentation from `docs-site/`.
- xUnit, Microsoft.NET.Test.Sdk, coverlet, Moq/NSubstitute, and Testcontainers PostgreSQL test .NET projects in `tests/*/*.csproj`.
- Vitest, Testing Library, jsdom, and V8 coverage test the Vite applications from each frontend `package.json`.
- Vite compiles both React applications (`helix-dashboard/vite.config.ts`, `helix-desktop/vite.config.ts`).
- Docusaurus invokes `docs-site/scripts/generate-openapi.js` before development and production builds.
- Dockerfiles package Agent, Recorder, and SiteServer from `Helix.Orchestration.Agent/Dockerfile`, `Helix.Orchestration.Recorder/Dockerfile`, and `Helix.Orchestration.SiteServer/Dockerfile`; `docker-compose.yml` is present for local multi-service deployment.

## Key Dependencies

- `Cleipnir.ResilientFunctions` 4.x and PostgreSQL persistence coordinate durable runspaces in `Helix.Orchestration.Agent/Helix.Orchestration.Agent.csproj` and `Helix.Orchestration.Core/Helix.Orchestration.Core.csproj`.
- `MQTTnet` 4.x transports live trend/event streams in Agent, Recorder, SiteServer, and Core projects.
- gRPC (`Grpc.AspNetCore`, `Grpc.Net.Client`, `Google.Protobuf`) connects SiteServer to Agent and generates contracts from `Helix.Orchestration.Contracts/`.
- `Npgsql` 9.x plus `Dapper` 2.x access PostgreSQL in `Helix.Orchestration.Infrastructure/`.
- `AWSSDK.S3` 4.0.23.4 implements S3-compatible/MinIO object storage in `Helix.Orchestration.Infrastructure/Storage/MinioObjectStorageSink.cs`.
- `OpenTelemetry` 1.x instruments the Agent runtime in `Helix.Orchestration.Agent/Program.cs` and `Helix.Orchestration.Agent/Telemetry/HelixTelemetry.cs`.
- Serilog hosting, compact formatting, console, and file sinks provide Agent/Recorder structured logging in their respective project files.
- `NATS.Net` 2.x is declared by `Helix.Orchestration.Core/Helix.Orchestration.Core.csproj`; no concrete NATS client use is detected.
- OPC UA, FluentModbus, `System.IO.Ports`, and TCP sockets connect laboratory hardware through `Helix.Orchestration.Devices/`.
- SciChart powers dashboard trend charts (`helix-dashboard/package.json`); React Flow, Allotment, rc-dock, and Recharts support the desktop UI (`helix-desktop/package.json`).

## Configuration

- .NET uses standard app configuration from `appsettings.json`, with development overrides where present under `Helix.Orchestration.Agent/`, `Helix.Orchestration.Recorder/`, and `Helix.Orchestration.SiteServer/`.
- Configure MQTT, service listener, PostgreSQL, object-storage, device transport, and Agent proxy options through their named configuration sections; option types reside in the owning service or infrastructure project.
- `.env` files are not detected by this analysis.
- Solution membership is defined in `Helix.Orchestration.slnx`.
- TypeScript/Vite settings live beside each frontend in `helix-dashboard/` and `helix-desktop/`; Tauri settings are in `helix-desktop/src-tauri/tauri.conf.json`.
- Documentation configuration is `docs-site/docusaurus.config.ts` and its OpenAPI source is `docs-site/static/openapi.json`.

## Platform Requirements

- Install .NET 10, Node.js 20+ and npm; install Rust/Cargo for desktop builds. Device-connected services additionally require reachable serial/TCP/Modbus/OPC UA equipment as configured under `Helix.Orchestration.Devices/`.
- Deploy Agent, Recorder, and SiteServer as .NET services or their provided containers. Supply connectivity to PostgreSQL, an MQTT broker, S3-compatible storage, and configured laboratory devices.

<!-- GSD:stack-end -->

<!-- GSD:conventions-start source:CONVENTIONS.md -->

## Conventions

## Naming Patterns

- Use PascalCase C# files whose name matches the primary type, such as `Helix.Orchestration.Core/WSL/WslCompiler.cs`, `Helix.Orchestration.Devices/Transports/Tcp/TcpDeviceTransport.cs`, and `tests/Helix.Orchestration.Core.Tests/WSL/WslCompilerTests.cs`.
- Keep TypeScript/React implementation files in PascalCase for components and pages (`helix-dashboard/src/pages/CampaignsPage.tsx`), and camelCase for hooks and libraries (`helix-dashboard/src/hooks/useTrendWebSocket.ts`, `helix-desktop/src/lib/workflowLibrary.ts`). Co-locate frontend tests with the target as `*.test.ts(x)`.
- Use PascalCase for public C# methods and properties and camelCase for local variables, parameters, and private fields. Async C# methods end in `Async`, for example `TcpDeviceTransport.ConnectAsync` in `Helix.Orchestration.Devices/Transports/Tcp/TcpDeviceTransport.cs`.
- Use camelCase function names and `useX` names for React hooks, for example `useTrendWebSocket` in `helix-dashboard/src/hooks/useTrendWebSocket.ts`.
- Use `_camelCase` for private C# instance fields, such as `_messageChannel` and `_logger` in `Helix.Orchestration.Devices/Transports/Tcp/TcpDeviceTransport.cs`.
- Use descriptive camelCase in TypeScript, such as `baseUrl`, `trendValues`, and `connected` in `helix-dashboard/src/App.tsx`.
- Use PascalCase for C# classes, interfaces, records, enums, and DTOs. Prefix C# interfaces with `I`, such as `IWslCompiler` and `IDeviceTransport`.
- Use PascalCase TypeScript component and type/interface names; keep props and hook return members camelCase.

## Code Style

- C# uses four-space indentation, file-scoped namespaces, and braces on new lines. Follow the layout in `Helix.Orchestration.Core/WSL/WslCompiler.cs`.
- Prefer modern C# collection expressions (`[]`), target-typed construction (`new()`), records/`with` expressions, pattern matching, and nullable reference types as configured in `tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj`.
- TypeScript formatting differs between apps: dashboard source imports use double quotes and semicolons in `helix-dashboard/src/App.tsx`; its ESLint config uses no semicolons in `helix-dashboard/eslint.config.js`. Preserve the conventions of the edited subtree rather than reformatting unrelated files.
- Run `npm run lint` in `helix-dashboard`; `helix-dashboard/eslint.config.js` applies ESLint recommended, TypeScript recommended, React Hooks, and Vite React Refresh rules to `*.ts` and `*.tsx`, excluding `dist`.
- Run `dotnet format --verify-no-changes` for C# formatting validation; `.github/workflows/ci.yml` enforces it as documented by `docs/CI.md`.
- CI builds C# with warnings as errors; avoid new compiler warnings and honor nullable annotations.

## Import Organization

- Not detected in application imports. Use relative paths such as `./components/Layout` in `helix-dashboard/src/App.tsx`.

## Error Handling

- Guard constructor dependencies with `ArgumentNullException`, as in `TcpDeviceTransport` at `Helix.Orchestration.Devices/Transports/Tcp/TcpDeviceTransport.cs`.
- Validate domain input before work and return aggregate diagnostics for compiler-style validation; `WslCompiler.Compile` in `Helix.Orchestration.Core/WSL/WslCompiler.cs` accumulates `CompilationDiagnostic` values and returns a failed result for errors.
- For transport boundaries, catch specific operational exceptions, log contextual details, update state, and throw a domain exception that preserves the inner exception; use the `TransportFaultException` pattern in `TcpDeviceTransport.SendReceiveAsync`.
- Accept `CancellationToken` on I/O and long-running public async APIs, propagate it to framework calls, and do not swallow unexpected exceptions. Intentional cleanup exceptions are narrowly caught in `DisconnectAsync`.
- In React, represent failed remote state in component/hook state and render user-visible error UI; use the fetch-response checks exercised by `helix-dashboard/src/pages/CampaignsPage.test.tsx`.

## Logging

- Inject `ILogger<T>` through constructors, keep it in a readonly `_logger` field, and use structured message templates with named values, as in `Helix.Orchestration.Devices/Transports/Tcp/TcpDeviceTransport.cs`.
- Use `LogInformation` for lifecycle/state transitions, `LogDebug`/`LogTrace` for transport detail, `LogWarning` for recoverable conditions, and `LogError(exception, ...)` before rethrowing boundary faults.

## Comments

- Comment non-obvious domain rules, concurrency behavior, protocol framing, and security/threat mitigations. `Helix.Orchestration.Core/WSL/WslCompiler.cs` labels its validation/transformation phases and explains loop-limit threats.
- Use Arrange/Act/Assert comments only where a test setup or assertion needs clarification, as in `tests/Helix.Orchestration.Core.Tests/WSL/WslCompilerTests.cs`.
- Use XML documentation for public C# types and members when behavior is externally meaningful; use `<summary>`, `<see>`, and `<inheritdoc/>` as in `Helix.Orchestration.Devices/Transports/Tcp/TcpDeviceTransport.cs`.
- JSDoc/TSDoc is not a prevalent frontend convention; use clear names and small components/hooks instead.

## Function Design

- Extract focused private helpers for validation, state transitions, and framing modes. `WslCompiler` separates validation methods and graph transforms; `TcpDeviceTransport` separates framing reads and `SetStatus`.
- Use expression-bodied members for simple forwarding/read-only members and block bodies for observable work.
- Prefer typed options, interfaces, and request/record types over unstructured arguments. Use optional `CancellationToken cancellationToken = default` for cancellable async public APIs.
- Return `Task`/`Task<T>` for asynchronous operations; return typed result/diagnostic objects for expected validation failures, and throw typed exceptions for unrecoverable transport failures.
- Use nullable return types deliberately for missing domain records, as in the repositories replaced by `tests/Helix.Orchestration.SiteServer.Tests/HelixWebApplicationFactory.cs`.

## Module Design

- Keep C# types in feature namespaces matching their folders, e.g. `Helix.Orchestration.Core.WSL` in `Helix.Orchestration.Core/WSL/WslCompiler.cs`.
- Export React components as named exports in dashboard code (`helix-dashboard/src/App.tsx`) and use the existing default-export component convention in desktop entry files (`helix-desktop/src/App.tsx`). Preserve the local convention.
- Not detected. Import concrete files directly rather than adding barrels.

<!-- GSD:conventions-end -->

<!-- GSD:architecture-start source:ARCHITECTURE.md -->

## Architecture

## System Overview

```text

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

- Keep domain contracts and interfaces in `Helix.Orchestration.Core`; bind concrete infrastructure in host `Program.cs` files or service-registration extensions.
- Cross-tier experiment control goes through `IAgentProxy`; select `GrpcAgentProxy` or `SimAgentProxy` during Site Server registration in `Helix.Orchestration.SiteServer/Agent/AgentProxyServiceExtensions.cs`.
- Device workflows invoke logical, namespaced capabilities through `ICapabilityInvoker`/`CapabilityRegistry`, not hardware protocols directly.
- Use protobuf for Agent control-plane gRPC (`Helix.Orchestration.Contracts/Protos/agent_control.proto`) and MQTT for streaming trend events.

## Layers

- Purpose: provide browser dashboard and installed desktop user interfaces.
- Location: `helix-dashboard/src/` and `helix-desktop/src/`.
- Contains: React routes/pages, hooks, visual components, workflow builder, desktop shell.
- Depends on: Site Server REST/WebSocket endpoints and Agent REST/WebSocket endpoints.
- Used by: laboratory operators.
- Purpose: expose HTTP/gRPC transport boundaries and compose runtime dependencies.
- Location: `Helix.Orchestration.Agent/`, `Helix.Orchestration.SiteServer/`, and `Helix.Orchestration.Recorder/`.
- Contains: minimal endpoint modules, hosted workers, proxies, event streaming, DI setup.
- Depends on: Core contracts plus Infrastructure, Devices, Simulation, and Contracts as needed.
- Used by: clients, Docker services, and system service definitions in `deploy/`.
- Purpose: define orchestration behavior independently of transport and hardware implementation.
- Location: `Helix.Orchestration.Core/`.
- Contains: WSL compiler, workflow engine, execution primitives, capability/safety/leasing/state/trend abstractions and models.
- Depends on: BCL and durable workflow library abstractions; should not take dependencies on host endpoint projects.
- Used by: Agent, Site Server, Simulation, and tests.
- Purpose: adapt device protocols and persistent services to Core-facing ports.
- Location: `Helix.Orchestration.Devices/`, `Helix.Orchestration.Infrastructure/`, and `Helix.Orchestration.Simulation/`.
- Contains: driver classes, serial/TCP/Modbus/OPC transports, PostgreSQL repositories, MinIO sink, and simulators.
- Depends on: external protocol/client packages and Core models/interfaces.
- Used by: application host DI composition.

## Data Flow

### Primary Experiment Submission Path

### Trend Streaming and Recording Path

### Campaign Path

- Agent-local process state uses singleton registries/services (`CapabilityRegistry`, `DeviceRegistry`, in-memory leasing/trends/global state) registered in `Helix.Orchestration.Agent/Program.cs`.
- Campaigns maintain an in-memory concurrent cache backed by PostgreSQL in `Helix.Orchestration.SiteServer/Campaigns/CampaignManager.cs`.
- `WorkflowExecutor` retains run status and cancellation token maps in concurrent dictionaries while Cleipnir's PostgreSQL function store provides durable-function backing (`Helix.Orchestration.Core/Execution/WorkflowExecutor.cs`).

## Key Abstractions

- Purpose: declarative graph format for authored workflows and compiler input.
- Examples: `Helix.Orchestration.Core/WSL/WslDocument.cs`, `WslNode.cs`, `WslEdge.cs`, `WslCompiler.cs`.
- Pattern: validate then transform a graph (safety injection, loop expansion, branch flattening, parallel fork/join) into `RunspaceDefinition`.
- Purpose: hide local simulation versus remote Agent control behind one Site Server dependency.
- Examples: `Helix.Orchestration.SiteServer/Agent/IAgentProxy.cs`, `GrpcAgentProxy.cs`, `SimAgentProxy.cs`.
- Pattern: port with production gRPC and development simulation adapters.
- Purpose: map a workflow capability key such as `device.Verb` to an invoker.
- Examples: `Helix.Orchestration.Core/Capabilities/CapabilityRegistry.cs`, `Helix.Orchestration.Agent/Adapters/Devices/HelixAdapterCapabilityBridge.cs`.
- Pattern: thread-safe registry and adapter bridge; device registration belongs in `DeviceAdapterExtensions.cs`.
- Purpose: represent, start, pause, resume, abort, and observe a submitted execution.
- Examples: `Helix.Orchestration.Core/Runspace/IRunspace.cs`, `RunspaceManager.cs`, `Helix.Orchestration.Agent/Runspaces/DurableRunspace.cs`.
- Pattern: runspace state machine protected by `SemaphoreSlim`; execution may be backed by `IWorkflowExecutor`/Cleipnir or the Agent's registered in-memory graph traversal factory.

## Entry Points

- Location: `Helix.Orchestration.Agent/Program.cs`.
- Triggers: Docker `helix-agent`, systemd `deploy/systemd/helix-agent.service`, or local `dotnet run`.
- Responsibilities: register device adapters and application services; expose REST endpoint groups, gRPC `AgentGrpcService`, `/health`, and `/ws`.
- Location: `Helix.Orchestration.SiteServer/Program.cs`.
- Triggers: Docker `helix-siteserver` or local `dotnet run`.
- Responsibilities: register proxy/compiler/campaign/MQTT services; map REST API groups and `/api/trends/ws`.
- Location: `Helix.Orchestration.Recorder/Program.cs`.
- Triggers: Docker `helix-recorder` or systemd `deploy/systemd/helix-recorder.service`.
- Responsibilities: configure bounded event channel, MQTT consumer, MinIO sink, and writer worker.
- Location: `helix-dashboard/src/main.tsx`.
- Triggers: Vite-built SPA load.
- Responsibilities: configure SciChart, React root, and router; routes in `helix-dashboard/src/App.tsx` call the Site Server.
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

### Putting composition into Core

## Error Handling

- `CapabilityRegistry.InvokeAsync` returns `CapabilityInvocationResult.Failed` for unregistered or throwing invokers (`Helix.Orchestration.Core/Capabilities/CapabilityRegistry.cs:43-60`).
- gRPC endpoint methods translate failures to acknowledgement payloads (`Helix.Orchestration.Agent/gRPC/Server/AgentGrpcService.cs`).
- Runspace execution transitions to faulted/suspended statuses on exception/cancellation (`Helix.Orchestration.Agent/Runspaces/DurableRunspace.cs`).

## Cross-Cutting Concerns

<!-- GSD:architecture-end -->

<!-- GSD:skills-start source:skills/ -->

## Project Skills

No project skills found. Add skills to any of: `.claude/skills/`, `.agents/skills/`, `.cursor/skills/`, `.github/skills/`, or `.codex/skills/` with a `SKILL.md` index file.
<!-- GSD:skills-end -->

<!-- GSD:workflow-start source:GSD defaults -->

## GSD Workflow Enforcement

Before using Edit, Write, or other file-changing tools, start work through a GSD command so planning artifacts and execution context stay in sync.

Use these entry points:
- `/gsd-fast` for a trivial task inline, with no subagents and no PLAN.md
- `/gsd-quick` for small fixes, doc updates, and ad-hoc tasks
- `/gsd-debug` for investigation and bug fixing
- `/gsd-execute-phase` for planned phase work

Do not make direct repo edits outside a GSD workflow unless the user explicitly asks to bypass it.
<!-- GSD:workflow-end -->

<!-- GSD:profile-start -->

## Developer Profile

> Profile not yet configured. Run `/gsd-profile-user` to generate your developer profile.
> This section is managed by `generate-claude-profile` -- do not edit manually.
<!-- GSD:profile-end -->
