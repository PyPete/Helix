---
phase: 02-scientific-setup-foundation
plan: "39"
subsystem: scientific-policy-freeze
tags: [csharp, postgresql, policy, provenance, immutable-evidence]
requires: [02-38]
provides: [immutable-policy-content, independent-approval-provenance, frozen-manifest-port]
affects: [APP-04, 02-40, 02-41, 02-42]
tech-stack:
  added: []
  patterns: [content-is-not-approval, fail-closed-authority, immutable-postgresql-evidence]
key-files:
  created:
    - Helix.Orchestration.Core/ScientificSetup/IPolicyRevisionProvider.cs
    - Helix.Orchestration.Core/ScientificSetup/FrozenSetupManifest.cs
    - Helix.Orchestration.Core/ScientificSetup/IFrozenSetupRepository.cs
    - Helix.Orchestration.Infrastructure/Persistence/PostgresPolicyRevisionProvider.cs
    - Helix.Orchestration.Infrastructure/Persistence/PostgresFrozenSetupRepository.cs
    - postgres/migrations/010_scientific_policy_freeze.sql
  modified:
    - Helix.Orchestration.Infrastructure/Persistence/InfrastructureServiceExtensions.cs
decisions:
  - Policy content registration is immutable and never itself creates an approval.
  - Production uses an unavailable approval authority so approved resolution fails closed until a later trusted authority is wired.
  - Frozen compilation artifacts retain and validate their original byte hash rather than accepting regenerated evidence.
metrics:
  duration: 15min
  completed: 2026-11-01
  tasks: 2
status: complete
plan_head_before: 5cc273847c164ae7ff4b6e5c3ecc86df562c3633
plan_head_after: 5127535ef0bdf82c190dc8874ddace228ba03378
commits: 4
actuals:
  tokens: 6298
  tasks: 2
  commits: 4
requirements-completed: [APP-04]
---

# Phase 02 Plan 39: Immutable Policy Evidence Summary

Immutable policy bytes, their independently verified approval provenance, and exact frozen setup evidence now have typed Core and PostgreSQL foundations that fail closed without an authority provider.

## Accomplishments

- Added typed policy content, independent approval provenance/authority, immutable frozen references, manifest, execution binding, and repository ports.
- Validated policy-content and compiled-artifact hashes against exact bytes; manifest provenance must bind the same policy revision, schema, and hash.
- Added additive migration 010 with immutable runtime guards for policy revisions and frozen setup manifests.
- Added actor-scoped PostgreSQL policy registration/resolution, frozen manifest storage, and a deliberately unavailable production authority registration.

## Task Commits

1. **Task 1: Validate immutable policy content and independent approval provenance** — `61089eb`, `a7da938`
2. **Task 2: Persist exact immutable policy and compare-freeze evidence** — `7234aee`, `5127535`

## Verification

- RED: both initial reflection-based contract tests executed and failed on the missing planned contract types (1 test each).
- `dotnet test tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj --no-restore --filter FullyQualifiedName~PolicyReferenceTests --verbosity minimal` — passed (2 tests).
- `dotnet test tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj --no-restore --filter FullyQualifiedName~PolicyReferenceTests --verbosity minimal` — passed (3 tests, including real Testcontainers PostgreSQL policy round-trip and rejected authority resolution).

## Decisions Made

- Registering exact policy content is not approval and cannot be used as an authorization substitute.
- No production test authority is installed; a missing trusted authority returns an explicit fail-closed result.

## Deviations from Plan

### Auto-fixed Issues

1. **[Rule 1 - Persistence mapping] Corrected timestamp rehydration for policy rows**
- **Found during:** Task 2
- **Issue:** Dapper materialized PostgreSQL timestamps as `DateTime`, which could not construct the `DateTimeOffset` policy record directly.
- **Fix:** Added a persistence row projection and explicit UTC reconstruction.
- **Files modified:** `PostgresPolicyRevisionProvider.cs`
- **Commit:** `5127535`

2. **[Rule 2 - Evidence integrity] Validated frozen compiled-artifact hashes**
- **Found during:** Task 2
- **Issue:** A manifest could carry bytes and an unverified artifact hash.
- **Fix:** Validate the SHA-256 digest of the stored artifact before freezing or reconstruction.
- **Files modified:** `FrozenSetupManifest.cs`, `PolicyReferenceTests.cs`
- **Commit:** `5127535`

## Known Stubs

None.

## Self-Check: PASSED

- Required Core ports, PostgreSQL providers, migration, and both policy-reference test files exist.
- Task commits `61089eb`, `a7da938`, `7234aee`, and `5127535` are ancestors of the recorded plan head.
