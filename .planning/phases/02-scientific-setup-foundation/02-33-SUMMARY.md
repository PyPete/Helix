---
phase: 02-scientific-setup-foundation
plan: "33"
subsystem: scientific-samples
tags: [csharp, postgresql, dapper, xunit, testcontainers]
requires:
  - phase: 02-32
    provides: completed scientific setup prerequisites
provides:
  - immutable collection identity and append-only custody evidence
  - PostgreSQL sample heads and actor-scoped operation receipts
affects: [samples, scientific-setup, agent-api]
tech-stack:
  added: []
  patterns: [server-sequenced projection, expected-head append transaction, immutable SQL events]
key-files:
  created: [Helix.Orchestration.Core/Samples/SampleFacts.cs, Helix.Orchestration.Core/Samples/SampleProjection.cs, Helix.Orchestration.Core/Samples/ISampleRepository.cs, Helix.Orchestration.Infrastructure/Persistence/PostgresSampleRepository.cs, postgres/migrations/008_scientific_samples.sql]
  modified: [Helix.Orchestration.Infrastructure/Persistence/InfrastructureServiceExtensions.cs, tests/Helix.Orchestration.Core.Tests/Samples/SampleFactsTests.cs, tests/Helix.Orchestration.Infrastructure.Tests/Persistence/SamplesTests.cs]
decisions:
  - "Custody projection is ordered only by server sequence; reported occurrence time may be backdated evidence."
  - "Sample collection and custody operations use actor-scoped receipts and expected stream heads."
metrics:
  duration: 25min
  completed: 2026-10-31
status: complete
actuals:
  tokens: 4375
  tasks: 2
  commits: 4
plan_head_before: df28b88bc2214e4cf017167c950a08d548c5e36a
plan_head_after: e50d4a13b7d9c3fcc15d3dc498e7e728331c6d64
---

# Phase 02 Plan 33: Sample Collection and Custody Foundation Summary

**Immutable sample UUIDs, attributed collection facts, and server-sequenced custody evidence now persist atomically in isolated PostgreSQL.**

## Accomplishments

- Added typed collection, event, projection, scoped repository, and receipt contracts with future-time rejection and correction provenance validation.
- Added migration 008 for immutable sample facts, stream heads, and actor-bound operation receipts.
- Added a scoped Dapper repository that confirms a sample and appends custody evidence using an expected-head transaction.
- Proved core server-sequence behavior and isolated Testcontainers confirmation/append reconstruction.

## Task Commits

1. **Validate collection origins and sequence-safe custody corrections** — `e406a83`, `fa94336`
2. **Commit complete collection and custody evidence in isolated SQL** — `f51bd20`, `e50d4a1`

## Verification Evidence

- `dotnet test tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj --no-restore --filter FullyQualifiedName~SampleFactsTests --verbosity minimal` — passed: 3 tests.
- `dotnet test tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj --no-restore --filter FullyQualifiedName~SamplesTests --verbosity minimal` — passed: 2 tests, including an isolated Testcontainers PostgreSQL confirmation and custody append/reconstruction.

## Decisions Made

- A server sequence, not reported occurrence time, selects the current custody projection.
- A correction carries a same-stream superseded event and reason; the original event remains immutable.

## Deviations from Plan

### Auto-fixed Issues

1. **[Rule 1 - Bug] Corrected append ordering and PostgreSQL event materialization**
   - **Found during:** Task 2
   - **Fix:** Insert the immutable event before linking the head foreign key, then map durable timestamp rows explicitly.
   - **Files modified:** `PostgresSampleRepository.cs`, `SamplesTests.cs`
   - **Commit:** `e50d4a1`

## Known Stubs

None.

## Self-Check: PASSED

- Required sample contracts, migration, repository, and both focused test files exist.
- Commits `e406a83`, `fa94336`, `f51bd20`, and `e50d4a1` are ancestors of HEAD.
