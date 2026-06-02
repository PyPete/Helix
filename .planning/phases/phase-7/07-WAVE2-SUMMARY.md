---
phase: "7"
plan: "wave-2"
subsystem: "helix-dashboard page components — SiteOverview, Campaigns, History, Trends, Analytics"
tags: [react, typescript, scichart, react-router-v7, vitest]
dependency_graph:
  requires:
    - helix-dashboard/src/App.tsx (Wave 1 — inline placeholders)
    - helix-dashboard/src/types/dashboard.ts (TrendValue, FumehoodStatus)
    - helix-dashboard/src/hooks/useTrendWebSocket.ts
    - helix-dashboard/src/components/Navbar.tsx + Layout.tsx
    - helix-dashboard/src/test/setup.ts (SciChart mock)
  provides:
    - helix-dashboard/src/pages/SiteOverviewPage.tsx
    - helix-dashboard/src/pages/CampaignsPage.tsx
    - helix-dashboard/src/pages/HistoryPage.tsx
    - helix-dashboard/src/pages/TrendsPage.tsx
    - helix-dashboard/src/components/SciChartLine.tsx
    - helix-dashboard/src/pages/AnalyticsPage.tsx
    - helix-dashboard/src/App.tsx (updated: all 5 page imports, no inline placeholders)
  affects:
    - helix-dashboard/src/components/SciChartLine.tsx (type fix for initChart param)
tech_stack:
  added: []
  patterns:
    - SciChartLine: useCallback(fn,[]) for initChart stable ref + XyDataSeries fifoCapacity 500
    - seriesRef.current.append() for data feeding (not React state)
    - fetch/error pattern from ExecutionPanel analog (useEffect load + try/catch + role=alert)
    - PLACEHOLDER_FUMEHOODS static array per D-05 (no API call in Phase 7)
    - CSS var() variables exclusively — zero raw hex literals
key_files:
  created:
    - helix-dashboard/src/pages/SiteOverviewPage.tsx
    - helix-dashboard/src/pages/CampaignsPage.tsx
    - helix-dashboard/src/pages/HistoryPage.tsx
    - helix-dashboard/src/pages/TrendsPage.tsx
    - helix-dashboard/src/components/SciChartLine.tsx
    - helix-dashboard/src/pages/AnalyticsPage.tsx
  modified:
    - helix-dashboard/src/App.tsx (replaced all 5 inline placeholder consts with imports)
decisions:
  - "SciChartLine initChart uses string | HTMLDivElement param (scichart-react TInitFunction signature) with cast"
  - "AnalyticsPage uses no new route/NavLink — Wave 1 already added /analytics and Analytics NavLink"
  - "App.tsx trendValues destructured from useTrendWebSocket and passed to TrendsPage"
metrics:
  duration: "~6 minutes"
  completed: "2026-06-02"
  tasks_completed: 5
  tasks_total: 5
  files_created: 6
  files_modified: 1
---

# Phase 7 Wave 2: Page Components Summary

## One-liner

Five React page components (SiteOverview with placeholder fumehood cards, Campaigns CRUD, History experiment list, Trends live SciChart, Analytics placeholder charts) replacing all Wave 1 inline stubs in App.tsx.

## Tasks Completed

| Task | Description | Commit | Status |
|------|-------------|--------|--------|
| 07-2-001 | SiteOverviewPage with PLACEHOLDER_FUMEHOODS grid and active-count | 0e76a35 | Done |
| 07-2-002 | CampaignsPage with list/create/start/delete CRUD UI | c3f2c66 | Done |
| 07-2-003 | HistoryPage fetching GET /api/experiments | 0d38fc0 | Done |
| 07-2-004 | TrendsPage + SciChartLine with fifoCapacity=500 | 6a52740 | Done |
| 07-2-005 | AnalyticsPage with two static SciChartLine placeholders | 1d77cdd | Done |

## Test Results

| Suite | Tests | Result |
|-------|-------|--------|
| src/App.test.tsx | 2/2 | GREEN |
| src/hooks/useTrendWebSocket.test.ts | 6/6 | GREEN |
| **Total** | **8/8** | **GREEN** |

`npm run build` exits 0 (chunk size warning only — expected for SciChart bundle).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] SciChartLine initChart parameter type mismatch**
- **Found during:** Task 07-2-004 (`npm run build` / `tsc -b`)
- **Issue:** `SciChartReact`'s `initChart` prop is typed as `TInitFunction<..>` where the `rootElement` parameter is `string | HTMLDivElement`, not just `HTMLDivElement`. TypeScript error TS2322 blocked the build.
- **Fix:** Changed `initChart` parameter to `string | HTMLDivElement` with a cast to `HTMLDivElement` for `SciChartSurface.create`. Commit: d1391cd.
- **Files modified:** `helix-dashboard/src/components/SciChartLine.tsx`

### Scope Notes

- **AnalyticsPage**: `/analytics` route and Analytics NavLink were already present in App.tsx and Navbar.tsx from Wave 1 (deviation documented in 07-WAVE1-SUMMARY.md). Wave 2 task 07-2-005 only replaced the inline placeholder const — no new route or NavLink additions needed.

## Success Criteria Verification

- [x] helix-dashboard/src/pages/SiteOverviewPage.tsx exists with placeholder fumehood cards and active-count
- [x] helix-dashboard/src/pages/CampaignsPage.tsx exists with campaign CRUD UI
- [x] helix-dashboard/src/pages/HistoryPage.tsx exists with experiment list
- [x] helix-dashboard/src/pages/TrendsPage.tsx + src/components/SciChartLine.tsx exist
- [x] helix-dashboard/src/pages/AnalyticsPage.tsx exists with two SciChartLine placeholders
- [x] App.tsx imports all 5 pages (no inline placeholder consts)
- [x] `npm test -- --run` exits 0 (8/8 tests pass)
- [x] 0 raw hex literals in src/pages/ or src/components/
- [x] `npm run build` exits 0

## Known Stubs

| Stub | File | Reason |
|------|------|--------|
| PLACEHOLDER_FUMEHOODS static array | SiteOverviewPage.tsx | Phase 7 design — real fumehood API in a later phase (D-05) |
| makeSpectrum() / makeDAD() static data | AnalyticsPage.tsx | Analytical data API not in scope for Phase 7 (E6 placeholder per plan) |

These stubs are intentional per the plan (D-05, E6) and are documented as known. Both pages display labelled "placeholder" text visible in the UI.

## Threat Model Coverage

| Threat ID | Mitigation | Location |
|-----------|-----------|----------|
| T-7-01 | React JSX escapes by default; no dangerouslySetInnerHTML in any page | All pages |

## Threat Flags

None — no new network endpoints beyond those planned (/api/campaigns, /api/experiments); no auth paths or trust boundary changes.

## Self-Check: PASSED

Files exist:
- helix-dashboard/src/pages/SiteOverviewPage.tsx: FOUND
- helix-dashboard/src/pages/CampaignsPage.tsx: FOUND
- helix-dashboard/src/pages/HistoryPage.tsx: FOUND
- helix-dashboard/src/pages/TrendsPage.tsx: FOUND
- helix-dashboard/src/components/SciChartLine.tsx: FOUND
- helix-dashboard/src/pages/AnalyticsPage.tsx: FOUND

Commits verified: 0e76a35, c3f2c66, 0d38fc0, 6a52740, 1d77cdd, d1391cd

Tests: 8/8 passed (`npm test -- --run`)
Build: `npm run build` exits 0
