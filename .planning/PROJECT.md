# Helix Orchestration Platform

## What This Is

A distributed lab automation platform that enables scientists and technicians to build visual workflows, connect real laboratory hardware (reactors, pumps, HPLC, spectrometers), execute automated experiments, and visualise live data — all without writing code.

**Two tiers:**
- **Agent** — per-fumehood service: local execution, device control, safety engine, gRPC + REST API
- **Site Server** — site-wide aggregation, campaign management, React dashboard

**Clients:**
- **Tauri desktop app** (helix-desktop) — visual workflow authoring (Rete.js), execution panel, live trends, device manual control
- **React web app** (helix-dashboard) — site-wide operations: fumehood overview, campaigns, experiment history

## Core Value

A scientist can build a workflow, connect real lab hardware, execute an experiment, and see the results — without writing code.

---

## Current Milestone: v0.2 — Production-Grade Platform

**Goal:** Rebuild helix-desktop on a professional IDE shell with a coherent experiment model, three-mode workflow authoring, real-time charting, autodiscovery, Python API, and a hardened production stack.

**Target features:**
- Ribbon + Docking Workspace Shell (SEED-001) — VS-Ribbon, activity bar, VS-Split panels, document tabs, dockable panels, workspace layout save/restore
- Experiment as Top-Level Scope (SEED-003) — experimentId container, pre/main/post phases, full replayability, Agent ExperimentManager
- Three Workflow Modes (SEED-004) — Start/Stop (operator), Kanban (SOP-writer), Node-Flow (power user); WSL is canonical, mode is a view
- ReactFlow Node Canvas (SEED-005) — full Rete.js migration; dark IDE / Blender/UE theme; live execution traffic lights; strict authoring
- SciChart Integration — replaces recharts in helix-desktop + helix-dashboard; Chart Panel docks; Trend Explorer drag-and-drop
- Device Autodiscovery Layer + Developer Debugger — Identify() lifecycle, capability validation, dockable Debugger panel, Developer Mode Ribbon tab
- Real-Time Trend System — naming protocol, TrendAggregator windowing + downsampling, record/replay, Python engine integration
- Python API — scripted experiment submission/monitoring + programmatic WSL construction; Python sidecar
- Production Hardening — SiteServer CORS, auth layer, structured logging (Serilog/OpenTelemetry), live-stack verification

---
## Current State (v0.1 — SHIPPED 2026-06-03)

### Shipped

- **48 device drivers** ported from POP.DeviceCore to DeviceAdapterBase (VapourtecR4, KnauerAzura, SystagOpcUaClient, AtlasReactionSystem, Crystalline, IfmMasterController, OceanView UV/NIR, PicoTc08, and all remaining audited drivers)
- **Tauri desktop app** (helix-desktop): Rete.js node-flow workflow builder (7 node types + DeviceAction), execution panel (start/pause/abort), live ScalarTrendChart + SpectralChart + SafetyPanel, DeviceManualControl, workflow library (save/load/duplicate via tauri-plugin-fs)
- **Agent REST + gRPC API**: WorkflowEndpoints, RunspaceEndpoints, DeviceEndpoints (with capability invocation), AgentGrpcService (SubmitExperiment, GetRunspaceStatus, AbortRunspace), durable Cleipnir PostgreSQL runspace store
- **Site Server**: FumehoodEndpoints (GET /api/fumehoods), ExperimentEndpoints, CampaignEndpoints, TrendEndpoints, DeviceEndpoints, WebSocket trend bridge, GrpcAgentProxy
- **Site dashboard** (helix-dashboard): SiteOverviewPage (live API fetch), CampaignsPage, HistoryPage, live trend WebSocket
- **Test coverage**: 1,262 .NET xUnit tests, 76 helix-desktop Vitest tests (all in CI), 32 helix-dashboard Vitest tests (all in CI)
- **Docusaurus documentation**: developer architecture guide, adapter authoring guide, REST/gRPC API reference, workflow authoring guide, deployment guide
- **Docker Compose** full stack (6 services: Agent, SiteServer, Recorder, Postgres, Mosquitto, MinIO)
- **Setup scripts**: setup.ps1 (Windows), setup.sh (Linux), preflight.ps1/preflight.sh, systemd units, Windows service install scripts

### Known Gaps (deferred to v0.2)

- CORS policy not configured in SiteServer (production deployment requires AddCors/UseCors)
- SciChart integration — placeholder wrapper; recharts used in production
- DeviceStatusEvent emits full capability key as deviceId (e.g. "simulator.SetFlow") instead of adapter prefix
- WorkflowBuilder.tsx async useState cleanup (functional but stale closure risk)
- Phases 1–3 have SUMMARY-only verification (no formal VERIFICATION.md)
- Human verification items outstanding: Tauri runtime UI tests (5), Docusaurus rendered site (2), docker-compose live run (4), systemd on Debian 12 (1)

---

## Architecture

```
[helix-desktop (Tauri)]          [helix-dashboard (React/Vite)]
        |                                    |
  Agent REST/gRPC                     SiteServer REST
  localhost:5080                      localhost:5090
        |                                    |
  Helix.Orchestration.Agent      Helix.Orchestration.SiteServer
  - WorkflowExecutor             - CampaignManager
  - DurableRunspace (Cleipnir)   - IAgentProxy (gRPC/sim)
  - CapabilityRegistry           - FumehoodEndpoints
  - DeviceAdapterBase drivers    - WebSocket trend bridge
  - AgentGrpcService             - PostgreSQL repositories
        |
  Helix.Orchestration.Devices
  - 48 device adapters (serial, TCP, OPC UA, MQTT, Modbus)
  - Transport seam (ISerialTransport, ITcpTransport, etc.)
        |
  Helix.Orchestration.Recorder (Worker Service)
  - MQTT ingestion, JSONL batch flushing, MinIO storage
        |
  [PostgreSQL] [MinIO] [Mosquitto MQTT]
```

## Constraints

- Safety engine evaluates locally — never depends on network connectivity
- Standalone mode: Tauri UI connects directly to Agent without requiring Site Server
- Cross-platform: Tauri app runs on Windows 11 and Linux (Debian/Ubuntu)
- Protocol fidelity: device drivers exactly match legacy POP.DeviceCore command vocabularies
- Durable execution: runspace state survives Agent process restart (PostgreSQL-backed Cleipnir)
- No breaking changes to WSL contract: WSL JSON schema changes must be additive

## Key Decisions (v0.1)

| Decision | Rationale |
|----------|-----------|
| DeviceAdapterBase composition model | Enables transport seam testing without real hardware |
| Tauri + Rete.js for local UI | Cross-platform desktop; Rete.js graph model matches WSL |
| Cleipnir PostgreSQL for durable execution | Runspace state survives Agent restart |
| OPC Foundation library | Replaced Opc.UaFx.Advanced commercial dependency |
| tauri-plugin-fs for workflow library | Built-in persistence, type-safe, no custom Rust needed |
| recharts for production charts | SciChart wrapper placeholder; full integration deferred |
| BaseSchemes as NodeEditor type parameter | ClassicPreset.Schemes does not exist in rete@2.0.6 |
| vi.hoisted() + vi.mock("recharts") | SVG renderer errors in jsdom require hoisted module mock |
| depends_on uses service names not container_name | Docker Compose requirement |
| DeviceRegistry populated via CapabilityRegistry factory lambda | Adapter prefix split on "." |

## Out of Scope (v0.1)

- Cloud orchestration across multiple sites (architecture supports it)
- Mobile app
- AI/ML optimisation loop integration (Campaign Manager seam exists)
- SiLA 2 protocol adapters (transport seam exists)
- ELN/LIMS connector (WSL authoring seam exists)
- Multi-tenant Site Server

---

## Next Milestone (v0.2)

Start planning with `/gsd:new-milestone` to define requirements and roadmap.

Candidates:
- Production hardening: CORS, SciChart, DeviceStatusEvent fix
- Human verification pass for outstanding live-stack tests
- Cloud multi-site orchestration (Phase 11+)
- SiLA 2 adapters, ELN/LIMS connector

---

*Last updated: 2026-06-03 after v0.1 milestone close*

