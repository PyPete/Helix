---
phase: 02-scientific-setup-foundation
plan: "24"
subsystem: scientific-setup-agent
tags: [csharp, frozen-execution, authority, runspace]
requires: [02-42]
provides: [canonical-frozen-binding, durable-runspace-consumption, scoped-source-verification]
affects: [APP-04, phase-03]
tech-stack:
  added: []
  patterns: [exact-revision-resolution, durable-runspace-deserialization, fresh-admission]
key-files:
  created: []
  modified:
    - Helix.Orchestration.Agent/Experiments/IExperimentManager.cs
    - Helix.Orchestration.Agent/ScientificSetup/FrozenSetupResolver.cs
    - Helix.Orchestration.Agent/Api/ExperimentEndpoints.cs
    - tests/Helix.Orchestration.Agent.Tests/Integration/FrozenSetupTests.cs
decisions:
  - Scientific submissions with a frozen manifest reconstruct its persisted RunspaceDefinition only after exact actor-scoped procedure and plan revision verification.
  - Stored compilation is compared through its definition hash after fresh compilation; compiledAt is never an equality key.
  - Production approval remains fail-closed and Phase 3 remains responsible for production approval issuance and campaign execution.
metrics:
  duration: 18min
  completed: 2026-11-01
  tasks: 2
status: complete
plan_head_before: 091fcad43dea2318835db8d34033bde477bc9817
plan_head_after: 731bb2f2e9a2662340b5289b784c948d320735a0
commits: 3
actuals:
  tokens: 15000
  tasks: 2
  commits: 3
requirements-completed: [APP-04]
---

# Phase 02 Plan 24: Canonical Frozen Execution Binding Summary

Canonical experiment admission can now consume a durable frozen RunspaceDefinition only after current authority and exact immutable scientific-source checks.

## Accomplishments

- Added an optional typed `FrozenExecutionBinding` to the Agent-owned canonical experiment definition without breaking legacy callers.
- Added a frozen submission branch that resolves the exact manifest, actor-scoped procedure and plan revisions, confirms their hashes and pin relationship, re-instantiates the expected workflow, and rejects any different submitted workflow.
- Deserializes the persisted compiled artifact as a `RunspaceDefinition`, then preserves fresh compiler/device admission and compares definition hashes without treating `compiledAt` as a stable equality key.
- Retained the legacy submission path and production's unavailable policy authority; no approval issuance, campaign runner, or recovery workflow was added.

## Task Commits

1. **Task 1: Bind a scientific manifest through the actual canonical submission seam** — `deb90fa`
2. **Task 2: Revalidate approval and immutable content on consumption** — `d6b3d23`

## Verification

- `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter FullyQualifiedName~FrozenSetupTests --verbosity minimal` — passed (4 tests; zero skipped).

## Decisions Made

- Frozen submissions fail closed when a scoped revision, source hash, plan pin, submitted workflow, serialized artifact, policy identity, or fresh admission check differs.
- Phase 3 must wire production approval authority and execute campaign start, resume, and recovery against this binding.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 4 - Approved architectural change] Added durable runspace deserialization and scoped source-revision verification**
- **Found during:** Task 1
- **Issue:** Existing frozen artifacts were opaque bytes and the canonical endpoint had no source-revision resolver.
- **Fix:** Scientist approved an explicit durable `RunspaceDefinition` serialization/deserialization contract and exact actor-scoped source verification at the existing canonical seam.
- **Files modified:** `FrozenSetupResolver.cs`, `ExperimentEndpoints.cs`, `IExperimentManager.cs`
- **Commit:** `deb90fa`

## Known Stubs


None.

## Self-Check: PASSED

- Canonical binding files and `FrozenSetupTests.cs` exist.
- Task commits `deb90fa` and `d6b3d23` are present in HEAD ancestry.
