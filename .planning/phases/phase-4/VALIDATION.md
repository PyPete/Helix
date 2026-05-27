# Phase 4 Validation

**Phase:** 4 — Transport + Execution Integration Tests
**Requirements:** D2, D3, D5
**Generated from:** RESEARCH.md § Validation Architecture

---

## Validation Map

| Task | Req ID | Behavior | Automated Command |
|------|--------|----------|-------------------|
| T1 | D2 | Modbus write path (`WriteMultipleRegisters`) | `dotnet test Helix.Orchestration.slnx --filter "FullyQualifiedName~ModbusTransportTests" -c Debug` |
| T1 | D2 | Modbus read-input registers | same |
| T1 | D2 | Modbus fault paths (Write IOException, ReadInput IOException) | same |
| T2 | D2 | Serial BaudRate propagation through factory | `dotnet test Helix.Orchestration.slnx --filter "FullyQualifiedName~SerialTransportTests" -c Debug` |
| T2 | D2 | Serial write-exception fault path | same |
| T3 | D2 | OPC UA subscribe → unsubscribe (Dispose) path | `dotnet test Helix.Orchestration.slnx --filter "FullyQualifiedName~OpcTransportTests" -c Debug` |
| T4 | D2 | TCP LengthPrefix framing round-trip | `dotnet test Helix.Orchestration.slnx --filter "FullyQualifiedName~TcpTransportTests" -c Debug` |
| T5 | D3 | DurableRunspace → CapabilityRegistry → InMemoryTrendProvider → Completed | `dotnet test Helix.Orchestration.slnx --filter "FullyQualifiedName~E2eWorkflowExecutionTests" -c Debug` |
| T6 | D5 | PostgresExperimentRepository save/get (no [Skip]) | `dotnet test Helix.Orchestration.slnx --filter "FullyQualifiedName~PostgresRepositoriesTests" -c Debug` |
| T6 | D5 | PostgresCampaignRepository save/get (no [Skip]) | same |
| T7 | — | All test projects included in slnx, no build=false | `dotnet test Helix.Orchestration.slnx -c Debug --logger "console;verbosity=normal"` |

---

## Environment Requirements

| Dependency | Required By | Available | Fallback |
|------------|------------|-----------|----------|
| Docker daemon | Testcontainers.PostgreSql (T6) | ubuntu-latest CI runner | `IsAvailable` guard — silent pass on dev machines without Docker |
| .NET 10.0 SDK | All test projects | ci.yml: `dotnet-version: 10.0.x` | None required |
| postgres:16 image | T6 infrastructure tests | Pulled by Testcontainers on first run | None |

---

## Full Suite Command

```bash
dotnet test Helix.Orchestration.slnx -c Debug --logger "console;verbosity=normal"
```

**Expected:** All tests pass. Zero `[Skip]` entries for `PostgresRepositoriesTests`. All T1–T4 transport tests present in Devices.Tests output.

---

## Assumptions

| # | Claim | Risk if Wrong |
|---|-------|---------------|
| A1 | ubuntu-latest GitHub Actions runner has Docker daemon running | Testcontainers tests fail on CI; add `docker-in-docker` step to ci.yml |
| A2 | `InMemoryFunctionStore` is in base Cleipnir package (no extra PackageReference needed) | E2E test fails to compile |
| A3 | `IsAvailable` guard makes PostgreSQL tests safe to run without Docker (silent pass) | False-green if Docker absent in CI — acceptable given A1 |
