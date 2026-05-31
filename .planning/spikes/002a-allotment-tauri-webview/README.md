---
spike: "002a"
name: allotment-tauri-webview
type: standard
validates: "Given allotment renders in Vite dev (spike 001), when npm run tauri dev launches the same layout in Tauri 2, then resize handles respond and no CSP errors appear"
verdict: VALIDATED
related: ["001-allotment-basic-layout"]
tags: [allotment, tauri, webview, csp]
---

# Spike 002a: Allotment in Tauri Webview

## What This Validates
Confirms allotment works inside the Tauri 2 webview — CSP compatibility, resize behaviour,
and panel collapse in the native window.

## How to Run

```bash
cp .planning/spikes/001-allotment-basic-layout/HelixShellSpike.tsx helix-desktop/src/
# swap App.tsx to: import { HelixShellSpike } from "./HelixShellSpike"; export default function App() { return <HelixShellSpike />; }
cd helix-desktop && npm run tauri dev
```

## Checklist
- [x] Left / right / bottom panel drag-resize works perfectly
- [x] Double-click sash collapses/expands panel
- [x] No CSP errors, no build errors
- [x] Status bar interactive
- [x] No white-flash on startup

## Investigation Trail

### Round 1 — Basic render
Allotment renders correctly inside the Tauri 2 webview. Drag handles respond, double-click
collapse works. No CSP errors. Layout and interaction identical to browser dev server.

### Round 2 — Scrollbar issue
Tauri webview showed a vertical scrollbar — the shell content was slightly taller than the
viewport. Root cause: `body` default margin/padding not fully reset, combined with `height:100vh`
on the shell div.

**Fix:** Add `html, body { height: 100%; margin: 0; padding: 0; overflow: hidden; }` to the
component's CSS. This is a **Tauri-specific requirement** — the webview body must be locked to
prevent any overflow scrolling. Browser dev server is more forgiving; Tauri native window is not.

After fix: no scrollbar, IDE shell fills the full webview area below the native title bar.

## Results

**Verdict: VALIDATED ✓**

allotment works in Tauri 2 webview with no CSP issues and no functional degradation.

**One required CSS rule for Tauri desktop apps:**
```css
html, body {
  height: 100%;
  margin: 0;
  padding: 0;
  overflow: hidden; /* required — Tauri webview does not clip body overflow by default */
}
```

This goes in the global CSS (e.g. `index.css`) for the real implementation, not inline.