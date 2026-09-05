---
gsd_state_version: 1.0
milestone: v0.2
milestone_name: Phase Overview
current_phase: 12
status: ready_to_execute
last_updated: "2026-09-05T15:39:40.734Z"
state_head: c3c5b1c48082e5c31e43dccebe1a76a5a0c363b4
progress:
  total_phases: 10
  completed_phases: 0
  total_plans: 10
  completed_plans: 7
  percent: 0
current_phase_name: Experiment as Top-Level Scope
---

# Project State

**Last updated:** 2026-09-05
**Current phase:** 12
**Workflow:** YOLO mode, fine granularity, parallel execution enabled

---

## Project Reference

See: .planning/PROJECT.md (updated 2026-05-26)

**Core value:** A scientist can build a workflow, connect real lab hardware, execute an experiment, and see the results without writing code.

**Current focus:** Phase 12 — Experiment as Top-Level Scope

---

## Status

| Artifact | Status |
|----------|--------|
| PROJECT.md | Written |
| REQUIREMENTS.md | Written (A-G, 49 requirements) |
| ROADMAP.md | Written (10 phases) |
| config.json | Written |
| Codebase map | Written (.planning/codebase/, 7 documents) |
| Phase 1 PLAN.md | Executed -- all 8 tasks complete |
| Phase 2 PLAN.md | Executed -- all 7 tasks complete |
| Phase 3 PLAN.md | Executed -- all 5 tasks complete |
| Phase 4 PLAN.md | Executed -- all tasks complete |
| Phase 5 Plan 01 | Executed -- scaffold + types complete |
| Phase 5 Plan 02 | Executed -- useAgentHealth + ConnectionConfig + DeviceRegistry + App.tsx + Rust commands complete |
| Phase 5 Plan 03 | Executed -- tauri-build CI job added to .github/workflows/ci.yml |
| Phase 6 Plan 01 | Executed -- WorkflowEndpoints, RunspaceEndpoints, DeviceEndpoints + Program.cs wiring complete |
| Phase 6 Plan 04 | Executed -- WorkflowBuilder Rete.js canvas + 7 node types + useAgentWebSocket hook complete |
| Phase 6 Plan 05 | Executed -- ExecutionPanel + compile-then-start flow + 6 tests + App.tsx wired complete |
| Phase 6 Plan 06 | Executed -- ScalarTrendChart + SpectralChart (recharts) + SafetyPanel + 9 tests + App.tsx complete |
| Phase 6 Plan 07 | Executed -- tauri-plugin-fs 3-part wiring + workflowLibrary save/load/list/duplicate + 5 tests complete
| Phase 6 Plan 08 | Executed -- WorkflowBuilder save/load UI + App.tsx final wiring + 47/47 tests + build green complete |
| Phase 6 Plan 09 | Executed -- ContextMenuPlugin wired (C2 BLOCKER closed) + capability-picker onChange (C3 WARNING closed) + 48/48 tests complete |
| Phase 6 Plan 10 | Executed -- SpectralChart wired to UV:/MIR: TrendValue prefix (C5 BLOCKER closed) + SafetyEvent CurrentValue (C6 WARNING closed) + 49/49 tests complete |
| Phase 6.1 Plan 01 | Executed -- CapabilityDescriptor types, DeviceStatusEvent + ManualActionEvent records, POST /devices/{id}/execute, AgentService DeviceHealthPollLoopAsync complete |
| Phase 6.1 Plan 02 | Executed -- TS CapabilityDescriptor types, device-status WebSocket dispatch, DeviceManualControl panel (53/53 tests), SciChart wrapper placeholder complete |
| Phase 6.2 Plan 01 | Executed -- DeviceActionNodeComponent (8th ReactFlow node type), PORT_COLOURS, DevicePalette, handleDrop, addDeviceActionNode, exportWsl Step mapping (62/62 tests) complete |
| Phase 6.2 Plan 02 | Executed -- useNodeExecutionState hook, NodeTimingFooter on all 8 node types, WorkflowBuilder live execution wiring, EstimatedDurationMs C# + TS types (76/76 tests) complete |
| Phase 6.3 Plan 01 | Executed -- DeviceEndpoints.cs execute-404 fix (C7/C9), CapabilityRegistryÃ¢â€ â€™DeviceRegistry bridge in Program.cs (C4/C8), 6 new xUnit tests (588 pass) |
| Phase 6.3 Plan 02 | Executed -- DeviceRegistry.tsx /healthÃ¢â€ â€™/devices fix, mapDevicesToAdapters, 5 DeviceRegistry tests pass (C1) |

---

## Decisions

- Use Rust tonic proxy (not gRPC-Web) for Phase 5 gRPC calls -- no Agent changes required
- TypeScript strict mode mirrors C# Nullable:enable
- helix-desktop/ lives in monorepo root alongside .NET projects
- DEFAULT_CONNECTION: localhost:5080 (from AgentProxyOptions.cs)
- tonic = "0.14" with no explicit prost (transitive resolution avoids version conflict)
- tsconfig.node.json uses composite:true without allowImportingTsExtensions (incompatible)
- LazyStore (sync constructor) used instead of Store (private constructor) in @tauri-apps/plugin-store v2.4.3
- vi.useFakeTimers() removed from hook tests -- conflicts with @testing-library/react waitFor
- vi.hoisted() required for vitest constructor mocks referencing module-level variables
- DeviceRegistry populated via factory lambda from CapabilityRegistry.GetAllCapabilityNames() in Program.cs -- no longer empty singleton. Bridge extracts unique adapter prefixes (split on "."), registers one DeviceDescriptor per prefix.
- IRunspaceEventEmitter injected as nullable in RunspaceEndpoints (emitter?.Publish) for wave-ordering safety
- Use BaseSchemes (not ClassicPreset.Schemes) as NodeEditor type parameter -- ClassicPreset.Schemes does not exist in rete@2.0.6
- ClassicScheme + ReactArea2D imported directly from rete-react-plugin (not as ReactPlugin.* namespace)
- findLast (ES2023) not available in ES2020 target -- use array[array.length - 1] pattern
- currentWorkflow state deferred to Plan 08 where WorkflowBuilder.onExport is wired (noUnusedLocals constraint)
- vi.hoisted() + vi.mock("recharts") required for all recharts tests to avoid SVG renderer errors in jsdom
- isAnimationActive={false} on Line and Area components prevents animation-related test failures in jsdom
- SpectralChart mounted with empty arrays in App.tsx -- spectral events not yet emitted by Agent WebSocket; full wiring in Plan 08
- sanitizeName throws when no alphanumeric chars remain after replacement (not just empty string) -- "!!!" becomes "___" which is truthy but has no letters/digits
- vi.clearAllMocks() required alongside vi.restoreAllMocks() in afterEach to reset call history between tests
- Phase 6 scope for handleLoad: fires onExport with loaded doc; editor re-population deferred to Phase 7
- Python used to write files with JSX template literals to avoid PowerShell backtick escaping
- JSON.stringify uses 2-space indent (pretty-print) for human-readable workflow files; test assertions must use "key": "value" format (with space after colon)
- ContextMenuPlugin registered via area.use() (not editor.use()) -- matches rete-area-plugin pattern for area-level signals
- handleCapabilityChange targets first StepNode by instanceof -- no public selected-node API in rete-area-plugin v2.0.6 without selectableNodes extension
- rete-context-menu-plugin test mock uses regular function (not arrow) to be new-able -- consistent with NodeEditorMock/AreaPluginMock pattern
- SpectralChart data sourced from UV:/MIR: prefixed TrendValue frames (no new event type needed)
- Wavelength parsed from trendName suffix with parseInt(..., 10) + isNaN guard; invalid wavelengths silently discarded
- SafetyEvent.CurrentValue added as optional C# record parameter with default null (backward-compatible positional call sites)
- IRunspaceEventEmitter namespace is Helix.Orchestration.Agent.Events (not Streaming) -- confirmed by build
- CapabilityRegistry.GetAllCapabilityNames() used to enumerate adapters in DeviceHealthPollLoopAsync (no per-adapter registry needed)
- SciChart wrapper layer (charts/index.ts) is pure re-export in Step 1 -- no npm install scichart, no App.tsx changes
- Docusaurus 3.10.1 docs site: organizationName=PyPete (from git remote), @docusaurus/theme-mermaid installed separately (not bundled in preset-classic)
- markdown.hooks.onBrokenMarkdownLinks used instead of deprecated top-level onBrokenMarkdownLinks (Docusaurus v4 migration)
- Docusaurus home page index.tsx updated to link to /docs/developer/intro + /docs/operator/intro (deleted /docs/intro)
- MockWebSocket class pattern (stub global.WebSocket) used in WorkflowBuilder.test.tsx for execution state test -- adding vi.mock() calls to this file crashes the Vitest worker with V8 address space exhaustion (4 large module mocks already present)
- NodeTimingFooter execution state tests placed in HelixNodes.test.tsx (new) to avoid WorkflowBuilder.test.tsx V8 crash
- docusaurus-plugin-redoc v2.5.0 cannot be used standalone -- requires redocusaurus preset for @theme/ApiDoc; fallback to endpoint summary table in rest-api.md (F4 satisfied)
- MDX in Docusaurus parses {id} in table cells as JSX expressions -- use HTML entity escaping (&#123;id&#125;) for path parameters
- systemd units use Type=simple (not notify) -- Agent does not call UseSystemd() so Type=notify would timeout waiting for readiness signal
- CHANGE_ME placeholders in systemd Environment= lines with comment directing to EnvironmentFile=/etc/helix/agent-secrets (mode 0600)
- preflight.sh uses nc for TCP checks with fallback apt-get install netcat-openbsd if missing
- depends_on uses service names (mosquitto/postgres/minio) not container names (helix-*) -- Docker Compose depends_on requires service names; container_name provides DNS alias but cannot be used in depends_on
- AgentProxy__Mode=grpc set in SiteServer environment (not real) -- code checks StringComparison.OrdinalIgnoreCase for "grpc"
- Recorder uses dotnet/runtime:10.0 not aspnet:10.0 -- Worker-only service has no HTTP listener, smaller image
- dotnet-tools.json command name for swashbuckle.aspnetcore.cli is "swagger" (not "dotnet-swagger")
- Phase 9 complete (2026-06-02): Docusaurus 3 site at docs-site/ with 9 content pages; npm run build exits 0; all F1-F5 requirements satisfied; human checkpoint approved

---

## Next Steps

Phase 11.1 complete (2026-09-05). All 8 backend execution GAPs closed. Backend is now a standalone service with gRPC agent control, in-memory trend storage, cooperative runspace pause/resume, WSL document compilation, campaign persistence, and live device listing.

Run /gsd-discuss-phase 12 or /gsd-plan-phase 12 to start Phase 12 (Experiment as Top-Level Scope).

## Phase History

| Phase | Status | Completed |
|-------|--------|-----------|
| (pre-existing Phases 1-14) | Complete | See Helix.Orchestration/docs/ |
| 1. Technical Debt Resolution | Complete | 2026-05-26 |
| 2. VapourtecR4 Reference Driver | Complete | 2026-05-26 |
| 3. Remaining Device Drivers | Complete | 2026-05-26 |
| 4. Integration Test Coverage | Complete | 2026-05-27 |
| 5. Tauri Desktop App Foundation | Complete | 2026-05-28 |
| 6. Tauri Desktop App - Workflow Builder + Execution | Complete | 2026-05-30 |
| 6.1. Desktop App Polish -- Device Controls, Status Events & Chart Foundation | Complete | 2026-05-30 |
| 6.2. Workflow Builder - Device Palette, Hybrid Ports and Live Execution | Complete | 2026-05-30 |
| 6.3. Close Gap: Device Integration Bugs (C1, C4, C7-C9) | Complete | 2026-06-03 |
| 6.4. Close Gap: Remaining Audit Items (D5, D6, E1-E4, G2) | Complete | 2026-06-03 |
| 7. Site-Level React Dashboard | Complete | 2026-05-31 |
| 8. Test Coverage | Complete | 2026-05-31 |
| 9. Docusaurus Documentation | Complete | 2026-06-02 |
| 10. Deployment + Setup Scripts | Complete | 2026-06-03 |
| **v0.1 MILESTONE** | **SHIPPED** | **2026-06-03** |
| 11. Ribbon + Docking Workspace Shell | Not started | -- |
| 11.1. Remove UI / Backend Focus (GAPs 1-8) | **Complete** | **2026-09-05** |
| 12. Experiment as Top-Level Scope | Not started | -- |
| 13. Three Workflow Modes | Not started | -- |
| 14. ReactFlow Node Canvas | Not started | -- |
| 15. SciChart Integration | Not started | -- |
| 16. Device Autodiscovery + Developer Debugger | Not started | -- |
| 17. Real-Time Trend System | Not started | -- |
| 18. Python API | Not started | -- |
| 19. Production Hardening | Not started | -- |

---

## Performance Metrics

| Phase | Plan | Duration | Tasks | Files |
|-------|------|----------|-------|-------|
| phase-9 | 01 | ~8 min | 2/2 | 32 |
| phase-5 | 01 | ~30 min | 2/2 | 18 |
| phase-5 | 02 | ~20 min | 4/4 | 11 |
| phase-5 | 03 | ~5 min | 1/1 | 1 |
| phase-6 | 01 | ~5 min | 2/2 | 4 |
| phase-6 | 04 | ~12 min | 2/2 | 9 |
| phase-6 | 05 | ~15 min | 2/2 | 3 |
| phase-6 | 06 | ~12 min | 2/2 | 7 |
| phase-6 | 07 | ~10 min | 2/2 | 9
| phase-6 | 08 | ~15 min | 2/2 | 3 |
| phase-6 | 09 | ~15 min | 2/2 | 3 |
| phase-6 | 10 | ~4 min | 2/2 | 5 |
| phase-6.1 | 01 | ~15 min | 3/3 | 4 |
| phase-6.1 | 02 | ~20 min | 2/2 | 8 |
| driver-port | 03 | ~90 min | 8/8 | 27 |
| phase-6.2 | 01 | ~15 min | 2/2 | 6 |
| phase-6.2 | 02 | ~25 min | 2/2 | 8 |
| phase-9 | 03 | ~25 min | 2/2 | 7 |
| phase-9 | 04 | ~4 min | 2/2 | 5 |
| phase-9 | 05 | ~5 min | 3/3 | 1 |
| phase-10 | 01 | ~5 min | 2/2 | 4 |
| phase-10 | 02 | ~2 min | 2/2 | 2 |
| phase-10 | 03 | ~4 min | 2/2 | 8 |
| phase-6.4 | 01 | ~10 min | 3/3 | 4 |
| phase-6.4 | 02 | ~12 min | 2/2 | 5 |

---

## Key Files

- `.planning/PROJECT.md` -- project context, requirements, constraints, decisions
- `.planning/REQUIREMENTS.md` -- detailed requirements A1-G6
- `.planning/ROADMAP.md` -- 10-phase roadmap
- `.planning/config.json` -- workflow preferences
- `.planning/codebase/` -- codebase map (7 documents, produced 2026-05-26)
- `.planning/phases/phase-5/01-SUMMARY.md` -- Phase 5 Plan 01 complete summary
- `.planning/phases/phase-5/02-SUMMARY.md` -- Phase 5 Plan 02 complete summary
- `.planning/phases/phase-6/06-04-SUMMARY.md` -- Phase 6 Plan 04 complete summary
- `.planning/phases/phase-6/06-05-SUMMARY.md` -- Phase 6 Plan 05 complete summary
- `.planning/phases/phase-6/06-06-SUMMARY.md` -- Phase 6 Plan 06 complete summary
- `.planning/phases/phase-6/06-07-SUMMARY.md` -- Phase 6 Plan 07 complete summary
- `helix-desktop/src/lib/workflowLibrary.ts` -- Workflow save/load/list/duplicate via tauri-plugin-fs
- `helix-desktop/` -- Tauri v2 desktop application scaffold
- `helix-desktop/src/types/agent.ts` -- TypeScript Agent interface contracts (includes WSL + WebSocket frame types)
- `helix-desktop/src-tauri/Cargo.toml` -- Rust dependencies (tonic 0.14, tauri 2.x)
- `helix-desktop/src/hooks/useAgentHealth.ts` -- REST health polling hook
- `helix-desktop/src/hooks/useAgentWebSocket.ts` -- WebSocket hook dispatching RunspaceEvent/TrendValue/SafetyEvent
- `helix-desktop/src/components/ConnectionConfig.tsx` -- agent connection form with LazyStore
- `helix-desktop/src/components/DeviceRegistry.tsx` -- adapter list from /health endpoint
- `helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx` -- Rete.js canvas + export button
- `helix-desktop/src/components/WorkflowBuilder/nodes/HelixNodes.ts` -- 7 Rete.js node types + exportToWsl
- `helix-desktop/src/components/ExecutionPanel/ExecutionPanel.tsx` -- execution control panel with REST compile+start flow
- `helix-desktop/src/App.tsx` -- composition root
- `helix-desktop/src-tauri/src/commands/connection.rs` -- load_config/save_config Tauri commands
- `Helix.Orchestration.Core/Capabilities/CapabilityDescriptor.cs` -- CapabilityDescriptor schema types (Phase 6.1)
- `helix-desktop/src/components/DeviceManualControl/DeviceManualControl.tsx` -- schema-driven manual control panel (Phase 6.1)
- `helix-desktop/src/components/charts/index.ts` -- SciChart wrapper re-export layer (Phase 6.1)

## Accumulated Context

### Roadmap Evolution

- Phase 11.1 inserted after Phase 11: remove ui components and focus on backend (remove tauri etc) (URGENT)
