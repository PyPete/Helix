---
id: T008
title: Correct deployment service-port guidance
wave: 3
deps: []
status: pending
agent: null
base: null
worktree: null
task_branch: null
files:
  - docs-site/docs/operator/deployment-setup.md
---
# T008 — Correct deployment service-port guidance
## Context
"Agent HTTP 5000 and gRPC 5050; SiteServer 7020" | stale | docs-site/docs/operator/deployment-setup.md:53-54,72 conflicts with docker-compose.yml:78,105 and Agent appsettings.json:12 (5080/5090).
The accepted ruling is fix-doc against the checked-in Compose and Agent configuration.
## Approach
- Document Agent 5080 and SiteServer 5090 as the supported Compose ports; do not modify runtime networking.
## Interface contract
- None
## Intent coverage
- None
## Acceptance criteria
1. Deployment setup directs local operators to Agent 5080 and SiteServer 5090, not obsolete 5000/5050/7020 values.
## Verify
```bash
powershell -NoProfile -Command "$p='docs-site/docs/operator/deployment-setup.md'; $c=Get-Content -Raw $p; if($c -match '5000|5050|7020' -or $c -notmatch '5080' -or $c -notmatch '5090'){throw 'service-port claim not aligned'}" # docs-site/docs/operator/deployment-setup.md
```
Heavy: no
## Log
- 2026-10-28 — created by planner
