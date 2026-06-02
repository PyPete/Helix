# Phase 8: CI Hardening + Full Test Coverage

**Phase Goal:** Every test project builds and runs in CI with no skipped tests, a coverage report is generated, and the CI pipeline is a reliable gatekeeper for all future work.

**Dependencies:** Phase 4 (transport + infrastructure tests clean), Phase 6 (UI component tests exist), Phase 7 (dashboard UI tests exist)

**Requirements covered:** D1, D6, D7

---

## Context

### What exists today

| Area | State |
|------|-------|
| `.github/workflows/ci.yml` | Runs `dotnet test Helix.Orchestration.slnx` + Tauri Linux build. No coverage flag, no React dashboard test step, no Postgres service container. |
| All 6 .NET test projects | In `Helix.Orchestration.slnx`. All build. Driver-port verification confirmed **394 tests pass** in Devices.Tests. Phase 7 verification confirmed SiteServer builds 0 errors. |
| Coverlet | Present in `Devices.Tests`, `SiteServer.Tests`, `Recorder.Tests`, `Agent.Tests`. Missing from `Core.Tests` and `Infrastructure.Tests`. |
| Postgres repository tests | `PostgresRepositoriesTests.cs` uses Testcontainers with a graceful `IsAvailable` guard — silently skips if Docker unavailable. Tests pass when Docker is present. No `[Fact(Skip)]` annotations. |
| React dashboard tests | `helix-dashboard/` has `vitest` + `@vitest/coverage-v8`. **32 tests pass**. No CI step for this yet. |
| `[Fact(Skip)]` annotations | One method name contains `Skips` (not a skip annotation). Zero actual `[Fact(Skip)]` annotations found by grep. |

### What needs to change

1. **CI: Add Postgres service container** so Testcontainers connects to the pre-started container rather than starting its own (faster, no Docker-in-Docker).
2. **CI: Add coverlet flags** to `dotnet test` step to collect coverage and upload lcov/HTML report artifact.
3. **CI: Add React dashboard test + build step** for `helix-dashboard/`.
4. **Coverlet: Add to Core.Tests and Infrastructure.Tests** — two projects currently lack the package reference.
5. **Recorder.Tests: Confirm tests pass** — already has `RecordedEventParseTests` (7 tests) and `RecorderWriterServiceTests` (5 tests). Need to verify they pass in CI.
6. **Deliberate-failure regression guard** — the Phase 8 success criteria require a documented CI failure path; this is validated by confirming the test run fails on a broken test (verified in CI output, not code changes).

---

## Wave Structure

**Wave 0 (prerequisites — single plan):** Add coverlet packages to the two missing projects.

**Wave 1 (parallel — 2 plans):**
- 08-02: Update `ci.yml` — Postgres service container + coverlet flags + dashboard step
- 08-03: Verify and document CI green run + deliberate-failure gate

---

## Plans

### 08-01-PLAN.md — Add Coverlet to Core.Tests and Infrastructure.Tests

**Goal:** All 6 test projects have `coverlet.collector` so the coverage flag works uniformly.

**Changes:**

`tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj` — add:
```xml
<PackageReference Include="coverlet.collector" Version="6.*">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

`tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj` — add:
```xml
<PackageReference Include="coverlet.collector" Version="6.*">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

**Verification:**
- `dotnet restore Helix.Orchestration.slnx` exits 0
- `dotnet build Helix.Orchestration.slnx -c Release` exits 0

---

### 08-02-PLAN.md — Update ci.yml (Service Container + Coverage + Dashboard Step)

**Goal:** CI pipeline runs all tests with coverage, uses Postgres service container for Infrastructure tests, and adds the React dashboard build/test step.

**Key decisions:**
- Postgres service container: `postgres:16` with `POSTGRES_DB=helix_orchestration POSTGRES_USER=helix POSTGRES_PASSWORD=helix_dev_password`
- Testcontainers must be told to use the service container. The `PostgresFixture` uses `PostgreSqlBuilder` which respects `PGHOST` / `PGPORT` env vars **only if** `WithReuse(true)` or a custom connection string is set. The cleanest approach: pass `PGHOST=localhost PGPORT=5432 ...` and update `PostgresFixture` to detect the `PGHOST` env var and use a manual connection string instead of spinning a container.
- Coverage: `--collect:"XPlat Code Coverage"` + `--coverage-output-format cobertura` uploaded as artifact
- Dashboard: `npm ci && npm test -- --run && npm run build` in `helix-dashboard/`

**Changes to `PostgresFixture` in `PostgresRepositoriesTests.cs`:**

Add environment-variable bypass so when `PGHOST` is set (CI service container), no Testcontainers spin-up occurs:

```csharp
public PostgresFixture()
{
    var pgHost = Environment.GetEnvironmentVariable("PGHOST");
    if (!string.IsNullOrEmpty(pgHost))
    {
        var pgPort = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";
        var pgDb = Environment.GetEnvironmentVariable("PGDATABASE") ?? "helix_orchestration";
        var pgUser = Environment.GetEnvironmentVariable("PGUSER") ?? "helix";
        var pgPassword = Environment.GetEnvironmentVariable("PGPASSWORD") ?? "helix_dev_password";
        _connectionString = $"Host={pgHost};Port={pgPort};Database={pgDb};Username={pgUser};Password={pgPassword}";
        _container = null;
        IsAvailable = true;
        return;
    }
    try
    {
        _container = new PostgreSqlBuilder().Build();
        IsAvailable = true;
    }
    catch
    {
        _container = null;
        IsAvailable = false;
    }
}
```

Add `_connectionString` nullable field. Update `InitializeAsync` to use it when `_container` is null but `IsAvailable` is true (service container path).

**Changes to `.github/workflows/ci.yml`:**

Update `build-and-test` job:
- Add `services:` block with `postgres:16` service container (env vars, health check via `pg_isready`)
- Add env vars `PGHOST: localhost`, `PGPORT: 5432`, `PGDATABASE: helix_orchestration`, `PGUSER: helix`, `PGPASSWORD: helix_dev_password`
- Update `Test` step: add `--collect:"XPlat Code Coverage" --results-directory coverage-results`
- Add `Upload coverage report` step using `actions/upload-artifact@v4` with path `coverage-results`

Add new `dashboard` job (parallel to `build-and-test`):
```yaml
dashboard:
  name: Dashboard Build and Test
  runs-on: ubuntu-latest

  steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-node@v4
      with:
        node-version: 20
        cache: npm
        cache-dependency-path: helix-dashboard/package-lock.json
    - name: Install dependencies
      working-directory: helix-dashboard
      run: npm ci
    - name: Run tests
      working-directory: helix-dashboard
      run: npm test -- --run
    - name: Build
      working-directory: helix-dashboard
      run: npm run build
```

**Verification:**
- `dotnet test` with coverage flag completes locally (or CI dry-run output reviewed)
- `npm test -- --run` passes 32 tests
- `npm run build` exits 0

---

### 08-03-PLAN.md — Verify Full CI Run and Regression Guard

**Goal:** Confirm the CI pipeline exits green on `dev` branch and that a deliberate test failure causes the pipeline to fail.

**Tasks:**

1. **Run full dotnet test suite locally** with coverage flag to confirm baseline:
   ```
   dotnet test Helix.Orchestration.slnx -c Release --collect:"XPlat Code Coverage" --results-directory coverage-results
   ```
   Capture pass/fail count and confirm 0 failures.

2. **Confirm Recorder.Tests passes:** `dotnet test tests/Helix.Orchestration.Recorder.Tests/ -c Release` — 12 tests (7 parse + 5 writer) should pass.

3. **Deliberate-failure regression guard:** Temporarily introduce a failing assertion in any test, confirm `dotnet test` exits non-zero, then revert. Document the exit code in the SUMMARY.

4. **Commit and push to `dev`** to trigger CI. Confirm:
   - `build-and-test` job exits green
   - `dashboard` job exits green (32 vitest tests pass, build succeeds)
   - `tauri-build` job exits green (pre-existing)
   - Coverage artifact appears in run artifacts
   - Postgres service container started and Infrastructure tests ran (not skipped)

5. **Update ROADMAP.md** Phase 8 progress row to `Complete`.

**Verification criteria (maps to ROADMAP success criteria):**
- [ ] CI pipeline exits 0 on clean dev push
- [ ] No `[Skip]` annotations in any test file (grep confirms — already true)
- [ ] Coverage report artifact produced for every CI run
- [ ] Recorder.Tests has at least one passing test (12 total)
- [ ] A deliberate test failure causes CI to fail (exit code 1 confirmed)

---

## Execution Order

```
Wave 0:  08-01-PLAN.md (coverlet packages)
            ↓
Wave 1:  08-02-PLAN.md  ──── (ci.yml + PostgresFixture)
         08-03-PLAN.md  ──── (verify + regression guard)   [run after 08-02]
```

Wave 1 plans are effectively sequential (03 depends on 02 being merged), but both are lightweight enough to batch in a single execution wave.

---

## Threat Model

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Postgres service container not ready when tests run | Medium | `health-check` with `pg_isready` + `options: --health-retries 5`; `depends_on` not needed (service containers are separate from job steps) |
| `PostgresFixture` env-var path skips migration | Low | `InitializeAsync` runs the same `001_initial.sql` migration in both paths |
| Coverage report too large for artifact upload | Low | Default coverlet cobertura output is small; 30-day retention |
| Dashboard `npm ci` fails due to scichart WASM postinstall | Medium | Already works in Phase 7 local build; `copy:wasm` script copies from `node_modules/scichart/` — no network needed |
| Tauri build time exceeds job timeout | Low | Already passing pre-Phase 8; no changes to Tauri |

---

## Files Modified

| File | Change |
|------|--------|
| `tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj` | Add coverlet.collector package reference |
| `tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj` | Add coverlet.collector package reference |
| `tests/Helix.Orchestration.Infrastructure.Tests/Persistence/PostgresRepositoriesTests.cs` | Add env-var bypass path to `PostgresFixture` for service container |
| `.github/workflows/ci.yml` | Add postgres service, env vars, coverage flag, dashboard job |
| `.planning/ROADMAP.md` | Update Phase 8 progress to Complete |

---

## Success Criteria Mapping

| Criterion | Plan | How verified |
|-----------|------|-------------|
| CI exits 0 on clean dev push | 08-03 | CI run output |
| No [Skip] annotations in any test file | 08-03 | `grep -r "\[Fact(Skip" tests/` returns 0 matches |
| Coverage report artifact produced | 08-02 | Artifact visible in GitHub Actions run |
| Recorder.Tests has at least one passing test | 08-03 | `dotnet test` output shows 12 pass |
| Deliberate failure causes CI failure | 08-03 | Exit code 1 documented |
