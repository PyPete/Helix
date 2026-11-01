---
phase: 02-scientific-setup-foundation
plan: "36"
subsystem: scientific-sample-lineage
tags: [csharp, postgresql, samples, provenance, lineage]
requires: [02-35]
provides: [validated-transformations, atomic-lineage-persistence, bounded-lineage-readback]
affects: [SAMP-02]
tech-stack:
  added: []
  patterns: [new-child-only-lineage, uuid-ordered-parent-locks, scoped-recursive-traversal]
key-files:
  created:
    - Helix.Orchestration.Core/Samples/SampleLineageValidator.cs
    - postgres/migrations/009_scientific_transformations.sql
  modified:
    - Helix.Orchestration.Core/Samples/SampleFacts.cs
    - Helix.Orchestration.Core/Samples/SampleProjection.cs
    - Helix.Orchestration.Core/Samples/ISampleRepository.cs
    - Helix.Orchestration.Infrastructure/Persistence/PostgresSampleRepository.cs
    - tests/Helix.Orchestration.Core.Tests/Samples/SampleFactsTests.cs
    - tests/Helix.Orchestration.Infrastructure.Tests/Persistence/SamplesTests.cs
decisions:
  - Transformation child identities are transformation-origin samples with immutable parent links, never inferred collection records.
  - Transformation writes lock scoped parent samples in UUID order and persist activity, children, edges, heads, and operation receipt in one transaction.
  - Lineage traversal is scoped, depth- and item-bounded, deterministic, cancellation-aware, and returns an explicit continuation when truncated.
metrics:
  duration: 18min
  completed: 2026-11-01
  tasks: 2
status: complete
plan_head_before: 2aeaadc9bdf6540b9477b125daab483c88f811ca
plan_head_after: 04c1fbfb47a590db6bbd20442679a20fd111495c
commits: 3
actuals:
  tokens: 11541
  tasks: 2
  commits: 3
requirements-completed: [SAMP-02]
---

# Phase 02 Plan 36: Atomic Sample Transformation Lineage Summary

Validated Split, Pool, and Processing transformations now create immutable, scoped transformation-origin children and reconstruct bounded parent/child lineage from isolated PostgreSQL storage.

## Accomplishments

- Added typed transformation facts, exact-unit quantity validation, and new-child/distinct-parent constraints without conservation or depletion assumptions.
- Added additive migration 009 for immutable transformation activities and parent-to-child edges with runtime-role grants.
- Persisted validated activity, child samples, origin events, heads, edges, and actor-bound operation receipts in one transaction; operation replay is content-hash checked.
- Added scoped, deterministic ancestor/descendant traversal with depth 32, page max 100, 1,000-row guard, five-second commands, cycle protection, and explicit truncation continuation.

## Task Commits

1. **Task 1: Validate complete many-parent/new-child transformations** — `56c78c3`, `8f11036`
2. **Task 2: Commit atomic lineage and reconstruct bounded evidence** — `04c1fbf`

## Verification

- `docker version --format '{{.Server.Version}}'` — passed; Docker Server 29.9.2 available before the isolated PostgreSQL suite.
- `dotnet test tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj --no-restore --filter FullyQualifiedName~SampleFactsTests --verbosity minimal` — passed (7 tests).
- `dotnet test tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj --no-restore --filter FullyQualifiedName~SamplesTests --verbosity minimal` — passed (3 tests), including isolated PostgreSQL transformation write, replay, and cache-free reconstruction.

## Decisions Made

- Transformation-origin evidence uses `SampleEventKind.Transformed`, preventing transformed samples from being represented as collected samples.
- Scope checks occur before lineage reconstruction, so foreign samples cannot be used as traversal roots or edges.

## Deviations from Plan

### Auto-fixed Issues

1. **[Rule 1 - Bug] Preserve transformation origin during repository reconstruction**
- **Found during:** Task 2
- **Issue:** `ReadAsync` reconstructed every sample with a collection origin, which would reject transformation-origin event history after restart.
- **Fix:** Read persisted origin and parent IDs before projection reconstruction; allow the initial event kind required by the immutable origin.
- **Files modified:** `SampleFacts.cs`, `SampleProjection.cs`, `PostgresSampleRepository.cs`, `SamplesTests.cs`
- **Commit:** `04c1fbf`

## Known Stubs

None.

## Downstream Readiness

Plan 37 can expose the typed transformation and bounded lineage port through Agent routes without creating a pre-existing-child attachment path or inventing collection/inventory facts.

## Self-Check: PASSED

- Required migration, validator, repository, and focused test artifacts exist.
- Task commits `56c78c3`, `8f11036`, and `04c1fbf` are ancestors of the recorded plan head.
