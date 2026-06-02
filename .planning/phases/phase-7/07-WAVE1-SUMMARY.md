---
phase: "7"
plan: "wave-1"
subsystem: "helix-dashboard app shell — types, WebSocket hook, navigation, routing"
tags: [react, typescript, react-router-v7, websocket, vitest, tdd-green]
dependency_graph:
  requires:
    - helix-dashboard Vite scaffold (Wave 0)
    - src/test/setup.ts SciChart mock (Wave 0)
    - RED test contracts App.test.tsx + useTrendWebSocket.test.ts (Wave 0)
  provides:
    - helix-dashboard/src/types/dashboard.ts (TrendValue, RunspaceEvent, FumehoodStatus)
    - helix-dashboard/src/hooks/useTrendWebSocket.ts (WebSocket hook for /api/trends/ws)
    - helix-dashboard/src/components/Navbar.tsx + Layout.tsx
    - helix-dashboard/src/main.tsx (SciChart licence init + BrowserRouter)
    - helix-dashboard/src/App.tsx (5 routes, inline placeholders)
    - GREEN App.test.tsx (2/2) + useTrendWebSocket.test.ts (6/6)
  affects:
    - helix-dashboard/src/test/setup.ts (vi.clearAllMocks fix)
    - helix-dashboard/tsconfig.app.json (vitest/globals type)
    - helix-dashboard/vite.config.ts (removed esbuild minify)
tech_stack:
  added: []
  patterns:
    - TDD GREEN: Wave 0 RED contracts turned GREEN
    - useTrendWebSocket: useState+useEffect WebSocket hook with ring-buffer (slice -200)
    - react-router v7 Routes/Route/NavLink/Outlet pattern
    - SciChartSurface.setRuntimeLicenseKey before createRoot (D-01)
    - vi.clearAllMocks() in global beforeEach for RTL cleanup isolation
key_files:
  created:
    - helix-dashboard/src/types/dashboard.ts
    - helix-dashboard/src/hooks/useTrendWebSocket.ts
    - helix-dashboard/src/components/Navbar.tsx
    - helix-dashboard/src/components/Layout.tsx
    - helix-dashboard/src/main.tsx
    - helix-dashboard/src/App.tsx
  modified:
    - helix-dashboard/src/test/setup.ts
    - helix-dashboard/tsconfig.app.json
    - helix-dashboard/vite.config.ts
decisions:
  - "App.tsx has 5 routes including /analytics (wave_scope requirement, Wave 2 plan has 4)"
  - "Navbar.tsx has 5 NavLinks including Analytics (wave_scope requirement)"
  - "vi.clearAllMocks() in setup.ts beforeEach fixes RTL deferred cleanup bleeding mockClose counts"
  - "tsconfig.app.json types includes vitest/globals so tsc -b does not fail on test setup files"
  - "vite.config.ts minify removed (Vite 8 requires esbuild as separate package; using rolldown default)"
metrics:
  duration: "~9 minutes"
  completed: "2026-06-02"
  tasks_completed: 4
  tasks_total: 4
  files_created: 6
  files_modified: 3
---

# Phase 7 Wave 1: App Shell + Types + Hook Summary

## One-liner

React app shell with TrendValue/FumehoodStatus types, useTrendWebSocket hook connecting to /api/trends/ws, 5-link top Navbar, Layout with Outlet, SciChart licence init, and 5-route App — turning all Wave 0 RED test contracts GREEN.

## Tasks Completed

| Task | Description | Commit | Status |
|------|-------------|--------|--------|
| 07-1-001 | src/types/dashboard.ts — TrendValue, RunspaceEvent, FumehoodStatus | 0b1370f | Done |
| 07-1-002 | src/hooks/useTrendWebSocket.ts + test isolation fix | e152cdb | Done |
| 07-1-003 | src/components/Navbar.tsx + Layout.tsx | d74bcae | Done |
| 07-1-004 | src/main.tsx + src/App.tsx + build config fixes | 843b711 | Done |

## Test Results

| Suite | Tests | Result |
|-------|-------|--------|
| src/App.test.tsx | 2/2 | GREEN |
| src/hooks/useTrendWebSocket.test.ts | 6/6 | GREEN |
| **Total** | **8/8** | **GREEN** |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Test Isolation] vi.clearAllMocks() in setup.ts beforeEach**
- **Found during:** Task 07-1-002
- **Issue:** RTL's global `afterEach` (cleanup) fires AFTER the user's describe-scoped `afterEach` (mockClose.mockReset()). When testing-library unmounts the previous test's component, ws.close() increments mockClose AFTER it was reset. The next test starts with mockClose count=1, then unmount adds another call, causing `toHaveBeenCalledOnce()` to fail with "called 2 times".
- **Fix:** Added `beforeEach(() => vi.clearAllMocks())` to src/test/setup.ts. Global beforeEach runs before describe-scoped beforeEach, clearing mockClose to 0 before each test starts. `clearAllMocks` preserves implementations (unlike resetAllMocks), so scichart mock factories remain functional.
- **Files modified:** `helix-dashboard/src/test/setup.ts`
- **Commit:** e152cdb

**2. [Rule 3 - Blocking] tsconfig.app.json missing vitest/globals type**
- **Found during:** Task 07-1-004 (npm run build)
- **Issue:** `tsc -b` compiles `src/test/setup.ts` (included via `"include": ["src"]`). The file uses `vi`, `beforeEach` etc. but tsconfig.app.json only had `"types": ["vite/client"]` — vitest globals not recognized.
- **Fix:** Added `"vitest/globals"` to types array in tsconfig.app.json.
- **Files modified:** `helix-dashboard/tsconfig.app.json`
- **Commit:** 843b711

**3. [Rule 3 - Blocking] vite.config.ts minify: "esbuild" not available in Vite 8**
- **Found during:** Task 07-1-004 (npm run build)
- **Issue:** Vite 8 uses rolldown/oxc by default; esbuild is no longer bundled and must be installed separately. Build failed with "Cannot find package 'esbuild'".
- **Fix:** Removed `minify: "esbuild"` from vite.config.ts build options (defaults to rolldown's oxc minifier).
- **Files modified:** `helix-dashboard/vite.config.ts`
- **Commit:** 843b711

**4. [Deviation - Scope Extension] App.tsx and Navbar.tsx include /analytics route and NavLink**
- **Found during:** Task 07-1-003 and 07-1-004
- **Reason:** Wave scope critical notes explicitly require 5 NavLinks (including Analytics) and 5 routes (including /analytics). The PLAN task 07-1-003 only shows 4 NavLinks; Wave 2 task 07-2-005 was planned to add Analytics. Wave scope takes precedence.
- **Impact:** Wave 2 task 07-2-005 will replace the inline AnalyticsPage placeholder with the real component — no rework needed in Navbar.tsx (link already present).
- **Files modified:** `helix-dashboard/src/components/Navbar.tsx`, `helix-dashboard/src/App.tsx`

## Threat Model Coverage

| Threat ID | Mitigation | Location |
|-----------|-----------|----------|
| T-7-02 | frame.type === "trend-value" validated before dispatch | useTrendWebSocket.ts |

## Known Stubs

| Stub | File | Reason |
|------|------|--------|
| SiteOverviewPage inline placeholder | src/App.tsx:7 | Wave 2 (07-2-001) replaces with real component |
| CampaignsPage inline placeholder | src/App.tsx:8 | Wave 2 (07-2-002) replaces |
| HistoryPage inline placeholder | src/App.tsx:9 | Wave 2 (07-2-003) replaces |
| TrendsPage inline placeholder | src/App.tsx:10 | Wave 2 (07-2-004) replaces |
| AnalyticsPage inline placeholder | src/App.tsx:11 | Wave 2 (07-2-005) replaces |

These stubs are intentional per the plan design — Wave 1 establishes routing, Wave 2 provides real page implementations.

## Threat Flags

None — no new network endpoints, auth paths, or trust boundary changes introduced.

## Self-Check: PASSED

Files exist:
- helix-dashboard/src/types/dashboard.ts: FOUND
- helix-dashboard/src/hooks/useTrendWebSocket.ts: FOUND
- helix-dashboard/src/components/Navbar.tsx: FOUND
- helix-dashboard/src/components/Layout.tsx: FOUND
- helix-dashboard/src/main.tsx: FOUND
- helix-dashboard/src/App.tsx: FOUND

Commits verified: 0b1370f, e152cdb, d74bcae, 843b711

Tests: 8/8 passed (`npm test -- --run`)
Build: `npm run build` exits 0
