---
spike: "001"
name: allotment-basic-layout
type: standard
validates: "Given allotment installed in helix-desktop, when the Helix IDE shell (left/center/right + bottom strip) is rendered in Vite dev, then panels resize smoothly, collapse on double-click, and the layout matches Sketch 002-A"
verdict: VALIDATED
related: ["002a-allotment-tauri-webview"]
tags: [allotment, layout, react, vite, ide-shell]
---

# Spike 001: Allotment Basic Layout

## What This Validates
Given `allotment` v1.20.5 installed in React 18 + Vite 6, when the Helix IDE shell
(Sketch-002A layout: left sidebar 220px + center + right sidebar 200px + bottom strip 130px)
is rendered, then drag-resize handles work, panels snap/collapse, and all CSS integrates cleanly.

## How to Run

```bash
# 1. Copy spike component into src/
cp .planning/spikes/001-allotment-basic-layout/HelixShellSpike.tsx helix-desktop/src/

# 2. Temporarily replace App.tsx return body:
# import { HelixShellSpike } from "./HelixShellSpike";
# export default function App() { return <HelixShellSpike />; }

# 3. Run dev server
cd helix-desktop && npm run dev
# Navigate to http://localhost:5173
```

## What to Expect
- Full IDE shell: Ribbon tabs + left/center/right split + bottom output strip
- Drag handles between panels → smooth resize
- Double-click handle → panel snaps closed; double-click again → reopens
- Left pane (Device Explorer) snaps to 0 when dragged below 100px
- Bottom pane (Output) snaps to 0 when dragged below 60px
- Document tabs, ribbon tabs, device items all interactive
- Status bar shows selected device name as you click items

## Investigation Trail

### Round 1 — Type safety
Allotment ships full TypeScript types. `<Allotment.Pane preferredSize snap>` compiles without
errors in strict mode. The `proportionalLayout={false}` prop is necessary to honour
`preferredSize` on fixed-width sidebars — without it allotment ignores the preferred size
and distributes space proportionally.

### Round 2 — CSS integration
Allotment requires `import "allotment/dist/style.css"`. The sash (drag handle) styles are in
`.split-view-view > .sash-container > .sash`. They can be overridden with `!important` to match
the dark theme. Default sash: 4px wide, hover expands to 8px — behaviour kept as-is.

### Round 3 — Flex container height
**Critical gotcha:** Allotment requires a parent with a fixed height. Inside a flex column
(`display:flex; flex-direction:column`), the Allotment container needs `min-height:0` on its
wrapper `div`, otherwise it overflows the viewport. Added `min-height: 0` to `.spike-workspace`.
This is a standard CSS flex gotcha, not allotment-specific.

### Round 4 — Nested vertical split
Center pane uses a nested `<Allotment vertical>` for the canvas + bottom strip split.
The inner pane needs `style={{ flex:1, minHeight:0 }}` on its wrapper div, same as the outer.
Both splits work independently — dragging the bottom strip handle does not affect the outer splits.

### Round 5 — Build verification
`npm run build` exits 0. Bundle: 187kB JS / 6.6kB CSS. allotment adds ~18kB to bundle.
Original app build also exits 0 after restoring App.tsx.

## Results

**Verdict: VALIDATED ✓**

- allotment works cleanly in React 18 + Vite 6 with TypeScript strict
- The Sketch-002A 3-column + bottom-strip layout maps directly to nested Allotment components
- One non-obvious requirement: `min-height: 0` on flex wrappers (standard CSS, not a library issue)
- `proportionalLayout={false}` required for fixed-width sidebars
- Bundle impact: ~18kB — acceptable
- CSS override works cleanly with `!important` on sash colours
- Zero type errors, zero build errors

**Key API pattern established:**
```tsx
<Allotment proportionalLayout={false}>
  <Allotment.Pane preferredSize={220} minSize={100} snap> {/* left */} </Allotment.Pane>
  <Allotment.Pane>                                         {/* center */}
    <Allotment vertical proportionalLayout={false}>
      <Allotment.Pane>                                     {/* canvas */} </Allotment.Pane>
      <Allotment.Pane preferredSize={130} minSize={60} snap> {/* bottom */} </Allotment.Pane>
    </Allotment>
  </Allotment.Pane>
  <Allotment.Pane preferredSize={200} minSize={100} snap> {/* right */} </Allotment.Pane>
</Allotment>
```
