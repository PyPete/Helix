---
phase: 02-scientific-setup-foundation
plan: "29"
subsystem: scientific-setup
tags: [procedures, experiment-plans, postgres, wsl, validation]
requires: [02-19]
provides: [independent-procedure-plan-drafts, exact-parameter-instantiation, immutable-procedure-plan-publications]
affects: [02-30, 02-31, 02-32, scientific-setup]
tech-stack:
  added: []
  patterns: [exact-unit-validation, declared-wsl-targets, scoped-publication-receipts]
key-files:
  created: [Helix.Orchestration.Core/ScientificSetup/ProcedureDefinition.cs, Helix.Orchestration.Core/ScientificSetup/ExperimentPlanDefinition.cs, postgres/migrations/007_scientific_procedures_plans.sql]
  modified: [Helix.Orchestration.Core/ScientificSetup/ScientificSetupValidator.cs, Helix.Orchestration.Infrastructure/Persistence/PostgresScientificSetupRepository.cs]
key-decisions:
  - "Plans replace only explicitly declared decimal WSL targets, never inferred paths or units."
  - "Plan publication requires a published procedure revision in the same actor scope."
requirements-completed: [APP-03]
coverage:
  - id: D1
    description: "Independent procedure and plan content validates exact targets, units, bounds, and untouched WSL content."
    requirement: APP-03
    verification:
      - kind: unit
        ref: "tests/Helix.Orchestration.Core.Tests/ScientificSetup/ScientificSetupTests.cs"
        status: pass
    human_judgment: false
  - id: D2
    description: "Scoped procedure and plan drafts and immutable publication pins persist through PostgreSQL."
    requirement: APP-03
    verification:
      - kind: integration
        ref: "tests/Helix.Orchestration.Infrastructure.Tests/Persistence/ScientificSetupTests.cs"
        status: pass
    human_judgment: false
actuals:
  tokens: 13703
  tasks: 2
  commits: 3
plan_head_before: fbb82e54e63af06b88474b48fad96d36d4594277
plan_head_after: 3989a56248d367f8d437b02bd951efcf5ddcbbce
metrics:
  duration: 30min
  completed: 2026-10-31
status: complete
---

# Phase 02 Plan 29: Independent Procedure and Plan Publications Summary

Exact WSL parameter declarations, plan instantiation, and actor-scoped PostgreSQL procedure/plan publication pins are available without dispatching equipment.

## Accomplishments

- Added independently identified procedures and plans with declared WSL node/key decimal targets, exact units, dimensions, and inclusive bounds.
- Instantiates a deep WSL copy by replacing only validated declared targets; unrelated nodes, parameters, edges, IDs, and ordering remain intact.
- Added additive 007 schema for one-draft aggregates, immutable publication rows, exact procedure-revision foreign keys, and scope-protected publication operations.
- Added focused Core and PostgreSQL checks for validation, instantiation, draft isolation, procedure publication, and plan pinning.

## Verification Evidence

- `dotnet test tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj --no-restore --filter FullyQualifiedName~ScientificSetupTests --verbosity minimal` — 5 passed.
- `dotnet test tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj --no-restore --filter FullyQualifiedName~ScientificSetupTests --verbosity minimal` — 1 passed.

## Task Commits

1. Task 1: Validate independent procedure and plan scientific content — `a24a0d1`.
2. Task 2: Publish independent procedures and plans atomically — `f04aba8`, `3989a56`.

## Decisions Made

- Procedure values must target an existing decimal-valued WSL node parameter through an explicit node/key pair; strings, guessed paths, conversions, and overlapping targets are rejected.
- A plan must pin a procedure publication owned by the same actor scope; later procedure publications do not mutate stored plan pins.

## Deviations from Plan

### Auto-fixed Issues

1. **[Rule 1 - Bug] Completed publication operations after draft persistence exposed the missing immutable publication path.**
   - **Found during:** Task 2 verification.
   - **Fix:** Added locked publication transactions with replay-safe receipts and same-scope published-procedure pin validation.
   - **Files modified:** `IScientificSetupRepository.cs`, `PostgresScientificSetupRepository.cs`, `ScientificSetupTests.cs`.
   - **Commit:** `3989a56`.

## Known Stubs

None.

## Next Phase Readiness

02-30 can consume the typed, actor-scoped procedure/plan lifecycle ports. The Agent/native/UI expansion remains downstream, and these Core/persistence operations remain scientific intent only—no driver or physical command path was added.

## Self-Check: PASSED

- Required Core types, migration, repository, and both focused test files exist.
- Task commits `a24a0d1`, `f04aba8`, and `3989a56` are ancestors of this branch.
