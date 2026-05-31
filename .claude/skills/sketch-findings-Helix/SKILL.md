---
name: sketch-findings-Helix
description: Validated design decisions, CSS patterns, and visual direction from Helix Desktop sketch experiments. Auto-load during any UI implementation for the Helix Tauri desktop app.
---

<context>
## Project: Helix (helix-desktop Tauri app)

Dark, dense, professional IDE layout for a lab automation desktop app. The feel of a scientific instrument control surface — not a consumer SaaS product. Primary audience: scientists who are power users comfortable with VS Code / JetBrains–style UIs.

**Reference points:** Visual Studio Code (activity bar, split panels, document tabs), JetBrains Rider (dock groups), Visual Studio (Ribbon).

Sketch sessions wrapped: 2026-05-30
</context>

<design_direction>
## Overall Direction

**Shell:** VS-style Ribbon (68px band with grouped icon buttons) sits above a split-panel workspace. Activity bar (40px) on the far left. Left sidebar (Device Explorer, 220px) and right sidebar (Trend Explorer, 200px) flank a central document-tab canvas. Bottom output strip (140px) for logs, events, and Python console.

**Palette:** Dark IDE — `#1e1e2e` outermost chrome, `#252535` panel surfaces, `#9d7fff` primary accent. All CSS variables defined in `sources/themes/default.css`.

**Typography:** Segoe UI / system-ui, 0.875rem base. Monospace: Cascadia Code / JetBrains Mono. All labels: uppercase, 0.05–0.08em letter-spacing, `var(--text-xs)`.

**Interaction patterns:** 2px left-edge active indicators on icon buttons (never filled backgrounds for active state). Active document tabs: 2px top border in primary color. Hover: surface-overlay fill + border. Transitions: `all 0.12s ease` baseline.

**Manual control:** Card grid layout. One card per capability. Quick Execute inline for no-parameter capabilities; expandable inputs for parameterised ones. Destructive capabilities styled with dark red border/header. Result badge (in-context, not toast). Confirm dialog for destructive actions.
</design_direction>

<findings_index>
## Design Areas

| Area | Reference | Key Decision |
|------|-----------|--------------|
| Layout & Chrome | references/layout-and-chrome.md | VS-Ribbon + VS-Split panels + activity bar + document tabs |
| Manual Control UI | references/manual-control-ui.md | Card grid with Quick Execute + expandable params + danger card styling |

## Theme

Winning theme file: `sources/themes/default.css`

All CSS custom properties: `--color-bg`, `--color-surface`, `--color-surface-raised`, `--color-surface-overlay`, `--color-primary`, `--color-primary-dim`, `--color-border`, `--color-ribbon-bg`, `--color-ribbon-active`, `--font-sans`, `--font-mono`, `--text-xs` through `--text-xl`, `--space-1` through `--space-8`, `--radius-sm/md/lg`, `--shadow-sm/md/lg`.

## Source Files

Original sketch HTML files are in `sources/` for full reference:
- `sources/001-ribbon-workspace-shell/index.html` — Ribbon variants (A selected)
- `sources/002-docking-panel-model/index.html` — Docking panel variants (A selected)
- `sources/003-manual-control-dialog/index.html` — Manual control variants (B selected)
</findings_index>

<metadata>
## Processed Sketches

- 001-ribbon-workspace-shell (Winner: A — Dense VS-Ribbon)
- 002-docking-panel-model (Winner: A — VS Split Panels)
- 003-manual-control-dialog (Winner: B — Card Grid)
</metadata>
