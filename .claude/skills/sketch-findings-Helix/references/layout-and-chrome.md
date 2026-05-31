# Layout & Chrome

## Design Decisions

### Ribbon (Sketch 001 — Variant A)
- **Full VS-style Ribbon** with tab strip above the band. Tabs: Home, Devices, Trends, Workflows, Charts, Optimisation, Settings.
- **Ribbon band height: 68px.** Divided into named groups (Execution / Open Panel / Workspace / Devices) separated by 1px borders.
- **Large buttons (44px min-width)** for primary actions (Run, Open Devices, Open Builder). Small stacked buttons for secondary actions (Pause/Abort, Save/Load Layout).
- **Ribbon background: `#1a1a2a`** — slightly darker than the surface, giving the chrome visual weight distinct from the workspace.
- **Active tab indicator:** background lifts to `--color-ribbon-active` with border-bottom matching that color (hidden bottom border trick).
- **Button hover:** `var(--color-surface-overlay)` fill + `var(--color-border)` border. Never a solid colored fill.

### OS Title Bar (Sketch 001 — Variant A)
- 30px height. Logo icon (14px, primary-colored square), centered document title, macOS-style traffic light controls on right.
- Title format: `{active-document} · Helix Desktop`

### Activity Bar (Sketch 002 — Variant A)
- **40px wide**, left-most chrome column.
- Icon buttons: 32×32px, `border-radius: var(--radius-sm)`, no label.
- Active indicator: **2px left-edge bar** in `--color-primary`, not a background fill.
- Bottom-anchored: Settings icon at the foot; explorer icons top-down.

### Split Panel Layout (Sketch 002 — Variant A)
- **Left sidebar: 220px default**, min 100px, max 400px. Draggable splitter (4px, `cursor: col-resize`).
- **Right sidebar: 200px default**, same constraints.
- **Center area:** flex-1, contains document tab strip + canvas + bottom output strip.
- **Bottom output strip: 140px default.** Tab strip (Output / Device Log / Events / Python) + monospace log area.
- All sidebars collapsible (width → 0, `overflow: hidden`, `transition: width 0.2s`).

### Document Tabs
- **Height: ~34px.** Background: `var(--color-surface)` inactive, `var(--color-bg)` active.
- **Active indicator: 2px top border** in `var(--color-primary)`.
- Close button (✕) appears on hover only (`opacity: 0` → `opacity: 1`).
- Tab text: emoji prefix + name (e.g. `⬡ Workflow Builder`, `📈 Temperature`).

### Status Bar
- **22px height**, background `var(--color-primary-dim)`, text `var(--color-primary)`.
- Left: status dots (6px circles) + agent/device connection names.
- Right: active runspace state + version.

## CSS Patterns

```css
/* Ribbon tab strip */
.ribbon-tabs {
  background: var(--color-ribbon-bg);
  display: flex;
  align-items: flex-end;
  padding: 0 8px;
  height: 26px;
  gap: 1px;
  border-bottom: 1px solid var(--color-border);
}
.ribbon-tab {
  padding: 4px 14px;
  font-size: var(--text-xs);
  border-radius: var(--radius-sm) var(--radius-sm) 0 0;
  border: 1px solid transparent;
  border-bottom: none;
  cursor: pointer;
  transition: all 0.12s;
}
.ribbon-tab.active {
  background: var(--color-ribbon-active);
  border-color: var(--color-border);
  border-bottom-color: var(--color-ribbon-active); /* hides bottom border */
}

/* Activity bar active indicator */
.abtn.active::before {
  content: '';
  position: absolute;
  left: 0; top: 25%; bottom: 25%;
  width: 2px;
  background: var(--color-primary);
  border-radius: 0 2px 2px 0;
}

/* Document tab active */
.doctab.active {
  background: var(--color-bg);
  color: var(--color-text);
  border-top: 2px solid var(--color-primary);
}

/* Splitter */
.splitter.h {
  width: 4px;
  cursor: col-resize;
  background: var(--color-border);
  transition: background 0.12s;
}
.splitter.h:hover { background: var(--color-primary); }
```

## HTML Structures

```html
<!-- Shell skeleton (top to bottom) -->
<div class="titlebar">…</div>
<div class="ribbon-tabs">…</div>
<div class="ribbon-band">…</div>
<div class="workspace"> <!-- flex row -->
  <div class="actbar">…</div>
  <div class="sidebar-left">…</div>
  <div class="splitter h">…</div>
  <div class="main-area"> <!-- flex col -->
    <div class="doctabs">…</div>
    <div class="canvas">…</div>
    <div class="bottom-panel">…</div>
  </div>
  <div class="splitter h">…</div>
  <div class="sidebar-right">…</div>
</div>
<div class="statusbar">…</div>
```

## What to Avoid
- **Variant B (Figma-style):** Single topbar hides the Ribbon's discoverability advantage. Scientists need to find capabilities without memorising icon meanings.
- **Variant C (menu+toolbar):** Classic menu bar feels dated and wastes two chrome rows. The Ribbon consolidates both.
- **Floating panels (002-C):** Too chaotic for a data-dense lab tool. Scientists need predictable panel positions during experiments.
- **JetBrains dock groups (002-B):** Tab groups are good but add UI complexity; VS-style split panels are simpler and sufficient for the initial panel set.

## Origin
Synthesized from sketches: 001-ribbon-workspace-shell (Winner: A), 002-docking-panel-model (Winner: A)
Source files: `sources/001-ribbon-workspace-shell/`, `sources/002-docking-panel-model/`
