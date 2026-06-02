---
phase: "7"
plan: "wave-0"
subsystem: "helix-dashboard + SiteServer WebSocket + Experiment API"
tags: [react, vite, typescript, vitest, websocket, aspnetcore, tdd-red]
dependency_graph:
  requires: []
  provides:
    - helix-dashboard Vite scaffold with SciChart WASM
    - TrendWebSocketManager singleton (C#)
    - GET /api/trends/ws endpoint wired in Program.cs
    - GET /api/experiments endpoint (E4 gap closed)
    - RED tests for App + useTrendWebSocket (Wave 1 TDD contract)
  affects:
    - Helix.Orchestration.SiteServer/Program.cs
    - Helix.Orchestration.SiteServer/Mqtt/MqttTrendBridge.cs
    - Helix.Orchestration.Infrastructure/Persistence/IExperimentRepository.cs
    - Helix.Orchestration.Infrastructure/Persistence/PostgresExperimentRepository.cs
tech_stack:
  added:
    - scichart@5.2.32 (WebGL chart engine + WASM)
    - scichart-react@1.0.0 (React wrapper)
    - react-router@7.16.0 (v7 declarative routing)
    - vitest@4.1.8 + @testing-library/* (mirrored from helix-desktop)
  patterns:
    - TDD RED: test contracts written before implementations
    - ConcurrentDictionary WebSocket fan-out (T-7-04 DoS mitigation)
    - postinstall WASM copy (Vite public/ pattern)
    - Minimal API MapGet before MapPost (endpoint order)
key_files:
  created:
    - helix-dashboard/package.json
    - helix-dashboard/vite.config.ts
    - helix-dashboard/vitest.config.ts
    - helix-dashboard/index.html
    - helix-dashboard/tsconfig.json + tsconfig.app.json + tsconfig.node.json
    - helix-dashboard/scripts/copy-wasm.js
    - helix-dashboard/public/scichart2d.wasm (+ nosimd + .js)
    - helix-dashboard/src/themes/default.css
    - helix-dashboard/src/test/setup.ts
    - helix-dashboard/src/App.test.tsx (RED)
    - helix-dashboard/src/hooks/useTrendWebSocket.test.ts (RED)
    - Helix.Orchestration.SiteServer/WebSockets/TrendWebSocketManager.cs
  modified:
    - Helix.Orchestration.SiteServer/Program.cs
    - Helix.Orchestration.SiteServer/Mqtt/MqttTrendBridge.cs
    - Helix.Orchestration.SiteServer/Contracts/ExperimentContracts.cs
    - Helix.Orchestration.SiteServer/Endpoints/ExperimentEndpoints.cs
    - Helix.Orchestration.Infrastructure/Persistence/IExperimentRepository.cs
    - Helix.Orchestration.Infrastructure/Persistence/PostgresExperimentRepository.cs
decisions:
  - "SiteServer dev port is 56523 (HTTP) not 5000 — Vite proxy updated accordingly"
  - "copy-wasm.js uses ESM import syntax (package.json has type:module)"
  - "TrendValueWireDto introduced for WebSocket wire format (Core TrendValue is abstract)"
  - "MqttTrendBridge broadcasts only ScalarValue<double> — other TrendValue subtypes skipped"
  - "vitest --passWithNoTests added to test script (vitest 4.x exits 1 with no files)"
metrics:
  duration: "~45 minutes"
  completed: "2026-06-02"
  tasks_completed: 10
  tasks_total: 10
  files_created: 17
  files_modified: 6
---

# Phase 7 Wave 0: Backend Prerequisites + Scaffold Summary

## One-liner

Vite react-ts scaffold with SciChart 5.2.32 WASM, Vitest test setup, dark IDE theme, RED TDD test contracts, TrendWebSocketManager C# singleton, MqttTrendBridge WebSocket relay, and GET /api/experiments history endpoint.

## Tasks Completed

| Task | Description | Commit | Status |
|------|-------------|--------|--------|
| 07-0-001 | Scaffold helix-dashboard/ Vite react-ts | 816c3d1 | Done |
| 07-0-002 | Install npm dependencies + WASM copy | 5fcb546 | Done |
| 07-0-003 | vite.config.ts + vitest.config.ts + test/setup.ts | 8c905e5 | Done |
| 07-0-004 | Copy CSS theme to helix-dashboard | 034cfbb | Done |
| 07-0-005 | App smoke test skeleton (RED) | ac2fc2e | Done |
| 07-0-006 | useTrendWebSocket test skeleton (RED) | c89fabe | Done |
| 07-0-007 | TrendWebSocketManager singleton | a2f5a83 | Done |
| 07-0-008 | Wire WebSocket in Program.cs + extend MqttTrendBridge | 8a13f2f | Done |
| 07-0-009 | IExperimentRepository.GetAllAsync + implementation | 5fdedd6 | Done |
| 07-0-010 | GET /api/experiments endpoint | 9f39116 | Done |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] SiteServer proxy target was 5000; actual port is 56523**
- **Found during:** Task 07-0-003
- **Issue:** Plan said to verify `launchSettings.json` and update if different. SiteServer runs on 56523 (HTTP) and 56522 (HTTPS), not 5000.
- **Fix:** Updated `vite.config.ts` proxy targets to `http://localhost:56523` and `ws://localhost:56523`
- **Files modified:** `helix-dashboard/vite.config.ts`
- **Commit:** 8c905e5

**2. [Rule 3 - Blocking] scripts/copy-wasm.js requires ESM syntax**
- **Found during:** Task 07-0-003
- **Issue:** The plan provided a CommonJS script (`require("fs")`), but `package.json` has `"type": "module"` so Node treats all `.js` as ES modules.
- **Fix:** Rewrote script using `import fs from "fs"`, `import { fileURLToPath } from "url"` and `__dirname` via `fileURLToPath`
- **Files modified:** `helix-dashboard/scripts/copy-wasm.js`
- **Commit:** 5fcb546

**3. [Rule 3 - Blocking] vitest 4.x exits 1 with no test files**
- **Found during:** Task 07-0-003
- **Issue:** Plan expects `npm test -- --run` to exit 0 when no test files exist. vitest 4.x changed this to exit 1.
- **Fix:** Added `--passWithNoTests` to the `"test"` script in `package.json`
- **Files modified:** `helix-dashboard/package.json`
- **Commit:** 8c905e5

**4. [Rule 2 - Missing Critical] TrendValueWireDto for WebSocket wire format**
- **Found during:** Task 07-0-007
- **Issue:** Plan code showed `BroadcastAsync(TrendValue value)` where `TrendValue` is the Core abstract record. Serializing it directly would not produce the `{type, trendName, value, unit, timestamp}` JSON the frontend hook expects.
- **Fix:** Added `TrendValueWireDto` record inside `TrendWebSocketManager.cs` as the wire type. `MqttTrendBridge` constructs this DTO from `ITrend` + `ScalarValue<double>`.
- **Files modified:** `TrendWebSocketManager.cs`, `MqttTrendBridge.cs`
- **Commit:** a2f5a83, 8a13f2f

**5. [Rule 3 - Blocking] AddWebSockets requires using Microsoft.AspNetCore.WebSockets**
- **Found during:** Task 07-0-008
- **Issue:** `AddWebSockets` extension method was not found without explicit `using Microsoft.AspNetCore.WebSockets;` (confirmed by checking Agent's Program.cs which has it).
- **Fix:** Added `using Microsoft.AspNetCore.WebSockets;` to SiteServer Program.cs
- **Files modified:** `Helix.Orchestration.SiteServer/Program.cs`
- **Commit:** 8a13f2f

## TDD Gate Status

Wave 0 intentionally leaves two test files in RED state:
- `src/App.test.tsx` — 2 tests failing (App.tsx not created until 07-1-004)
- `src/hooks/useTrendWebSocket.test.ts` — 6 tests failing (hook not created until 07-1-002)

This is the correct TDD RED gate. Wave 1 must turn these GREEN.

## Known Stubs

None — Wave 0 contains no UI rendering components. The RED test files are contracts, not stubs.

## Threat Flags

| Flag | File | Description |
|------|------|-------------|
| threat_flag: T-7-04 mitigated | TrendWebSocketManager.cs | Dead socket removal on every BroadcastAsync — DoS mitigation implemented |
| threat_flag: T-7-03 mitigated | Program.cs | AllowedOrigins includes http://localhost:5174 |

## Self-Check: PASSED

Files exist:
- helix-dashboard/package.json: FOUND
- helix-dashboard/public/scichart2d.wasm: FOUND
- helix-dashboard/src/themes/default.css: FOUND
- helix-dashboard/src/test/setup.ts: FOUND
- helix-dashboard/src/App.test.tsx: FOUND
- helix-dashboard/src/hooks/useTrendWebSocket.test.ts: FOUND
- Helix.Orchestration.SiteServer/WebSockets/TrendWebSocketManager.cs: FOUND

Commits verified: 816c3d1, 5fcb546, 8c905e5, 034cfbb, ac2fc2e, c89fabe, a2f5a83, 8a13f2f, 5fdedd6, 9f39116

dotnet build SiteServer: PASSED (0 errors, 0 warnings)
