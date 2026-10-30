---
last_mapped_commit: 25fb557bd430bea160370af41b8deb877170076b
last_mapped_at: 2026-10-29
---
# Testing Patterns

**Analysis Date:** 2026-10-29

## Test Framework

**Runner:**
- C#: xUnit 2 with `Microsoft.NET.Test.Sdk`; test projects target `net10.0`, enable nullable and implicit usings, and use `coverlet.collector`, as in `tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj`.
- Frontend: Vitest 4 with Vite and jsdom in `helix-dashboard/vitest.config.ts` and `helix-desktop/vitest.config.ts`.

**Assertion Library:**
- C#: xUnit `Assert` APIs (`Assert.Equal`, `Assert.Throws`, `Assert.Contains`).
- Frontend: Vitest `expect`, React Testing Library, and `@testing-library/jest-dom` matchers.

**Run Commands:**
```bash
dotnet test Helix.Orchestration.slnx                         # Run C# tests
dotnet test Helix.Orchestration.slnx --collect:"XPlat Code Coverage"  # C# coverage
npm test --prefix helix-dashboard                            # Dashboard tests
npm test --prefix helix-desktop                              # Desktop tests
```

## Test File Organization

**Location:**
- Put C# tests in the matching project under `tests/Helix.Orchestration.<Area>.Tests/`, grouped by production feature (for example `tests/Helix.Orchestration.Core.Tests/WSL/WslCompilerTests.cs` and `tests/Helix.Orchestration.Devices.Tests/FaultTolerance/ReconnectPolicyTests.cs`).
- Co-locate frontend tests beside their React component, hook, or library implementation, for example `helix-dashboard/src/pages/CampaignsPage.test.tsx` and `helix-desktop/src/hooks/useAgentHealth.test.ts`.

**Naming:**
- Name C# classes `<Subject>Tests`, and methods `<Member>_<Scenario>_<ExpectedOutcome>`, such as `ComputeDelay_WithNonPositiveAttempt_Throws` in `tests/Helix.Orchestration.Devices.Tests/FaultTolerance/ReconnectPolicyTests.cs`.
- Name frontend suites after the unit and use readable behavior sentences, such as `"loads and displays campaigns"` in `helix-dashboard/src/pages/CampaignsPage.test.tsx`.

**Structure:**
```
tests/Helix.Orchestration.<Area>.Tests/<Feature>/<Subject>Tests.cs
helix-dashboard/src/<feature>/<Subject>.test.ts(x)
helix-desktop/src/<feature>/<Subject>.test.ts(x)
```

## Test Structure

**Suite Organization:**
```csharp
[Fact]
public void ComputeDelay_CapsAtMaxDelay()
{
    var policy = new ReconnectPolicy { InitialDelayMs = 500, BackoffMultiplier = 3.0, MaxDelayMs = 2_000 };

    Assert.Equal(2_000, policy.ComputeDelay(3));
}
```
Use the same arrange/act/assert separation demonstrated in `tests/Helix.Orchestration.Devices.Tests/FaultTolerance/ReconnectPolicyTests.cs`. Use `[Theory]` plus `[InlineData]` for compact parameter matrices.

**Patterns:**
- Build valid domain fixtures with private static factory helpers, then alter only the relevant field with `with`, following `BuildValidDocument` in `tests/Helix.Orchestration.Core.Tests/WSL/WslCompilerTests.cs`.
- Test successful results and failure diagnostics/exception types; assert error codes when validating WSL compilation.
- Use `async Task` test methods for asynchronous system behavior; await HTTP calls and content parsing as in `tests/Helix.Orchestration.SiteServer.Tests/Endpoints/ExperimentEndpointsTests.cs`.
- In frontend tests, render with RTL, query accessible roles or `data-testid` hooks, and use `waitFor` for eventual UI/hook updates, as in `helix-dashboard/src/pages/CampaignsPage.test.tsx`.

## Mocking

**Framework:**
- Use NSubstitute extensively for device/transport interfaces, e.g. `Substitute.For<IDeviceTransport>()` in `tests/Helix.Orchestration.Devices.Tests/Abstractions/DeviceAdapterBaseTests.cs`.
- Use Moq for agent, repository, gRPC, recorder, and site-server dependencies, e.g. `new Mock<IRunspaceManager>()` in `tests/Helix.Orchestration.Agent.Tests/gRPC/AgentGrpcServiceTests.cs`.
- Use Vitest `vi` mocks/spies/stubbed globals for browser dependencies and network APIs.

**Patterns:**
```typescript
beforeEach(() => {
  vi.stubGlobal("fetch", vi.fn().mockResolvedValue({ ok: true, json: async () => [] }));
});
afterEach(() => vi.restoreAllMocks());
```
Follow this fetch lifecycle from `helix-dashboard/src/pages/CampaignsPage.test.tsx`; use `vi.mocked(fetch)` to make assertions. Use `vi.hoisted` when a mock must be available to module initialization, as in `helix-dashboard/src/hooks/useTrendWebSocket.test.ts`.

**What to Mock:**
- Mock hardware transports, external SDK sessions, repositories, agent control clients, browser `fetch`/`WebSocket`, and charting libraries.
- Keep reusable setup mocks in frontend `src/test/setup.ts`; dashboard setup globally mocks SciChart in `helix-dashboard/src/test/setup.ts`.

**What NOT to Mock:**
- Do not mock the unit under test or its simple value/domain collaborators. Prefer real in-memory registries and hand-written fakes when behavior matters; `FakeSafetyEngine` in `tests/Helix.Orchestration.Core.Tests/WSL/WslCompilerTests.cs` records observable calls.
- Do not connect Site Server endpoint tests to live MQTT or PostgreSQL; use `HelixWebApplicationFactory` in `tests/Helix.Orchestration.SiteServer.Tests/HelixWebApplicationFactory.cs`.

## Fixtures and Factories

**Test Data:**
```csharp
private static WslDocument BuildValidDocument() => new()
{
    DocumentId = "test-doc-1",
    Name = "TestWorkflow",
    SchemaVersion = "1.0",
    Nodes = [/* minimal valid nodes */],
    Edges = [/* valid edges */]
};
```
Use minimal valid in-code builders like `BuildValidDocument` in `tests/Helix.Orchestration.Core.Tests/WSL/WslCompilerTests.cs`; create the smallest scenario-specific data in the test.

**Location:**
- Keep simple factories/fakes private in the test file. Put shared HTTP service substitution in `tests/Helix.Orchestration.SiteServer.Tests/HelixWebApplicationFactory.cs`.
- Use xUnit class fixtures and collections for costly shared infrastructure; PostgreSQL tests use `PostgresFixture` and `PostgresRepositoriesTestsCollection` in `tests/Helix.Orchestration.Infrastructure.Tests/Persistence/PostgresRepositoriesTests.cs`.

## Coverage

**Requirements:**
- Coverlet collection is configured in C# test projects. GitHub Actions invokes `dotnet test` with `XPlat Code Coverage` and uploads Cobertura XML from `test-results/**/coverage.cobertura.xml` in `.github/workflows/ci.yml`.
- No coverage threshold or frontend coverage command is detected. `@vitest/coverage-v8` is installed in `helix-dashboard/package.json` and `helix-desktop/package.json` but not wired to a package script.

**View Coverage:**
```bash
dotnet test Helix.Orchestration.slnx --collect:"XPlat Code Coverage"
npx --prefix helix-dashboard vitest run --coverage
npx --prefix helix-desktop vitest run --coverage
```

## Test Types

**Unit Tests:**
- Exercise pure domain behavior, validation, fault tolerance, adapters, and device drivers with fakes or substitutes; see `tests/Helix.Orchestration.Core.Tests/WSL/WslCompilerTests.cs` and `tests/Helix.Orchestration.Devices.Tests/FaultTolerance/ReconnectPolicyTests.cs`.

**Integration Tests:**
- Exercise ASP.NET Core endpoints through `WebApplicationFactory<Program>` with in-memory replacements in `tests/Helix.Orchestration.SiteServer.Tests/Endpoints/ExperimentEndpointsTests.cs`.
- Exercise PostgreSQL repositories against a Testcontainers PostgreSQL instance or CI PostgreSQL service in `tests/Helix.Orchestration.Infrastructure.Tests/Persistence/PostgresRepositoriesTests.cs`; the tests skip their assertions when the fixture is unavailable.

**E2E Tests:**
- No browser automation framework is detected. C# contains workflow execution integration stubs at `tests/Helix.Orchestration.Agent.Tests/Integration/E2eWorkflowExecutionTests.cs`.

## Common Patterns

**Async Testing:**
```typescript
const { result } = renderHook(() => useAgentHealth("http://localhost:5080", 999999));
await waitFor(() => expect(result.current.healthy).toBe(true));
```
Use RTL `waitFor` after asynchronous browser state changes, as in `helix-desktop/src/hooks/useAgentHealth.test.ts`. In C#, await the operation, then assert the response/result rather than blocking.

**Error Testing:**
```csharp
[Theory]
[InlineData(0)]
[InlineData(-1)]
public void ComputeDelay_WithNonPositiveAttempt_Throws(int attempt)
    => Assert.Throws<ArgumentOutOfRangeException>(() => new ReconnectPolicy().ComputeDelay(attempt));
```
Test invalid inputs with exact domain diagnostic codes, exception types, or HTTP status codes; examples appear in `tests/Helix.Orchestration.Core.Tests/WSL/WslCompilerTests.cs` and `tests/Helix.Orchestration.SiteServer.Tests/Endpoints/ExperimentEndpointsTests.cs`.

---

*Testing analysis: 2026-10-29*
