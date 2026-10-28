---
id: T012
title: Correct manual device-capability API guidance
wave: 3
deps: []
status: pending
agent: null
base: null
worktree: null
task_branch: null
files:
  - docs-site/docs/operator/device-capabilities.md
---
# T012 — Correct manual device-capability API guidance
## Context
"Manual capability execution is POST http://localhost:5000/devices/{id}/execute with capabilityName" | stale | docs-site/docs/operator/device-capabilities.md:59-63; Agent/Api/DeviceEndpoints.cs maps execution at the Agent endpoint and its request DTO uses `Capability`; Agent listens on 5080 (appsettings.json:12).
The accepted ruling is fix-doc; the Agent route and request DTO are authoritative.
## Approach
- Align the example with `POST http://localhost:5080/devices/{id}/execute` and the `Capability` request field; do not expand manual-control authority.
## Interface contract
- None
## Intent coverage
- None
## Acceptance criteria
1. Operator API guidance uses Agent port 5080 and the shipped `Capability` request field, not `capabilityName` on port 5000.
## Verify
```bash
powershell -NoProfile -Command "$p='docs-site/docs/operator/device-capabilities.md'; $c=Get-Content -Raw $p; if($c -match 'localhost:5000|capabilityName' -or $c -notmatch 'localhost:5080' -or $c -notmatch 'Capability'){throw 'capability API claim not aligned'}" # docs-site/docs/operator/device-capabilities.md
```
Heavy: no
## Log
- 2026-10-28 — created by planner
