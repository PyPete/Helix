# Phase 11: Ribbon + Docking Workspace Shell - Pattern Map

**Mapped:** 2026-06-04
**Files analyzed:** 12 new/modified files
**Analogs found:** 11 / 12

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `src/shell/HelixShell.tsx` | component (composition root) | request-response | `src/App.tsx` | exact |
| `src/shell/Ribbon.tsx` | component (chrome) | event-driven | `src/components/ExecutionPanel/ExecutionPanel.tsx` | role-match |
| `src/shell/ActivityBar.tsx` | component (chrome) | event-driven | `src/components/SafetyPanel/SafetyPanel.tsx` | role-match |
| `src/shell/StatusBar.tsx` | component (chrome) | request-response | `src/components/SafetyPanel/SafetyPanel.tsx` | role-match |
| `src/shell/panels/DeviceExplorer.tsx` | component (panel/dock) | CRUD | `src/components/DeviceRegistry.tsx` | exact |
| `src/shell/panels/TrendExplorer.tsx` | component (panel/dock) | event-driven | `src/components/SafetyPanel/SafetyPanel.tsx` | role-match |
| `src/shell/panels/LogOutput.tsx` | component (panel/dock) | event-driven | `src/components/ExecutionPanel/ExecutionPanel.tsx` | role-match |
| `src/shell/panels/ChartPanel.tsx` | component (panel/dock) | event-driven | `src/components/TrendCharts/ScalarTrendChart.tsx` | exact |
| `src/hooks/useTrendRegistry.ts` | hook | event-driven | `src/hooks/useAgentWebSocket.ts` | exact |
| `src/hooks/useWorkspaceLayouts.ts` | hook | CRUD | `src/components/ConnectionConfig.tsx` (LazyStore) | exact |
| `src/styles/helix-dock.css` | config (CSS overrides) | — | `src/App.css` | role-match |
| `src/App.tsx` | component (entry) | request-response | `src/main.tsx` | exact |

---

## Pattern Assignments

### `src/shell/HelixShell.tsx` (composition root, request-response)

**Analog:** `src/App.tsx`

**Imports pattern** (App.tsx lines 1–13):
```tsx
import { useState } from "react";
import { DEFAULT_CONNECTION } from "./types/agent";
import type { AgentConnection, WslDocument } from "./types/agent";
import { useAgentHealth } from "./hooks/useAgentHealth";
import { useAgentWebSocket } from "./hooks/useAgentWebSocket";
import ConnectionConfig from "./components/ConnectionConfig";
import DeviceRegistry from "./components/DeviceRegistry";
import "./App.css";
```

**Core pattern — state lifted to shell** (App.tsx lines 15–22):
```tsx
export default function App() {
  const [connection, setConnection] = useState<AgentConnection>(DEFAULT_CONNECTION);
  const baseUrl = `${connection.protocol}://${connection.host}:${connection.port}`;
  const health = useAgentHealth(baseUrl);
  const { runspaceEvents, trendValues, safetyEvents } = useAgentWebSocket(baseUrl);
  const [currentWorkflow, setCurrentWorkflow] = useState<WslDocument | null>(null);
  const [activeTab, setActiveTab] = useState<"builder" | "execution" | "monitor">("builder");
```

**Key migration notes:**
- HelixShell replaces `div.app` with the rc-dock `DockLayout` as composition root
- `connection`, `health`, `runspaceEvents`, `trendValues`, `safetyEvents` all lift to HelixShell and are passed as props to panels
- Ribbon and ActivityBar sit outside `DockLayout` in the HTML skeleton from the design reference
- Default export, no props interface (root component)

**Shell HTML skeleton** (from `.claude/skills/sketch-findings-Helix/references/layout-and-chrome.md`):
```html
<div class="titlebar">…</div>
<div class="ribbon-tabs">…</div>
<div class="ribbon-band">…</div>
<div class="workspace">
  <div class="actbar">…</div>
  <div class="sidebar-left">…</div>
  <div class="splitter h">…</div>
  <div class="main-area">
    <div class="doctabs">…</div>
    <div class="canvas">…</div>
    <div class="bottom-panel">…</div>
  </div>
  <div class="splitter h">…</div>
  <div class="sidebar-right">…</div>
</div>
<div class="statusbar">…</div>
```

---

### `src/shell/Ribbon.tsx` (chrome component, event-driven)

**Analog:** `src/components/ExecutionPanel/ExecutionPanel.tsx`

**Props interface pattern** (ExecutionPanel.tsx lines 9–13):
```tsx
interface ExecutionPanelProps {
  baseUrl: string;
  definition: WslDocument | null;
  runspaceEvents: RunspaceEvent[];
}
```
Ribbon will use a similar explicit typed props interface: `activeTab`, `onTabChange`, `onRun`, `onSaveLayout`, `onLoadLayout`.

**Named export pattern** (ExecutionPanel.tsx line 40):
```tsx
export function ExecutionPanel({ baseUrl, definition, runspaceEvents }: ExecutionPanelProps) {
```
Ribbon uses named export: `export function Ribbon({ ... }: RibbonProps)`.

**Ribbon CSS patterns** (from layout-and-chrome.md):
```css
.ribbon-tabs {
  background: var(--color-ribbon-bg);
  display: flex;
  align-items: flex-end;
  padding: 0 8px;
  height: 26px;
  gap: 1px;
  border-bottom: 1px solid var(--color-border);
}
.ribbon-tab.active {
  background: var(--color-ribbon-active);
  border-color: var(--color-border);
  border-bottom-color: var(--color-ribbon-active);
}
```

**Button/tab state pattern** (ExecutionPanel.tsx lines 82–92):
```tsx
const isRunning = currentStatus === "runspace-started" || currentStatus === "step-started";
const isPaused = currentStatus === "runspace-paused";

<button onClick={handleStart} disabled={!definition || submitting || isRunning || isPaused}
  data-testid="start-btn">
```
Ribbon tab active state follows same boolean-driven disabled/active pattern.

---

### `src/shell/ActivityBar.tsx` (chrome component, event-driven)

**Analog:** `src/components/SafetyPanel/SafetyPanel.tsx`

**Minimal props + named export pattern** (SafetyPanel.tsx lines 3–6):
```tsx
interface SafetyPanelProps {
  safetyEvents: SafetyEvent[];
}

export function SafetyPanel({ safetyEvents }: SafetyPanelProps) {
```
ActivityBar equivalent:
```tsx
interface ActivityBarProps {
  activePanel: string | null;
  onPanelChange: (panel: string | null) => void;
}
export function ActivityBar({ activePanel, onPanelChange }: ActivityBarProps) {
```

**Active indicator CSS** (from layout-and-chrome.md):
```css
.abtn.active::before {
  content: '';
  position: absolute;
  left: 0; top: 25%; bottom: 25%;
  width: 2px;
  background: var(--color-primary);
  border-radius: 0 2px 2px 0;
}
```

**Section/aria-label pattern** (SafetyPanel.tsx line 14):
```tsx
<section aria-label="Safety Status" data-testid="safety-panel">
```
ActivityBar: `<nav aria-label="Activity Bar" data-testid="activity-bar">`.

---

### `src/shell/StatusBar.tsx` (chrome component, request-response)

**Analog:** `src/components/SafetyPanel/SafetyPanel.tsx`

**Props receiving live data** (SafetyPanel.tsx lines 3–5):
```tsx
interface SafetyPanelProps {
  safetyEvents: SafetyEvent[];
}
```
StatusBar equivalent — receives pre-computed state, no fetching:
```tsx
interface StatusBarProps {
  connected: boolean;
  healthy: boolean | null;
  agentUrl: string;
}
export function StatusBar({ connected, healthy, agentUrl }: StatusBarProps) {
```

**CSS pattern** (from layout-and-chrome.md):
```css
/* Status bar: 22px, primary-dim background */
.statusbar {
  height: 22px;
  background: var(--color-primary-dim);
  color: var(--color-primary);
}
```

**Conditional text/class pattern** (App.tsx lines 53–56):
```tsx
<span className={health.healthy ? "status-healthy" : "status-unhealthy"}>
  {health.healthy === null ? "Connecting..." : health.healthy ? "Agent Online" : `Agent Offline: ${health.error}`}
</span>
```

---

### `src/shell/panels/DeviceExplorer.tsx` (panel/dock, CRUD)

**Analog:** `src/components/DeviceRegistry.tsx`

**Full component — DeviceRegistry.tsx is essentially the panel body.** Extract its pattern wholesale:

**Imports + props** (DeviceRegistry.tsx lines 1–11):
```tsx
import { useState, useEffect } from "react";
import type { DeviceAdapter } from "../types/agent";
import DeviceManualControl from "./DeviceManualControl/DeviceManualControl";

interface DeviceRegistryProps {
  agentBaseUrl: string;
}
```

**Poll with cleanup** (DeviceRegistry.tsx lines 23–40):
```tsx
useEffect(() => {
  const poll = async () => {
    setLoading(true);
    try {
      const res = await fetch(`${agentBaseUrl}/devices`);
      if (!res.ok) throw new Error(`Agent returned ${res.status}`);
      const data: { adapterId: string; capabilities: string[] }[] = await res.json();
      setDevices(mapDevicesToAdapters(data));
      setError(null);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setLoading(false);
    }
  };
  poll();
  const interval = setInterval(poll, 5000);
  return () => clearInterval(interval);
}, [agentBaseUrl]);
```

**Error + list render** (DeviceRegistry.tsx lines 42–58):
```tsx
if (loading && devices.length === 0) return <p>Loading...</p>;

return (
  <section aria-label="Device Registry">
    {error && <p role="alert" className="error">{error}</p>}
    {devices.length === 0 && !error && <p>No adapters found</p>}
    <ul>
      {devices.map(d => (
        <li key={d.adapterId}>
          <strong>{d.adapterId}</strong>
          <button onClick={() => setSelectedDevice(d.adapterId)}>Manual Control</button>
        </li>
      ))}
    </ul>
    {selectedDevice && <DeviceManualControl deviceId={selectedDevice} baseUrl={agentBaseUrl} />}
  </section>
);
```

**DeviceManualControl mounting pattern** (DeviceRegistry.tsx line 57):
```tsx
{selectedDevice && <DeviceManualControl deviceId={selectedDevice} baseUrl={agentBaseUrl} />}
```
DeviceManualControl takes exactly two props: `deviceId: string` and `baseUrl: string`.

---

### `src/shell/panels/TrendExplorer.tsx` (panel/dock, event-driven)

**Analog:** `src/components/SafetyPanel/SafetyPanel.tsx`

**Derives display data from incoming array** (SafetyPanel.tsx lines 8–16):
```tsx
const latestByRule = safetyEvents.reduce(
  (acc, evt) => {
    acc[evt.ruleId] = evt;
    return acc;
  },
  {} as Record<string, SafetyEvent>
);
const rules = Object.values(latestByRule);
```
TrendExplorer will receive `trendNames: string[]` from `useTrendRegistry` and render a draggable list. Same pattern — props in, derived render out, no internal fetching.

**Drag-and-drop pattern** (decision D-08): HTML5 `draggable` + `onDragStart` with `dataTransfer.setData("trendName", name)`. No analog in codebase — use CONTEXT.md decision directly.

**Named export, section wrapper**:
```tsx
export function TrendExplorer({ trendNames }: TrendExplorerProps) {
  return (
    <section aria-label="Trend Explorer" data-testid="trend-explorer">
```

---

### `src/shell/panels/LogOutput.tsx` (panel/dock, event-driven)

**Analog:** `src/components/ExecutionPanel/ExecutionPanel.tsx`

**Event list render pattern** (ExecutionPanel.tsx lines 155–175):
```tsx
<ul data-testid="step-list" style={{ listStyle: "none", padding: 0, margin: "8px 0" }}>
  {eventsForRunspace.map((e, i) => {
    const eventColor =
      e.type === "step-completed" || e.type === "runspace-completed" ? "#27ae60"
      : e.type === "step-faulted" || e.type === "runspace-faulted" ? "#e74c3c"
      : e.type === "step-started" || e.type === "runspace-started" ? "#f5a623"
      : "#6c7086";
    return (
      <li key={i} data-testid={`step-event-${e.type}`}
        style={{ color: eventColor, fontSize: "0.8rem", padding: "2px 0" }}>
        {e.type}{stepLabel ? ` - ${stepLabel}` : ""}
      </li>
    );
  })}
</ul>
```
Use `var(--color-*)` tokens instead of hardcoded hex — LogOutput is written after the design system is in place.

**Monospace log display**: `font-family: var(--font-mono); font-size: var(--text-xs);` per design theme.

---

### `src/shell/panels/ChartPanel.tsx` (panel/dock, event-driven)

**Analog:** `src/components/TrendCharts/ScalarTrendChart.tsx`

**Full component — ScalarTrendChart is the direct body pattern:**

**Imports + typed props** (ScalarTrendChart.tsx lines 1–16):
```tsx
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from "recharts";

interface ScalarTrendChartProps {
  trendName: string;
  data: Array<{ t: number; v: number }>;
  unit?: string;
}

export function ScalarTrendChart({ trendName, data, unit }: ScalarTrendChartProps) {
  return (
    <section aria-label={`Trend: ${trendName}`} data-testid="scalar-trend-chart">
```

**ChartPanel extends this** with:
- `onDrop` / `onDragOver` handlers for receiving trendName from TrendExplorer drag
- Multiple chart layers (ScalarTrendChart + SpectralChart) per panel, chosen via local state
- Props: `trendValues: TrendValue[]` (sliced from shell state by trendName)

**Drop target pattern** (decision D-08 — no codebase analog, implement from spec):
```tsx
<div onDrop={(e) => { const name = e.dataTransfer.getData("trendName"); setTrackedTrend(name); }}
     onDragOver={(e) => e.preventDefault()}>
```

---

### `src/hooks/useTrendRegistry.ts` (hook, event-driven)

**Analog:** `src/hooks/useAgentWebSocket.ts`

**Hook signature pattern** (useAgentWebSocket.ts lines 12–19):
```ts
export interface AgentWebSocketState { ... }

export function useAgentWebSocket(baseUrl: string): AgentWebSocketState {
  const [runspaceEvents, setRunspaceEvents] = useState<RunspaceEvent[]>([]);
  const [trendValues, setTrendValues] = useState<TrendValue[]>([]);
  ...
```

**Accumulate-without-duplicates pattern** (useAgentWebSocket.ts lines 34–36):
```ts
if (frame.type === "trend-value") {
  setTrendValues((prev) => [...prev.slice(-200), frame as TrendValue]);
}
```
`useTrendRegistry` follows the same slice-bounded accumulation but deduplicates by trendName using a `Set`:

```ts
export function useTrendRegistry(trendValues: TrendValue[]): string[] {
  const [names, setNames] = useState<string[]>([]);

  useEffect(() => {
    if (trendValues.length === 0) return;
    const latest = trendValues[trendValues.length - 1];
    setNames((prev) => {
      if (prev.includes(latest.trendName)) return prev;
      return [...prev, latest.trendName];
    });
  }, [trendValues]);

  return names;
}
```
`trendValues` comes from `useAgentWebSocket` already wired in `HelixShell`; `useTrendRegistry` wraps it.

**Cleanup pattern** (useAgentWebSocket.ts line 46):
```ts
return () => ws.close();
```
`useTrendRegistry` has no subscription to close — no cleanup needed. useEffect dependency is `[trendValues]`.

---

### `src/hooks/useWorkspaceLayouts.ts` (hook, CRUD)

**Analog:** `src/components/ConnectionConfig.tsx` (LazyStore pattern)

**LazyStore full usage** (ConnectionConfig.tsx lines 1–10 + 22–27):
```tsx
import { LazyStore } from "@tauri-apps/plugin-store";

const store = new LazyStore("helix-config.json");  // sync constructor, module-level singleton

// Inside handler:
const handleSave = async () => {
  const updated: AgentConnection = { host, port, protocol };
  await store.set("agentConnection", updated);   // store.set(key, value) — async
  await store.save();                            // flush to disk — async
  onSave(updated);
};
```

**Read pattern** (not in ConnectionConfig; follows LazyStore v2 API):
```ts
const value = await store.get<T>("key");  // returns T | null
```

**`useWorkspaceLayouts` pattern** (modeled on ConnectionConfig, adapted for hook):
```ts
import { LazyStore } from "@tauri-apps/plugin-store";
import type { LayoutBase } from "rc-dock";

const store = new LazyStore("helix-config.json");  // reuse same store file

export function useWorkspaceLayouts() {
  const saveLayout = async (name: string, layout: LayoutBase) => {
    const existing = (await store.get<Record<string, LayoutBase>>("workspace-layouts")) ?? {};
    existing[name] = layout;
    await store.set("workspace-layouts", existing);
    await store.save();
  };

  const loadLayouts = async (): Promise<Record<string, LayoutBase>> => {
    return (await store.get<Record<string, LayoutBase>>("workspace-layouts")) ?? {};
  };

  return { saveLayout, loadLayouts };
}
```

**Test mock pattern** (ConnectionConfig.test.tsx lines 6–13):
```tsx
const { mockSet, mockSave } = vi.hoisted(() => ({
  mockSet: vi.fn().mockResolvedValue(undefined),
  mockSave: vi.fn().mockResolvedValue(undefined),
}));

vi.mock("@tauri-apps/plugin-store", () => {
  const LazyStoreMock = vi.fn(function (this: Record<string, unknown>, _name: string) {
    this.set = mockSet;
    this.save = mockSave;
  });
  return { LazyStore: LazyStoreMock };
});
```
Add `mockGet` alongside `mockSet`/`mockSave` for `useWorkspaceLayouts` tests.

---

### `src/styles/helix-dock.css` (CSS override layer)

**Analog:** `src/App.css`

**Plain CSS, no modules.** All existing CSS in helix-desktop is plain `.css` files imported as side-effects — no `.module.css` files exist.

**Import pattern** (App.tsx line 13, WorkflowBuilder.tsx line 11):
```tsx
import "./App.css";
import "@xyflow/react/dist/style.css";
```
Import `helix-dock.css` as a side-effect in `HelixShell.tsx`:
```tsx
import "../styles/helix-dock.css";
```

**CSS variable usage pattern** (App.css excerpt — existing files mix hex and inherit; new files MUST use tokens):
```css
/* From default.css — ALL new CSS must reference these, not hex values */
--color-bg:           #1e1e2e;
--color-surface:      #252535;
--color-primary:      #9d7fff;
--color-border:       #3e3e5a;
--color-ribbon-bg:    #1a1a2a;
--color-ribbon-active:#2d2d42;
```

**helix-dock.css purpose:** Map rc-dock class names to CSS variables. Pattern established by design reference:
```css
/* rc-dock class → helix token mapping pattern */
.dock-layout { background: var(--color-bg); }
.dock-panel { background: var(--color-surface); }
.dock-tab-active { border-top: 2px solid var(--color-primary); }
.dock-divider { background: var(--color-border); }
.dock-divider:hover { background: var(--color-primary); }
```

---

### `src/App.tsx` (rewritten entry, request-response)

**Analog:** `src/main.tsx`

**Current main.tsx** (full file):
```tsx
import React from "react";
import ReactDOM from "react-dom/client";
import App from "./App";
import "./style.css";

ReactDOM.createRoot(document.getElementById("root") as HTMLElement).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
```
`main.tsx` stays unchanged. `App.tsx` is rewritten to simply mount `HelixShell`:

```tsx
import HelixShell from "./shell/HelixShell";

export default function App() {
  return <HelixShell />;
}
```
This preserves `main.tsx` unchanged and keeps the rewrite isolated to the shell layer.

---

## Shared Patterns

### TypeScript Import Paths — No Path Aliases

**Source:** `tsconfig.json` + `vite.config.ts`

No `paths` aliases configured. All imports are bare relative paths:

```tsx
// From components deep in subdirectory:
import type { DeviceAdapter } from "../../types/agent";     // two levels up
import { useAgentWebSocket } from "../../hooks/useAgentWebSocket";
import DeviceManualControl from "./DeviceManualControl/DeviceManualControl";
```
Shell files at `src/shell/` import hooks as `"../hooks/..."`, types as `"../types/..."`.
Panel files at `src/shell/panels/` import hooks as `"../../hooks/..."`.

### Named vs Default Export Convention

**Source:** All existing components

- **Default export:** root-of-subtree components consumed directly by App.tsx — `ConnectionConfig`, `DeviceRegistry`, `DeviceManualControl`
- **Named export:** domain components used within composition — `ExecutionPanel`, `SafetyPanel`, `ScalarTrendChart`, `SpectralChart`, `WorkflowBuilder`
- **Named export:** all hooks — `useAgentHealth`, `useAgentWebSocket`, `useNodeExecutionState`

Phase 11 convention: `HelixShell` = default (App.tsx imports it). All other shell components and hooks = named exports.

### Error Display Pattern

**Source:** `src/components/DeviceRegistry.tsx` line 43, `src/components/ExecutionPanel/ExecutionPanel.tsx` line 55

```tsx
{error && <p role="alert" className="error">{error}</p>}
```
Consistent across all components. Use `role="alert"` + `className="error"` everywhere.

### data-testid Naming Convention

**Source:** All test files

Pattern: `kebab-case` noun describing the element's role in context:
- Containers: `data-testid="device-manual-control"`, `data-testid="execution-panel"`, `data-testid="safety-panel"`
- Specific items: `data-testid={`capability-${descriptor.verbName}`}`, `data-testid={`rule-${rule.ruleId}`}`
- Buttons: `data-testid="start-btn"`, `data-testid="abort-btn"`, `data-testid="save-workflow-btn"`

New shell components: `data-testid="helix-shell"`, `data-testid="ribbon"`, `data-testid="activity-bar"`, `data-testid="status-bar"`, `data-testid="device-explorer"`, `data-testid="trend-explorer"`, `data-testid="log-output"`, `data-testid="chart-panel"`.

### useEffect Cleanup Pattern

**Source:** `src/hooks/useAgentHealth.ts` lines 27–30, `src/components/DeviceRegistry.tsx` lines 38–40

```ts
// Interval cleanup:
const id = setInterval(check, intervalMs);
return () => clearInterval(id);

// WebSocket cleanup:
return () => ws.close();
```
Every `useEffect` that creates a subscription, interval, or external resource MUST return a cleanup function.

### CSS Variables — No Hardcoded Hex

**Source:** `.claude/skills/sketch-findings-Helix/sources/themes/default.css`

All new Phase 11 CSS uses `var(--color-*)`, `var(--space-*)`, `var(--text-*)`, `var(--radius-*)`. The full token set is defined in `default.css` and must be imported into `index.html` or `style.css` before any component CSS.

Note: existing components (ExecutionPanel, WorkflowBuilder) contain hardcoded hex values — Phase 11 new files should NOT follow this pattern; use tokens exclusively.

---

## Test File Patterns

### Component Test Location and Import Style

**Source:** All `.test.tsx` files

Tests co-located with their component:
- `src/components/ConnectionConfig.tsx` → `src/components/ConnectionConfig.test.tsx`
- `src/components/DeviceRegistry.tsx` → `src/components/DeviceRegistry.test.tsx`
- `src/components/SafetyPanel/SafetyPanel.tsx` → `src/components/SafetyPanel/SafetyPanel.test.tsx`

Shell components follow same pattern:
- `src/shell/HelixShell.tsx` → `src/shell/HelixShell.test.tsx`
- `src/shell/panels/DeviceExplorer.tsx` → `src/shell/panels/DeviceExplorer.test.tsx`

**Import style** (ConnectionConfig.test.tsx lines 1–4):
```tsx
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import ConnectionConfig from "./ConnectionConfig";
import type { AgentConnection } from "../types/agent";
```

### Hook Test Location and Structure

**Source:** `src/hooks/useAgentHealth.test.ts`, `src/hooks/useAgentWebSocket.test.ts`

Hook tests co-located in `src/hooks/`:
- `src/hooks/useTrendRegistry.ts` → `src/hooks/useTrendRegistry.test.ts`
- `src/hooks/useWorkspaceLayouts.ts` → `src/hooks/useWorkspaceLayouts.test.ts`

**renderHook pattern** (useAgentHealth.test.ts lines 1–3):
```ts
import { renderHook, waitFor } from "@testing-library/react";
import { describe, it, expect, vi, afterEach } from "vitest";
import { useAgentHealth } from "./useAgentHealth";
```

**vi.hoisted pattern for mocks that must be available at vi.mock time** (ConnectionConfig.test.tsx lines 6–13):
```tsx
const { mockSet, mockSave } = vi.hoisted(() => ({
  mockSet: vi.fn().mockResolvedValue(undefined),
  mockSave: vi.fn().mockResolvedValue(undefined),
}));
vi.mock("@tauri-apps/plugin-store", () => { ... });
```
Use this same `vi.hoisted` + `vi.mock` pattern in `useWorkspaceLayouts.test.ts`.

### Vitest Configuration

**Source:** `helix-desktop/vitest.config.ts`

```ts
export default defineConfig({
  plugins: [react()],
  test: {
    globals: true,
    environment: "jsdom",
    setupFiles: ["./src/test/setup.ts"],
  },
});
```
Setup file (`src/test/setup.ts`) contains only: `import "@testing-library/jest-dom";`

---

## No Analog Found

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| `src/styles/helix-dock.css` | CSS override | — | No third-party library CSS override pattern in codebase. Use rc-dock class list from its source and map to `var(--color-*)` tokens per design system. Partial analog: `App.css` shows the flat CSS file convention. |

---

## Metadata

**Analog search scope:** `helix-desktop/src/` (all `.tsx`, `.ts`, `.css` files)
**Files scanned:** 24 source files + 2 config files + design skill references
**Pattern extraction date:** 2026-06-04

**Key patterns summary:**
1. All components: explicit `interface XxxProps`, named export (except composition-root defaults), `section` wrapper with `aria-label` + `data-testid`
2. All hooks: named export, `useState` + `useEffect` with cleanup return, typed return interface
3. LazyStore: module-level `new LazyStore("file.json")` singleton, `await store.set(key, val)` + `await store.save()`, read via `await store.get<T>(key)`
4. CSS: plain `.css` files (no modules), imported as side-effects, ALL new files use `var(--color-*)` tokens exclusively
5. Tests: co-located `.test.tsx`/`.test.ts`, `vi.hoisted()` for Tauri mock setup, `vi.stubGlobal("fetch", ...)` for HTTP mocks, `renderHook` for hooks
6. No path aliases — all imports are bare relative paths (`../../hooks/...`, `../types/...`)
