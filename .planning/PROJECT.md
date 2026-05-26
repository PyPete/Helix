# Helix Orchestration Platform

## What This Is

A distributed lab automation platform that enables scientists and technicians to build visual workflows, connect real laboratory hardware (reactors, pumps, HPLC, spectrometers), execute automated experiments, and visualise live data — all without writing code. The system runs two tiers: a per-fumehood Agent (local execution, device control, safety) and a Site Server (aggregation, campaign management, dashboards). A Tauri desktop app provides the local workflow authoring and monitoring experience; a React web app provides site-wide operations visibility.

## Core Value

A scientist can build a workflow, connect real lab hardware, execute an experiment, and see the results — without writing code.

## Requirements

### Validated

<!-- Capabilities confirmed built and tested across 14 phases -->

- ✓ Core domain: workflow engine (WorkflowExecutor, WorkflowContext, IWorkflow, WorkflowRegistry) — existing
- ✓ Capability invocation: CapabilityRegistry dispatch, ICapabilityInvoker — existing
- ✓ Stratified trend model: ITrend hierarchy, TrendWindow<T>, TrendFunctions, InMemoryScalarTrend — existing
- ✓ Safety engine: InterlockRule evaluation, EmergencyStopToken broadcast — existing
- ✓ Device leasing: IDeviceLeasingService, InMemoryDeviceLeasingService, heartbeat expiry — existing
- ✓ WSL contract: WslDocument, RunspaceDefinition, IWslCompiler, diagnostics — existing
- ✓ Experiment lifecycle: ExperimentContext, ExperimentLifecycle state machine — existing
- ✓ Durable runspace: DurableRunspace via Cleipnir, RunspaceManager — existing
- ✓ Variable management: workflow-scoped, global/campaign-scoped, WSL substitution — existing
- ✓ Agent runtime: gRPC server, adapter discovery/proxy, LeaseExpiryWorker, MqttTrendPublisher — existing
- ✓ Site Server: REST API, CampaignManager, IAgentProxy (sim/gRPC), Swagger — existing
- ✓ Simulation mode: SimReactor, SimPump, SimSpectrometer, SimAgentProxy — existing
- ✓ PostgreSQL persistence: experiment/campaign repositories, EF Core migrations — existing
- ✓ Recorder Worker Service: MQTT ingestion, JSONL batch flushing, MinIO via IObjectStorageSink — existing
- ✓ Device transport layer: IModbusTransport, ITcpTransport, IOpcTransport, ISerialTransport — existing
- ✓ Typed analytical data model: Spectrum1D/2D, NMR/MS/FTIR metadata records — existing

### Active

<!-- Current work cycle — building toward these -->

#### Phase A — Technical Debt Resolution (prerequisite)
- [ ] Merge root-level Recorder implementation into solution (replace placeholder Worker.cs)
- [ ] Resolve IObjectStorageSink interface divergence (single canonical interface + client library)
- [ ] Populate CapabilityRegistry in DeviceAdapterExtensions (currently registers nothing)
- [ ] Replace InMemoryFunctionStore with Cleipnir PostgreSQL store in Agent Program.cs
- [ ] Remove Opc.UaFx.Advanced commercial dependency; complete migration to OPC Foundation library
- [ ] Replace FormatterServices.GetUninitializedObject with Activator.CreateInstance<T>()
- [ ] Wire IExperimentRepository + ICampaignRepository into SiteServer DI
- [ ] Fix solution build configuration (Debug build excludes Agent, Recorder, Infrastructure, Devices)

#### Phase B — Device Driver Audit + Port
- [ ] Audit POP.DeviceCore/Drivers — catalogue all drivers, protocols, command vocabularies
- [ ] Port VapourtecR4 using new DeviceAdapterBase composition model
- [ ] Port KnauerAzura (HPLC pump)
- [ ] Port SystagOpcUaClient (OPC UA reactor)
- [ ] Port remaining drivers identified in audit
- [ ] Fix ISerialTransport.Open signature to accept baudRate parameter
- [ ] Fix DI lifetime mismatch (transports registered Transient, captured by Singleton registry)

#### Phase C — Local Fumehood Desktop App (Tauri + Rete.js)
- [ ] Tauri app scaffold (cross-platform: Windows + Linux)
- [ ] Rete.js workflow builder (visual node-flow editor generating WSL JSON)
- [ ] Experiment execution panel (start, pause, abort runspace)
- [ ] Live data dashboard (scalar trends, spectral charts, device status)
- [ ] Safety status and interlock display
- [ ] Standalone mode (direct Agent REST/gRPC, no Site Server required)
- [ ] Device registry and adapter status view

#### Phase D — Test Coverage
- [ ] Unit tests for all device adapters and capability dispatch
- [ ] Integration tests for transport layer (hardware simulation, no real device)
- [ ] E2E workflow execution tests (submit WSL → execute steps → verify trend output)
- [ ] UI component tests (Tauri/React workflow builder, dashboards)
- [ ] Fix CI build to include all test projects (Devices, Infrastructure, Recorder)
- [ ] Remove [Fact(Skip)] from PostgreSQL repository tests; add Docker Compose test harness

#### Phase E — Site-Level React Dashboard
- [ ] React app scaffold (Vite + TypeScript)
- [ ] Site overview: fumehood status, active experiments, device health
- [ ] Campaign management UI (create, monitor, abort campaigns)
- [ ] Experiment history and result browser
- [ ] MQTT-backed live trend visualisation (WebSocket/SignalR bridge)
- [ ] Analytical data viewer (spectra, DAD chromatograms)

#### Phase F — Docusaurus Documentation
- [ ] Docusaurus site scaffold (GitHub Pages deployment)
- [ ] Developer section: architecture, adapter authoring guide, API reference
- [ ] Scientist/operator section: workflow authoring guide, device capability reference
- [ ] Deployment and setup guide (local + production)
- [ ] Auto-generated API docs from XML comments / OpenAPI spec

#### Phase G — Local Deployment + Setup Scripts
- [ ] Docker Compose full-stack script (Agent + Site Server + Recorder + Postgres + MQTT + MinIO)
- [ ] One-command dev environment bootstrap (Windows + Linux)
- [ ] Production deployment guide (systemd services, Windows service install)
- [ ] Hardware pre-flight check script (validates device connections before experiment start)

### Out of Scope

- Cloud-level orchestration across multiple sites — architecture supports it but not in scope for v1
- Mobile app — desktop (Tauri) + web (React) covers target users
- AI/ML-driven optimisation loop — Campaign Manager seam exists; ML integration is a future milestone
- SiLA 2 adapters — protocol seam exists; not in current driver list
- ELN/LIMS integration — WSL authoring seam exists; connector work deferred

## Context

- 14 development phases completed; ~107 tests passing at last known-good state
- Significant mid-flight refactor in progress on `dev` branch: old inheritance-based device base classes deleted, new composition-based DeviceAdapterBase model introduced but no concrete drivers yet written
- Several critical gaps exist before hardware can be controlled: CapabilityRegistry is empty, InMemoryFunctionStore means runspace state is lost on restart, and there are zero concrete device driver implementations in the solution tree
- POP.DeviceCore is the legacy device driver library (C:\Users搶64\source\repos\CSharpGitLab\PopNextGen\POP.DeviceCore\Drivers) — the driver logic exists there and needs to be ported to the new DeviceAdapterBase architecture
- Local UI (Tauri) must work standalone without a Site Server, since fumehoods may operate in isolated network segments
- Primary deployment target: Windows PCs in chemistry labs, with Linux (Raspberry Pi or similar) as secondary target for embedded fumehood controllers

## Constraints

- **Safety**: Safety engine must evaluate locally — never depends on network connectivity or Site Server availability
- **Standalone mode**: Local Tauri UI must connect directly to Agent without requiring Site Server
- **Cross-platform**: Tauri app must run on Windows 11 and Linux (Debian/Ubuntu)
- **Protocol fidelity**: Device drivers must exactly match legacy POP.DeviceCore command vocabularies — lab instruments have strict protocol requirements
- **Durable execution**: Runspace state must survive Agent process restart (PostgreSQL-backed Cleipnir store)
- **No breaking changes to WSL contract**: WSL JSON schema is a public authoring contract; changes must be additive

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Tauri + Rete.js for local UI | Cross-platform desktop wrapping Agent gRPC/REST; Rete.js provides visual node-flow authoring matching WSL graph model | — Pending |
| React (Vite) for site dashboard | Standard SPA stack; separate from Tauri to allow web browser access from any machine on site network | — Pending |
| Docusaurus for docs | GitHub Pages deployment; developer + scientist sections; can auto-generate from OpenAPI and XML comments | — Pending |
| Composition over inheritance for device adapters | DeviceAdapterBase with injected transport rather than per-protocol base classes; enables testing transports independently | — Pending |
| OPC Foundation library over Opc.UaFx.Advanced | Open-source; removes commercial licence dependency from production deployments | — Pending |
| Cleipnir PostgreSQL store for durable execution | Survives Agent process restart; consistent with existing PostgreSQL infrastructure choice | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd:complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-05-26 after initialization*
