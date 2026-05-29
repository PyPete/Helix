---
phase: phase-6
plan: "06"
subsystem: ui
tags: [react, recharts, vitest, testing-library, websocket, safety, trends]

# Dependency graph
requires:
  - phase: phase-6/06-04
    provides: recharts installed, useAgentWebSocket hook with trendValues/safetyEvents
  - phase: phase-6/06-05
    provides: ExecutionPanel, App.tsx wiring pattern

provides:
  - ScalarTrendChart component (recharts LineChart, isAnimationActive=false)
  - SpectralChart component (recharts AreaChart UV/MIR overlay, isAnimationActive=false)
  - SafetyPanel component (interlock rules, deduplication by ruleId, role=alert on trigger)
  - App.tsx wired with all three monitoring components and live trendValues/safetyEvents

affects: [phase-6/06-07, phase-6/06-08]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "vi.hoisted() + vi.mock('recharts') to prevent SVG assertion failures in jsdom"
    - "isAnimationActive={false} on all recharts Line/Area components for test stability"
    - "SafetyEvent deduplication by ruleId: reduce into Record<string, SafetyEvent>"
    - "Pure display components (no state/effects/fetch) fed from WebSocket hook"

key-files:
  created:
    - helix-desktop/src/components/TrendCharts/ScalarTrendChart.tsx
    - helix-desktop/src/components/TrendCharts/ScalarTrendChart.test.tsx
    - helix-desktop/src/components/TrendCharts/SpectralChart.tsx
    - helix-desktop/src/components/TrendCharts/SpectralChart.test.tsx
    - helix-desktop/src/components/SafetyPanel/SafetyPanel.tsx
    - helix-desktop/src/components/SafetyPanel/SafetyPanel.test.tsx
  modified:
    - helix-desktop/src/App.tsx

key-decisions:
  - "vi.hoisted() + vi.mock('recharts') required for all recharts tests to avoid SVG renderer errors in jsdom"
  - "isAnimationActive={false} on Line and Area components prevents animation-related test failures"
  - "SafetyPanel deduplicates by ruleId using reduce — last event for each rule wins"
  - "SpectralChart mounted with empty arrays in App.tsx (spectral data from specialised events — Plan 08 wiring)"

patterns-established:
  - "TrendCharts mock pattern: vi.hoisted mock + vi.mock('recharts') returning div data-testid=mock-chart"
  - "Pure display components receive pre-processed arrays from App.tsx (filter+map from trendValues)"

requirements-completed: [C5, C6, D4]

# Metrics
duration: 12min
completed: 2026-05-29
---

# Phase 6 Plan 06: Live Trend Charts + Safety Panel Summary

**recharts ScalarTrendChart + SpectralChart + SafetyPanel with 9 tests; App.tsx wired to live WebSocket trendValues and safetyEvents**

## Performance

- **Duration:** ~12 min
- **Started:** 2026-05-29T18:20:00Z
- **Completed:** 2026-05-29T18:32:00Z
- **Tasks:** 2/2
- **Files modified:** 7

## Accomplishments
- ScalarTrendChart: recharts LineChart with isAnimationActive=false, data-testid="scalar-trend-chart", section aria-label
- SpectralChart: recharts AreaChart UV/MIR dual-Area overlay with isAnimationActive=false on both Areas
- SafetyPanel: deduplicates SafetyEvents by ruleId, shows role="alert" when any rule triggered
- App.tsx: imports and mounts all three components with live data from useAgentWebSocket hook

## Task Commits

Each task was committed atomically:

1. **Task 1: Create ScalarTrendChart and SpectralChart components with smoke tests** - `00a69ec` (feat)
2. **Task 2: Create SafetyPanel component + tests, wire charts and panel into App.tsx** - `89a7a48` (feat)

**Plan metadata:** (docs commit — see below)

## Files Created/Modified
- `helix-desktop/src/components/TrendCharts/ScalarTrendChart.tsx` - Live scalar trend chart using recharts LineChart
- `helix-desktop/src/components/TrendCharts/ScalarTrendChart.test.tsx` - 2 smoke tests (container render + empty data)
- `helix-desktop/src/components/TrendCharts/SpectralChart.tsx` - UV/MIR spectral overlay using recharts AreaChart
- `helix-desktop/src/components/TrendCharts/SpectralChart.test.tsx` - 2 smoke tests (container render + empty data)
- `helix-desktop/src/components/SafetyPanel/SafetyPanel.tsx` - Interlock rule list with triggered/safe state
- `helix-desktop/src/components/SafetyPanel/SafetyPanel.test.tsx` - 5 tests (empty state, safe/triggered, dedup, multi-rule)
- `helix-desktop/src/App.tsx` - Imports and mounts ScalarTrendChart, SpectralChart, SafetyPanel with live data

## Decisions Made
- vi.hoisted() + vi.mock("recharts") pattern required — recharts renders SVG in jsdom which causes assertion failures; mocking returns simple divs
- isAnimationActive={false} on all Line/Area components — prevents recharts animation-related test failures in jsdom
- SpectralChart mounted with empty arrays in App.tsx — spectral data comes from specialised WebSocket events; full wiring deferred to Plan 08
- SafetyPanel deduplication approach: reduce over safetyEvents array into Record<string, SafetyEvent>, last event per ruleId wins

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- None. vi.hoisted() + vi.mock("recharts") pattern (specified in prior_wave_context) worked first time.

## Known Stubs
- SpectralChart rendered with `uvData={[]}` and `mirData={[]}` in App.tsx. SpectralChart shows empty state. Intentional: spectral data frames are not yet emitted by the Agent WebSocket. Plan 08 or a future plan will wire real spectral data.

## Threat Flags
None — no new network endpoints, auth paths, or schema changes introduced. SafetyEvent ruleId used only as React key and DOM testid (T-06-14 mitigated as planned).

## Self-Check
- ScalarTrendChart.tsx: FOUND
- SpectralChart.tsx: FOUND
- SafetyPanel.tsx: FOUND
- All test files: FOUND
- App.tsx updated: FOUND
- Commits 00a69ec and 89a7a48: FOUND

## Self-Check: PASSED

## Next Phase Readiness
- Plan 06-07 (Workflow Library: save/load/duplicate via Tauri fs plugin) can proceed immediately
- All 39 tests pass; npm run build exits 0
- ScalarTrendChart and SafetyPanel live-update from WebSocket frames once Agent emits real trend/safety data

---
*Phase: phase-6*
*Completed: 2026-05-29*
