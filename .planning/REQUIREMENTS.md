# Requirements

**Project:** Helix Orchestration Platform
**Scope:** Milestone v0.2 — Production-Grade Platform
**Status:** Active
**Last updated:** 2026-06-03

---

## Vision

A scientist can open Helix Desktop, choose how they want to author their protocol (visual graph, Kanban blocks, or simple start/stop), run a traceable experiment, see live high-performance charts, and trust the platform with real chemistry — on a hardened, authenticated, production-ready stack.

---

## User Stories

### Scientist — Professional Authoring Environment
- As a scientist, I can work in a Visual Studio–style IDE shell with dockable panels (Device Explorer, Trend Explorer, Chart Panels, Workflow Editor, Experiment Timeline), so my workflow has the feel and ergonomics of a professional tool.
- As a scientist, I can choose between Node-Flow (visual graph), Kanban (swimlane blocks), or Start/Stop (operator) mode for the same workflow, so I can match the complexity of the UI to the task at hand.
- As a scientist, I can author a Kanban workflow by dragging task blocks into pre/main/post swimlanes without needing to understand the underlying graph, so I can build SOPs without graph editing expertise.

### Scientist — Experiment Lifecycle
- As a scientist, I can start an experiment that creates a traceable experimentId spanning pre/main/post phases, so every device command — automated or manual — is captured in one auditable record.
- As a scientist, I can replay any past experiment from its event log to reconstruct the exact device command sequence, so I can diagnose what happened without relying on memory or manual notes.
- As a scientist, I can drag trends from the Trend Explorer dock into Chart Panel docks and see high-performance real-time data via SciChart, so I can monitor the experiment without performance bottlenecks.

### Technician / Operator
- As a technician, I can open a workflow in Start/Stop mode and see only Start/Stop/Pause controls and a live step progress log, so I can run routine SOPs without risk of accidentally modifying the workflow.

### Developer / Integrator
- As a developer, I can open the Device Discovery Debugger dock to see real-time discovery events, Identify() responses, capability validation results, and raw logs, so I can diagnose adapter issues without reading log files.
- As a developer, I can manually trigger Identify() and reconnect adapters from the Developer Mode Ribbon tab, so I can test device integration without restarting the Agent.
- As a developer, I can write Python scripts that submit experiments, monitor step progress, and construct WSL workflow definitions programmatically, so I can integrate Helix into automated lab workflows (Jupyter, CI pipelines).

### Site Operations
- As a site manager, I can connect Helix to production infrastructure and trust that endpoints require authentication, CORS is locked down, and all events are structured logs, so the platform is safe to deploy outside a lab LAN.

---

## Functional Requirements

### Phase H — Ribbon + Docking Workspace Shell (helix-desktop)

| ID | Requirement | Priority |
|----|-------------|----------|
| H1 | Implement VS-Ribbon chrome (68px band) with tab groups: Home, Devices, Trends, Workflows, Charts, Developer, Settings | Must Have |
| H2 | Implement activity bar (40px left edge) with icon buttons; 2px left-edge active indicator | Must Have |
| H3 | Implement VS-Split panel workspace: left sidebar (Device Explorer, 220px), right sidebar (Trend Explorer, 200px), central document-tab canvas, bottom output strip (140px) | Must Have |
| H4 | Implement dockable panel system: panels are dockable, floatable, pinnable, closeable | Must Have |
| H5 | Implement Document Tabs in central canvas for Workflow Editor, Chart Panels, Experiment Timeline | Must Have |
| H6 | Implement Device Explorer dock: device list grouped by type/capability; selecting opens auto-generated Manual Control panel | Must Have |
| H7 | Implement Trend Explorer dock: raw trends, derived trends, custom trends; drag trend into Chart Panel dock | Must Have |
| H8 | Implement Chart Panel docks: dockable, real-time or replayed SciChart panels opened from Trend Explorer | Must Have |
| H9 | Implement Log/Output dock: system logs, device logs, execution logs | Must Have |
| H10 | Workspace layout save/restore: user can save named layouts and restore them | Should Have |
| H11 | Apply established design system: `#1e1e2e` chrome, `#252535` surfaces, `#9d7fff` accent, Segoe UI/system-ui 0.875rem, all CSS variables from sketch theme | Must Have |

### Phase I — Experiment as Top-Level Scope (Agent + helix-desktop)

| ID | Requirement | Priority |
|----|-------------|----------|
| I1 | Introduce ExperimentId as the top-level execution container; Agent exposes POST /experiments endpoint | Must Have |
| I2 | An experiment supports pre/main/post phase scopes as sibling execution scopes under the same experimentId | Must Have |
| I3 | Manual actions taken via the Manual Control panel are recorded inside the active experiment scope with identical event structure to automated steps | Must Have |
| I4 | Experiment scope tree (entry/exit timestamps, step invocations, parameters, results) is persisted and queryable | Must Have |
| I5 | Given an experimentId, the system can reconstruct the full sequence of device commands in order (replay log) | Must Have |
| I6 | Start/Stop/Pause controls in helix-desktop operate on the experiment lifecycle (not directly on individual runspaces) | Must Have |
| I7 | Experiment Timeline dock in helix-desktop shows live event protocol with scope nesting, timestamps, and manual action markers | Should Have |

### Phase J — Three Workflow Modes (helix-desktop)

| ID | Requirement | Priority |
|----|-------------|----------|
| J1 | WslDocument metadata stores a `viewMode` field; switching mode preserves the document unchanged | Must Have |
| J2 | Start/Stop mode: operator shell with no canvas; shows workflow name/phase, large Start/Stop/Pause controls, live step progress, event log | Must Have |
| J3 | Kanban mode: swimlane-based workflow editor with pre/main/post lanes; task blocks dragged from Task Library dock; sequential execution within lanes | Must Have |
| J4 | Kanban blocks compile to the same WslDocument structure as Node-Flow nodes; WslCompiler is unaware of Kanban | Must Have |
| J5 | Kanban authoring constraints: blocks added only by drag from Task Library; inputs validated via CapabilityDescriptor.ParameterSchema; controlled condition builder; units enforced | Must Have |
| J6 | Switching Kanban → Node-Flow: lane/order metadata preserved; sequential edges made explicit in the graph | Must Have |
| J7 | Switching Node-Flow → Kanban: nodes without lane metadata placed in staging area; complex structures (nested loops, non-linear branches) flagged as not representable | Should Have |
| J8 | Mode buttons live in Ribbon Home tab (View group); mode label shown in document tab header | Must Have |

### Phase K — ReactFlow Node Canvas (helix-desktop)

| ID | Requirement | Priority |
|----|-------------|----------|
| K1 | Replace Rete.js with ReactFlow as the Node-Flow canvas library; all 8 existing node types ported (Step, Condition, Branch, Loop, Parallel, Delay, WaitFor, DeviceAction) | Must Have |
| K2 | Apply dark IDE / Blender-UE node aesthetic using established theme palette: node backgrounds `#252535`, connectors `#9d7fff`, execution state indicators via border/glow | Must Have |
| K3 | Device Action palette: devices from Device Explorer dragged onto canvas create DeviceAction nodes pre-wired to the adapter; no manual capability entry required | Must Have |
| K4 | Live execution traffic lights: nodes highlight running/completed/error state during experiment execution | Must Have |
| K5 | Strict authoring (SEED-002 validation): controlled condition builder on Condition/Branch nodes; units enforced on numeric parameters; no free-form expression fields | Should Have |
| K6 | Node-Flow canvas hosted inside Workflow Editor document tab in the Ribbon workspace | Must Have |
| K7 | All existing WorkflowBuilder tests ported to ReactFlow equivalents; no regression in WSL export | Must Have |

### Phase L — SciChart Integration (helix-desktop + helix-dashboard)

| ID | Requirement | Priority |
|----|-------------|----------|
| L1 | Replace recharts ScalarTrendChart with SciChart in helix-desktop; real-time scalar data (temperature, pressure, flow) renders without animation artifacts | Must Have |
| L2 | Replace recharts SpectralChart with SciChart in helix-desktop; UV/MIR spectral overlay renders correctly | Must Have |
| L3 | SciChart chart panels are hosted inside dockable Chart Panel docks in the Ribbon workspace | Must Have |
| L4 | Trends can be dragged from the Trend Explorer dock into a Chart Panel dock to add them to the live chart | Should Have |
| L5 | Replace recharts in helix-dashboard (SiteOverviewPage / HistoryPage charts) with SciChart | Should Have |
| L6 | All existing chart tests updated for SciChart; no regression in test count | Must Have |

### Phase M — Device Autodiscovery Layer + Developer Debugger

| ID | Requirement | Priority |
|----|-------------|----------|
| M1 | Agent implements a Device Autodiscovery engine: on startup, calls Identify() on each registered adapter; emits discovery lifecycle events (discovered, failed, capability-validated, trend-schema-registered) | Must Have |
| M2 | Autodiscovery hooks expose real-time events over the Agent WebSocket stream (new event type: DeviceDiscoveryEvent) | Must Have |
| M3 | Agent exposes POST /devices/{id}/rediscover endpoint to trigger Identify() and reconnect on a live adapter without restarting the service | Must Have |
| M4 | helix-desktop implements dockable Device Discovery Debugger panel: shows live DeviceDiscoveryEvent stream, adapter info, Identify() responses, capability validation, trend schema, health status, raw log | Must Have |
| M5 | Developer Mode Ribbon tab provides quick-access controls: Trigger Identify(), Reconnect Adapter, Replay Discovery Lifecycle, Export Discovery Log | Must Have |
| M6 | Discovery Debugger is read-only by default; write actions (Identify, reconnect) require explicit confirmation; debug mode does not affect experiment execution or device stability | Must Have |
| M7 | Discovery results (device descriptor, capabilities, trend schema) are inspectable per-adapter in the Device Explorer dock | Should Have |

### Phase N — Real-Time Trend System

| ID | Requirement | Priority |
|----|-------------|----------|
| N1 | Standardise trend naming protocol: device trend names follow `{adapterId}.{metric}` convention (e.g. `simulator.temperature`); Agent validates names at registration time | Must Have |
| N2 | TrendAggregator supports configurable time-window downsampling (e.g. 1s, 10s, 1min buckets) for long experiment data | Must Have |
| N3 | Trend data is persisted to disk during experiment execution (per-experimentId, per-trendName JSONL files) | Must Have |
| N4 | The Trend Explorer dock supports replay mode: given an experimentId, loads persisted trend files and replays them through the chart timeline | Must Have |
| N5 | Python sidecar can emit trend values into the Agent's WebSocket/MQTT pipeline using the standard TrendValue event format | Should Have |
| N6 | Trend schema (name, unit, type, range) is registered at adapter initialisation and inspectable via the Discovery Debugger (links to M7) | Should Have |

### Phase O — Python API

| ID | Requirement | Priority |
|----|-------------|----------|
| O1 | Python package `helix-client` published (initially local/editable install): exposes `HelixClient` class connecting to Agent REST/WebSocket | Must Have |
| O2 | `HelixClient.submit_experiment(workflow_path)` submits a saved WslDocument and returns an `ExperimentHandle` with `experiment_id` | Must Have |
| O3 | `ExperimentHandle` supports async iteration over experiment events (`async for event in handle.events()`) and `await handle.wait_until_complete()` | Must Have |
| O4 | Python WSL builder: fluent API for constructing WslDocument programmatically (`Workflow().step("Prime").device("simulator", "SetFlow", flow=10).loop(...)`) | Should Have |
| O5 | Python sidecar process: optional companion process that Agent can spawn; exposes a local Unix/TCP socket for Python trend emission and capability invocation | Should Have |
| O6 | Jupyter-friendly: `HelixClient` and `ExperimentHandle` work inside a Jupyter notebook cell without blocking the kernel event loop | Should Have |
| O7 | Documentation: Python API reference and Jupyter example notebook added to Docusaurus docs site | Should Have |

### Phase P — Production Hardening

| ID | Requirement | Priority |
|----|-------------|----------|
| P1 | SiteServer: AddCors/UseCors configured with origin policy (allow helix-dashboard origin; deny all others by default) | Must Have |
| P2 | DeviceStatusEvent emits adapter prefix as deviceId (e.g. `simulator`) not full capability key (e.g. `simulator.SetFlow`) | Must Have |
| P3 | Agent REST API endpoints require authentication: API key header (`X-Helix-Key`) validated on all non-health endpoints | Must Have |
| P4 | SiteServer REST API endpoints require authentication: same API key scheme | Should Have |
| P5 | Structured logging: Serilog configured in Agent + SiteServer with JSON output, minimum severity Info, enriched with experimentId/runspaceId correlation | Must Have |
| P6 | OpenTelemetry traces emitted from Agent (experiment lifecycle, device invocations, gRPC calls) | Should Have |
| P7 | Live-stack verification: docker-compose up all 6 services; confirm SiteServer starts clean, Agent gRPC reachable, dashboard loads fumehoods page | Must Have |
| P8 | Live-stack verification: Tauri app launches on Windows, connects to local Agent, runs a sim workflow end-to-end | Must Have |
| P9 | Live-stack verification: systemd helix-agent.service starts successfully on Debian 12 | Should Have |
| P10 | WorkflowBuilder.tsx async useState cleanup: replace stale-closure pattern with useEffect + useRef or useCallback | Should Have |

---

## Non-Functional Requirements

| Category | Requirement |
|----------|-------------|
| Performance | SciChart panels must render 1000+ data points/second without frame drops on the target Windows PC |
| Docking UX | Panel docking library must support drag-to-dock, float, pin, and close without full page re-render |
| Experiment integrity | Discovery Debugger operations (Identify, reconnect) must not interrupt an active experiment execution |
| Python compatibility | helix-client targets Python 3.10+; async API compatible with asyncio and Jupyter kernel |
| Auth backwards compat | API key auth must be opt-in configurable; local dev should work without setting a key |
| Trend naming | Existing trend names from v0.1 adapters must be migrated to the new `{adapterId}.{metric}` convention without breaking replay of v0.1 experiment logs |

---

## Future Requirements (deferred from v0.2)

- Cloud-level orchestration across multiple sites
- SiLA 2 protocol adapters
- ELN/LIMS connector
- AI/ML optimisation loop integration (Campaign Manager seam exists)
- Mobile app
- Multi-tenant Site Server
- SEED-002: Rete.js execution scopes + strict authoring (strict authoring rules captured in K5/J5; full scope inspector deferred)
- Real-time collaboration on workflow authoring

---

## Out of Scope (v0.2)

- SiLA 2 or other protocol standards — transport seam exists; not in driver list
- Cloud multi-site — architecture supports it; deferred
- Mobile app — desktop + web covers target users

---

## Traceability

| Req Group | Phase | IDs |
|-----------|-------|-----|
| H (Ribbon Shell) | Phase 11 | H1–H11 |
| I (Experiment Scope) | Phase 12 | I1–I7 |
| J (Three Modes) | Phase 13 | J1–J8 |
| K (ReactFlow Canvas) | Phase 14 | K1–K7 |
| L (SciChart) | Phase 15 | L1–L6 |
| M (Autodiscovery) | Phase 16 | M1–M7 |
| N (Trend System) | Phase 17 | N1–N6 |
| O (Python API) | Phase 18 | O1–O7 |
| P (Production Hardening) | Phase 19 | P1–P10 |
