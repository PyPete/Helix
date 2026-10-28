---
id: T010
title: Correct deployment connection and proxy guidance
wave: 3
deps: [T009]
status: pending
agent: null
base: null
worktree: null
task_branch: null
files:
  - docs-site/docs/operator/deployment-setup.md
---
# T010 — Correct deployment connection and proxy guidance
## Context
"ConnectionStrings__DefaultConnection and AgentProxy.Agents[] with GrpcAddress 5050" | stale | docs-site/docs/operator/deployment-setup.md:60,77-82 conflicts with docker-compose.yml:84,108-114 (`ConnectionStrings__HelixOrchestration`, `AgentProxy__Mode`, `AgentProxy__GrpcAddress`).
The accepted ruling is fix-doc, retaining current Compose configuration as ground truth.
## Approach
- Preserve earlier deployment corrections and document the checked-in connection-string and AgentProxy environment keys only; do not alter service configuration.
## Interface contract
- None
## Intent coverage
- None
## Acceptance criteria
1. Deployment guidance uses `ConnectionStrings__HelixOrchestration`, `AgentProxy__Mode`, and `AgentProxy__GrpcAddress` rather than obsolete proxy configuration.
## Verify
```bash
powershell -NoProfile -Command "$p='docs-site/docs/operator/deployment-setup.md'; $c=Get-Content -Raw $p; if($c -match 'DefaultConnection|AgentProxy\.Agents' -or $c -notmatch 'ConnectionStrings__HelixOrchestration' -or $c -notmatch 'AgentProxy__GrpcAddress'){throw 'connection claim not aligned'}" # docs-site/docs/operator/deployment-setup.md
```
Heavy: no
## Log
- 2026-10-28 — created by planner
