---
phase: 02-scientific-setup-foundation
plan: "40"
subsystem: scientific-setup-agent
tags: [csharp, agent, policy, frozen-evidence]
requires: [02-39]
provides: [fail-closed-agent-authority, frozen-evidence-resolver, authenticated-frozen-reads]
affects: [APP-04, 02-41, 02-42]
tech-stack:
  added: []
  patterns: [scoped-services, fail-closed-authority, immutable-artifact-reconstruction]
key-files:
  created:
    - Helix.Orchestration.Agent/ScientificSetup/FrozenSetupService.cs
    - Helix.Orchestration.Agent/ScientificSetup/FrozenSetupResolver.cs
    - Helix.Orchestration.Agent/ScientificSetup/UnavailablePolicyApprovalAuthority.cs
    - Helix.Orchestration.Agent/Api/FrozenSetupEndpoints.cs
    - Helix.Orchestration.Agent/ScientificSetup/ScientificOperationResultService.cs
    - tests/Helix.Orchestration.Agent.Tests/Integration/FrozenSetupTests.cs
  modified:
    - Helix.Orchestration.Agent/AgentApplicationComposition.cs
decisions:
  - Agent production composition explicitly overrides policy approval with a fail-closed authority.
  - Frozen resolution validates stored evidence and fresh policy authority without recompiling or dispatching.
metrics:
  duration: 12min
  completed: 2026-11-01
  tasks: 2
status: complete
plan_head_before: d47830ee4a94698bb376d7eb2fe8fbb7ae370b5e
plan_head_after: 59a217f909b3fa1a946253a17559b848069c90a7
commits: 3
actuals:
  tokens: 1920
  tasks: 2
  commits: 3
requirements-completed: [APP-04]
---

# Phase 02 Plan 40: Frozen Agent Evidence Summary

The Agent now exposes scoped, authenticated reads for immutable policy and assembly evidence while production approval remains explicitly unavailable and frozen resolution never recompiles or dispatches.

## Accomplishments

- Added Agent-owned fail-closed `IPolicyApprovalAuthority` implementation and explicit production DI override.
- Added scoped freeze and resolver facades that preserve the stored compiled artifact, validate immutable manifest evidence, and revalidate the exact policy authority before use.
- Added read-only authenticated frozen assembly and policy routes; no approval issuance, content registration, or physical execution route was introduced.
- Added actor-scoped operation receipt facade and focused fail-closed authority coverage.

## Task Commits

1. **Task 1: Freeze exact scientific content with independent authority checks** — `fef1b0e`, `24935ad`
2. **Task 2: Admit scoped assembly/content/outcome routes without issuing approval** — `59a217f`

## Verification

- RED: `FrozenSetupTests` first failed to compile because the planned Agent-owned unavailable authority did not exist.
- `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter FullyQualifiedName~FrozenSetupTests --verbosity minimal` — passed (1 test; no skips).

## Decisions Made

- Production Agent registration intentionally overrides infrastructure's policy authority registration with the Agent fail-closed authority.
- Resolution is limited to immutable stored evidence and performs no compilation or dispatch.

## Deviations from Plan

None - plan executed exactly as written.

## Known Stubs

None.

## Self-Check: PASSED

- All seven planned Agent/test files exist.
- Task commits `fef1b0e`, `24935ad`, and `59a217f` are ancestors of the recorded plan head.
