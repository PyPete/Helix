# Roadmap

**Project:** Helix Orchestration Platform
**Milestone:** 1 -- Hardware-ready platform with local UI, full test coverage, documentation, and deployment scripts
**Approach:** Unblock first (Phase 1 resolves all blocking technical debt); prove the device integration architecture end-to-end with VapourtecR4 (Phase 2) before scaling to remaining drivers (Phase 3); weave basic unit tests into each implementation phase; run UI work (Phases 5-6) in parallel once the execution path is proven; close with docs and deployment (Phases 8-10).
**Updated:** 2026-05-26

---

## Phase Overview

| # | Phase | Goal | Depends On | Est. Size |
|---|-------|------|------------|-----------|
| 1 | Technical Debt Resolution | Make the solution build, run, and execute durably | -- | L |
| 2 | VapourtecR4 Reference Driver | Prove the DeviceAdapterBase architecture end-to-end with one real driver | 1 | M |
| 3 | Remaining Device Drivers | Port KnauerAzura, SystagOpcUaClient, and all audited drivers | 2 | M |
| 4 | Transport + Execution Integration Tests | Verify transport layer and E2E workflow execution without real hardware | 1, 2, 3 | M |
| 5 | Tauri Desktop App - Foundation | Scaffold the cross-platform fumehood UI and wire it to Agent | 2 | M |
| 6 | Tauri Desktop App - Workflow Builder + Execution | Agent REST API + WebSocket layer, WSL compiler completions, Rete.js node editor, execution panel, live trends, safety display | 5 | XL |
| 7 | Site-Level React Dashboard | Vite + TypeScript SPA with site overview, campaigns, history, and live trends | 2 | M |
| 8 | CI Hardening + Full Test Coverage | All projects build and test in CI; coverage report; no skipped tests | 1, 4, 6, 7 | M |
| 9 | Docusaurus Documentation | Scaffold and populate developer + scientist docs; auto-generate API reference | 7, 8 | M |
| 10 | Deployment + Setup Scripts | Docker Compose full-stack, bootstrap scripts, service installers, pre-flight check | 8, 9 | S |

---

## Phase 1: Technical Debt Resolution

**Goal:** The Helix solution builds in all configurations, the Recorder ships a real MQTT pipeline, runspace state survives process restart, and every critical DI gap (CapabilityRegistry, repositories, OPC UA library, deprecated API) is closed. Nothing downstream can be trusted until these eight issues are resolved.

**Deliverables:**
- Helix.Orchestration/Helix.Orchestration.Recorder/ contains the full MqttRecorderService + RecorderWriterService + RecordedEvent + RecorderOptions pipeline (root-level implementation merged in, stub Worker.cs replaced)
- Single canonical IObjectStorageSink interface (WriteAsync(key, stream, contentType, ct)) with one Minio SDK client across Recorder and Infrastructure; divergent root-level copy removed
- DeviceAdapterExtensions.AddMigratedDeviceAdapters() populates CapabilityRegistry (even with placeholder registrations demonstrating the wiring); capability invocations no longer dispatch into a void
- Helix.Orchestration.Agent/Program.cs uses the Cleipnir PostgreSQL function store; InMemoryFunctionStore removed
- Helix.Orchestration.Agent.csproj no longer references Opc.UaFx.Advanced; OpcTransport.cs migrated to use OpcDeviceTransport / OpcFoundationSession from the Devices project
- FormatterServices.GetUninitializedObject replaced with Activator.CreateInstance<T>() in DevicesServiceCollectionExtensions.cs
- Helix.Orchestration.SiteServer/Program.cs calls AddHelixInfrastructure; IExperimentRepository and ICampaignRepository are live in the DI container
- Helix.Orchestration.slnx build configuration corrected -- Agent, Recorder, Infrastructure, Devices, Simulation, Contracts all build in Debug configuration
- docker-compose.yml (root) consolidated -- single file with Mosquitto + PostgreSQL + MinIO; solution-internal duplicate reconciled

**Requirements covered:** A1, A2, A3, A4, A5, A6, A7, A8

**Dependencies:** None (this is the prerequisite for everything)

**Success criteria:**
- [ ] `dotnet build Helix.Orchestration/Helix.Orchestration.slnx` exits 0 in Debug configuration with all projects included
- [ ] `dotnet run --project Helix.Orchestration/Helix.Orchestration.Recorder` starts without exception and logs MQTT connection attempts (not a 30-second delay loop)
- [ ] `dotnet run --project Helix.Orchestration/Helix.Orchestration.Agent` starts without exception; CapabilityRegistry logs at least one registered entry (or explicit diagnostic, not a silent void)
- [ ] Agent process can be stopped and restarted; Cleipnir confirms the PostgreSQL store is used (log line or observable table rows in docker-compose Postgres)
- [ ] `grep -r "Opc.UaFx" Helix.Orchestration/` returns no matches (commercial OPC UA package fully removed)
- [ ] SiteServer /api/campaigns and /api/experiments endpoints return JSON (not 500) when backed by a running Postgres instance
- [ ] All eight issues listed in CONCERNS.md "Recommended Immediate Actions" are resolved

---

## Phase 2: VapourtecR4 Reference Driver

**Goal:** One complete, tested device driver implementing the new DeviceAdapterBase composition model exists in the solution tree. VapourtecR4 is chosen because it exercises serial transport, a non-trivial command vocabulary, status polling, and fault detection -- proving the full device integration path end-to-end before other drivers are written.

**Deliverables:**
- Driver audit document: inventory of all drivers in POP.DeviceCore/Drivers (protocol, command vocabulary, connection type, test status) -- feeds Phase 3 scope
- ISerialTransport.Open signature updated to accept baudRate parameter; all implementing classes updated
- DI lifetime mismatch resolved: transports registered as Singleton to match CapabilityRegistry Singleton lifetime; hot-reconnect scenario documented
- VapourtecR4.cs implementing DeviceAdapterBase with full POP command vocabulary (FW, KP, GA, HF, HT, HP, GF, GT), GetStatus() returning VapourtecStatus with fault detection, GetMeasurements() results routed into ITrendProvider (resolves TrendAggregator silent discard)
- DeviceAdapterExtensions registers VapourtecR4 into CapabilityRegistry with correct capability verb names
- VapourtecR4Tests.cs covering command byte sequences, status parsing for all known response formats including fault codes, and GetMeasurements output

**Requirements covered:** B1, B2, B6, B7, B8 (VapourtecR4)

**Dependencies:** Phase 1

**Success criteria:**
- [ ] `dotnet build` includes Helix.Orchestration.Devices.Tests and all VapourtecR4Tests pass with no [Fact(Skip)]
- [ ] A smoke test with a simulated serial transport shows VapourtecR4 sending the FW query without exception
- [ ] CapabilityRegistry at Agent startup logs VapourtecR4 capabilities as registered (observable in agent log output)
- [ ] TrendAggregator no longer silently discards scalar TrendValue entries for VapourtecR4 measurements
- [ ] Driver audit document is committed to Helix.Orchestration/docs/ listing every driver found in POP.DeviceCore

---

## Phase 3: Remaining Device Drivers

**Goal:** All drivers identified in the Phase 2 audit are ported to DeviceAdapterBase. KnauerAzura and SystagOpcUaClient are the named priorities; additional drivers from the audit complete the set. Every driver has unit tests using transport test seams.

**Deliverables:**
- KnauerAzura.cs implementing DeviceAdapterBase: correct command vocabulary matching POP.DeviceCore, ACK/NAK response handling, gradient and UV configuration commands, run state polling
- SystagOpcUaClient.cs implementing DeviceAdapterBase: setpoint write methods via OPC Foundation library, correct OPC UA node path conventions, change subscriptions for live value updates, AutoAcceptUntrustedCertificates removed or made configurable (not hardcoded true)
- All remaining drivers from the B1 audit implemented with the same composition pattern
- DeviceAdapterExtensions registers all new drivers into CapabilityRegistry
- Unit tests for each new driver in Helix.Orchestration.Devices.Tests

**Requirements covered:** B3, B4, B5, B8 (KnauerAzura, Systag, remaining drivers)

**Dependencies:** Phase 2 (architecture proven, audit complete, ISerialTransport fixed, DI lifetime fixed)

**Success criteria:**
- [ ] All driver unit tests pass in CI with no skips
- [ ] CapabilityRegistry at Agent startup logs capability registrations for KnauerAzura, SystagOpcUaClient, and all audited drivers
- [ ] OpcDeviceTransport.cs no longer sets AutoAcceptUntrustedCertificates = true unconditionally
- [ ] A full capability invocation round-trip (WSL step to CapabilityRegistry to driver method to transport mock to result) is demonstrable for at least one capability on each new driver
- [ ] All B-series requirements are closed; REQUIREMENTS.md B5 lists every driver by name with its status

---

## Phase 4: Transport + Execution Integration Tests

**Goal:** The transport layer and the full WSL-to-trend execution path are covered by automated tests that run without real hardware. PostgreSQL repository tests run against a Docker container with no [Skip] tags. E2E workflow execution is verifiable in CI.

**Deliverables:**
- Transport integration tests in Helix.Orchestration.Devices.Tests:
  - ModbusTransport test with FluentModbus server simulator
  - SerialTransport test with loopback mock
  - OpcDeviceTransport test with mock OpcFoundationSession
  - TcpTransport test with in-process TCP server
- E2E workflow execution test: submit WSL document -> DurableRunspace executes steps -> trend output captured -> ExperimentContext reaches Completed state; uses SimulatedTrendProvider and transport mocks (no real devices)
- Helix.Orchestration.Infrastructure.Tests: [Fact(Skip="Requires Docker.")] removed; Testcontainers.PostgreSql spins up a real container; all repository tests run
- InMemoryTrendProvider stub paths documented (NotSupportedException cases identified as known gaps or fixed)

**Requirements covered:** D2, D3, D5

**Dependencies:** Phase 1 (build fixed), Phase 2 (VapourtecR4 transport seam established as pattern), Phase 3 (all drivers available for transport test coverage)

**Success criteria:**
- [ ] All transport integration tests pass in CI (dotnet test includes Helix.Orchestration.Devices.Tests)
- [ ] E2E test: a workflow containing at least one SetTemperature capability step completes successfully via SimulatedTrendProvider and SimReactor
- [ ] Helix.Orchestration.Infrastructure.Tests runs all repository tests (not skipped) and all pass against a Testcontainers PostgreSQL instance
- [ ] CI pipeline dotnet test includes Helix.Orchestration.Devices.Tests, Helix.Orchestration.Infrastructure.Tests, and Helix.Orchestration.Recorder.Tests without build=false entries in slnx

---

## Phase 5: Tauri Desktop App - Foundation

**Goal:** The Tauri desktop application scaffolds and runs on Windows 11 and Linux, connects directly to the Agent REST/gRPC in standalone mode, and displays live device status and adapter health without requiring a Site Server.

**Deliverables:**
- Tauri v2 project at helix-desktop/ targeting Windows 11 + Linux (Debian/Ubuntu)
- React + TypeScript frontend (Vite) inside Tauri shell
- Agent connection configuration screen (host, port, gRPC/REST toggle)
- Standalone mode: app connects directly to Agent REST/gRPC; no Site Server required
- Device registry view: list of registered adapters, connection status, health check results (polls Agent /health and device status endpoint)
- Basic CI build step for Tauri on ubuntu-latest

**Requirements covered:** C1, C7, C8

**Dependencies:** Phase 2 (Agent has at least one real driver registered; device status is meaningful)

**Success criteria:**
- [ ] `npm run tauri dev` starts the desktop app on Windows 11 without error
- [ ] `npm run tauri build` produces a Linux .AppImage or .deb package (or CI runner confirms a successful Linux build)
- [ ] With Agent running locally, the app displays at least one registered device adapter with its health status
- [ ] The app loads and shows a graceful "no Site Server" state when no Site Server is reachable
- [ ] Standalone mode verified: disconnect from Site Server, app continues to show Agent device status without error

**UI hint**: yes

---

## Phase 6: Tauri Desktop App - Workflow Builder + Execution

**Goal:** A scientist can visually build a workflow, submit it for compilation and execution, monitor live telemetry, and see safety interlock status -- all within the desktop app connected directly to the Agent. The Agent gains its full REST control API, WebSocket streaming layer, and the remaining WSL compiler features required to make this loop work end-to-end.

**Deliverables:**

Agent-side (Helix.Orchestration.Agent):
- REST control API endpoints wired in Program.cs:
  - POST /workflows/compile -- accepts WSL JSON, returns RunspaceDefinition or diagnostics
  - POST /runspaces -- submits a RunspaceDefinition, returns runspace ID
  - GET  /runspaces/{id} -- returns runspace status and step-level detail
  - POST /runspaces/{id}/pause
  - POST /runspaces/{id}/resume
  - POST /runspaces/{id}/abort
  - GET  /devices -- returns registered adapters from CapabilityRegistry with health status
  - GET  /devices/{id}/capabilities -- returns capability list for one adapter
- WebSocket endpoint at /ws -- streams RunspaceEvents, SafetyEvents, TrendValues, and VariableUpdates as JSON frames; one connection per client
- EventEmitter: IRunspaceEventEmitter interface + implementation; publishes step-started, step-completed, step-faulted, runspace-started, runspace-completed, runspace-aborted events; consumed by WebSocket broadcast and optionally by Recorder
- WslCompiler completions (currently stubbed):
  - Safety envelope injection: safety interlock rules from ISafetyEngine inserted as guard nodes at compile time
  - Loop expansion: Loop nodes unrolled into repeated linear node sequences in RunspaceDefinition
  - Branch and parallel block flattening: Branch/Parallel WSL nodes resolved into conditional edge sets in the execution graph
- AgentService.cs promoted from stub to real hosted service: subscribes to EventEmitter, forwards events to WebSocket clients, triggers SafetyEngine evaluation on each trend update

Tauri desktop app (helix-desktop):
- Rete.js workflow builder embedded in the Tauri app:
  - Node types: Step, Condition, Branch, Loop, Parallel, Delay, WaitFor
  - Device capability picker: calls GET /devices and GET /devices/{id}/capabilities; node inputs bound to capability parameters
  - Edge wiring generates valid WSL JSON (round-trippable through POST /workflows/compile)
- Workflow library: save, load, duplicate workflow definitions to local filesystem via Tauri fs plugin
- Experiment execution panel: calls POST /runspaces, POST /runspaces/{id}/pause|resume|abort; displays step-level status from WebSocket RunspaceEvents
- Live trend charts: scalar trends (temperature, pressure, flow), spectral chart (UV/MIR overlay); data from WebSocket TrendValue frames
- Safety status panel: interlock rule list, current values per rule, triggered / safe state; data from WebSocket SafetyEvents
- UI component tests (Vitest + Testing Library): node creation, edge wiring, WSL JSON export, execution panel state transitions

**Requirements covered:** C2, C3, C4, C5, C6, C9, D4

**Dependencies:** Phase 5 (Tauri foundation, Agent connection, device registry view)

**Success criteria:**
- [ ] POST /workflows/compile with a valid WSL JSON document returns a RunspaceDefinition (HTTP 200); an invalid document returns diagnostics (HTTP 422)
- [ ] POST /runspaces starts execution and GET /runspaces/{id} returns status=Running while the runspace is active
- [ ] WebSocket /ws delivers at least one RunspaceEvent and one TrendValue frame during a live execution
- [ ] A scientist can drag two nodes onto the canvas, wire them together, and export WSL JSON that passes POST /workflows/compile
- [ ] A workflow can be submitted from the desktop app and step-level execution status updates in real time (at least one step visible as running then completed)
- [ ] Live scalar trend charts update during experiment execution (temperature or flow values change visibly)
- [ ] Safety panel shows at least one interlock rule with its current value and safe/triggered state
- [ ] Workflow save/load round-trip works: save a workflow, reload the app, re-open -- all nodes and edges present
- [ ] UI component tests pass in CI

**Plans:** 8 plans

Plans:
- [x] 06-01-PLAN.md -- Agent REST Control API (POST /workflows/compile, POST/GET /runspaces, pause/resume/abort, GET /devices)
- [ ] 06-02-PLAN.md -- Agent WebSocket + IRunspaceEventEmitter (AgentService promotion, /ws endpoint)
- [ ] 06-03-PLAN.md -- WslCompiler Completions (safety envelope, loop expansion, branch/parallel flattening)
- [x] 06-04-PLAN.md -- Rete.js Workflow Builder + useAgentWebSocket hook (7 node types, WSL export, tests)
- [ ] 06-05-PLAN.md -- Experiment Execution Panel (start/pause/resume/abort, step status display)
- [ ] 06-06-PLAN.md -- Live Trend Charts + Safety Panel (recharts ScalarTrendChart/SpectralChart, SafetyPanel)
- [ ] 06-07-PLAN.md -- Workflow Library (save/load/duplicate via Tauri fs plugin)
- [ ] 06-08-PLAN.md -- Integration + Full Suite Green (App.tsx complete wiring, all tests pass)

**UI hint**: yes

---

## Phase 7: Site-Level React Dashboard

**Goal:** A site manager can open a web browser, see all fumehoods and their status, manage campaigns, browse experiment history, and watch live trend charts -- without installing any desktop software.

**Deliverables:**
- React + TypeScript SPA at helix-dashboard/ (Vite), deployable as static files served from Site Server or CDN
- Site overview page: fumehood list (idle / running / faulted), active experiment count, last-seen timestamp
- Campaign management page: create, start, monitor per-iteration status, abort campaigns
- Experiment history browser: filter by fumehood / device / date range; view result summaries
- Live trend visualisation: WebSocket or SignalR bridge from Site Server MQTT bridge; scalar trend charts update in real time
- Analytical data viewer: spectrum chart (UV/MIR overlay), DAD chromatogram 2D view
- UI component tests (Vitest + Testing Library) for key pages

**Requirements covered:** E1, E2, E3, E4, E5, E6

**Dependencies:** Phase 2 (Site Server has working campaign and experiment endpoints; Infrastructure repositories wired in Phase 1)

**Success criteria:**
- [ ] React app builds (`npm run build`) and serves from dist/ without 404 errors on any route
- [ ] Site overview page loads and shows at least one fumehood entry (or empty state) when connected to a running Site Server
- [ ] A campaign can be created, started, and aborted through the UI without triggering a 5xx response
- [ ] Experiment history browser shows paginated results filtered by date when the Site Server has Postgres data
- [ ] A live scalar trend chart value updates at least once per second during an active experiment (WebSocket/SignalR connection confirmed)
- [ ] UI component tests pass in CI

**UI hint**: yes

---

## Phase 8: CI Hardening + Full Test Coverage

**Goal:** Every test project builds and runs in CI with no skipped tests, a coverage report is generated, and the CI pipeline is a reliable gatekeeper for all future work.

**Deliverables:**
- .github/workflows/ci.yml updated:
  - `dotnet test` targets Helix.Orchestration.slnx (all projects) with no build=false exclusions for test projects
  - Testcontainers PostgreSQL used for Infrastructure tests (Docker-in-Docker or service container in GitHub Actions)
  - Tauri app build step (at minimum a Linux cargo build check)
  - React dashboard build step (`npm ci && npm run build`)
- Unit tests for all device adapters (D1): coverage of capability dispatch, DI registration, and adapter-level behaviour for every driver from Phase 3
- coverlet.collector coverage report generated and uploaded as CI artifact (HTML or lcov)
- All [Fact(Skip)] and [Fact(Skip="Requires Docker.")] annotations removed from all test projects

**Requirements covered:** D1, D6, D7

**Dependencies:** Phase 4 (transport + infrastructure tests clean), Phase 6 (UI component tests exist), Phase 7 (dashboard UI tests exist)

**Success criteria:**
- [ ] CI pipeline runs all test projects and exits 0 (green) on a clean dev branch push
- [ ] No [Skip] annotations remain in any test file (grep confirms)
- [ ] Coverage report artifact is produced for every CI run and shows total line coverage
- [ ] Helix.Orchestration.Recorder.Tests has at least one passing test (MQTT parse or JSONL write logic)
- [ ] A deliberate test failure on dev causes the CI pipeline to fail (regression guard confirmed)

---

## Phase 9: Docusaurus Documentation

**Goal:** A developer can read the Docusaurus site to understand the architecture and write a new device adapter. A scientist can read it to author workflows and understand device capabilities. No requirement to read 14 phase handover docs.

**Deliverables:**
- Docusaurus 3 site at docs-site/ configured for GitHub Pages deployment
- Developer section: architecture overview (two-tier diagram, data flow, key concepts), adapter authoring guide (step-by-step with VapourtecR4 as worked example), transport guide, REST API reference, gRPC reference
- Scientist/operator section: workflow authoring guide (with screenshots from Tauri app), device capability reference, experiment setup guide
- Deployment and setup guide (mirrors Phase 10 scripts with explanatory prose)
- Auto-generated REST API docs from Site Server OpenAPI spec (redocly or swagger-codegen)
- gRPC reference generated from agent_control.proto and adapter.proto

**Requirements covered:** F1, F2, F3, F4, F5

**Dependencies:** Phase 8 (stable, tested codebase to document); Phase 7 (Site Server API stable; screenshots from Tauri app available from Phase 6)

**Success criteria:**
- [ ] `npm run build` in docs-site/ exits 0 with no broken internal links
- [ ] The adapter authoring guide walks through VapourtecR4 as the worked example and is accurate against the Phase 2 implementation
- [ ] The REST API reference page renders the full Site Server OpenAPI spec (all endpoints documented)
- [ ] The gRPC reference page lists all RPCs from agent_control.proto and adapter.proto
- [ ] A developer unfamiliar with the codebase can follow the adapter authoring guide and produce a compilable stub driver

---

## Phase 10: Deployment + Setup Scripts

**Goal:** The entire Helix stack can be started with one command on a clean machine. Production deployments have documented hardening steps. Hardware pre-flight validation runs before any experiment starts.

**Deliverables:**
- docker-compose.yml (root) full-stack: Agent + Site Server + Recorder + PostgreSQL + Mosquitto + MinIO with health checks and dependency ordering for all six services
- setup.ps1 (Windows): installs .NET 10 SDK, Node.js, checks Docker Desktop, builds all projects, starts compose stack; idempotent
- setup.sh (Linux/Raspberry Pi): same for Debian/Ubuntu; includes systemd unit file installation
- Windows service install scripts: Agent and Recorder as Windows services (sc.exe or NSSM)
- systemd unit files: Agent and Recorder as systemd services on Linux
- Hardware pre-flight check script (preflight.ps1 / preflight.sh): validates serial/TCP/OPC UA transport connections, MinIO bucket access, PostgreSQL connection, MQTT broker reachability; exits with pass/warn/fail summary report
- Production hardening guide: TLS for MQTT (Mosquitto config with certs), MinIO access key rotation, PostgreSQL password rotation, AutoAcceptUntrustedCertificates set false for OPC UA

**Requirements covered:** G1, G2, G3, G4, G5, G6

**Dependencies:** Phase 8 (stable, tested codebase), Phase 9 (Docusaurus scaffold exists for deployment guide cross-reference)

**Success criteria:**
- [ ] `docker-compose up` (root) starts all six services with all health checks green within 60 seconds
- [ ] `setup.ps1` runs to completion on a Windows 11 machine that has only Git and Docker Desktop installed; compose stack is running at the end
- [ ] Agent runs as a Windows service (installed via script) and survives a machine reboot
- [ ] Agent runs as a systemd service on Debian 12; `systemctl status helix-agent` shows active (running)
- [ ] Pre-flight script exits with pass for all checks against the compose stack (no real hardware required)
- [ ] Production hardening guide in Docusaurus is accurate: TLS + credential rotation steps are followable without consulting source code

---

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Technical Debt Resolution | 8/8 | Complete | 2026-05-26 |
| 2. VapourtecR4 Reference Driver | 7/7 | Complete | 2026-05-26 |
| 3. Remaining Device Drivers | 5/5 | Complete | 2026-05-26 |
| 4. Transport + Execution Integration Tests | 0/4 | Not started | - |
| 5. Tauri Desktop App - Foundation | 3/3 | Complete | 2026-05-28 |
| 6. Tauri Desktop App - Workflow Builder + Execution | 1/8 | In progress | - |
| 7. Site-Level React Dashboard | 0/6 | Not started | - |
| 8. CI Hardening + Full Test Coverage | 0/5 | Not started | - |
| 9. Docusaurus Documentation | 0/5 | Not started | - |
| 10. Deployment + Setup Scripts | 0/6 | Not started | - |

---

## Requirement Coverage

| Requirement | Phase | Status |
|-------------|-------|--------|
| A1 | Phase 1 | Pending |
| A2 | Phase 1 | Pending |
| A3 | Phase 1 | Pending |
| A4 | Phase 1 | Pending |
| A5 | Phase 1 | Pending |
| A6 | Phase 1 | Pending |
| A7 | Phase 1 | Pending |
| A8 | Phase 1 | Pending |
| B1 | Phase 2 | Pending |
| B2 | Phase 2 | Pending |
| B3 | Phase 3 | Pending |
| B4 | Phase 3 | Pending |
| B5 | Phase 3 | Pending |
| B6 | Phase 2 | Pending |
| B7 | Phase 2 | Pending |
| B8 | Phase 2 + 3 | Pending |
| C1 | Phase 5 | Complete |
| C2 | Phase 6 | Pending |
| C3 | Phase 6 | Pending |
| C4 | Phase 6 | Pending |
| C5 | Phase 6 | Pending |
| C6 | Phase 6 | Pending |
| C7 | Phase 5 | Complete |
| C8 | Phase 5 | Complete |
| C9 | Phase 6 | Pending |
| D1 | Phase 8 | Pending |
| D2 | Phase 4 | Pending |
| D3 | Phase 4 | Pending |
| D4 | Phase 6 | Pending |
| D5 | Phase 4 | Pending |
| D6 | Phase 8 | Pending |
| D7 | Phase 8 | Pending |
| E1 | Phase 7 | Pending |
| E2 | Phase 7 | Pending |
| E3 | Phase 7 | Pending |
| E4 | Phase 7 | Pending |
| E5 | Phase 7 | Pending |
| E6 | Phase 7 | Pending |
| F1 | Phase 9 | Pending |
| F2 | Phase 9 | Pending |
| F3 | Phase 9 | Pending |
| F4 | Phase 9 | Pending |
| F5 | Phase 9 | Pending |
| G1 | Phase 10 | Pending |
| G2 | Phase 10 | Pending |
| G3 | Phase 10 | Pending |
| G4 | Phase 10 | Pending |
| G5 | Phase 10 | Pending |
| G6 | Phase 10 | Pending |

**Coverage: 49/49 requirements mapped (100%)**

