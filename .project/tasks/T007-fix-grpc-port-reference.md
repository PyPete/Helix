---
id: T007
title: Correct the Agent gRPC port reference
wave: 3
deps: []
status: pending
agent: null
base: null
worktree: null
task_branch: null
files:
  - docs-site/docs/developer/grpc-reference.md
---
# T007 — Correct the Agent gRPC port reference
## Context
"Agent gRPC default port is 5050 and adapter port 5051" | stale | docs-site/docs/developer/grpc-reference.md:142,157 conflicts with Helix.Orchestration.Agent/appsettings.json:12 and docker-compose.yml:78, which use 5080.
The accepted ruling is fix-doc; checked-in local runtime configuration is authoritative.
## Approach
- Align the Agent endpoint documentation to port 5080 and remove unsupported 5050/5051 default claims without changing transport code.
## Interface contract
- None
## Intent coverage
- None
## Acceptance criteria
1. The gRPC reference names the configured Agent endpoint on 5080 and does not present 5050/5051 as defaults.
## Verify
```bash
powershell -NoProfile -Command "$p='docs-site/docs/developer/grpc-reference.md'; $c=Get-Content -Raw $p; if($c -match '5050|5051' -or $c -notmatch '5080'){throw 'gRPC port claim not aligned'}" # docs-site/docs/developer/grpc-reference.md
```
Heavy: no
## Log
- 2026-10-28 — created by planner
