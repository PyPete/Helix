---
id: T011
title: Correct deployment dashboard endpoint guidance
wave: 3
deps: [T010]
status: pending
agent: null
base: null
worktree: null
task_branch: null
files:
  - docs-site/docs/operator/deployment-setup.md
---
# T011 — Correct deployment dashboard endpoint guidance
## Context
"Dashboard points at VITE_SITE_SERVER_URL=http://localhost:7020" | stale | docs-site/docs/operator/deployment-setup.md:110; checked-in SiteServer Compose port is 5090 at docker-compose.yml:105.
The accepted ruling is fix-doc and does not make the parked dashboard an M001 delivery surface.
## Approach
- Preserve earlier deployment corrections and align the optional dashboard endpoint example to SiteServer 5090 without implementing dashboard work.
## Interface contract
- None
## Intent coverage
- None
## Acceptance criteria
1. Deployment setup no longer directs operators to a SiteServer dashboard endpoint on 7020 and uses 5090 where the optional example remains.
## Verify
```bash
powershell -NoProfile -Command "$p='docs-site/docs/operator/deployment-setup.md'; $c=Get-Content -Raw $p; if($c -match 'VITE_SITE_SERVER_URL=http://localhost:7020' -or $c -notmatch '5090'){throw 'dashboard endpoint claim not aligned'}" # docs-site/docs/operator/deployment-setup.md
```
Heavy: no
## Log
- 2026-10-28 — created by planner
