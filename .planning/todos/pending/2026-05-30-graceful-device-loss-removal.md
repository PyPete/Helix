---
created: 2026-05-30T19:15:00Z
title: Graceful Handling of Device Loss & Workflow/Trend Removal
area: api
files:
  - Helix.Orchestration.Core/Devices/CapabilityRegistry.cs
  - Helix.Orchestration.Agent/AgentService.cs
  - Helix.Orchestration.Agent/Events/RunspaceEvent.cs
  - Helix.Orchestration.Agent/Api/DeviceEndpoints.cs
  - helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx
  - helix-desktop/src/components/WorkflowBuilder/nodes/HelixNodes.ts
  - helix-desktop/src/components/TrendCharts/ScalarTrendChart.tsx
  - helix-desktop/src/App.tsx
---

## Problem

The current system has no defined contract for what happens when:
- A device is unplugged mid-session or mid-workflow
- A workflow referencing a missing device is compiled or submitted
- A custom trend loses one of its input device feeds
- A user deletes a workflow, trend definition, or device configuration

Without explicit degradation semantics, these events produce undefined behaviour: crashes, silent data loss, or stale UI that gives scientists false confidence in instrument state.

## Solution

**Explicit degradation states at every layer** — device loss, workflow invalidation, and trend degradation are modelled as first-class states, not error conditions. Users remain in control at all times.

---

### 1. Device Loss → Capability Removal → UI Update

**Agent side:**

When a device adapter loses its connection (serial disconnect, TCP timeout, OPC UA session drop):

1. `CapabilityRegistry` marks the adapter's capabilities as `unavailable` (not removed — the registry entry remains)
2. Agent publishes a `DeviceStatusEvent` WebSocket frame:
   ```json
   { "type": "device-status", "deviceId": "vapourtec-r4-01", "status": "offline", "capabilities": ["set_temperature", "set_flow"] }
   ```
3. `AgentService` stops forwarding trend values from that device; publishes a final `TrendValueEvent` with `{ "stale": true }` for each affected trend
4. `GET /devices` returns the device with `"status": "offline"` (not omitted from the list)

**Desktop UI:**

On receiving `DeviceStatusEvent { status: "offline" }`:
- All nodes/cards referencing that device's capabilities render in "device unavailable" state (greyed + warning icon)
- Capability picker removes that device's capabilities from the add-node menu
- Trend charts for that device show last-known value + offline annotation, stop updating
- **No data is deleted.** The WslDocument is unchanged.

On reconnection (`DeviceStatusEvent { status: "online" }`):
- Nodes/cards return to normal state automatically
- Trend charts resume updating
- No user action required; no page reload

---

### 2. Workflows Referencing Missing Devices → Invalid but Editable

**Validation model — not a binary pass/fail:**

A `WslDocument` with steps that reference unavailable devices is **invalid for execution** but **valid for editing**. These are two different states.

```typescript
type WorkflowValidationState =
  | { status: "valid" }                        // all devices online, all capabilities available
  | { status: "invalid"; errors: ValidationError[] }  // cannot compile/submit
  | { status: "degraded"; warnings: ValidationWarning[] }  // can submit, but some features unavailable

type ValidationError = {
  nodeId: string;
  capability: string;
  reason: "device_offline" | "capability_removed" | "parameter_type_mismatch";
}
```

**UI behaviour:**
- `status: "invalid"` → Start button disabled, error banner listing which steps/devices are affected
- `status: "degraded"` → Start button enabled with warning, user can proceed
- Nodes with errors rendered with a red outline; tooltip shows the specific error
- User can still edit the workflow, swap out the missing device for an available one, or delete the invalid step

**Compilation (Agent side):**
- `POST /workflows/compile` with a document referencing an offline device returns `HTTP 422` with structured diagnostics
- Each diagnostic: `{ "nodeId": "...", "capability": "...", "error": "device_offline" }`
- Compilation is not blocked by `degraded` states — only `invalid` states

---

### 3. Custom Trends → Degraded, Not Crashed

When a trend's input device goes offline:

**Local Engine trends:**
- Expression evaluator receives `null` for the missing input
- Expression result is `null` → Agent publishes `TrendValueEvent { stale: true, reason: "input_unavailable" }`
- Trend chart shows last-known value + staleness indicator
- Trend remains defined and active; resumes automatically when input comes back

**Python Sidecar trends:**
- `compute(inputs)` receives `null` for the missing key
- If the script handles `None` gracefully → degraded output (user-defined fallback)
- If the script raises → sidecar catches it, returns error frame
- Agent publishes `TrendValueEvent { stale: true, reason: "compute_error" }`

**Trend degradation states:**

| State | Meaning | UI display |
|---|---|---|
| `active` | All inputs live, compute succeeds | Normal chart update |
| `stale` | No update within 2× polling interval | Greyed value, clock icon |
| `degraded` | Input unavailable, compute returned null/error | Last value + warning icon |
| `unavailable` | Trend definition itself is invalid (script missing, venv broken) | "Trend unavailable" label |

---

### 4. Deletion Semantics — Non-Destructive by Default

**Workflow deletion:**
- Workflows are stored locally (Tauri fs plugin) or in a server-side library
- Deleting a workflow removes its definition file only
- Active runspaces using that workflow are **not affected** (they hold a compiled `RunspaceDefinition`, not a reference to the workflow file)
- Deletion is immediate with a single undo opportunity (30s undo toast)

**Trend definition deletion:**
- Removing a trend definition stops the Agent from evaluating and publishing it
- Historical recorded values (in Recorder/MQTT/MinIO) are **not deleted**; they remain queryable via `GET /trends/{id}/history`
- Workflows with a `WaitFor` step referencing the deleted trend have that step marked `invalid` (same mechanism as device loss)
- No cascade delete: deletion of a trend does not delete workflows that referenced it

**Device configuration deletion:**
- Removing a device from the Agent config (not the same as it going offline — a deliberate administrative action)
- Capabilities are removed from `CapabilityRegistry`
- All affected workflows, nodes, and trends enter the same `invalid` / `degraded` states as device loss
- A confirmation dialog lists all affected workflows and trends before confirming deletion

**Delete vs. archive:**
- "Delete" removes the definition immediately
- "Archive" marks it as `archived: true` — hidden from active UI but recoverable
- Archived items do not participate in capability matching, trend evaluation, or workflow validation
- Archive is preferred for workflows that may be needed again; delete is permanent

---

### 5. System Invariants (must never be violated)

1. **No silent data loss.** A device going offline, a user deleting a workflow, or a trend becoming stale never causes recorded data to be deleted without explicit user confirmation.
2. **Degraded ≠ broken.** The system remains operational when individual components are unavailable. One offline device does not prevent using other devices.
3. **Editable under error.** A workflow in `invalid` state is always editable. The scientist can fix it, not just discard it.
4. **Deterministic state.** Every component has one of a small, defined set of states (online/offline, valid/invalid/degraded, active/stale/unavailable). No undefined states.
5. **Reconnection is automatic.** When a device comes back online, UI and trends restore without user intervention.

---

## Implementation hints

This is a cross-cutting concern that touches multiple phases. Implementation should be layered:

**Agent changes (new):**
- `DeviceStatusEvent` WebSocket frame type (currently absent)
- `CapabilityRegistry.MarkUnavailable(adapterId)` / `MarkAvailable(adapterId)` methods
- Adapter health-check loop in `AgentService` detecting connection loss
- `POST /workflows/compile` returning structured per-node diagnostics (currently returns flat diagnostics)

**Frontend changes (new):**
- `DeviceStatusEvent` handling in `useAgentWebSocket`
- `validateWorkflow(doc, onlineDevices)` pure function → `WorkflowValidationState`
- Node rendering: `invalid` and `degraded` visual states in `HelixNodes.ts`
- Start button gating on `WorkflowValidationState`
- Trend chart: `stale` + `degraded` visual states in `ScalarTrendChart` and `SpectralChart`
- Delete/archive flow in `workflowLibrary.ts`

**Cross-links:**
- Device loss contract: `2026-05-30-multi-mode-workflow-authoring.md` (first raised device lifecycle)
- Trend degradation states: `2026-05-30-real-time-trend-system.md` (staleness guard)
- Python sidecar error handling: `2026-05-30-python-sidecar-architecture.md` (failure mode table)
- Deployment: `2026-05-30-deployment-strategy-lab.md` (device service isolation)
