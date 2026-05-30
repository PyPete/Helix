---
phase: 06-tauri-workflow-builder-execution
verified: 2026-05-30T08:15:00Z
status: gaps_found
score: 10/14 must-haves verified
overrides_applied: 0
gaps:
  - truth: "A scientist can add nodes via the context menu and connect them with edges"
    status: failed
    reason: "rete-context-menu-plugin@2.0.6 is installed in package.json but never imported or wired into useReteEditor.ts. The canvas renders as an empty non-interactive viewport. No addNode() call exists anywhere in src/."
    artifacts:
      - path: "helix-desktop/src/components/WorkflowBuilder/useReteEditor.ts"
        issue: "Does not import or register rete-context-menu-plugin; no addNode() call"
      - path: "helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx"
        issue: "No programmatic node-seeding; canvas starts empty with no interactive add-node mechanism"
    missing:
      - "Import ContextMenuPlugin from rete-context-menu-plugin and register via area.use(contextMenu)"
      - "Configure context menu items for all 7 HelixNode types so right-click creates nodes"

  - truth: "The capability picker calls GET /devices and binds capability names to Step node inputs"
    status: partial
    reason: "GET /devices is called on mount. A capability-picker select renders with capabilities from the response. However, the select element has NO onChange handler -- selecting a capability does nothing and no value flows to any StepNode capability InputControl."
    artifacts:
      - path: "helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx"
        issue: "capability-picker select has no onChange handler; selection state not stored or applied to any node"
    missing:
      - "Add state for selectedCapability; add onChange to the picker; on change update the selected node capability control value"

  - truth: "SpectralChart renders UV/MIR overlay with data from WebSocket TrendValue frames"
    status: failed
    reason: "App.tsx mounts SpectralChart with hardcoded uvData=[] and mirData=[]. TrendValueEvent is scalar-only. There is no data path from the Agent WebSocket to SpectralChart."
    artifacts:
      - path: "helix-desktop/src/App.tsx"
        issue: "SpectralChart mounted with empty arrays (line 46); no spectral data derivation from trendValues"
      - path: "Helix.Orchestration.Agent/Events/RunspaceEvent.cs"
        issue: "TrendValueEvent record is scalar-only; no SpectralDataEvent type"
    missing:
      - "Add a SpectralDataEvent to the Agent event hierarchy, or handle spectral-type TrendValue frames by trendName prefix to derive uvData/mirData in App.tsx"

  - truth: "Safety panel shows at least one interlock rule with its current value and triggered/safe state"
    status: partial
    reason: "SafetyPanel shows description, severity, and triggered/safe state. C6 requires current values but neither SafetyEvent (agent.ts) nor the Agent SafetyEvent record includes a currentValue field."
    artifacts:
      - path: "helix-desktop/src/components/SafetyPanel/SafetyPanel.tsx"
        issue: "No currentValue display because SafetyEvent has no such field"
      - path: "Helix.Orchestration.Agent/Events/RunspaceEvent.cs"
        issue: "SafetyEvent record has no CurrentValue property"
    missing:
      - "Add CurrentValue (double?) to Agent SafetyEvent record and frontend SafetyEvent interface"
      - "Display currentValue in SafetyPanel list item alongside description"

human_verification:
  - test: "Verify Rete.js canvas renders correctly at runtime"
    expected: "The workflow builder shows a canvas area without JavaScript console errors"
    why_human: "Cannot test Tauri renderer output programmatically"
  - test: "Verify WebSocket /ws streams events during live execution"
    expected: "After starting a runspace, WebSocket client receives runspace-started and trend-value frames within 10 seconds"
    why_human: "Requires a running Agent process and active experiment"
  - test: "Verify live ScalarTrendChart updates during experiment"
    expected: "The Temperature LineChart updates visibly as TrendValue frames arrive from the Agent WebSocket"
    why_human: "Requires a running Agent with VapourtecR4 or SimulatedTrendProvider emitting trend data"
  - test: "Verify safety panel shows interlock rules from running Agent"
    expected: "SafetyPanel shows at least one rule entry after SafetyPollLoopAsync evaluates and publishes SafetyEvents"
    why_human: "Requires a running Agent with ISafetyEngine configured with registered rules"
  - test: "Verify workflow save/load round-trip in Tauri runtime"
    expected: "Click Save, reload app, click Load -- saved workflow appears in dropdown and loads correctly"
    why_human: "Tauri fs plugin calls are no-ops in browser context; requires actual Tauri desktop runtime"
  - test: "Verify POST /workflows/compile returns HTTP 200 for valid WSL"
    expected: "Sending a well-formed WslDocument to POST /workflows/compile returns 200 with RunspaceDefinition body"
    why_human: "Requires running Agent process; endpoint verified by code but not by live request"
---


# Phase 6: Tauri Desktop App -- Workflow Builder + Execution -- Verification Report

# Phase 6: Tauri Desktop App -- Workflow Builder + Execution -- Verification Report

**Phase Goal:** A scientist can visually build a workflow, submit it for compilation and execution, monitor live telemetry, and see safety interlock status -- all within the desktop app connected directly to the Agent. The Agent gains its full REST control API, WebSocket streaming layer, and the remaining WSL compiler features required to make this loop work end-to-end.

**Verified:** 2026-05-30T08:15:00Z
**Status:** GAPS_FOUND
**Re-verification:** No -- initial verification

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | POST /workflows/compile returns 200 or 422 | VERIFIED | WorkflowEndpoints.cs lines 33-38 |
| 2 | POST /runspaces accepts RunspaceDefinition, returns runspaceId | VERIFIED | RunspaceEndpoints.cs lines 32-35 |
| 3 | GET /runspaces/{id} returns status; unknown ID returns 404 | VERIFIED | RunspaceEndpoints.cs lines 55-62 |
| 4 | POST /runspaces/{id}/pause, /resume, /abort return 200 | VERIFIED | RunspaceEndpoints.cs lines 80-141 |
| 5 | GET /devices returns array from CapabilityRegistry | VERIFIED | DeviceEndpoints.cs lines 26-28 |
| 6 | IRunspaceEventEmitter + 9-type RunspaceEvent hierarchy registered | VERIFIED | Events/ has all 3 files; Program.cs line 101 |
| 7 | AgentService hosts WebSocket /ws and broadcasts events | VERIFIED | AgentService.cs: HandleClientAsync, BroadcastLoopAsync, TrendForwardLoopAsync, SafetyPollLoopAsync |
| 8 | WslCompiler.Compile() has all 4 transforms (safety, loop, branch, parallel) | VERIFIED | WslCompiler.cs lines 57-61; 19 WslCompiler tests pass |
| 9 | A scientist can add nodes via the context menu and connect them with edges | FAILED | rete-context-menu-plugin installed but never imported; canvas stays empty |
| 10 | The capability picker calls GET /devices and binds capability names to Step node inputs | PARTIAL | GET /devices called on mount; capability-picker has NO onChange; no binding to node control |
| 11 | ExecutionPanel Start calls POST /compile then POST /runspaces; 422 blocks | VERIFIED | ExecutionPanel.tsx lines 37-83 |
| 12 | SpectralChart renders UV/MIR overlay with WebSocket data | FAILED | App.tsx line 46: uvData=[] mirData=[] hardcoded; no spectral event type |
| 13 | Safety panel shows interlock rules with current values and triggered/safe state | PARTIAL | SafetyPanel shows description/severity/triggered; SafetyEvent has no currentValue field |
| 14 | Workflow save/load/duplicate via Tauri fs plugin works | VERIFIED | workflowLibrary.ts 4 functions; all 3 Tauri fs plugin wiring points confirmed |

**Score:** 10/14 truths verified (2 FAILED, 2 PARTIAL)

---
### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| Helix.Orchestration.Agent/Api/WorkflowEndpoints.cs | POST /workflows/compile | VERIFIED | MapWorkflowEndpoints() with 200/422 logic |
| Helix.Orchestration.Agent/Api/RunspaceEndpoints.cs | POST/GET/pause/resume/abort | VERIFIED | MapRunspaceEndpoints() with all 5 verbs |
| Helix.Orchestration.Agent/Api/DeviceEndpoints.cs | GET /devices and /capabilities | VERIFIED | MapDeviceEndpoints() wired to CapabilityRegistry |
| Helix.Orchestration.Agent/Program.cs | Wires all endpoints + UseWebSockets | VERIFIED | Lines 137-152 confirmed |
| Helix.Orchestration.Agent/Events/IRunspaceEventEmitter.cs | Event bus interface | VERIFIED | Publish() + ChannelReader Events |
| Helix.Orchestration.Agent/Events/RunspaceEvent.cs | 9 sealed record types | VERIFIED | All 9 records present |
| Helix.Orchestration.Agent/Events/RunspaceEventEmitter.cs | Channel-backed implementation | VERIFIED | Channel.CreateUnbounded + TryWrite |
| Helix.Orchestration.Agent/AgentService.cs | Promoted hosted service | VERIFIED | 4 async loop methods confirmed |
| Helix.Orchestration.Core/WSL/WslCompiler.cs | 4 transform methods | VERIFIED | All 4 methods present and called from Compile() |
| tests/Helix.Orchestration.Core.Tests/WSL/WslCompilerTests.cs | 5+ transform tests | VERIFIED | 19 tests all passing |
| helix-desktop/src/components/WorkflowBuilder/nodes/HelixNodes.ts | 7 node classes + exportToWsl | VERIFIED | 7 classes confirmed |
| helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx | Rete.js canvas wrapper | PARTIAL | Canvas renders; GET /devices wired; capability picker unbound; NO context menu |
| helix-desktop/src/components/WorkflowBuilder/useReteEditor.ts | Editor hook | PARTIAL | Editor/Connection/Render wired; ContextMenuPlugin missing |
| helix-desktop/src/hooks/useAgentWebSocket.ts | WebSocket hook | VERIFIED | Frame dispatch by type field confirmed |
| helix-desktop/src/components/ExecutionPanel/ExecutionPanel.tsx | Execution control panel | VERIFIED | compile-then-submit flow + all 4 controls |
| helix-desktop/src/components/TrendCharts/ScalarTrendChart.tsx | Scalar trend chart | VERIFIED | recharts LineChart, isAnimationActive=false |
| helix-desktop/src/components/TrendCharts/SpectralChart.tsx | UV/MIR spectral chart | STUB | Component exists but always receives empty arrays; no data source |
| helix-desktop/src/components/SafetyPanel/SafetyPanel.tsx | Interlock rule list | PARTIAL | Rule list + triggered/safe; missing currentValue |
| helix-desktop/src/lib/workflowLibrary.ts | Save/load/list/duplicate | VERIFIED | 4 functions + sanitizeName + Tauri fs plugin |
| helix-desktop/src-tauri/Cargo.toml | tauri-plugin-fs dep | VERIFIED | tauri-plugin-fs = 2 at line 27 |
| helix-desktop/src-tauri/src/lib.rs | fs plugin registered | VERIFIED | plugin tauri_plugin_fs init at line 11 |
| helix-desktop/src-tauri/capabilities/default.json | fs permissions | VERIFIED | All 4 fs:allow-* scoped to APPDATA/workflows/* |
| helix-desktop/src/App.tsx | Full composition root | PARTIAL | 6 components mounted; SpectralChart empty; WorkflowBuilder -> ExecutionPanel wired |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|----|--------|---------|
| Program.cs | WorkflowEndpoints.cs | app.MapWorkflowEndpoints() | WIRED | Line 139 |
| Program.cs | RunspaceEndpoints.cs | app.MapRunspaceEndpoints() | WIRED | Line 140 |
| Program.cs | DeviceEndpoints.cs | app.MapDeviceEndpoints() | WIRED | Line 141 |
| WorkflowEndpoints.cs | WslCompiler.cs | compiler.Compile(doc, registry, safety) | WIRED | Line 33 |
| DeviceEndpoints.cs | CapabilityRegistry.cs | registry.GetAllCapabilityNames() | WIRED | Line 26 |
| AgentService.cs | IRunspaceEventEmitter | constructor injection | WIRED | Line 20 |
| AgentService.cs | TrendAggregator | TrendForwardLoopAsync reads ScalarValues | WIRED | Lines 144-167 |
| Program.cs | WebSocket /ws endpoint | app.Use + HandleClientAsync | WIRED | Lines 147-152 |
| App.tsx | useAgentWebSocket | hook destructure | WIRED | Line 19 |
| App.tsx | WorkflowBuilder | onExport={setCurrentWorkflow} | WIRED | Line 38 |
| App.tsx | ExecutionPanel | definition={currentWorkflow} | WIRED | Line 41 |
| App.tsx | ScalarTrendChart | temperatureData from trendValues filter | WIRED | Lines 23-27 |
| App.tsx | SafetyPanel | safetyEvents={safetyEvents} | WIRED | Line 47 |
| App.tsx | SpectralChart | uvData=[] mirData=[] | NOT_WIRED | Hardcoded empty; no spectral data source |
| WorkflowBuilder.tsx | capability-picker | GET /devices fetch on mount | PARTIAL | Fetch wired; onChange binding to node absent |
| WorkflowBuilder.tsx | rete-context-menu-plugin | useReteEditor integration | NOT_WIRED | Package installed, never imported |
| WorkflowBuilder.tsx | workflowLibrary.ts | saveWorkflow/loadWorkflow/listWorkflows | WIRED | Lines 5, 56, 62, 67 |

---

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|--------------|--------|--------------------|---------| 
| ScalarTrendChart.tsx | data prop | trendValues.filter(temperature) from useAgentWebSocket | YES when Agent emits | FLOWING |
| SpectralChart.tsx | uvData/mirData props | hardcoded [] in App.tsx | NO | DISCONNECTED |
| SafetyPanel.tsx | safetyEvents prop | safetyEvents from useAgentWebSocket | YES when SafetyPollLoopAsync emits | FLOWING |
| ExecutionPanel.tsx | definition prop | currentWorkflow from WorkflowBuilder.onExport | YES after user exports | FLOWING |
| useAgentWebSocket.ts | frames | WebSocket /ws from Agent | YES when Agent running | FLOWING |
| capability-picker select | capabilities options | devices state from GET /devices | YES | HOLLOW_PROP -- no onChange binding to node |

---

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| All 47 helix-desktop UI tests pass | npm run test -- --run | 10 files, 47 tests, 0 failures | PASS |
| WslCompiler 19 tests pass | dotnet test --filter WslCompiler | 19 passed, 0 failed | PASS |
| Full .NET test suite | dotnet test Helix.Orchestration.slnx --no-build | 269 passed (6 projects), 0 failed | PASS |
| dotnet build solution | dotnet build Helix.Orchestration.slnx -c Debug | Build succeeded, 0 errors | PASS |

---

### Requirements Coverage

| Requirement | Source Plan(s) | Description | Status | Evidence |
|-------------|---------------|-------------|--------|----------|
| C2 | 06-01, 06-04, 06-08 | Rete.js workflow builder: 7 node types, WSL JSON export | PARTIAL | 7 node classes + exportToWsl verified; context menu NOT wired |
| C3 | 06-01, 06-04 | Device capability picker: shows capabilities, binds to node inputs | PARTIAL | GET /devices and dropdown verified; binding to node inputs NOT implemented |
| C4 | 06-01, 06-02, 06-05 | Execution panel: start/pause/abort; step-level status | SATISFIED | ExecutionPanel verified; all 4 REST calls wired; RunspaceEvent stream displayed |
| C5 | 06-06 | Live trend charts: scalar, spectral, device status | PARTIAL | Temperature chart wired; SpectralChart always empty; pressure/flow not shown |
| C6 | 06-06 | Safety status panel: rule list, current values, triggered/safe | PARTIAL | Rule list + triggered/safe verified; current values missing from SafetyEvent type |
| C9 | 06-07 | Workflow library: save/load/duplicate locally | SATISFIED | workflowLibrary.ts + Tauri fs plugin wiring all verified |
| D4 | 06-04, 06-05, 06-06, 06-08 | UI component tests | SATISFIED | 47/47 tests pass across 10 test files |

---

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|---------| 
| helix-desktop/src/App.tsx | 46 | SpectralChart uvData=[] mirData=[] hardcoded | Warning | SpectralChart always empty; C5 spectral requirement not met |
| helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx | 97 | capability-picker select no onChange | Warning | Capability picker display-only; C3 binding not satisfied |
| helix-desktop/src/components/WorkflowBuilder/useReteEditor.ts | whole file | rete-context-menu-plugin installed but not imported | Blocker | Canvas non-interactive; C2 must-have and ROADMAP SC4 fail |

No TBD, FIXME, or XXX markers found in any Phase 6 modified source files.

---

### Human Verification Required

#### 1. Rete.js Canvas Renders Without Errors

**Test:** Launch the Tauri desktop app (npm run tauri dev); navigate to the Workflow Builder panel.
**Expected:** A canvas area renders at 600px height with no JavaScript console errors.
**Why human:** Cannot test Tauri renderer output with grep.

#### 2. WebSocket /ws Delivers Live Frames During Execution

**Test:** Start the Agent, start a runspace via the ExecutionPanel, observe the step list.
**Expected:** step-started and runspace-started events appear in the step list within 5 seconds.
**Why human:** Requires a live Agent process and an active runspace.

#### 3. Live ScalarTrendChart Updates During Execution

**Test:** With a running Agent emitting temperature trends, observe the chart in the app.
**Expected:** The Temperature LineChart updates at least once per second with new data points.
**Why human:** Requires live TrendValue frames from a real or simulated device.

#### 4. Safety Panel Shows Active Interlock Rules

**Test:** Register interlock rules via ISafetyEngine before starting a runspace; observe SafetyPanel.
**Expected:** At least one rule appears in the rule list with description, severity, and safe/triggered state within 4 seconds.
**Why human:** Requires Agent process with configured ISafetyEngine rules.

#### 5. Workflow Save/Load Round-Trip in Tauri Runtime

**Test:** In the running Tauri app, type a workflow name, click Save, close and reopen, click Load...
**Expected:** The workflow dropdown shows the saved name; selecting it fires onExport with the correct WslDocument.
**Why human:** Tauri fs plugin calls are no-ops in browser context; requires native Tauri runtime.

#### 6. POST /workflows/compile Live Endpoint Response

**Test:** With Agent running, POST a valid WslDocument JSON to http://localhost:5080/workflows/compile.
**Expected:** HTTP 200 with RunspaceDefinition body; an invalid document returns 422 with diagnostics array.
**Why human:** Endpoint code verified but not tested with a live HTTP request.

---

### Gaps Summary

**4 gaps identified across 4 concerns. 2 are BLOCKERS; 2 are PARTIAL (WARNING).**

**Gap 1 -- BLOCKER (C2, ROADMAP SC4):** The rete-context-menu-plugin package is installed but never imported into useReteEditor.ts. The canvas renders as an empty non-interactive viewport. A scientist cannot add any nodes at runtime. PLAN 04 must-have truth is definitively FALSE.

**Gap 2 -- BLOCKER (C5):** SpectralChart always renders empty. App.tsx passes hardcoded empty arrays; the Agent has no spectral event type; TrendValueEvent is scalar-only. The UV/MIR overlay portion of C5 has no data path at all.

**Gap 3 -- WARNING (C3):** The capability picker renders options from GET /devices but has no onChange handler. Selecting a capability produces no effect on any node. The node-input binding portion of C3 is missing.

**Gap 4 -- WARNING (C6):** SafetyEvent (both Agent and frontend) lacks a currentValue field. SafetyPanel cannot display the current values column required by C6.

**Recommended grouping for fix plan:** Gaps 1 and 3 share root cause (Rete.js interactive authoring layer incomplete) and should be addressed together. Gaps 2 and 4 are event-type extensions and can be addressed in a separate plan.

---

_Verified: 2026-05-30T08:15:00Z_
_Verifier: Claude (gsd-verifier)_