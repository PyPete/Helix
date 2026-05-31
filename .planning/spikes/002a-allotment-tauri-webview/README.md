---
spike: "002a"
name: allotment-tauri-webview
type: standard
validates: "Given allotment renders in Vite dev (spike 001), when npm run tauri dev launches the same layout in Tauri 2, then resize handles respond and no CSP errors appear"
verdict: PENDING
related: ["001-allotment-basic-layout"]
tags: [allotment, tauri, webview, csp]
---

# Spike 002a: Allotment in Tauri Webview

## What This Validates
Spike 001 proved allotment works in the browser. This confirms it works inside the
Tauri 2 webview — checking for CSP conflicts, renderer quirks, and resize degradation.

## How to Run

```bash
# 1. Copy spike component into src/
cp .planning/spikes/001-allotment-basic-layout/HelixShellSpike.tsx helix-desktop/src/

# 2. Temporarily swap App.tsx return:
#    import { HelixShellSpike } from "./HelixShellSpike";
#    export default function App() { return <HelixShellSpike />; }

# 3. Launch Tauri dev
cd helix-desktop && npm run tauri dev
```

## Checklist
- [ ] Left / right / bottom panel drag-resize works
- [ ] Double-click sash collapses/expands panel
- [ ] No CSP errors in webview console (F12)
- [ ] No white-flash on startup
- [ ] Status bar device selection responds to click

## Results
Pending human verification — run the steps above and report back.