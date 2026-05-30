---
created: 2026-05-30T19:40:00Z
title: Auto-Generated Manual Control Dialogs from CapabilityRegistry
area: ui
files:
  - Helix.Orchestration.Core/Devices/CapabilityRegistry.cs
  - Helix.Orchestration.Agent/Api/DeviceEndpoints.cs
  - Helix.Orchestration.Agent/Events/RunspaceEvent.cs
  - helix-desktop/src/components/DeviceRegistry.tsx
  - helix-desktop/src/types/agent.ts
---

## Problem

Every new device type currently requires custom UI code for manual control. This creates a maintenance burden, inconsistent UX across devices, and a hard barrier to using new devices: a driver must be implemented AND a UI panel must be written before a scientist can manually interact with the device. Manual actions are also invisible to the experiment record — there is no way to know whether a scientist made an out-of-band adjustment during a run.

## Solution

**Auto-generated manual control dialogs** driven by capability metadata from `CapabilityRegistry`. No per-device UI code. Every capability becomes a widget; every manual action is recorded identically to a workflow step.

---

### Capability Schema Extension

The current `CapabilityRegistry` stores capability verb names and handler delegates. To drive UI generation, it must also store **parameter schemas**:

```csharp
public record CapabilityDescriptor(
    string VerbName,              // e.g. "set_temperature"
    string DisplayName,           // e.g. "Set Temperature"
    string Description,           // e.g. "Set the reactor temperature setpoint"
    CapabilityCategory Category,  // Control | Measurement | Status | Utility
    ParameterSchema[] Parameters, // ordered list of parameter descriptors
    bool IsDestructive,           // true → confirm before executing
    bool RequiresActiveRunspace   // true → only available during a run
);

public record ParameterSchema(
    string Name,          // e.g. "value"
    string DisplayName,   // e.g. "Temperature"
    ParameterType Type,   // Numeric | Boolean | Enum | String | Duration
    string Unit,          // e.g. "°C"
    double? Min,
    double? Max,
    double? Step,         // numeric increment for steppers
    string[]? Options,    // for Enum type
    object? DefaultValue
);

public enum ParameterType { Numeric, Boolean, Enum, String, Duration }
public enum CapabilityCategory { Control, Measurement, Status, Utility }
```

`GET /devices/{id}/capabilities` extended to return `CapabilityDescriptor[]` instead of plain string names.

---

### Widget Generation Rules

The UI maps each `CapabilityDescriptor` to a widget type deterministically:

| CapabilityCategory | Parameter count | ParameterType | Widget rendered |
|---|---|---|---|
| Control | 0 | — | Button (label = DisplayName) |
| Control | 1 | Boolean | Toggle switch |
| Control | 1 | Numeric | Numeric input with unit label + Min/Max validation |
| Control | 1 | Enum | Select dropdown |
| Control | N > 1 | Mixed | Expandable form (one field per parameter) |
| Measurement | 0 | — | "Run Now" action button + result display area |
| Measurement | 1 | Duration | Duration input + "Start" button |
| Status | — | — | Read-only value display (auto-refreshes) |
| Utility | — | — | Expandable form with confirm step |

**Examples:**

```
SetTemperature (Control, Numeric, unit="°C", min=0, max=200)
→ [Temperature: ___°C  [Set]]

StartStir (Control, Boolean)
→ [Stirring  [OFF ●──]  ON]

SetFlowRate (Control, Numeric, unit="mL/min", min=0.1, max=10, step=0.1)
→ [Flow Rate: ___mL/min  [Set]]

Inject (Control, 2 params: volume_ul Numeric, solvent Enum)
→ [▼ Inject]
     Volume: ___µL
     Solvent: [MeOH ▼]
     [Execute]

TakeMeasurement (Measurement, 0 params)
→ [▶ Take Measurement]  → result: 0.342 AU @ 280nm

GetStatus (Status, 0 params)
→ Temperature: 79.8°C  Pressure: 2.1 bar  [↻ refreshing]
```

**Destructive capabilities** (`IsDestructive: true`) show a confirmation dialog before executing:
```
⚠ Confirm: Set Temperature to 150°C on VapourtecR4-01?
[Cancel]  [Confirm]
```

---

### Manual Control Panel Component

**`DeviceManualControl.tsx`** — a single generic component, no per-device code:

```typescript
interface DeviceManualControlProps {
  deviceId: string;
  baseUrl: string;
}
```

On mount:
1. `GET /devices/{deviceId}/capabilities` → `CapabilityDescriptor[]`
2. Group by `Category`: Control, Measurement, Status, Utility (separate sections)
3. Render one widget per capability using the generation rules above

On capability execution:
1. `POST /devices/{deviceId}/execute` with `{ capability, parameters }`
2. Agent invokes capability through `CapabilityRegistry` → same path as workflow steps
3. Response: `{ success, result?, error? }`
4. Display inline result or error; update Status widgets

**Panel layout:**
```
┌─────────────────────────────────────┐
│  VapourtecR4-01  ●  Online          │
├─────────────────────────────────────┤
│  CONTROL                            │
│  Temperature  [80°C ___]  [Set]     │
│  Flow Rate    [1.5 ___mL/min][Set]  │
│  Stirrer      [● ON  ────○]         │
├─────────────────────────────────────┤
│  MEASUREMENTS                       │
│  [▶ Take UV Sample]                 │
│  [▶ Read Pressure]  2.1 bar         │
├─────────────────────────────────────┤
│  STATUS                             │
│  Temperature setpoint: 80°C   ↻     │
│  Temperature actual:   79.8°C ↻     │
└─────────────────────────────────────┘
```

---

### Execution Pipeline — Same Path as Workflows

Manual actions do not use a shortcut path. They go through the same `CapabilityRegistry` → device adapter → transport pipeline as workflow steps:

```
User clicks "Set Temperature" in DeviceManualControl
  ↓
POST /devices/{deviceId}/execute  { capability: "set_temperature", parameters: { value: 80 } }
  ↓
DeviceEndpoints → CapabilityRegistry.InvokeAsync(deviceId, "set_temperature", parameters)
  ↓
VapourtecR4.SetTemperature(80) → SerialTransport.Write("ST80\r")
  ↓
Response → DeviceEndpoints → HTTP 200 { success: true }
```

**This is the same `CapabilityRegistry.InvokeAsync` called by `WslExecutionAdapter` during workflow runs.** No special-case path. No divergence.

---

### Recording — Manual Actions in the Unified Timeline

Every manual action is written to the experiment timeline as a `ManualActionEvent`:

```csharp
public sealed record ManualActionEvent(
    string RunspaceId,       // null if no active runspace — standalone manual action
    string DeviceId,
    string Capability,
    Dictionary<string, object> Parameters,
    bool Success,
    object? Result,
    string? Error
) : RunspaceEvent("manual-action");
```

**Two recording contexts:**

1. **During a runspace (mid-workflow manual intervention):**
   - `ManualActionEvent` inserted into the timeline at its exact timestamp
   - Tagged `"manual"` in EventProtocol
   - Appears in automation summary as a deviation: "Manual intervention: SetTemperature(85°C) at t+00:14:22"
   - Deviation analysis can flag it as unplanned

2. **Outside a runspace (standalone manual operation):**
   - A lightweight "manual session" runspace is created automatically (type: `"manual"`, no planned protocol)
   - All actions recorded under this session
   - Closed when the user closes the Manual Control panel or after an idle timeout
   - Manual session timelines are queryable via `GET /experiments` with `type=manual`

**Why record standalone manual actions:**
- A scientist who primes a pump before starting an experiment needs that prime in the audit trail
- Safety: if a device was manually set to an unsafe value before a run, the timeline captures it
- Reproducibility: full device interaction history, not just workflow-driven history

---

### Integration with DeviceRegistry Component (Phase 5 → Phase 6 extension)

The existing `DeviceRegistry.tsx` (Phase 5) shows adapter name + health status. It gains a "Manual Control" button per device row:

```tsx
<DeviceRow key={adapter.id}>
  <span>{adapter.name}</span>
  <StatusBadge status={adapter.health} />
  <button onClick={() => setSelectedDevice(adapter.id)}>
    Manual Control
  </button>
</DeviceRow>

{selectedDevice && (
  <DeviceManualControl deviceId={selectedDevice} baseUrl={baseUrl} />
)}
```

---

### REST API Additions

```
GET  /devices/{id}/capabilities
  Returns: CapabilityDescriptor[]  (extended from current string[] response)

POST /devices/{id}/execute
  Body: { capability: string, parameters: Record<string, unknown> }
  Returns: { success: boolean, result?: unknown, error?: string }
  Side effect: publishes ManualActionEvent to WebSocket + timeline
```

The `POST /devices/{id}/execute` endpoint is also useful for the Python client:
```python
client.device("vapourtec-r4-01").execute("set_temperature", value=80)
```

---

### Testing Strategy

- `DeviceManualControl.test.tsx`: mock `GET /devices/{id}/capabilities` returning a fixture `CapabilityDescriptor[]`; assert correct widget type rendered per schema
- `POST /devices/{id}/execute` mock: assert correct payload sent, result displayed
- Confirm dialog: assert dialog appears for `IsDestructive: true` capabilities
- Placeholder test: assert panel shows "Device offline" when adapter status is `offline`

No SciChart dependency — this component is pure form/button UI.

---

## Implementation hints

**Agent (Core/API):**
- Extend `CapabilityDescriptor` and `ParameterSchema` types in `Helix.Orchestration.Core`
- Device adapters declare their schemas in `DeviceAdapterBase` registration (each driver author fills in the schema alongside the handler delegate)
- `GET /devices/{id}/capabilities` update in `DeviceEndpoints.cs`
- `POST /devices/{id}/execute` new endpoint in `DeviceEndpoints.cs`
- `ManualActionEvent` record in `RunspaceEvent.cs`
- Manual session lifecycle in `AgentService.cs`

**Frontend:**
- `DeviceManualControl.tsx` — generic, schema-driven
- Widget sub-components: `NumericCapabilityControl`, `BooleanCapabilityControl`, `EnumCapabilityControl`, `ActionCapabilityControl`, `StatusCapabilityDisplay`
- All widgets share a `CapabilityControl` interface — testable independently

**Cross-links:**
- Capability schema: `2026-05-30-multi-mode-workflow-authoring.md` (CapabilityRegistry schema introspection gap — this todo defines what the schema looks like)
- Recording: `2026-05-30-record-replay-system.md` (ManualActionEvent in the unified timeline)
- Protocol model: `2026-05-30-protocol-standardisation-trend-naming.md` (manual actions in EventProtocol as tagged deviations)
- Python client: `2026-05-30-python-workflow-interface.md` (client.device().execute() uses the same POST /devices/{id}/execute endpoint)
- Graceful degradation: `2026-05-30-graceful-device-loss-removal.md` (panel shows "Device offline" state when adapter unavailable)
