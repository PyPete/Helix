---
phase: phase-6
plan: "07"
subsystem: ui
tags: [tauri, typescript, vitest, tauri-plugin-fs, workflow, filesystem]

# Dependency graph
requires:
  - phase: phase-6-04
    provides: WorkflowBuilder + WslDocument type
  - phase: phase-6-05
    provides: ExecutionPanel + Agent REST integration
provides:
  - saveWorkflow(name, doc) — writes WslDocument JSON to $APPDATA/workflows/{name}.json
  - loadWorkflow(name) — reads and parses WslDocument from filesystem
  - listWorkflows() — returns names of all .json files in $APPDATA/workflows/
  - duplicateWorkflow(name, newName) — clones workflow with new documentId
  - tauri-plugin-fs 3-part wiring (Cargo.toml + lib.rs + capabilities/default.json)
affects: [phase-6-08, phase-7]

# Tech tracking
tech-stack:
  added: [tauri-plugin-fs@2.5.1]
  patterns: [vi.hoisted + vi.mock for ESM module mocks, sanitizeName path-traversal prevention]

key-files:
  created:
    - helix-desktop/src/lib/workflowLibrary.ts
    - helix-desktop/src/lib/workflowLibrary.test.ts
  modified:
    - helix-desktop/src-tauri/Cargo.toml
    - helix-desktop/src-tauri/src/lib.rs
    - helix-desktop/src-tauri/capabilities/default.json
    - helix-desktop/package.json
    - helix-desktop/package-lock.json

key-decisions:
  - "sanitizeName throws when no alphanumeric chars remain after replacement (not just empty string) -- '!!!' becomes '___' which is truthy but has no letters/digits"
  - "vi.clearAllMocks() added alongside vi.restoreAllMocks() in afterEach to reset call history between tests"
  - "JSON.stringify uses 2-space indent (pretty-print) for human-readable workflow files; test assertion updated to match '\"name\": \"test-copy\"' (with space after colon)"
  - "capabilities grant scoped fs permissions to $APPDATA/workflows/* only (not blanket fs:allow-read-write)"

patterns-established:
  - "vi.hoisted + vi.mock pattern for @tauri-apps/plugin-fs: mock functions created in hoisted block, factory returns them as named exports"
  - "afterEach: vi.clearAllMocks() before vi.restoreAllMocks() to fully reset state between tests"
  - "sanitizeName: replace /[^a-zA-Z0-9_-]/g then validate /[a-zA-Z0-9]/ remains -- prevents path traversal and all-separator names"

requirements-completed: [C9]

# Metrics
duration: 10min
completed: 2026-05-30
---

# Phase 6 Plan 07: Workflow Library Summary

**tauri-plugin-fs wired in 3 locations + workflowLibrary save/load/list/duplicate with sanitized path-traversal prevention; 44/44 tests pass**

## Performance

- **Duration:** ~10 min
- **Started:** 2026-05-30T06:36:37Z
- **Completed:** 2026-05-30T06:46:00Z
- **Tasks:** 2/2
- **Files modified:** 9

## Accomplishments
- Wired tauri-plugin-fs atomically in all three required locations (Cargo.toml + lib.rs + capabilities/default.json)
- Implemented workflowLibrary.ts with saveWorkflow, loadWorkflow, listWorkflows, duplicateWorkflow
- sanitizeName prevents path traversal (T-06-16): strips non-alphanumeric chars, throws if no letter/digit remains
- Full test suite 44/44 passes (5 new workflowLibrary tests + 39 prior)

## Task Commits

Each task was committed atomically:

1. **Task 1: Wire tauri-plugin-fs** - `5377a14` (chore)
2. **Task 2: workflowLibrary.ts + tests** - `1051349` (feat)

**Plan metadata:** (pending)

## Files Created/Modified
- `helix-desktop/src/lib/workflowLibrary.ts` - save/load/list/duplicate with sanitizeName
- `helix-desktop/src/lib/workflowLibrary.test.ts` - 5 vitest tests with mocked plugin-fs
- `helix-desktop/src-tauri/Cargo.toml` - tauri-plugin-fs = "2" added
- `helix-desktop/src-tauri/src/lib.rs` - .plugin(tauri_plugin_fs::init()) chained
- `helix-desktop/src-tauri/capabilities/default.json` - scoped fs permissions for $APPDATA/workflows/*
- `helix-desktop/package.json` - @tauri-apps/plugin-fs@^2.5.1 dependency added

## Decisions Made
- `sanitizeName` condition changed from `!safe` to `!safe || !/[a-zA-Z0-9]/.test(safe)` so that all-punctuation names (e.g. `"!!!"` → `"___"`) correctly throw instead of silently creating files named `___.json`.
- `vi.clearAllMocks()` added to afterEach to reset mock call history between tests; plan only specified `restoreAllMocks` which does not clear history.
- Test assertion for duplicateWorkflow updated from `'"name":"test-copy"'` to `'"name": "test-copy"'` to match `JSON.stringify(doc, null, 2)` pretty-print format.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] sanitizeName threw for all-underscore names but not all-symbol names**
- **Found during:** Task 2 (workflowLibrary test execution)
- **Issue:** `sanitizeName("!!!")` returned `"___"` (truthy) instead of throwing; test expected throw for "alphanumeric" constraint
- **Fix:** Changed guard to `!safe || !/[a-zA-Z0-9]/.test(safe)` to catch all-symbol-after-replace cases
- **Files modified:** helix-desktop/src/lib/workflowLibrary.ts
- **Verification:** `saveWorkflow("!!!", doc)` now rejects with "alphanumeric" message
- **Committed in:** `1051349` (Task 2 commit)

**2. [Rule 1 - Bug] vi.restoreAllMocks does not clear mock call history between tests**
- **Found during:** Task 2 (duplicateWorkflow test failure)
- **Issue:** mockWrite call history from test 1 carried over to test 5; toHaveBeenCalledWith checked all calls including prior test calls
- **Fix:** Added `vi.clearAllMocks()` before `vi.restoreAllMocks()` in afterEach
- **Files modified:** helix-desktop/src/lib/workflowLibrary.test.ts
- **Committed in:** `1051349` (Task 2 commit)

**3. [Rule 1 - Bug] Test assertion used compact JSON format but implementation uses pretty-print**
- **Found during:** Task 2 (duplicateWorkflow test failure after mock fix)
- **Issue:** Plan specified `expect.stringContaining('"name":"test-copy"')` but `JSON.stringify(doc, null, 2)` produces `"name": "test-copy"` with space after colon
- **Fix:** Updated assertion to `'"name": "test-copy"'` to match actual 2-space-indent JSON format
- **Files modified:** helix-desktop/src/lib/workflowLibrary.test.ts
- **Committed in:** `1051349` (Task 2 commit)

---

**Total deviations:** 3 auto-fixed (all Rule 1 bugs)
**Impact on plan:** All fixes required for test correctness. No scope creep. sanitizeName fix also strengthens security (T-06-16 mitigation).

## Issues Encountered
- None beyond the three auto-fixed bugs above.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Workflow save/load/list/duplicate ready for Plan 08 (WorkflowBuilder export wiring)
- All three tauri-plugin-fs wiring points active; runtime fs access to $APPDATA/workflows/ enabled
- Full test suite green at 44/44; cargo check exits 0; npm run build exits 0

## Self-Check: PASSED
- `helix-desktop/src/lib/workflowLibrary.ts` - FOUND
- `helix-desktop/src/lib/workflowLibrary.test.ts` - FOUND
- commit `5377a14` - FOUND (chore: wire tauri-plugin-fs)
- commit `1051349` - FOUND (feat: workflowLibrary.ts + tests)

---
*Phase: phase-6*
*Completed: 2026-05-30*
