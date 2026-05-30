---
phase: "06"
plan: "10"
subsystem: "helix-desktop + Helix.Orchestration.Agent"
tags: [gap-closure, spectral-chart, safety-panel, websocket-data, C5, C6]
dependency_graph:
  requires: [06-06, 06-08]
  provides: [spectral-chart-data-path, safety-event-current-value]
  affects: [helix-desktop/src/App.tsx, helix-desktop/src/types/agent.ts, helix-desktop/src/components/SafetyPanel/SafetyPanel.tsx, Helix.Orchestration.Agent/Events/RunspaceEvent.cs]
tech_stack:
  added: []
  patterns: [trendName-prefix-filter, optional-record-parameter, conditional-JSX-render]
key_files:
  created: []
  modified:
    - Helix.Orchestration.Agent/Events/RunspaceEvent.cs
    - helix-desktop/src/types/agent.ts
    - helix-desktop/src/components/SafetyPanel/SafetyPanel.tsx
    - helix-desktop/src/components/SafetyPanel/SafetyPanel.test.tsx
    - helix-desktop/src/App.tsx
decisions:
  - "SpectralChart data sourced from UV:/MIR: prefixed TrendValue frames (no new event type)"
  - "Wavelength parsed from trendName suffix with parseInt(..., 10) + isNaN guard"
  - "Latest value per wavelength kept via Map to avoid duplicate wavelength points"
  - "SafetyEvent.CurrentValue added as optional C# record parameter with default null (backward-compatible)"
  - "currentValue displayed with toFixed(2) in SafetyPanel span element"
metrics:
  duration: "~4 min"
  completed: "2026-05-30"
  tasks: 2
  files: 5
---

# Phase 6 Plan 10: Gap Closure - SpectralChart Data Wiring + SafetyEvent CurrentValue Summary

**One-liner:** Wired SpectralChart to UV:/MIR: prefixed TrendValue frames (C5 BLOCKER) and extended SafetyEvent with optional CurrentValue field displayed in SafetyPanel (C6 WARNING).

## What Was Built

Two gap closures from 06-VERIFICATION.md:

**Gap C5 (BLOCKER) — SpectralChart data path:**
App.tsx previously passed hardcoded `uvData=[]` and `mirData=[]` to SpectralChart. Now filters `trendValues` by `trendName.startsWith("UV:")` / `trendName.startsWith("MIR:")`, extracts wavelength from the suffix via `parseInt(trendName.slice(3/4), 10)` with NaN guard, aggregates latest value per wavelength into a Map, sorts by wavelength ascending, and passes the derived arrays to SpectralChart. Real spectral data from the WebSocket TrendValue stream now flows into the chart.

**Gap C6 (WARNING) — SafetyEvent currentValue:**
Added `double? CurrentValue = null` as an optional 6th positional parameter to the C# `SafetyEvent` sealed record (backward-compatible with all existing positional call sites). Added `currentValue?: number` to the TypeScript `SafetyEvent` interface. Updated `SafetyPanel.tsx` to conditionally render a `<span data-testid="rule-value-{ruleId}">` with `toFixed(2)` when `currentValue !== undefined`. Added a new test verifying the display.

## Tasks Completed

| Task | Description | Commit | Files |
|------|-------------|--------|-------|
| 1 | Add CurrentValue to Agent SafetyEvent + frontend interface | f1236b3 | RunspaceEvent.cs, agent.ts |
| 2 | SafetyPanel currentValue display + SpectralChart wiring in App.tsx | c742ecb | SafetyPanel.tsx, SafetyPanel.test.tsx, App.tsx |

## Verification Results

- `dotnet build Helix.Orchestration.slnx -c Debug`: 0 errors, 22 pre-existing warnings (unrelated)
- `npm run test -- --run`: 49/49 tests pass (10 test files)
- `npm run build`: exits 0, no TypeScript errors

## Deviations from Plan

None - plan executed exactly as written.

## Known Stubs

None. SpectralChart now receives real data when UV:/MIR: prefixed TrendValues arrive over WebSocket. SafetyPanel now shows currentValue when the Agent includes it in SafetyEvent frames.

## Threat Flags

No new unplanned threat surface introduced. T-06-C5a mitigation implemented as planned (parseInt with isNaN guard). T-06-C5b accepted (Map bounded by spectral wavelength range). T-06-C6 accepted (local desktop app, no PII).

## Self-Check: PASSED

- Helix.Orchestration.Agent/Events/RunspaceEvent.cs: contains `CurrentValue` (verified grep)
- helix-desktop/src/types/agent.ts: contains `currentValue?: number` (verified grep)
- helix-desktop/src/components/SafetyPanel/SafetyPanel.tsx: contains `rule.currentValue !== undefined` (verified)
- helix-desktop/src/App.tsx: contains `UV:` prefix filter and `uvData={uvData}` (verified grep)
- Commit f1236b3: exists (git log confirmed)
- Commit c742ecb: exists (git log confirmed)

## Self-Check: PASSED

All created/modified files exist on disk. Both task commits verified in git log.
