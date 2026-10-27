# Evidence — codebase

Repo root: C:\Users\u643664\source\repos\CSharpGitLab\Helix
Scanned: 2026-10-27
Checks run: `git rev-parse HEAD` → `d3c412e82781662b971a81fa35782e3ab4571adb` (recorded base); `git status --short` → assigned report was already modified in the sidecar; `git log -8 --oneline --name-only` → `d3c412e` Merge remote-tracking branch 'origin/dev'; `25fb557` chore: removal of gsd-old; `08b0f66` chore: remove stale Claude worktrees; `5a3bc00` docs(state): record inspect handoff artifacts; `2703aef` chore(repo): ignore local agent tool state; `1d2458b` fix(docs): transcode four Markdown files to UTF-8; `53996bb` docs(state): record phase 13 context session; `02da9ab` docs(13): capture phase context. Static inspection only, no build or tests run.

## Map

- **Stack**: Backend is C#/.NET 10 (`net10.0`) across eight projects in `Helix.Orchestration.slnx`; ASP.NET Core Minimal APIs/hosted workers, gRPC + Protobuf, MQTT, PostgreSQL (Npgsql/Dapper and Cleipnir durable functions), and device adapters/transports (OPC UA, Modbus, serial/TCP). Two separate TypeScript/React/Vite clients are present: `helix-dashboard` (React 19, TypeScript 6, Vite 8) and `helix-desktop` (React 18, TypeScript 5.5, Tauri 2). `docs-site` uses Docusaurus 3.10.1. Sources: solution lines 10–17; representative .NET project target/package references in `Helix.Orchestration.SiteServer/Helix.Orchestration.SiteServer.csproj:1-21`, `Helix.Orchestration.Agent/Helix.Orchestration.Agent.csproj:1-30`, and `Helix.Orchestration.Devices/Helix.Orchestration.Devices.csproj:1-24`; frontend manifests `helix-dashboard/package.json:1-40`, `helix-desktop/package.json:1-36`, `docs-site/package.json:1-35`.
- **Entry points**: `Helix.Orchestration.SiteServer/Program.cs` starts the control-plane web app and maps experiment, device, trend, campaign, and fumehood APIs; `Helix.Orchestration.Agent/Program.cs` starts the agent host and wires execution/device services; `Helix.Orchestration.Recorder/Program.cs` starts the MQTT recording worker. Frontend entry modules are `helix-dashboard/src/main.tsx` and `helix-desktop/src/main.tsx`; `helix-desktop` also has a Tauri shell. Sources: those `Program.cs` files; `Helix.Orchestration.SiteServer/Program.cs:96-106`; package scripts in both frontend manifests.
- **Architecture**: The SiteServer exposes HTTP APIs, compiles submitted WSL JSON through `IWslCompiler`, and sends compiled definitions through an `IAgentProxy` that selects a gRPC or simulated implementation. The Agent wires workflow/runspace execution, safety, leases, trends, capability invocation, and device adapters; shared Protobuf contracts describe gRPC interfaces, while Core, Devices, and Infrastructure separate domain/runtime, hardware access, and persistence/storage. SiteServer bridges trend observables to MQTT and WebSockets; the separate Recorder consumes MQTT into a bounded in-process channel and batches events to object storage. The two frontends are separate applications rather than a single shared client. Sources: `Helix.Orchestration.SiteServer/Program.cs:26-63,73-106`, `Helix.Orchestration.SiteServer/Endpoints/ExperimentEndpoints.cs:53-82`, `Helix.Orchestration.SiteServer/Agent/AgentProxyServiceExtensions.cs:7-19`, `Helix.Orchestration.Agent/Program.cs:62-99`, `Helix.Orchestration.Contracts/Helix.Orchestration.Contracts.csproj:20-23`, `Helix.Orchestration.Recorder/Program.cs:23-43`.
- **Conventions**: C# namespaces/folders follow project and feature areas (`Endpoints`, `Campaigns`, `Persistence`, `Transports`); asynchronous APIs use `Async` suffixes and accept cancellation tokens. Endpoint handlers generally return `IResult` and convert caught exceptions into problem responses; capability dispatch logs and returns failed result objects for exceptions. Tests are grouped by project/feature and use xUnit `*Tests` classes; web integration tests use `WebApplicationFactory`. Recent commit subjects use conventional prefixes such as `fix(docs):`, `docs(state):`, and `chore(repo):`. Sources: `Helix.Orchestration.SiteServer/Endpoints/ExperimentEndpoints.cs:21-51`; `Helix.Orchestration.Devices/Abstractions/DeviceAdapterBase.cs:51-76`; `tests/Helix.Orchestration.SiteServer.Tests/Helix.Orchestration.SiteServer.Tests.csproj`; `git log -8 --oneline --name-only`.
- **Maturity**: This is a multi-project product codebase with six .NET test projects, tests for core execution/WSL, gRPC and endpoint behavior, recorder batching, persistence, transports, and many device drivers; both React clients also have Vitest tests. CI declares .NET restore/build/test, dashboard test/build, desktop tests and cross-platform Tauri builds, plus docs build (`.github/workflows/ci.yml:44-158,181-266`). These are checked-in tests and CI definitions, not a result from this scan: no build or tests were run here. The simulation layer is registered in SiteServer and provides simulated reactor/pump/spectrometer services (`Helix.Orchestration.SiteServer/Program.cs:26-30`; `Helix.Orchestration.Simulation/SimulationServiceExtensions.cs:17-35`).
- **Recent activity**: The latest visible non-merge commits in `git log -8 --oneline --name-only` focus on repository/planning/agent-state cleanup and documentation (`25fb557` removes extensive `.planning`/`.claude` material; `08b0f66` removes stale Claude worktree references; `5a3bc00` records inspection artifacts; `2703aef` updates `.gitignore`; `1d2458b` transcodes Markdown). Product code does not dominate this recent slice.

## Finding: The repository contains two independent frontends with different stacks

- **Claim**: `helix-dashboard` and `helix-desktop` are distinct React applications with separate dependency versions, entry points, and CI jobs—not merely two folders in one frontend build.
- **Source**: `helix-dashboard/package.json:1-40`; `helix-desktop/package.json:1-36`; `helix-dashboard/src/main.tsx`; `helix-desktop/src/main.tsx`; `.github/workflows/ci.yml:91-139,181-266`.
- **Confidence**: high
- **Why it matters here**: A change scoped to one client may not change the other; planning must identify the intended client and account for different React/TypeScript toolchains and desktop packaging.

## Finding: Durable execution is mixed with explicitly in-memory runtime state

- **Claim**: The Agent configures a PostgreSQL-backed Cleipnir function store, but registers device leases, global state, and trend provider as in-memory services in the same startup path.
- **Source**: `Helix.Orchestration.Agent/Program.cs:70-88`.
- **Confidence**: high
- **Why it matters here**: “Durable” workflow execution does not establish that all associated state survives restart; inspection/planning should distinguish function persistence from leases, global workflow state, and trend retention.

## Finding: Recorder buffering can discard old events without the shown warning path

- **Claim**: The Recorder creates a 10,000-item channel with `BoundedChannelFullMode.DropOldest`, while its consumer logs only when `TryWrite` returns false; that check does not identify items evicted by the configured drop-oldest policy.
- **Source**: `Helix.Orchestration.Recorder/Program.cs:23-28`; `Helix.Orchestration.Recorder/Services/MqttRecorderService.cs:42-46`.
- **Confidence**: high
- **Why it matters here**: Data-loss guarantees and monitoring cannot be inferred from the warning branch alone; requirements for event loss, backpressure, and durable buffering should be settled explicitly.

## Apparent intent (inference)

- The code appears intended to provide a lab-automation control plane: author/submit workflows, execute them through an agent against simulated or physical devices, and expose experiments, campaigns, and live trends. This is an inference from SiteServer submission/endpoints, Agent execution/device registrations, and simulation and trend bridges (`Helix.Orchestration.SiteServer/Endpoints/ExperimentEndpoints.cs:53-82`; `Helix.Orchestration.Agent/Program.cs:62-99`; `Helix.Orchestration.SiteServer/Program.cs:26-59`).
- The repository also appears to target both a web dashboard and a native desktop operator workspace; this is an inference from the separate app manifests and Tauri CI build (`helix-dashboard/package.json:1-20`; `helix-desktop/package.json:1-25`; `.github/workflows/ci.yml:181-266`).

## Open questions for define

- Which client is the intended primary operator interface for new user-facing work: `helix-dashboard`, `helix-desktop`, or both?
- Which runtime state must survive Agent restart (leases, global workflow state, trends), and what durability/loss guarantees apply to recorded MQTT events?
- Is the SiteServer simulation path intended as a development/test default only, or a supported deployment mode alongside gRPC Agent operation?

## Blocked areas

- Generated/dependency/build outputs, `.git`, and `.project/archive` were intentionally excluded and not inspected. No other areas were blocked.
