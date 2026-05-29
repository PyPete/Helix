---
phase: "06"
plan: "03"
subsystem: "WSL Compiler"
tags: [wsl, compiler, safety, tdd, transforms]
dependency_graph:
  requires: []
  provides:
    - WslCompiler with InjectSafetyEnvelope, ExpandLoopNodes, FlattenBranchNodes, FlattenParallelNodes
    - CompilationResult, CompilationDiagnostic, DiagnosticSeverity support types
    - IWslCompiler interface
    - DeviceDescriptor, DeviceRegistry device binding support
  affects:
    - Helix.Orchestration.Core/WSL
    - tests/Helix.Orchestration.Core.Tests/WSL
tech_stack:
  added: []
  patterns:
    - TDD RED/GREEN with manual test doubles (no mock framework)
    - Graph transformation pipeline (4 phases applied before RunspaceDefinition emission)
    - Threat-model-driven iteration capping (T-06-07: MaxLoopIterations=1000)
    - Sync wrapper over async (GetAwaiter().GetResult() for safety snapshot on compile path)
key_files:
  created:
    - Helix.Orchestration/Helix.Orchestration.Core/WSL/WslCompiler.cs
    - Helix.Orchestration/Helix.Orchestration.Core/WSL/IWslCompiler.cs
    - Helix.Orchestration/Helix.Orchestration.Core/WSL/CompilationResult.cs
    - Helix.Orchestration/Helix.Orchestration.Core/WSL/CompilationDiagnostic.cs
    - Helix.Orchestration/Helix.Orchestration.Core/WSL/DiagnosticSeverity.cs
    - Helix.Orchestration/Helix.Orchestration.Core/Devices/DeviceDescriptor.cs
    - Helix.Orchestration/Helix.Orchestration.Core/Devices/DeviceRegistry.cs
    - Helix.Orchestration/tests/Helix.Orchestration.Core.Tests/WSL/WslCompilerTests.cs
  modified: []
decisions:
  - Manual test doubles (FakeSafetyEngine) used instead of Moq/NSubstitute — no mock framework in worktree test project
  - Sync compile path uses GetAwaiter().GetResult() for safety snapshot — acceptable since compilation is not on a hot path
  - Loop iterations capped at 1000 (Threat T-06-07) with WSL051 diagnostic emitted on cap/invalid
  - DeviceRegistry and DeviceDescriptor created in worktree since the worktree branch predates these types
  - WslCompiler baseline validation methods (ValidateSchema through ValidateVariableReferences) added to match dev branch interface
metrics:
  duration: "~25 min"
  completed: "2026-05-29"
  tasks_completed: 1
  tasks_total: 1
  files_created: 8
  files_modified: 0
---

# Phase 06 Plan 03: WslCompiler Transformations Summary

**One-liner:** WslCompiler with four graph transforms — safety envelope injection, loop unrolling, branch conditional labelling, and parallel fork-join expansion — all TDD-verified with 19 passing tests.

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 (RED) | Add failing WslCompiler transformation tests | 9ebed59 | WslCompilerTests.cs + 6 support types |
| 1 (GREEN) | Implement WslCompiler with all four transforms | 9dc4c19 | WslCompiler.cs |

## What Was Built

### WslCompiler.cs

A standalone `WslCompiler` class implementing `IWslCompiler` with:

- **`Compile(WslDocument, DeviceRegistry, ISafetyEngine? = null)`** — backward-compatible signature; validates then transforms
- **`InjectSafetyEnvelope`** — reads `GetViolationsAsync()` (read-only snapshot), prepends `SafetyGuard` nodes with `ruleId`/`severity`/`description` parameters; emits WSL050 warnings
- **`ExpandLoopNodes`** — unrolls `Loop` nodes with `iterations=N` parameter to N sequential copies of body nodes; caps at 1000 iterations (Threat T-06-07); emits WSL051/WSL052/WSL053 diagnostics
- **`FlattenBranchNodes`** — labels first/second outgoing `Branch` edges with `Condition="true"` / `Condition="false"`; emits WSL054 diagnostics
- **`FlattenParallelNodes`** — replaces `Parallel` nodes with `ParallelFork`/`ParallelJoin` synthetic nodes with correct predecessor/successor wiring; emits WSL055 diagnostics

### Support Types Created (worktree-specific)

The worktree branch predates the `dev` branch additions, so these types were created:
- `CompilationResult` — succeeded/failed factory methods with `Definition?` and `Diagnostics`
- `CompilationDiagnostic` — record with `Severity`, `Message`, `NodeId?`, `Code?`
- `DiagnosticSeverity` — `Info` / `Warning` / `Error` enum
- `IWslCompiler` — interface contract
- `DeviceDescriptor` — device record with `DeviceId`, `Alias`, `DeviceType`, `Capabilities`, `Metadata`
- `DeviceRegistry` — in-memory registry with `Register()` and `TryGetDevice()`

### WslCompilerTests.cs

19 tests total:
- 13 baseline validation tests (schema, graph, device binding, variable scopes)
- 5 new TDD tests: loop expansion (N=3), branch true/false edges, safety guard injection, safety read-only, parallel fork-join

## Verification

```
dotnet build → 0 errors, 1 pre-existing warning (xUnit2005 in SafetyEngineTests)
dotnet test --filter WslCompiler → Passed: 19, Failed: 0
dotnet test (all) → Passed: 75, Failed: 0
```

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Created support types missing from worktree**
- **Found during:** Task 1 (RED phase)
- **Issue:** The worktree is on a branch that predates the `dev` branch additions — `WslCompiler`, `IWslCompiler`, `CompilationResult`, `CompilationDiagnostic`, `DiagnosticSeverity`, `DeviceRegistry`, `DeviceDescriptor` all absent
- **Fix:** Created all 6 support types as new files in the worktree alongside the production `WslCompiler.cs`
- **Files created:** 6 new files in `Helix.Orchestration.Core/WSL/` and `Helix.Orchestration.Core/Devices/`
- **Commit:** 9ebed59 (RED phase commit includes support types)

**2. [Rule 2 - Security] Added loop iteration cap (Threat T-06-07)**
- **Found during:** Task 1 (GREEN phase) — threat model specifies mitigate disposition
- **Fix:** `const int MaxLoopIterations = 1000` with WSL051 diagnostic when exceeded; prevents memory exhaustion from malformed WSL with large iterations value
- **Commit:** 9dc4c19

**3. [Rule 1 - Deviation] Used manual test double instead of Moq/NSubstitute**
- **Found during:** Task 1 (RED phase) — test project has no mock framework dependency
- **Fix:** Created `file sealed class FakeSafetyEngine : ISafetyEngine` with `RegisterInterlockCalled` tracking property; avoids package install for a trivial test double
- **Commit:** 9ebed59

## Threat Surface Scan

No new network endpoints, auth paths, or file access patterns introduced. All changes are pure C# domain logic. The safety envelope injection is strictly read-only against `ISafetyEngine` — verified by test `Compile_SafetyInjection_DoesNotCallRegisterInterlock`.

## Known Stubs

None. All four transforms produce fully functional output verified by tests.

## TDD Gate Compliance

- RED gate: `test(06-03)` commit 9ebed59 — tests fail to compile (WslCompiler class absent) ✓
- GREEN gate: `feat(06-03)` commit 9dc4c19 — all 19 tests pass ✓

## Self-Check: PASSED

- `Helix.Orchestration/Helix.Orchestration.Core/WSL/WslCompiler.cs` → FOUND
- `Helix.Orchestration/tests/Helix.Orchestration.Core.Tests/WSL/WslCompilerTests.cs` → FOUND
- commit 9ebed59 → FOUND (RED phase)
- commit 9dc4c19 → FOUND (GREEN phase)
- `InjectSafetyEnvelope` in WslCompiler.cs → FOUND
- `ExpandLoopNodes` in WslCompiler.cs → FOUND
- `FlattenBranchNodes` in WslCompiler.cs → FOUND
- `FlattenParallelNodes` in WslCompiler.cs → FOUND
