# Phase 11: Ribbon + Docking Workspace Shell - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md.

**Date:** 2026-06-04
**Phase:** 11-ribbon-docking-workspace-shell
**Areas discussed:** Docking library choice, App.tsx migration strategy, Trend Explorer wiring, Workspace layout persistence

---

## Docking library choice

| Option | Selected |
|--------|----------|
| rc-dock — feature-complete docking | yes |
| Allotment (split panels only) | |
| Hand-rolled CSS flex | |

rc-dock selected.

| Option | Selected |
|--------|----------|
| Hardcoded DEFAULT_LAYOUT + user-overridable | yes |
| User picks layout on first launch | |
| You decide | |

Hardcoded default selected.

| Option | Selected |
|--------|----------|
| CSS variable override layer (helix-dock.css) | yes |
| Fork rc-dock CSS | |
| You decide | |

CSS variable override layer selected.

---

## App.tsx migration strategy

| Option | Selected |
|--------|----------|
| Big-bang replacement | yes |
| Wrap-and-expand | |
| New shell in parallel then swap | |

Big-bang replacement selected.

| Option | Selected |
|--------|----------|
| Home + Devices wired; rest placeholder | yes |
| All 7 tabs wired | |
| You decide | |

Home + Devices wired; rest placeholder selected.

| Option | Selected |
|--------|----------|
| Planner maps components to panels | yes |
| All in central canvas for now | |
| You specify the mapping | |

Planner maps components selected.

---

## Trend Explorer wiring

| Option | Selected |
|--------|----------|
| From live WebSocket trendValues (useTrendRegistry) | yes |
| Poll Agent /trends endpoint | |
| You decide | |

Live WebSocket discovery selected.

| Option | Selected |
|--------|----------|
| HTML5 drag-and-drop with panel drop target | yes |
| Click-to-add via Chart Panel selector | |
| You decide | |

HTML5 DnD selected.

| Option | Selected |
|--------|----------|
| Unlimited Chart Panel docks | yes |
| Fixed 2 docks (scalar + spectral) | |
| You decide | |

Unlimited docks selected.

---

## Workspace layout persistence

| Option | Selected |
|--------|----------|
| tauri-plugin-store via LazyStore | yes |
| tauri-plugin-fs JSON file | |
| You decide | |

LazyStore selected.

| Option | Selected |
|--------|----------|
| rc-dock LayoutBase JSON tree only | yes |
| rc-dock layout + open document tabs | |
| You decide | |

LayoutBase JSON tree only selected.

| Option | Selected |
|--------|----------|
| Save Layout in Ribbon Home + named input prompt | yes |
| Dedicated Layouts panel / Settings tab | |
| You decide | |

Ribbon Home Save Layout selected.

---

## Claude Discretion

- Panel IDs and rc-dock LayoutBase structure
- CSS class naming in helix-dock.css
- Ribbon button group details
- Status bar inclusion

## Deferred Ideas

- Multi-Mode Workflow Authoring — Phase 13
- SciChart full integration — Phase 15
- ReactFlow node canvas — Phase 14
- Trend Explorer replay mode — Phase 17
- Developer Debugger panel — Phase 16