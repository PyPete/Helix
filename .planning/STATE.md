---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
current_phase: Phase driver-port -- PopNextGen Driver Port (3 of 3 plans complete)
status: complete
last_updated: "2026-06-01T00:00:00.000Z"
progress:
  total_phases: 11
  completed_phases: 4
  total_plans: 19
  completed_plans: 20
  percent: 36
---

# Project State

**Last updated:** 2026-05-30
**Current phase:** Phase driver-port -- PopNextGen Driver Port (3 of 3 plans complete, VERIFIED)
**Workflow:** YOLO mode, fine granularity, parallel execution enabled

---

## Project Reference

See: .planning/PROJECT.md (updated 2026-05-26)

**Core value:** A scientist can build a workflow, connect real lab hardware, execute an experiment, and see the results without writing code.

**Current focus:** Phase 6.1 complete ï¿½ Phase 7 (Site-Level React Dashboard) is next

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
- DeviceRegistry registered as singleton in Agent DI (AddMigratedDeviceAdapters does not register it)
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

---

## Next Steps

Phase 6.1 Plan 02 complete. Phase 6.1 all 2 plans complete. Phase 7 (Site-Level React Dashboard) is next.

driver-port Plan 03 complete (2026-06-01). 8 special/facade drivers ported: AtlasReactionSystem, Crystalline, IfmMasterController, OceanViewDriver, OceanViewUv, OceanViewNir, PicoTc08LowLevelWrapper, PicoTc08. 262 tests passing. NetOceanDirect and usbtc08.dll still need to be added for production runtime.

---

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
| 6.1. Desktop App Polish ï¿½ Device Controls, Status Events & Chart Foundation | Complete | 2026-05-30 |

---

## Performance Metrics

| Phase | Plan | Duration | Tasks | Files |
|-------|------|----------|-------|-------|
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


