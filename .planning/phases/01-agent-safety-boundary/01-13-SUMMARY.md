---
phase: 01-agent-safety-boundary
plan: "13"
subsystem: desktop safety and command evidence UI
tags: [desktop, safety, evidence, accessibility, vitest]
requires: [01-06, 01-07]
provides:
  - Outcome-first, ordered command evidence with expandable immutable facts and appended review notes.
  - Explicit safety-rule loading, unknown, empty, partial, error, and populated state treatment.
affects: [SAFE-02, SAFE-04]
key-files:
  - helix-desktop/src/shell/panels/ExperimentTimeline.tsx
  - helix-desktop/src/shell/panels/ExperimentTimeline.test.tsx
  - helix-desktop/src/shell/panels/panels.css
  - helix-desktop/src/components/SafetyPanel/SafetyPanel.tsx
  - helix-desktop/src/components/SafetyPanel/SafetyPanel.test.tsx
  - helix-desktop/src/components/SafetyPanel/SafetyPanel.css
---

# Plan 01-13: Durable Evidence and Safety States

## Completed

- Completed the outcome-first command evidence timeline with durable ordering, critical semantic emphasis, expandable original facts, and append-only review notes.
- Completed the safety panel state matrix so empty rules, missing or stale data, loading, read errors, partial data, and populated rules remain explicit without inferring safety.
- Added focused RTL coverage for timeline ordering, empty evidence, fact expansion, review-note submission, and the safety panel states.

## Test Repair

The timeline fact assertion previously used a text-content callback that matched both the admission-check list and its list item. The test now scopes the assertion to the existing `timeline-command-evidence` test id and then checks the specific admission-check text, eliminating the ambiguous matcher while retaining user-visible coverage.

## Verification

- `npm --prefix helix-desktop test -- --run src/shell/panels/ExperimentTimeline.test.tsx src/hooks/useCommandEvidence.test.ts src/components/SafetyPanel/SafetyPanel.test.tsx` — 3 files, 9 tests passed.
- `npm --prefix helix-desktop run build` — passed (`tsc && vite build`).

## Commit

- `00b5527 feat(desktop): complete safety evidence panels`

## Visual Follow-up

Manual review remains appropriate for narrow-dock layout, keyboard expansion and note entry, long identifiers, overflow scrolling, and the quiet-success versus persistent-critical visual treatment described in the plan.
