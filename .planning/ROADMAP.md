# Roadmap

**Project:** Helix Orchestration Platform
**Updated:** 2026-06-04

---

## Milestones

| Milestone | Description | Status | Archived |
|-----------|-------------|--------|---------|
| v0.1 | Hardware-ready platform: 48 drivers, Tauri workflow builder, site dashboard, docs, Docker deployment | SHIPPED 2026-06-03 | [v0.1-ROADMAP.md](milestones/v0.1-ROADMAP.md) |
| v0.2 | Production-Grade Platform: IDE shell, experiment model, three-mode authoring, SciChart, autodiscovery, Python API | IN PROGRESS | â€” |

---

## v0.2 Phase Overview

| # | Phase | Goal | Depends On | Est. Size |
|---|-------|------|------------|-----------|
| 11 | Ribbon + Docking Workspace Shell | Rebuild helix-desktop on a VS-style IDE shell with ribbon, activity bar, dockable panels, and workspace persistence | â€” | XL |
| 12 | Experiment as Top-Level Scope | Introduce ExperimentId as the canonical execution container spanning pre/main/post phases with full event replay | â€” | L |
| 13 | Three Workflow Modes | Deliver Start/Stop, Kanban, and Node-Flow as three views over the same WslDocument with mode-switching and authoring constraints | 11, 12 | L |
| 14 | ReactFlow Node Canvas | Replace Rete.js with ReactFlow; port all 8 node types with dark IDE theme, device palette, and live execution traffic lights | 11 | L |
| 15 | SciChart Integration | Replace recharts with SciChart in helix-desktop and helix-dashboard; host chart panels in dockable Chart Panel docks | 11 | M |
| 16 | Device Autodiscovery + Developer Debugger | Implement Agent Identify() lifecycle, DeviceDiscoveryEvent stream, dockable Debugger panel, and Developer Mode Ribbon tab | 11 | M |
| 17 | Real-Time Trend System | Standardise trend naming, add TrendAggregator windowing, persist per-experiment trend files, and enable replay | 12, 15 | M |
| 18 | Python API | Publish helix-client Python package with HelixClient, ExperimentHandle, WSL builder, and Jupyter support | 12 | M |
| 19 | Production Hardening | Apply CORS, API key auth, structured logging, OpenTelemetry, and run full live-stack verification | 11â€“18 | M |

---

## Phase Details

### Phase 11: Ribbon + Docking Workspace Shell

**Goal**: Scientists open helix-desktop and find a professional IDE shell â€” VS-Ribbon, activity bar, dockable panels, document tabs, and workspace save/restore â€” replacing the flat layout.
**Requirements**: H1, H2, H3, H4, H5, H6, H7, H8, H9, H10, H11
**Depends on**: â€”
**Est. size**: XL
**Success criteria**:

1. Scientist opens helix-desktop and sees the VS-Ribbon (68px) with tab groups (Home, Devices, Trends, Workflows, Charts, Developer, Settings) and an activity bar (40px left edge).
2. Scientist drags a panel from its dock position, floats it as a window, pins it back, and closes it â€” all without a full page re-render.
3. Scientist selects a device in Device Explorer and sees an auto-generated Manual Control panel open as a document tab.
4. Scientist saves the current panel layout under a name and restores it after restarting the app.

**Plans**: 4 (11-01 through 11-04, wave 0â€"3)
**UI hint**: yes

---

### Phase 11.1: remove ui components and focus on backend (remove tauri etc) (INSERTED)

**Goal:** Close 8 backend execution gaps that block real experiment runs — trend provider stubs, variable substitution not wired, non-functional pause/resume, SiteServer bypassing WSL compiler, device list always empty in gRPC mode, fumehood status broken, and campaign persistence in-memory only.
**Requirements**: TBD
**Depends on:** Phase 11
**Plans:** 3 plans

Plans:

- [ ] 11.1-01-PLAN.md -- Core/Agent gaps: scalar trend provider, variable substitution wiring, cooperative pause/resume
- [ ] 11.1-02-PLAN.md -- Agent gRPC gaps: wire PauseRunspace/ResumeRunspace to RunspaceManager
- [ ] 11.1-03-PLAN.md -- SiteServer gaps: WSL compiler wiring, gRPC device listing, fumehood status, campaign PostgreSQL persistence

### Phase 12: Experiment as Top-Level Scope

**Goal**: Every experiment run is captured under a traceable ExperimentId that spans pre/main/post phases and can be fully replayed from its event log.
**Requirements**: I1, I2, I3, I4, I5, I6, I7
**Depends on**: â€”
**Est. size**: L
**Success criteria**:

1. Scientist starts an experiment from helix-desktop and the Agent creates a new ExperimentId; the Experiment Timeline dock shows live events with scope nesting and timestamps.
2. Manual device commands issued via the Manual Control panel appear in the experiment event log with the same structure as automated steps.
3. Scientist queries a past experimentId and receives the full ordered sequence of device commands (replay log) with entry/exit timestamps for every scope.
4. Start/Stop/Pause controls in helix-desktop operate on the experiment lifecycle, not on individual runspaces.

**Plans**: 4 (11-01 through 11-04, wave 0â€"3)

---

### Phase 13: Three Workflow Modes

**Goal**: The same WslDocument can be viewed and authored in Start/Stop, Kanban, or Node-Flow mode; switching modes never mutates the underlying document.
**Requirements**: J1, J2, J3, J4, J5, J6, J7, J8
**Depends on**: Phase 11, Phase 12
**Est. size**: L
**Success criteria**:

1. Scientist selects "Start/Stop" in the Ribbon Home tab and sees an operator shell with only Start/Stop/Pause controls, workflow name, live step progress, and event log â€” no canvas.
2. SOP-writer drags task blocks from the Task Library dock into Kanban pre/main/post swimlanes, fills in validated inputs, and runs the workflow without touching the node graph.
3. Switching a Kanban workflow to Node-Flow preserves lane/order metadata as explicit sequential edges; switching back flags any non-representable structures without data loss.
4. The current mode is shown in the document tab header; mode buttons live in the Ribbon Home tab View group.

**Plans**: 4 (11-01 through 11-04, wave 0â€"3)
**UI hint**: yes

---

### Phase 14: ReactFlow Node Canvas

**Goal**: The Node-Flow workflow editor is rebuilt on ReactFlow with all 8 node types, a dark IDE/Blender aesthetic, device drag-from-palette, and live execution traffic lights.
**Requirements**: K1, K2, K3, K4, K5, K6, K7
**Depends on**: Phase 11
**Est. size**: L
**Success criteria**:

1. Scientist opens a saved workflow in Node-Flow mode and sees all 8 node types (Step, Condition, Branch, Loop, Parallel, Delay, WaitFor, DeviceAction) rendered with dark node backgrounds (`#252535`) and purple connectors (`#9d7fff`).
2. Scientist drags a device from Device Explorer onto the canvas; a DeviceAction node appears pre-wired to that adapter with no manual capability entry required.
3. During execution, running nodes glow with the accent colour, completed nodes show green, and errored nodes show red â€” all updating in real time.
4. Existing WorkflowBuilder tests pass against the ReactFlow canvas with no regression in WSL export fidelity.

**Plans**: 4 (11-01 through 11-04, wave 0â€"3)
**UI hint**: yes

---

### Phase 15: SciChart Integration

**Goal**: Live and spectral charts in helix-desktop and site charts in helix-dashboard are powered by SciChart, hosted in dockable Chart Panel docks with drag-from-Trend-Explorer support.
**Requirements**: L1, L2, L3, L4, L5, L6
**Depends on**: Phase 11
**Est. size**: M
**Success criteria**:

1. Scientist runs an experiment and watches scalar trends (temperature, pressure, flow) rendered by SciChart at 1000+ data points/second with no visible animation artifacts.
2. Scientist opens a spectral channel; UV/MIR spectral overlays render correctly in a SciChart panel without recharts.
3. Scientist drags a trend from the Trend Explorer dock into an open Chart Panel dock; the trend line appears live on the chart immediately.
4. All existing chart tests pass against SciChart equivalents; recharts is no longer imported in helix-desktop or helix-dashboard.

**Plans**: 4 (11-01 through 11-04, wave 0â€"3)
**UI hint**: yes

---

### Phase 16: Device Autodiscovery + Developer Debugger

**Goal**: The Agent runs an Identify() discovery lifecycle on startup and exposes a real-time event stream; developers inspect and control discovery from a dockable Debugger panel.
**Requirements**: M1, M2, M3, M4, M5, M6, M7
**Depends on**: Phase 11
**Est. size**: M
**Success criteria**:

1. On Agent startup, each registered adapter's Identify() is called; discovery events (discovered, failed, capability-validated, trend-schema-registered) stream to the Device Discovery Debugger dock in real time.
2. Developer triggers Identify() on a live adapter from the Developer Mode Ribbon tab without restarting the Agent; the Debugger dock shows the updated capability validation result.
3. Debugger write actions (Identify, reconnect) show an explicit confirmation dialog; triggering them during an active experiment has no effect on execution or device stability.
4. Selecting an adapter in Device Explorer shows its device descriptor, capabilities, and trend schema as populated by the latest discovery pass.

**Plans**: 4 (11-01 through 11-04, wave 0â€"3)
**UI hint**: yes

---

### Phase 17: Real-Time Trend System

**Goal**: Trend names follow the `{adapterId}.{metric}` protocol, TrendAggregator supports time-window downsampling, trend data is persisted per-experiment, and replay is available from the Trend Explorer.
**Requirements**: N1, N2, N3, N4, N5, N6
**Depends on**: Phase 12, Phase 15
**Est. size**: M
**Success criteria**:

1. Agent rejects trend registration for any name that does not match `{adapterId}.{metric}`; existing v0.1 adapters emit corrected names without breaking replay of legacy logs.
2. Scientist opens a long experiment trend and selects a 10-second bucket view; TrendAggregator returns downsampled data without dropping the min/max envelope.
3. After an experiment completes, per-experiment per-trend JSONL files exist on disk and are loadable by the Trend Explorer replay mode.
4. Scientist selects a past experimentId in the Trend Explorer, enters replay mode, and watches the chart timeline play back the stored trend data.

**Plans**: 4 (11-01 through 11-04, wave 0â€"3)
**UI hint**: yes

---

### Phase 18: Python API

**Goal**: A published `helix-client` Python package lets developers submit experiments, stream events, and build WSL documents programmatically from scripts or Jupyter notebooks.
**Requirements**: O1, O2, O3, O4, O5, O6, O7
**Depends on**: Phase 12
**Est. size**: M
**Success criteria**:

1. Developer runs `pip install -e ./helix-client` and connects to a local Agent with `HelixClient("http://localhost:5080")`; `submit_experiment("workflow.json")` returns an `ExperimentHandle` with a valid `experiment_id`.
2. Developer iterates `async for event in handle.events()` inside a Jupyter notebook cell and sees experiment step events stream without blocking the kernel.
3. Developer constructs a workflow programmatically using the fluent WSL builder and submits it; the Agent executes it identically to a file-loaded workflow.
4. Python API reference and a runnable Jupyter example notebook are live on the Docusaurus docs site.

**Plans**: 4 (11-01 through 11-04, wave 0â€"3)

---

### Phase 19: Production Hardening

**Goal**: The full stack is safe to deploy outside a lab LAN: CORS locked down, all endpoints authenticated, structured logs with correlation IDs, OpenTelemetry traces, and live-stack verification passing.
**Requirements**: P1, P2, P3, P4, P5, P6, P7, P8, P9, P10
**Depends on**: Phase 11, Phase 12, Phase 13, Phase 14, Phase 15, Phase 16, Phase 17, Phase 18
**Est. size**: M
**Success criteria**:

1. Site manager deploys with `HELIX_API_KEY` set; direct calls to Agent or SiteServer REST endpoints without the `X-Helix-Key` header return 401; local dev without a key still works when auth is disabled.
2. SiteServer rejects cross-origin requests from any origin other than the configured helix-dashboard origin; CORS pre-flight returns correct headers.
3. `docker-compose up` brings all 6 services to healthy; SiteServer starts clean, Agent gRPC is reachable, and the dashboard fumehoods page loads live data.
4. Tauri app launches on Windows, connects to a local Agent, and runs a simulated workflow end-to-end with structured JSON logs visible in the Agent console (enriched with experimentId).

**Plans**: 4 (11-01 through 11-04, wave 0â€"3)

---

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 11. Ribbon + Docking Workspace Shell | 0/? | Not started | â€” |
| 12. Experiment as Top-Level Scope | 0/? | Not started | â€” |
| 13. Three Workflow Modes | 0/? | Not started | â€” |
| 14. ReactFlow Node Canvas | 0/? | Not started | â€” |
| 15. SciChart Integration | 0/? | Not started | â€” |
| 16. Device Autodiscovery + Developer Debugger | 0/? | Not started | â€” |
| 17. Real-Time Trend System | 0/? | Not started | â€” |
| 18. Python API | 0/? | Not started | â€” |
| 19. Production Hardening | 0/? | Not started | â€” |

---

## Backlog (v0.3+)

- Cloud-level orchestration across multiple sites
- SiLA 2 protocol adapters
- ELN/LIMS connector
- AI/ML optimisation loop
- Mobile app
- SEED-002: Full execution scope inspector in Node-Flow canvas
- Real-time collaboration on workflow authoring
