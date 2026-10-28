---
id: T015
title: Correct the Agent runtime discovery handover
wave: 3
deps: []
status: pending
agent: null
base: null
worktree: null
task_branch: null
files:
  - docs/PHASE2-AGENT-RUNTIME-HANDOVER.md
---
# T015 — Correct the Agent runtime discovery handover
## Context
"AdapterDiscoveryService provides config-driven adapter discovery" | stale | docs/PHASE2-AGENT-RUNTIME-HANDOVER.md:26 names the removed type; no AdapterDiscoveryService exists, while Helix.Orchestration.Agent/Adapters/IAdapterDiscovery.cs is the surviving abstraction.
The accepted ruling is fix-doc and preserves historic context without claiming a removed type is current.
## Approach
- Replace or label the obsolete service claim using the surviving `IAdapterDiscovery` abstraction and current runtime wording; do not restore a removed implementation.
## Interface contract
- None
## Intent coverage
- None
## Acceptance criteria
1. The handover does not claim `AdapterDiscoveryService` is a current implementation and accurately identifies `IAdapterDiscovery` where appropriate.
## Verify
```bash
powershell -NoProfile -Command "$p='docs/PHASE2-AGENT-RUNTIME-HANDOVER.md'; $c=Get-Content -Raw $p; if($c -match 'AdapterDiscoveryService' -or $c -notmatch 'IAdapterDiscovery'){throw 'adapter discovery handover not aligned'}" # docs/PHASE2-AGENT-RUNTIME-HANDOVER.md
```
Heavy: no
## Log
- 2026-10-28 — created by planner
