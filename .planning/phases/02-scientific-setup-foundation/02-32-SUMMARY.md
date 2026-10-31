---
phase: 02-scientific-setup-foundation
plan: "32"
subsystem: scientific procedure derivation and adoption
tags: [dotnet, postgres, tauri, react, scientific-setup]
requires:
  - phase: 02-31
    provides: procedure and plan workspace/client foundations
provides:
  - source-linked reusable procedure drafts with actor-scoped operation receipts
  - explicit compatible plan-draft adoption with no implicit publication
  - guarded native and desktop derive/adopt actions
affects: [02-33, APP-03]
tech-stack:
  added: []
  patterns: [source-linked immutable derivation, explicit plan pin adoption, fixed-route native commands]
key-files:
  created: []
  modified: [Helix.Orchestration.Infrastructure/Persistence/PostgresScientificSetupRepository.cs, Helix.Orchestration.Agent/Api/ScientificSetupEndpoints.cs, helix-desktop/src-tauri/src/commands/agent.rs, helix-desktop/src/components/ScientificSetup/ProcedurePlans.tsx]
key-decisions:
  - Derived procedures are distinct aggregates that retain the published source revision ID.
  - Procedure adoption updates a token-checked plan draft only; publication remains a separate intentional action.
requirements-completed: [APP-03]
coverage:
  - id: D1
    description: Source-linked procedure derivation and compatible draft-only plan adoption
    requirement: APP-03
    verification:
      - kind: integration
        ref: dotnet test tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj --no-restore --filter FullyQualifiedName~ScientificSetupTests --verbosity minimal
        status: pass
    human_judgment: false
  - id: D2
    description: Guarded native and desktop derive/adopt controls with explicit confirmation
    requirement: APP-03
    verification:
      - kind: unit
        ref: cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml agent_security
        status: pass
      - kind: automated_ui
        ref: npm --prefix helix-desktop test -- --run src/components/ScientificSetup/ProcedurePlans.test.tsx
        status: pass
    human_judgment: false
actuals:
  tokens: 80220
  tasks: 3
  commits: 4
plan_head_before: 7796855d911a896e9617cddf402a511d4cde3288
plan_head_after: ed3ca2c707c178d7ee5fd37430ec8e89cb44c634
duration: 20min
completed: 2026-10-31
status: complete
---

# Phase 02 Plan 32: Reusable Procedure Derivation and Adoption Summary

Reusable scientific procedures now preserve exact immutable source evidence and can be deliberately adopted into a compatible plan draft without publishing or changing the old pin.

## Accomplishments

- Added actor-scoped PostgreSQL derivation and adoption transactions with source/revision integrity, expected-token checks, and operation receipts.
- Exposed fixed Agent routes and main-window-only Tauri commands that validate IDs, payload bounds, session state, and adoption tokens before a request is sent.
- Added accessible derive source evidence and an explicit adoption confirmation in the scientific workspace.

## Task Commits

1. **Task 1: Commit explicit reusable derivation and compatible adoption** — `405ac5a` (feat)
2. **Task 2: Register dedicated guarded native derive/adopt operations** — `ae82491` (feat), `2cdfd22` (chore generated native manifest)
3. **Task 3: Derive and explicitly adopt from complete scientific UI** — `ed3ca2c` (feat)

## Verification

- `dotnet test tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj --no-restore --filter FullyQualifiedName~ScientificSetupTests --verbosity minimal` — passed: 3 tests, including derived source provenance and draft-only adoption.
- `cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml agent_security` — passed: 9 tests; one documented live integration test remained ignored.
- `npm --prefix helix-desktop test -- --run src/components/ScientificSetup/ProcedurePlans.test.tsx` — passed: 3 tests.
- `npm --prefix helix-desktop run build` — blocked by the documented pre-existing unused `ApparatusDraft` import in `ScientificSetup.tsx`; that unrelated file was not changed.

## Files Created/Modified

- `Helix.Orchestration.Core/ScientificSetup/IScientificSetupRepository.cs` — derive/adopt repository ports.
- `Helix.Orchestration.Infrastructure/Persistence/PostgresScientificSetupRepository.cs` — atomic source-linked derivation and compatible draft adoption.
- `Helix.Orchestration.Agent/Api/ScientificSetupEndpoints.cs` — typed derive/adopt Agent routes.
- `helix-desktop/src-tauri/src/commands/agent.rs` — dedicated guarded native commands and security tests.
- `helix-desktop/src/lib/scientificClient.ts` — typed native client calls.
- `helix-desktop/src/components/ScientificSetup/ProcedurePlans.tsx` — provenance and confirmed adoption controls.

## Decisions Made

- Derivation copies an immutable published source into a new reusable procedure aggregate instead of revising the source.
- Adoption validates compatibility and changes only the draft; publishing remains a separately labelled user action.

## Deviations from Plan

### Auto-fixed Issues

1. **[Rule 1 - Bug] Rehydrated canonical numeric WSL parameters before validating a derived procedure.**
- **Found during:** Task 1
- **Issue:** JSON canonicalization returns numeric WSL parameter values as `JsonElement`, which prevented a valid derived procedure from passing typed decimal validation.
- **Fix:** Rehydrate numeric parameter values as decimals before source validation and compatible adoption checks.
- **Files modified:** `Helix.Orchestration.Infrastructure/Persistence/PostgresScientificSetupRepository.cs`
- **Verification:** Focused PostgreSQL suite passed.
- **Committed in:** `405ac5a`

## Issues Encountered

- The desktop production build still fails on the pre-existing unused `ApparatusDraft` import in `ScientificSetup.tsx`. Per scope instruction, it remains unchanged.

## Known Stubs

None.

## Self-Check: PASSED

- Confirmed all listed source, native, and UI files exist.
- Confirmed task commits `405ac5a`, `ae82491`, `2cdfd22`, and `ed3ca2c` are ancestors of HEAD.
