---
phase: 02-scientific-setup-foundation
plan: "28"
subsystem: scientific-desktop-ui
tags: [typescript, react, vitest, accessibility, scientific-records]
requires: [02-27]
provides: [accessible-exact-id-evidence, confirmed-publication-dialogs, bounded-lineage-coverage]
affects: [APP-01, APP-02, APP-03, APP-04, SAMP-01, SAMP-02]
tech-stack:
  added: []
  patterns: [scientific-record-ui-evidence, explicit-immutable-confirmation, bounded-lineage-pagination]
key-files:
  created: []
  modified:
    - helix-desktop/src/components/ScientificSetup/ScientificSetup.tsx
    - helix-desktop/src/components/ScientificSetup/ProcedurePlans.tsx
    - helix-desktop/src/components/Samples/Samples.tsx
    - helix-desktop/src/components/ScientificSetup/FrozenAssembly.tsx
key-decisions:
  - Reuse ScientificRecordUi for immutable identifier evidence instead of adding another visual control pattern.
  - Named procedure and plan publication remains a separate explicit confirmation before any Agent request.
metrics:
  duration: 3min
  completed: 2026-11-01
  tasks: 3
status: complete
plan_head_before: e97936e347cbf4d19a8e18eb9137520d7e0dbf0a
plan_head_after: c36967cfd49ceb4a53f7e0e776e5afbb13695cd6
commits: 6
actuals:
  tokens: 4100
  tasks: 3
  commits: 6
requirements-completed: [APP-01, APP-02, APP-03, APP-04, SAMP-01, SAMP-02]
coverage:
  - id: D1
    description: Accessible exact-ID evidence and explicit immutable publication confirmation for apparatus, procedures, and plans.
    requirement: APP-01
    verification:
      - kind: automated_ui
        ref: helix-desktop/src/components/ScientificSetup/ScientificSetup.test.tsx and ProcedurePlans.test.tsx
        status: pass
    human_judgment: false
  - id: D2
    description: Truthful sample identity evidence and bounded lineage pagination retain immutable scientific provenance.
    requirement: SAMP-01
    verification:
      - kind: automated_ui
        ref: helix-desktop/src/components/Samples/Samples.test.tsx and SampleLineage.test.tsx
        status: pass
    human_judgment: false
  - id: D3
    description: Frozen assembly references remain exact, accessible, and separate from production authority or execution.
    requirement: APP-04
    verification:
      - kind: automated_ui
        ref: helix-desktop/src/components/ScientificSetup/FrozenAssembly.test.tsx
        status: pass
    human_judgment: false
---

# Phase 02 Plan 28: Scientific UI Fidelity Summary

Scientific setup, samples, and frozen assembly views now present exact immutable IDs through one accessible evidence pattern while preserving Agent-only authority and explicit confirmation boundaries.

## Accomplishments

- Added keyboard-operable full-ID evidence dialogs to apparatus drafts, procedure/plan records, selected samples, and assembly plan references.
- Made named procedure/plan publication explicitly confirm its immutable, non-execution effect before submitting to the Agent.
- Extended focused UI coverage for immutable evidence, bounded lineage pagination, sample provenance, and frozen-reference authority blocking.

## Task Commits

1. **Task 1: Preserve complete apparatus/procedure/plan states across remount and keyboard use** — `59f5e87`, `65dd6b6`
2. **Task 2: Preserve sample confirmation/custody/lineage truthfulness across all UI states** — `e77aa89`, `1fdb076`
3. **Task 3: Keep frozen reference and missing-authority presentation exact and recoverable** — `648ff26`, `c36967c`

## Verification

- `npm --prefix helix-desktop test -- --run src/components/ScientificSetup/ScientificSetup.test.tsx src/components/ScientificSetup/ProcedurePlans.test.tsx src/components/Samples/Samples.test.tsx src/components/Samples/SampleLineage.test.tsx src/components/ScientificSetup/FrozenAssembly.test.tsx` — passed: 5 files, 15 tests.

## Decisions Made

- Shared ScientificRecordUi evidence controls keep exact identifiers accessible without treating local UI state as scientific authority.
- Publication confirmation explicitly states that immutable revision publication does not execute equipment.

## Deviations from Plan

None - plan executed exactly as written.

## Known Stubs

None.

## Self-Check: PASSED

- All ten planned source/test files exist.
- Task commits `59f5e87`, `65dd6b6`, `e77aa89`, `1fdb076`, `648ff26`, and `c36967c` are present in HEAD ancestry.
