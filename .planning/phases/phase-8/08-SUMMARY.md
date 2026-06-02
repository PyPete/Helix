# Phase 8: CI Hardening + Full Test Coverage — Summary

**Completed:** 2026-06-02
**Status:** COMPLETE
**Test result:** 582 passed, 0 failed, 0 skipped

---

## What Was Done

### 08-01: Coverlet added to Core.Tests and Infrastructure.Tests

Added `coverlet.collector Version="6.*"` to the two test projects that were missing it. All 6 .NET test projects now have coverlet for coverage collection.

Also fixed a pre-existing build error: `InMemoryExperimentRepository` in `HelixWebApplicationFactory.cs` was missing the `GetAllAsync` method added by Phase 7 to `IExperimentRepository`. Added the implementation.

### 08-02: ci.yml — Postgres service container + coverage + dashboard job

- Added `postgres:16` service container to `build-and-test` job with `pg_isready` health check
- Added `PGHOST/PGPORT/PGDATABASE/PGUSER/PGPASSWORD` env vars so `PostgresFixture` connects to the service container instead of spinning a Testcontainers instance
- Updated `PostgresFixture` to detect `PGHOST` env var and bypass Testcontainers when running in CI
- Added `--collect:"XPlat Code Coverage"` to `dotnet test` step
- Added `Upload coverage report` artifact step (cobertura XML, 30-day retention)
- Added `if: always()` to test + coverage upload steps so artifacts are preserved on failure
- Added new `dashboard` job: `npm ci && npm test -- --run && npm run build` for `helix-dashboard/`

### 08-03: Verify + regression guard

**Pre-existing bugs found and fixed:**

1. **FK violation in test data**: `PostgresExperimentRepository_SaveAndGetByRunspaceId_Works` used `CampaignId: "camp-1"` but no campaign row existed — FK constraint `fk_experiments_campaign` violated. Fixed: use `CampaignId: null` (nullable FK).

2. **Dapper positional record deserialization failure**: Both `PostgresExperimentRepository` and `PostgresCampaignRepository` queried directly into positional C# records (`ExperimentRecord`, `CampaignRecord`, `CampaignIterationRecord`). Dapper 2.x cannot deserialize positional records because their constructor parameters are PascalCase but Dapper lowercases column names when matching. Fixed by:
   - Adding private `ExperimentRow` class with settable properties in `PostgresExperimentRepository`
   - Adding private `CampaignRow` + `CampaignIterationRow` classes with settable properties in `PostgresCampaignRepository`
   - Each row class has a `ToRecord()` method projecting to the immutable public record
   - Queries now target the row DTOs; all SELECT SQL uses snake_case column names (no AS aliases needed)
   - `DefaultTypeMap.MatchNamesWithUnderscores = true` added to `DbConnectionFactory` static constructor (harmless for existing code, enables `created_at` → `CreatedAt` mapping)

**Verification results:**

| Project | Tests | Result |
|---------|-------|--------|
| Helix.Orchestration.Core.Tests | 112 | PASS |
| Helix.Orchestration.Agent.Tests | 15 | PASS |
| Helix.Orchestration.Devices.Tests | 394 | PASS |
| Helix.Orchestration.SiteServer.Tests | 44 | PASS |
| Helix.Orchestration.Infrastructure.Tests | 2 | PASS |
| Helix.Orchestration.Recorder.Tests | 15 | PASS |
| **Total** | **582** | **PASS** |
| helix-dashboard (vitest) | 32 | PASS |

**Coverage:** 6 `coverage.cobertura.xml` files generated in `coverage-results/` directory.

**Regression guard:** Deliberately introduced `Assert.False(success)` into `RecordedEventParseTests`. `dotnet test` exited with code 1 (2 failed). Reverted. CI gate confirmed.

**[Fact(Skip)] check:** `grep -r "\[Fact(Skip" tests/` — 0 matches. Confirmed.

---

## Success Criteria

| Criterion | Status | Evidence |
|-----------|--------|----------|
| CI exits 0 on clean dev push | READY | All 582 tests pass locally; service container wired in ci.yml |
| No [Skip] annotations in any test file | VERIFIED | grep returns 0 matches |
| Coverage report artifact produced | VERIFIED | 6 cobertura XML files generated locally; ci.yml uploads them |
| Recorder.Tests has at least one passing test | VERIFIED | 15 tests pass (7 parse + 5 writer + 3 JSONL) |
| Deliberate failure causes CI failure | VERIFIED | exit code 1 confirmed |

---

## Files Changed

| File | Change |
|------|--------|
| `tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj` | Added coverlet.collector |
| `tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj` | Added coverlet.collector |
| `tests/Helix.Orchestration.SiteServer.Tests/HelixWebApplicationFactory.cs` | Added missing `GetAllAsync` to `InMemoryExperimentRepository` |
| `tests/Helix.Orchestration.Infrastructure.Tests/Persistence/PostgresRepositoriesTests.cs` | PGHOST env-var bypass in `PostgresFixture`; FK fix (CampaignId: null) |
| `Helix.Orchestration.Infrastructure/Persistence/DbConnectionFactory.cs` | Added Dapper `MatchNamesWithUnderscores = true` static init |
| `Helix.Orchestration.Infrastructure/Persistence/PostgresExperimentRepository.cs` | Added `ExperimentRow` DTO; all queries target row class then project |
| `Helix.Orchestration.Infrastructure/Persistence/PostgresCampaignRepository.cs` | Added `CampaignRow` + `CampaignIterationRow` DTOs; replaced positional record direct queries |
| `.github/workflows/ci.yml` | Postgres service container, coverage flag, coverage artifact, dashboard job |
| `.planning/ROADMAP.md` | Phase 7 and 8 progress rows updated to Complete |
