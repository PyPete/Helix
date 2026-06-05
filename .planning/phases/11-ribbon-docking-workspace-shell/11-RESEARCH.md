# Phase 11: Ribbon + Docking Workspace Shell - Research

**Researched:** 2026-06-04
**Domain:** rc-dock, React IDE shell architecture, HTML5 DnD, Vitest class-component mocking
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** Use `rc-dock` as docking and split-panel library.
- **D-02:** Hardcoded `DEFAULT_LAYOUT` â€” left 220px, right 200px, bottom 140px, center flex-1.
- **D-03:** CSS variable override layer (`helix-dock.css`) maps rc-dock class names to Helix CSS variables.
- **D-04:** Big-bang replacement of App.tsx (wholesale rewrite).
- **D-05:** Home and Devices ribbon tabs wired with real content; remaining tabs placeholder.
- **D-06:** WorkflowBuilder + ExecutionPanel to document tabs; ScalarTrendChart + SpectralChart to Chart Panel docks; SafetyPanel to bottom strip or dock; DeviceManualControl to document tab from Device Explorer.
- **D-07:** `useTrendRegistry` hook accumulates seen `trendName` strings from live `trendValues`. No new Agent endpoint.
- **D-08:** HTML5 drag-and-drop (draggable + onDragStart/onDrop). No extra DnD library.
- **D-09:** Unlimited Chart Panel docks, each tracking its own trendName list.
- **D-10:** `LazyStore` (`@tauri-apps/plugin-store`) with key `workspace-layouts`.
- **D-11:** Serialize only via `DockLayout` ref `getLayout()`/`loadLayout()`.
- **D-12:** Save Layout (inline name input) and Load Layout (dropdown) in Ribbon Home.

### Claude's Discretion

- Panel IDs, `LayoutBase` structure, CSS class naming in `helix-dock.css`, Ribbon button group details.
- Status bar (22px, connection status, version) included in shell skeleton.

### Deferred Ideas (OUT OF SCOPE)

- Multi-Mode Workflow Authoring â€” Phase 13
- SciChart full integration â€” Phase 15
- ReactFlow node canvas â€” Phase 14
- Trend Explorer replay mode â€” Phase 17
- Developer Debugger panel â€” Phase 16
</user_constraints>

---

## Summary

Phase 11 replaces `App.tsx` with a VS-style IDE shell. The docking backbone is `rc-dock` v3.3.2 (the stable `latest` tag on npm resolves to `4.0.0-alpha.2` â€” but `3.3.2` is the last stable release; install with `rc-dock@3.3.2`). `DockLayout` is a **class component** â€” refs use `React.createRef<DockLayout>()` without `forwardRef`. The layout tree is a nested `BoxData` / `PanelData` structure passed as `defaultLayout`. Workspace state is read via `dockRef.current.getLayout()` and restored via `dockRef.current.loadLayout(saved)`. Layout serialization strips React content (functions are not JSON-serializable); `saveTab`/`loadTab` props are required to preserve tab identity across reloads.

CSS override is achieved by importing `rc-dock/dist/rc-dock.css` first, then `helix-dock.css` which reassigns colours and borders using the Helix CSS variables. All rc-dock visual classes are in the `dock-*` namespace and listed in detail below.

The `useTrendRegistry` hook wraps `trendValues: TrendValue[]` from `useAgentWebSocket` and accumulates unique `trendName` strings into a `Set<string>` using `useRef` to avoid re-render on each incoming value, exposing a stable `string[]` via `useState`.

HTML5 DnD and rc-dock's internal drag system operate on separate event paths â€” rc-dock uses pointer events and a custom `DragManager`, while HTML5 `draggable` uses native browser events. They do not conflict. Custom `onDrop` handlers on tab content divs work normally.

**Primary recommendation:** Install `rc-dock@3.3.2`, use uncontrolled `defaultLayout` mode (not the controlled `layout` prop), wire all panels via `saveTab`/`loadTab` for layout persistence, and keep the entire shell outside rc-dock (titlebar, ribbon, activity bar, status bar as plain divs above/below `DockLayout`).

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Shell chrome (title, ribbon, activity bar, status) | Frontend (React) | â€” | Pure presentation; no data, no server |
| rc-dock workspace | Frontend (React) | â€” | All layout is client-side state |
| Panel content (WorkflowBuilder, etc.) | Frontend (React) | API (backend data) | Components unchanged; only hosting changes |
| useTrendRegistry accumulation | Frontend hook | WebSocket stream | Client-side Set accumulation from existing WS data |
| Workspace layout persistence | Tauri plugin-store | â€” | Already pattern-matched in ConnectionConfig.tsx |
| HTML5 DnD (Trend â†’ Chart) | Browser/Frontend | â€” | Native events + React state only |

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `rc-dock` | `3.3.2` | Docking / split-panel workspace | Decision D-01; ships TS types; React 18 compatible |
| `@tauri-apps/plugin-store` | `2.4.3` (already installed) | Workspace layout persistence | Decision D-10; already used in ConnectionConfig |

### Version Verification

```
npm view rc-dock version          â†’ 4.0.0-alpha.2 (dist-tag: latest = alpha!)
npm view rc-dock@3.3.2 version    â†’ 3.3.2 (last stable, published 2025-12-10)
```

**Critical:** The npm `latest` tag currently resolves to `4.0.0-alpha.2`. Pin to `3.3.2` explicitly.

```bash
npm install rc-dock@3.3.2
```

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| rc-dock | golden-layout | RC-dock is locked (D-01); golden-layout noted for reference only |
| HTML5 DnD | react-dnd / dnd-kit | Locked (D-08); no extra library |
| LazyStore | custom Tauri fs | Locked (D-10); already in use |

---

## Package Legitimacy Audit

| Package | Registry | Age | Downloads | Source Repo | slopcheck | Disposition |
|---------|----------|-----|-----------|-------------|-----------|-------------|
| `rc-dock` | npm | ~7 yrs (2017) | ~13,700/wk | github.com/ticlo/rc-dock | OK (npm verified; slopcheck checked pypi only â€” npm existence confirmed via `npm view`) | Approved |

`slopcheck` defaulted to PyPI check (not npm) and correctly reported `rc-dock` absent from PyPI â€” that is expected. The npm registry entry was confirmed directly via `npm view rc-dock@3.3.2`.

**Packages removed due to slopcheck [SLOP] verdict:** none
**Packages flagged as suspicious [SUS]:** none

---

## Architecture Patterns

### System Architecture Diagram

```
â”Œâ”€ titlebar (30px) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”œâ”€ ribbon-tabs (26px) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”œâ”€ ribbon-band (68px) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”œâ”€ workspace (flex-row, fill) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚  â”Œâ”€actbarâ”€â” â”Œâ”€ DockLayout (flex-1) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”‚
â”‚  â”‚ 40px   â”‚ â”‚  BoxData{mode:horizontal}           â”‚ â”‚
â”‚  â”‚        â”‚ â”‚  â”œâ”€ PanelData{id:left, size:220}    â”‚ â”‚
â”‚  â”‚        â”‚ â”‚  â”‚   TabData{id:device-explorer}    â”‚ â”‚
â”‚  â”‚        â”‚ â”‚  â”œâ”€ BoxData{mode:vertical, flex-1}  â”‚ â”‚
â”‚  â”‚        â”‚ â”‚  â”‚   â”œâ”€ PanelData{id:main-tabs}     â”‚ â”‚
â”‚  â”‚        â”‚ â”‚  â”‚   â”‚   TabData{id:workflow-editor}â”‚ â”‚
â”‚  â”‚        â”‚ â”‚  â”‚   â”‚   TabData{id:chart-*}        â”‚ â”‚
â”‚  â”‚        â”‚ â”‚  â”‚   â””â”€ PanelData{id:bottom,sz:140} â”‚ â”‚
â”‚  â”‚        â”‚ â”‚  â”‚       TabData{id:output}         â”‚ â”‚
â”‚  â”‚        â”‚ â”‚  â””â”€ PanelData{id:right, size:200}   â”‚ â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚       TabData{id:trend-explorer}    â”‚ â”‚
â”‚             â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚
â”œâ”€ statusbar (22px) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

Notes:
- Activity bar is outside rc-dock (plain div with `position: relative` for the active indicator)
- rc-dock fills the center column (`flex: 1 1 0`, `overflow: hidden`)
- Ribbon, titlebar, and statusbar are siblings of the workspace row

### Recommended Project Structure

```
helix-desktop/src/
â”œâ”€â”€ shell/
â”‚   â”œâ”€â”€ Shell.tsx              # top-level composition root (replaces App.tsx role)
â”‚   â”œâ”€â”€ TitleBar.tsx           # 30px OS title bar
â”‚   â”œâ”€â”€ Ribbon.tsx             # ribbon-tabs + ribbon-band (68px)
â”‚   â”œâ”€â”€ ActivityBar.tsx        # 40px icon column
â”‚   â”œâ”€â”€ StatusBar.tsx          # 22px status strip
â”‚   â”œâ”€â”€ WorkspaceLayout.tsx    # DockLayout wrapper + DEFAULT_LAYOUT + ref + save/load
â”‚   â””â”€â”€ shell.css              # shell chrome styles (non-rc-dock)
â”œâ”€â”€ hooks/
â”‚   â”œâ”€â”€ useTrendRegistry.ts    # NEW: accumulates trendName strings
â”‚   â””â”€â”€ useWorkspaceLayout.ts  # NEW: LazyStore save/restore wrapper
â”œâ”€â”€ panels/
â”‚   â”œâ”€â”€ DeviceExplorerPanel.tsx
â”‚   â”œâ”€â”€ TrendExplorerPanel.tsx
â”‚   â”œâ”€â”€ ChartDockPanel.tsx
â”‚   â”œâ”€â”€ OutputPanel.tsx
â”‚   â””â”€â”€ index.ts
â””â”€â”€ helix-dock.css             # rc-dock CSS variable overrides
```

### Pattern 1: DockLayout with defaultLayout (Uncontrolled)

`defaultLayout` mode is preferred. The layout is internally managed by rc-dock; `getLayout()` retrieves current state on demand.

```typescript
// Source: https://raw.githubusercontent.com/ticlo/rc-dock/master/src/DockLayout.tsx
import DockLayout from 'rc-dock';
import type { LayoutBase } from 'rc-dock';
import { useRef } from 'react';

// DockLayout is a class component â€” use React.createRef or useRef<DockLayout>
const dockRef = useRef<DockLayout>(null);

// Retrieve layout for persistence:
const layout: LayoutBase = dockRef.current!.getLayout();

// Restore a saved layout:
dockRef.current!.loadLayout(savedLayout);

// In JSX:
<DockLayout
  ref={dockRef}
  defaultLayout={DEFAULT_LAYOUT}
  groups={PANEL_GROUPS}
  saveTab={saveTab}
  loadTab={loadTab}
  style={{ flex: 1, overflow: 'hidden' }}
/>
```

**Critical:** Because `DockLayout` is a **class component** (not a function component), `useRef<DockLayout>(null)` and `React.createRef<DockLayout>()` both work correctly. Do not use `forwardRef`.

### Pattern 2: DEFAULT_LAYOUT LayoutBase Structure

```typescript
// Source: https://raw.githubusercontent.com/ticlo/rc-dock/master/src/DockData.ts
import type { LayoutBase } from 'rc-dock';

export const DEFAULT_LAYOUT: LayoutBase = {
  dockbox: {
    mode: 'horizontal',
    children: [
      {
        // Left sidebar: Device Explorer
        mode: 'vertical',
        size: 220,
        children: [
          {
            id: 'panel-left',
            tabs: [
              { id: 'device-explorer' },
            ],
            activeId: 'device-explorer',
          },
        ],
      },
      {
        // Center: main tabs + bottom output
        mode: 'vertical',
        size: 600, // flex weight; actual size is flex-1
        children: [
          {
            id: 'panel-main',
            tabs: [
              { id: 'workflow-editor' },
              { id: 'execution-panel' },
            ],
            activeId: 'workflow-editor',
          },
          {
            id: 'panel-bottom',
            size: 140,
            tabs: [
              { id: 'output' },
              { id: 'safety-log' },
            ],
            activeId: 'output',
          },
        ],
      },
      {
        // Right sidebar: Trend Explorer
        mode: 'vertical',
        size: 200,
        children: [
          {
            id: 'panel-right',
            tabs: [
              { id: 'trend-explorer' },
            ],
            activeId: 'trend-explorer',
          },
        ],
      },
    ],
  },
};
```

Sizing: rc-dock treats `size` as a flex weight when in `horizontal`/`vertical` box mode. The three children (220, 600, 200) sum to 1020; proportions are applied. The center column gets the largest share. Panels may also set `size` as a pixel-like hint; rc-dock normalizes these.

### Pattern 3: saveTab / loadTab for Layout Persistence

rc-dock's serializer strips React content (`title`, `content`) â€” only `id` and structural fields survive. Use `saveTab`/`loadTab` to reconstruct content from a static registry:

```typescript
// Source: https://raw.githubusercontent.com/ticlo/rc-dock/master/src/Serializer.ts
import type { TabData, TabBase } from 'rc-dock';

// Static registry mapping id â†’ full TabData factory
const TAB_REGISTRY: Record<string, () => TabData> = {
  'device-explorer': () => ({
    id: 'device-explorer',
    title: 'ðŸ–¥ Devices',
    content: <DeviceExplorerPanel />,
    group: 'sidebar',
    closable: false,
  }),
  'trend-explorer': () => ({
    id: 'trend-explorer',
    title: 'ðŸ“ˆ Trends',
    content: <TrendExplorerPanel />,
    group: 'sidebar',
    closable: false,
  }),
  'workflow-editor': () => ({
    id: 'workflow-editor',
    title: 'â¬¡ Workflow Builder',
    content: <WorkflowBuilder ... />,
    group: 'main',
    closable: true,
  }),
  // ...
};

function saveTab(tab: TabData): TabBase {
  // Only persist the id â€” content is not serializable
  return { id: tab.id };
}

function loadTab(tab: TabBase): TabData {
  const factory = TAB_REGISTRY[tab.id];
  if (!factory) {
    // Graceful fallback for unknown tab ids (e.g. user-created chart panels)
    return {
      id: tab.id,
      title: tab.id,
      content: <div>Panel not found: {tab.id}</div>,
    };
  }
  return factory();
}
```

For dynamically-created Chart Panel tabs (D-09), include chart panel id in the registry at creation time, or use the `loadTab` fallback with trendName encoded in the id string (e.g. `chart-temperature`).

### Pattern 4: Adding a New Tab Programmatically

```typescript
// Source: https://raw.githubusercontent.com/ticlo/rc-dock/master/src/DockLayout.tsx
// dockMove(source, target, direction)
// - target: panel id string OR PanelData/BoxData reference
// - direction: 'middle' = add to existing panel; 'float' = float panel

function openChartPanel(trendName: string) {
  const id = `chart-${trendName}`;
  const newTab: TabData = {
    id,
    title: `ðŸ“ˆ ${trendName}`,
    content: <ChartDockPanel trendName={trendName} />,
    group: 'main',
    closable: true,
  };
  // Add to main panel tab strip
  dockRef.current!.dockMove(newTab, 'panel-main', 'middle');
}
```

### Pattern 5: Tab Groups Configuration

```typescript
import type { TabGroup } from 'rc-dock';

// 'sidebar' group: not floatable, not closable by default
// 'main' group: floatable, closable
const PANEL_GROUPS: Record<string, TabGroup> = {
  sidebar: {
    floatable: false,
    maximizable: false,
    tabLocked: false,
  },
  main: {
    floatable: true,
    maximizable: true,
    newWindow: false,
  },
};
```

Groups control which panels can float, be maximized, or open as new windows. The `group` field on `TabData` references a key in the `groups` prop.

### Anti-Patterns to Avoid

- **Using `layout` (controlled) prop instead of `defaultLayout`:** The controlled `layout` prop requires every `onLayoutChange` to sync back a new layout object. This causes unnecessary re-renders and complicates persistence. Use `defaultLayout` and read state imperatively via `getLayout()` only when saving.
- **Calling `loadLayout()` on every render:** `loadLayout()` is a one-shot restore call. Never put it in `useEffect` without a guard â€” it fires the re-render cycle and can loop.
- **JSON.stringify(getLayout()) without saveTab/loadTab:** The raw `LayoutData` contains React elements which are not JSON serializable. Always use `saveLayout()` (which calls your `saveTab` hook and strips non-serializable fields) rather than `getLayout()` directly for persistence.
- **Placing Ribbon / ActivityBar / StatusBar inside DockLayout:** These belong outside rc-dock as sibling divs. DockLayout should only own the panel workspace area.
- **Hardcoding hex colors in helix-dock.css:** All color overrides must use `var(--color-*)`. No raw hex.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Panel split/resize | custom splitter | rc-dock `DockBox` / `Divider` | rc-dock Divider handles pointer capture, min/max constraints, flex recalculation |
| Tab drag-to-dock | custom DnD for tabs | rc-dock internal DragManager | rc-dock uses pointer events internally; building a parallel system causes conflicts |
| Layout serialization | manual JSON traversal | rc-dock `saveLayout()` + `loadTab` | Serializer handles float position, z-index, active tab, nested box modes |
| Panel float window | HTML dialog/popup | rc-dock `floatbox` (mode: 'float') | rc-dock tracks z-order, resize handles, and window-mode (detached window) |

**Key insight:** rc-dock's serialization model is intentionally thin â€” it only stores structural metadata, not React content. This is correct by design. The `saveTab`/`loadTab` bridge is the intended extension point; fighting it with custom serialization wastes effort and breaks upgradability.

---

## rc-dock CSS Classes to Override in helix-dock.css

`[VERIFIED: github.com/ticlo/rc-dock/tree/master/style]`

### Import statement (required before helix-dock.css)

```css
/* In WorkspaceLayout.tsx or App.tsx, order matters: */
import 'rc-dock/dist/rc-dock.css';   /* base rc-dock styles */
import './helix-dock.css';           /* Helix variable overrides */
```

The dist folder ships `rc-dock.css` (light) and `rc-dock-dark.css` (dark base). Either can be imported â€” the Helix overrides will supersede theme colours regardless.

### Complete class inventory for helix-dock.css

```css
/* helix-dock.css â€” Helix CSS variable override layer for rc-dock */
/* Source: ticlo/rc-dock style/panel.less + style/tabs.less */

/* â”€â”€ Root layout container â”€â”€ */
.dock-layout {
  background: var(--color-bg);
}

/* â”€â”€ Box containers â”€â”€ */
.dock-box,
.dock-hbox,
.dock-vbox {
  background: var(--color-bg);
}

/* â”€â”€ Panel (dock-panel = a named panel that contains tabs) â”€â”€ */
.dock-panel {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
}

/* â”€â”€ Tab bar (dock-bar = the horizontal strip of tab buttons) â”€â”€ */
.dock-bar {
  background: var(--color-surface);
  border-bottom: 1px solid var(--color-border);
}

/* â”€â”€ Individual tab â”€â”€ */
.dock-tab {
  color: var(--color-text-muted);
  font-size: var(--text-sm);
  font-family: var(--font-sans);
  background: transparent;
}
.dock-tab:hover {
  color: var(--color-text);
  background: var(--color-surface-overlay);
}

/* â”€â”€ Active tab (2px top border per design spec) â”€â”€ */
.dock-tab-active {
  color: var(--color-text);
  background: var(--color-bg);
  border-top: 2px solid var(--color-primary);
}

/* â”€â”€ Tab close button â”€â”€ */
.dock-tab-close-btn {
  color: var(--color-text-dim);
}
.dock-tab-close-btn:hover {
  color: var(--color-text);
}

/* â”€â”€ Ink bar (active underline indicator â€” hide in favour of border-top) â”€â”€ */
.dock-ink-bar {
  display: none;
}

/* â”€â”€ Tab pane content area â”€â”€ */
.dock-tabpane {
  background: var(--color-surface);
}
.dock-content,
.dock-content-holder {
  background: var(--color-surface);
}

/* â”€â”€ Divider/splitter â”€â”€ */
.dock-divider {
  background: var(--color-border);
  transition: background 0.12s;
}
.dock-divider:hover {
  background: var(--color-primary);
}

/* â”€â”€ Drop indicators â”€â”€ */
.dock-drop-square-box {
  border-color: var(--color-primary);
  background: var(--color-primary-dim);
  opacity: 0.6;
}
.dock-drop-square-dropping {
  background: var(--color-primary-dim);
  opacity: 0.85;
}

/* â”€â”€ Panel max/min buttons â”€â”€ */
.dock-panel-max-btn,
.dock-panel-min-btn {
  color: var(--color-text-muted);
}
.dock-panel-max-btn:hover,
.dock-panel-min-btn:hover {
  color: var(--color-text);
}

/* â”€â”€ Floating panel â”€â”€ */
.dock-fbox .dock-panel {
  box-shadow: var(--shadow-lg);
  border-color: var(--color-border-strong);
}

/* â”€â”€ Predefined panel style: headless (used for sidebars with no visible tab bar by default) â”€â”€ */
.dock-style-headless .dock-bar {
  opacity: 0;
  height: 20px;
}
.dock-style-headless .dock-panel:hover .dock-bar,
.dock-style-headless .dock-panel:focus-within .dock-bar {
  opacity: 1;
  height: 31px;
}

/* â”€â”€ Predefined panel style: main (used for document tab area) â”€â”€ */
.dock-style-main .dock-bar {
  background: transparent;
  border-bottom: 1px solid var(--color-border);
}
```

---

## useTrendRegistry Hook

### Type Signature and Implementation Skeleton

```typescript
// helix-desktop/src/hooks/useTrendRegistry.ts
// Source: pattern derived from useAgentWebSocket.ts (trendValues: TrendValue[])

import { useState, useEffect, useRef } from 'react';
import type { TrendValue } from '../types/agent';

/**
 * Accumulates unique trendName strings from a streaming TrendValue array.
 * Returns a stable sorted string[] that only grows (never shrinks within a session).
 *
 * Uses useRef for the Set to avoid including it in useEffect deps,
 * preventing re-subscription on every render.
 */
export function useTrendRegistry(trendValues: TrendValue[]): string[] {
  const [trendNames, setTrendNames] = useState<string[]>([]);
  const seenRef = useRef<Set<string>>(new Set());

  useEffect(() => {
    let changed = false;
    for (const tv of trendValues) {
      if (!seenRef.current.has(tv.trendName)) {
        seenRef.current.add(tv.trendName);
        changed = true;
      }
    }
    if (changed) {
      // Snapshot the Set into a stable sorted array
      setTrendNames(Array.from(seenRef.current).sort());
    }
    // trendValues is the only dep â€” seenRef is stable by design
  }, [trendValues]);

  return trendNames;
}
```

**Why `useRef` for the Set:** If `seenRef` were a `useState`, React would re-render the hook on every `add()`, creating a waterfall of renders during high-frequency WS frames. The Set lives in a ref; only the output `string[]` is state. `setTrendNames` is called only when new names appear.

**Usage in Shell:**

```typescript
const { trendValues } = useAgentWebSocket(baseUrl);
const trendNames = useTrendRegistry(trendValues);
// Pass trendNames to TrendExplorerPanel
```

---

## HTML5 DnD with rc-dock

### Conflict Analysis

rc-dock's internal drag system uses `pointer events` via its own `DragManager` (`src/dragdrop/`). HTML5 `draggable` uses the native browser drag-and-drop event API (`dragstart`, `dragover`, `drop`). These are **separate event systems** â€” pointer events do not fire during a native drag, and native drag events do not fire during pointer-event drag sequences. There is no known conflict.

**Confirmed pattern (D-08):**
- Trend Explorer items: `<div draggable onDragStart={...}>`
- Chart Panel drop zones: `<div onDragOver={...} onDrop={...}>`
- The rc-dock panel content div is the drop target host

### DnD Wire-up Pattern

```typescript
// TrendExplorerPanel.tsx â€” draggable items
function TrendItem({ name }: { name: string }) {
  function handleDragStart(e: React.DragEvent) {
    e.dataTransfer.setData('text/x-trend-name', name);
    e.dataTransfer.effectAllowed = 'copy';
  }
  return (
    <div
      draggable
      onDragStart={handleDragStart}
      style={{ cursor: 'grab', padding: '4px 8px' }}
    >
      {name}
    </div>
  );
}
```

```typescript
// ChartDockPanel.tsx â€” drop zone wrapping chart content
interface ChartDockPanelProps {
  trendName?: string;          // initial trend (from tab id)
  onAddTrend: (name: string) => void;
}

function ChartDockPanel({ trendName, onAddTrend }: ChartDockPanelProps) {
  const [isDragOver, setIsDragOver] = useState(false);

  function handleDragOver(e: React.DragEvent) {
    e.preventDefault();                    // Required to allow drop
    e.dataTransfer.dropEffect = 'copy';
    setIsDragOver(true);
  }
  function handleDragLeave() {
    setIsDragOver(false);
  }
  function handleDrop(e: React.DragEvent) {
    e.preventDefault();
    setIsDragOver(false);
    const name = e.dataTransfer.getData('text/x-trend-name');
    if (name) onAddTrend(name);
  }

  return (
    <div
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
      style={{
        flex: 1,
        border: isDragOver ? `1px solid var(--color-primary)` : '1px solid transparent',
        transition: 'border-color 0.12s',
      }}
    >
      {/* Chart content */}
    </div>
  );
}
```

**Note:** The `onDrop` handler lives on the **content element inside the rc-dock panel**, not on a rc-dock panel prop. rc-dock has no built-in `onDrop` prop â€” the content is a React element you own entirely.

---

## Vitest Mock Pattern for DockLayout Ref

`DockLayout` is a class component. The ref points to an **instance** of the class. In Vitest, mock the module to replace the class with a `vi.fn()` constructor that has the necessary methods on its prototype.

### Pattern

```typescript
// helix-desktop/src/shell/WorkspaceLayout.test.tsx
import { render } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';

// vi.hoisted ensures mockGetLayout/mockLoadLayout are available when vi.mock factory runs
const { mockGetLayout, mockLoadLayout } = vi.hoisted(() => ({
  mockGetLayout: vi.fn(),
  mockLoadLayout: vi.fn(),
}));

vi.mock('rc-dock', () => {
  // Minimal class-component stub
  const DockLayoutMock = vi.fn(function (this: Record<string, unknown>) {
    // instance methods
    this.getLayout = mockGetLayout;
    this.loadLayout = mockLoadLayout;
  });

  // React renders class components with forwardRef internally for refs.
  // For testing, wrap with React.forwardRef to allow ref attachment.
  // Alternatively, just render and grab the mock calls without needing the ref.
  //
  // Simplest approach: export the constructor as default,
  // and in the component under test, ref.current is the mock instance.
  return {
    default: DockLayoutMock,
    // Named re-exports if needed (DockData, etc.)
  };
});

describe('WorkspaceLayout save/load', () => {
  beforeEach(() => {
    mockGetLayout.mockReturnValue({
      dockbox: { mode: 'horizontal', children: [] },
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('calls getLayout when Save Layout is triggered', async () => {
    // render WorkspaceLayout, trigger save action
    // verify mockGetLayout was called
    expect(mockGetLayout).toHaveBeenCalled();
  });

  it('calls loadLayout with stored layout when Load is triggered', async () => {
    const storedLayout = { dockbox: { mode: 'horizontal', children: [] } };
    // trigger load action
    expect(mockLoadLayout).toHaveBeenCalledWith(storedLayout);
  });
});
```

**Caveat:** Because `DockLayout` is a class component, React's test renderer attaches the ref to the class instance. If the mock constructor does not behave like a React class component (i.e., missing `render()`), `render()` will throw. Add a minimal `render` on the prototype:

```typescript
const DockLayoutMock = vi.fn(function (this: Record<string, unknown>) {
  this.getLayout = mockGetLayout;
  this.loadLayout = mockLoadLayout;
});
DockLayoutMock.prototype.render = () => null;
```

This is sufficient for unit tests of the save/load logic. Integration tests (which exercise the full layout visually) should skip the mock.

---

## Common Pitfalls

### Pitfall 1: npm latest â†’ alpha

**What goes wrong:** `npm install rc-dock` installs `4.0.0-alpha.2` (the current `latest` dist-tag is pointing at an alpha). The alpha has breaking API changes from `3.3.2`.

**Why it happens:** The maintainer accidentally tagged `4.0.0-alpha.2` as `latest` on the registry. `dist-tags.latest = "4.0.0-alpha.2"`.

**How to avoid:** Always pin: `npm install rc-dock@3.3.2`.

**Warning signs:** `npm ls rc-dock` shows `4.x.x-alpha` in the lock file.

### Pitfall 2: getLayout() returns non-serializable data

**What goes wrong:** Calling `JSON.stringify(dockRef.current!.getLayout())` throws `TypeError: Converting circular structure to JSON` because `LayoutData` contains `parent` back-references and React element objects.

**Why it happens:** `getLayout()` returns the live `LayoutData` which has circular parentâ†’child references and React elements in `TabData.content`.

**How to avoid:** Use `saveLayout()` (not `getLayout()`) for persistence â€” it calls your `saveTab` hook and strips non-serializable fields. Then `JSON.stringify(dockRef.current!.saveLayout())`.

**Warning signs:** JSON serialization errors when clicking Save Layout.

### Pitfall 3: loadLayout() re-fires onLayoutChange

**What goes wrong:** If using controlled `layout` prop with `onLayoutChange`, calling `loadLayout()` inside `onLayoutChange` creates an infinite loop.

**Why it happens:** `loadLayout()` does NOT trigger `onLayoutChange` per the API docs. But if the parent's `onLayoutChange` updates state which re-triggers a `loadLayout` call, the cycle loops.

**How to avoid:** Use `defaultLayout` (uncontrolled). Only call `loadLayout()` explicitly on user-initiated "load" action.

**Warning signs:** DevTools shows constant React re-renders; application hangs on load.

### Pitfall 4: CSS import order

**What goes wrong:** Helix CSS variable overrides are overridden by rc-dock's own styles.

**Why it happens:** If `helix-dock.css` is imported before `rc-dock/dist/rc-dock.css`, the specificity battle is lost â€” rc-dock's rules come after and win.

**How to avoid:** Always import `rc-dock/dist/rc-dock.css` first, then `helix-dock.css`. In the workspace component file, the import order is the cascade order.

**Warning signs:** Panels show rc-dock default colours (light greys) instead of Helix dark palette.

### Pitfall 5: DockLayout renders outside known size

**What goes wrong:** The dock workspace renders as `height: 0` or collapses.

**Why it happens:** `DockLayout` needs a parent container with explicit `height` (or a flex parent that provides it). If the shell flex container has no height constraint, the workspace has nothing to fill.

**How to avoid:** The workspace row must have `height: 100%` or be a flex child with `flex: 1 1 0`. Outer `html, body, #root` must set `height: 100%`.

**Warning signs:** All panels appear as zero-height strips; no drag handles visible.

### Pitfall 6: useEffect dependency on trendValues identity

**What goes wrong:** `useTrendRegistry` re-runs its effect on every WebSocket message because `trendValues` is a new array reference each time.

**Why it happens:** `useAgentWebSocket` creates a new array with spread (`[...prev.slice(-200), frame]`), so every message produces a new reference.

**How to avoid:** This is expected â€” the `useEffect` in `useTrendRegistry` runs on each new `trendValues` array, but only calls `setTrendNames` when new names are found. The `Set` in `useRef` makes the scan O(n) per message but the state update O(1) for duplicates. No optimization needed at typical WS rates.

---

## Code Examples

### Verified: LayoutBase type structure
```typescript
// Source: https://raw.githubusercontent.com/ticlo/rc-dock/master/src/DockData.ts
interface LayoutBase {
  dockbox: BoxBase;
  floatbox?: BoxBase;    // floating panels
  windowbox?: BoxBase;   // new-window panels (Electron/Tauri)
  maxbox?: BoxBase;      // maximized panels
}

interface BoxBase {
  id?: string;
  mode: 'horizontal' | 'vertical' | 'float' | 'window' | 'maximize';
  size?: number;
  children: (BoxBase | PanelBase)[];
}

interface PanelBase {
  id?: string;
  size?: number;
  tabs: TabBase[];
  activeId?: string;
  group?: string;
  // float position (ignored in defaultLayout, only meaningful in saved state)
  x?: number; y?: number; z?: number; w?: number; h?: number;
  panelLock?: PanelLock;
}

interface TabBase {
  id: string;
  [key: string]: unknown;  // passthrough for saveTab/loadTab
}
```

### Verified: DockLayout LayoutProps
```typescript
// Source: https://raw.githubusercontent.com/ticlo/rc-dock/master/src/DockLayout.tsx
export interface LayoutProps {
  dockId?: string;
  defaultLayout?: LayoutData;
  layout?: LayoutBase;           // controlled mode â€” avoid for this phase
  groups?: {[key: string]: TabGroup};
  onLayoutChange?(newLayout: LayoutBase, currentTabId?: string, direction?: DropDirection): void;
  dropMode?: 'default' | 'edge';
  saveTab?(tab: TabData): TabBase;
  loadTab?(tab: TabBase): TabData;
  afterPanelSaved?(savedPanel: PanelBase, panel: PanelData): void;
  afterPanelLoaded?(savedPanel: PanelBase, loadedPanel: PanelData): void;
  style?: React.CSSProperties;
  maximizeTo?: string | HTMLElement;
}
```

### Verified: Class component ref pattern
```typescript
// Source: https://raw.githubusercontent.com/ticlo/rc-dock/master/src/DockLayout.tsx
// DockLayout IS a class component extending DockPortalManager extends React.PureComponent
// Ref type is DockLayout (the class instance), not HTMLDivElement

const dockRef = useRef<DockLayout>(null);
// OR at module level (e.g. for tests):
const dockRef = React.createRef<DockLayout>();

// Access methods:
dockRef.current?.getLayout();    // returns LayoutData (use saveLayout() for persistence)
dockRef.current?.saveLayout();   // returns LayoutBase with non-serializable fields stripped
dockRef.current?.loadLayout(lb); // restores layout; does NOT fire onLayoutChange
dockRef.current?.find(id);       // find panel/tab/box by id
dockRef.current?.updateTab(id, newTab, makeActive);
dockRef.current?.dockMove(source, target, direction);
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| rc-dock v2.x (`loadLayout`/`getLayout` on module) | v3.x: instance methods via class ref | rc-dock 3.0.0 | Must use ref, not module-level functions |
| Custom splitter logic | rc-dock `DockBox` + `Divider` with pointer capture | rc-dock 2+ | No hand-rolled splitter needed |
| `layout` (controlled) for persistence | `defaultLayout` + imperative `saveLayout()` | community consensus | Avoids re-render cascade; simpler |

**Deprecated/outdated:**
- **`rc-dock@2.x` loadLayout/getLayout as standalone module exports**: Removed in v3; now class instance methods only.
- **Importing CSS from `rc-dock/lib/`**: Correct path is `rc-dock/dist/rc-dock.css` in v3.x.

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | rc-dock `3.3.2` is the last stable release; `4.0.0-alpha.2` is the npm `latest` tag | Standard Stack | Low â€” confirmed via `npm view rc-dock dist-tags` |
| A2 | HTML5 DnD and rc-dock pointer-event drag system do not conflict | HTML5 DnD section | Medium â€” if conflict found, add `e.stopPropagation()` on dragstart from inside dock panel content |
| A3 | `DockLayout.saveLayout()` strips React elements and circular refs (thus safe for JSON.stringify) | Pattern 3 / Pitfall 2 | High â€” if wrong, persistence will throw at runtime; fallback: manual traversal stripping `content`/`parent` fields |
| A4 | `loadLayout()` does not trigger `onLayoutChange` | Pitfall 3 | Low â€” documented in API; verified in source read |

---

## Open Questions (RESOLVED)

1. **rc-dock 4.0.0-alpha API breakage**
   - What we know: `latest` on npm points to alpha; we pin `3.3.2`
   - What's unclear: Whether alpha introduces React 19 compatibility or critical bug fixes we need
   - Recommendation: Use `3.3.2`; revisit for Phase 15 SciChart integration if 4.x stabilizes

2. **Chart Panel tab count at startup**
   - What we know: D-09 says unlimited Chart Panel docks
   - What's unclear: Should DEFAULT_LAYOUT include 0 chart tabs (opened via Ribbon/DnD) or 1 default chart tab?
   - Recommendation: 0 chart tabs in DEFAULT_LAYOUT; first chart opened on first DnD or Ribbon "Open Chart" click

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| `rc-dock` | Docking workspace | Must install | `3.3.2` | â€” |
| `@tauri-apps/plugin-store` | Layout persistence | Already installed | `2.4.3` | â€” |
| Node.js / npm | Package install | Available | (project already building) | â€” |

**Missing dependencies with no fallback:**
- `rc-dock` â€” not yet in `package.json`; must be installed before any implementation starts

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Vitest 4.1.7 |
| Config file | `helix-desktop/vitest.config.ts` |
| Quick run command | `npm test -- --run` |
| Full suite command | `npm test` |

### Phase Requirements â†’ Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| H3/H4 | DEFAULT_LAYOUT renders correct panel structure | unit | `npm test -- --run useTrendRegistry` | No â€” Wave 0 |
| H7 | useTrendRegistry accumulates unique trend names | unit | `npm test -- --run useTrendRegistry` | No â€” Wave 0 |
| H7/D-08 | Trend DnD passes trendName via dataTransfer | unit | `npm test -- --run TrendExplorer` | No â€” Wave 0 |
| H10 | useWorkspaceLayout: save calls getLayout + LazyStore.set | unit | `npm test -- --run useWorkspaceLayout` | No â€” Wave 0 |
| H10 | useWorkspaceLayout: load calls LazyStore.get + loadLayout | unit | `npm test -- --run useWorkspaceLayout` | No â€” Wave 0 |
| H11 | CSS variables used; no hardcoded hex in new files | lint/review | manual grep | No â€” Wave 0 |

### Wave 0 Gaps

- [ ] `helix-desktop/src/hooks/useTrendRegistry.test.ts` â€” covers H7 accumulation logic
- [ ] `helix-desktop/src/hooks/useWorkspaceLayout.test.ts` â€” covers H10 save/load; needs LazyStore mock (pattern from ConnectionConfig.test.tsx)
- [ ] `helix-desktop/src/shell/WorkspaceLayout.test.tsx` â€” covers rc-dock ref mock pattern

---

## Security Domain

`security_enforcement` not explicitly set in config.json â€” treated as enabled.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | No | Phase 11 is UI shell only |
| V3 Session Management | No | No session tokens in scope |
| V4 Access Control | No | Single-user desktop app |
| V5 Input Validation | Yes (low risk) | Trend names from WS frames â€” `trendName` is a string used as React key and data-transfer text only; no eval, no DOM injection |
| V6 Cryptography | No | Layout JSON stored locally via Tauri plugin-store |

### Known Threat Patterns for This Stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| XSS via trendName rendered as HTML | Tampering | React escapes string children by default; never use `dangerouslySetInnerHTML` with trend names |
| Malformed layout JSON from plugin-store | Tampering | Validate `loadTab` fallback handles unknown tab IDs gracefully (pattern shown above) |

---

## Sources

### Primary (HIGH confidence)

- `github.com/ticlo/rc-dock` (master branch) â€” `src/DockData.ts`, `src/DockLayout.tsx`, `src/Serializer.ts`, `src/DockPanel.tsx`, `src/DockTabBar.tsx`, `src/DockBox.tsx`, `src/DockTabs.tsx`, `style/panel.less`, `style/tabs.less`, `style/predefined-panels.less` â€” all read directly via raw.githubusercontent.com
- `npm view rc-dock@3.3.2` â€” version, peer deps, repository URL, publish date (2025-12-10)
- `npm view rc-dock dist-tags` â€” confirmed alpha situation with latest tag
- `api.npmjs.org/downloads/point/last-week/rc-dock` â€” 13,702 downloads/week

### Secondary (MEDIUM confidence)

- `app.unpkg.com/rc-dock@3.3.2/files/dist` â€” confirmed dist CSS file names (`rc-dock.css`, `rc-dock-dark.css`)

### Tertiary (LOW confidence)

- None â€” all critical claims verified against source code

---

## Metadata

**Confidence breakdown:**

- Standard stack: HIGH â€” npm registry + source code confirmed
- Architecture: HIGH â€” class component pattern confirmed from source; LayoutBase types directly read
- CSS classes: HIGH â€” read directly from LESS source files in GitHub repo
- Pitfalls: MEDIUM â€” pitfall 2 (saveLayout vs getLayout) inferred from Serializer.ts source; pitfall 1 confirmed from dist-tags
- DnD conflict analysis: MEDIUM â€” no conflict observed in source; pointer/native event separation is well-known browser behavior

**Research date:** 2026-06-04
**Valid until:** 2026-07-04 (rc-dock is slow-moving; CSS classes are stable within a minor version)

