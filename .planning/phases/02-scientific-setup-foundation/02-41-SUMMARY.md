---
phase: 02-scientific-setup-foundation
plan: "41"
subsystem: desktop-scientific-transport
tags: [tauri, rust, typescript, scientific-setup, policy, frozen-evidence]
requires: [02-40]
provides: [guarded-assembly-policy-ipc, typed-frozen-evidence-client]
affects: [02-42, APP-04]
tech-stack:
  added: []
  patterns: [fixed-native-routes, main-window-guards, exact-envelope-validation]
key-files:
  created: []
  modified:
    - helix-desktop/src-tauri/src/commands/agent.rs
    - helix-desktop/src-tauri/src/lib.rs
    - helix-desktop/src-tauri/permissions/agent.toml
    - helix-desktop/src/lib/scientificClient.ts
    - helix-desktop/src/types/scientific.ts
    - helix-desktop/src/lib/scientificClient.test.ts
decisions:
  - Policy and assembly IPC remains fixed-route, main-window scoped, and cannot use generic mutation commands.
  - Client validation preserves original immutable refs and treats unavailable authority as a non-commit outcome.
metrics:
  duration: 12min
  completed: 2026-11-01
  tasks: 2
status: complete
plan_head_before: 969b2b73d37ee61dbed315316815fdfc94b3cec9
plan_head_after: c23a0993539093e4907673137e58abc1f034368d
commits: 5
actuals:
  tokens: 53833
  tasks: 2
  commits: 5
requirements-completed: [APP-04]
---

# Phase 02 Plan 41: Guarded Frozen Evidence Transport Summary

Native assembly and policy operations now use fixed localhost routes with main-window/session guards, while the typed renderer client validates exact immutable references and never treats unavailable authority as approval.

## Accomplishments

- Registered five bounded native policy/assembly commands with generated ACL coverage and fixed Agent paths.
- Prevented generic scientific mutation commands from creating policy or assembly content.
- Added typed defaults, policy, and assembly client operations with exact-key envelope validation.

## Task Commits

1. **Task 1: Register guarded exact assembly and policy commands** — `001dbec`, `7470dc4`, `d0e112f`
2. **Task 2: Consume exact policy/defaults/freeze content and receipts** — `28d7c09`, `c23a099`

## Verification

- RED Task 1: `cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml agent_security` failed on the new fixed policy route assertion (12 passed, 1 failed, 1 ignored).
- `cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml agent_security` — passed (13 passed, 1 ignored; ordinary fixture remains intentionally excluded).
- RED Task 2: `npm --prefix helix-desktop test -- --run src/lib/scientificClient.test.ts` failed because frozen client operations were absent.
- `npm --prefix helix-desktop test -- --run src/lib/scientificClient.test.ts` — passed (7 tests).

## Decisions Made

- Generic mutation routing is restricted to procedures and plans; policy/assembly content cannot be registered through it.
- Missing or unavailable authority remains a validated non-commit state rather than client-side approval.

## Deviations from Plan

None - plan executed exactly as written.

## Known Stubs

None.

## Self-Check: PASSED

- All six planned implementation/client files exist.
- Task commits `001dbec`, `7470dc4`, `d0e112f`, `28d7c09`, and `c23a099` are ancestors of the recorded plan head.
