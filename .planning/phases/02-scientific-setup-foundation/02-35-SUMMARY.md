---
phase: 02-scientific-setup-foundation
plan: "35"
subsystem: scientific-samples-ui
tags: [react, tauri, samples, custody, provenance]
requires: [02-34]
provides: [typed-sample-client, sample-evidence-workspace, guarded-outcome-recovery]
affects: [SAMP-01, SAMP-02]
tech-stack:
  added: []
  patterns: [typed-native-receipts, append-only-evidence-ui, stable-dock-tab-identity]
key-files:
  created:
    - helix-desktop/src/components/Samples/Samples.tsx
    - helix-desktop/src/components/Samples/Samples.test.tsx
  modified:
    - helix-desktop/src/lib/scientificClient.ts
    - helix-desktop/src/types/scientific.ts
    - helix-desktop/src/shell/WorkspaceLayout.tsx
    - helix-desktop/src-tauri/src/commands/agent.rs
decisions:
  - Sample outcome checking is a fixed, main-window-scoped, read-only receipt lookup that preserves the original operation ID.
  - Samples are a stable dock tab and activity-bar entry; labels remain supporting display data rather than sample identity.
metrics:
  duration: 9min
  completed: 2026-11-01
  tasks: 3
status: complete
plan_head_before: 19838519ec96df0a2cf71d3dd47de04fafe1c8fd
plan_head_after: dcad4fea2469fd0e6fa95c860bc0819b3535363c
commits: 7
actuals:
  tokens: 77694
  tasks: 3
  commits: 7
requirements-completed: [SAMP-01, SAMP-02]
coverage:
  - id: D1
    description: Typed desktop sample client rejects malformed, mismatched, and wrong-sample evidence envelopes.
    requirement: SAMP-01
    verification:
      - kind: unit
        ref: helix-desktop/src/lib/scientificClient.test.ts
        status: pass
    human_judgment: false
  - id: D2
    description: Scientist-facing Samples surface requires explicit collection and presents immutable custody evidence.
    requirement: SAMP-02
    verification:
      - kind: automated_ui
        ref: helix-desktop/src/components/Samples/Samples.test.tsx
        status: pass
    human_judgment: false
  - id: D3
    description: Samples is reachable from the retained desktop dock and activity bar.
    requirement: SAMP-01
    verification:
      - kind: automated_ui
        ref: npm --prefix helix-desktop test -- --run src/shell/WorkspaceLayout.test.tsx src/shell/HelixShell.test.tsx
        status: pass
    human_judgment: false
---

# Phase 02 Plan 35: Sample Evidence Workspace Summary

Typed native sample operations, explicit collection confirmation, append-only custody evidence, and a restored Samples dock entry now keep scientific sample identity and provenance visible without inferring collection from planned work.

## Performance

- **Duration:** 9min
- **Completed:** 2026-11-01T12:43:30Z
- **Tasks:** 3/3
- **Files modified:** 14

## Accomplishments

- Added validated sample list/read/confirm/event/history client methods and strict receipt/projection checks.
- Added a complete Samples surface with confirmation, relabel, custody, correction, immutable evidence history, uncertainty, and Agent-backed read states.
- Registered a stable Samples dock/activity entry and a fixed native operation-receipt lookup for original-key recovery.

## Task Commits

1. **Task 1: Consume exact sample receipts and append-only history** — `b2d2ff1`, `eacb992`, `9b68689`.
2. **Task 2: Confirm actual collection and inspect complete custody evidence** — `50f8852`.
3. **Task 3: Reach and restore Samples in the retained shell** — `a38fcfe`, `b97480e`.
4. **Cross-task reliability fix:** `dcad4fe`.

## Verification

- `npm --prefix helix-desktop test -- --run src/lib/scientificClient.test.ts src/components/Samples/Samples.test.tsx src/shell/WorkspaceLayout.test.tsx src/shell/HelixShell.test.tsx` — passed (4 files, 14 tests).
- `cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml agent_security` — passed (11 tests; one owned-host integration test remains intentionally ignored).
- `npm --prefix helix-desktop run build` — not clean because pre-existing `ScientificSetup.tsx` has unused `ApparatusDraft` TS6133; this plan did not alter that file.

## Decisions Made

- Outcome recovery uses a new fixed `sample_operation` native command; a missing read result remains uncertain and never authorizes a fresh mutation key.
- The UI labels current custody as derived from append-only history and displays server sequence rather than sorting scientific evidence by reported occurrence time.

## Deviations from Plan

### Auto-fixed Issues

1. **[Rule 2 - Critical functionality] Added guarded sample outcome lookup**
- **Found during:** Task 2
- **Issue:** The native broker had no sample operation-result command, so `Check outcome` could not safely reconcile an uncertain confirmation or custody mutation.
- **Fix:** Added fixed-kind `sample_operation`, main-window/session/UUID validation, permission metadata, generated ACL registration, and typed client recovery.
- **Files modified:** `agent.rs`, `lib.rs`, `agent.toml`, `acl-manifests.json`, `scientificClient.ts`, `Samples.tsx`.
- **Commit:** `dcad4fe`

## Known Stubs

None.

## Next Phase Readiness

The client and shell now expose the real Agent-backed collection/custody evidence surface. Future transformation and lineage work can retain the same stable IDs, immutable receipts, and dock restoration conventions.

## Self-Check: PASSED

- Required typed client, Samples UI, shell registration, and summary artifacts exist.
- Task commits `b2d2ff1`, `eacb992`, `50f8852`, `a38fcfe`, `b97480e`, `9b68689`, and `dcad4fe` are ancestors of the plan head.
