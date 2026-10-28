---
id: T009
title: Correct deployment MinIO guidance
wave: 3
deps: [T008]
status: pending
agent: null
base: null
worktree: null
task_branch: null
files:
  - docs-site/docs/operator/deployment-setup.md
---
# T009 — Correct deployment MinIO guidance
## Context
"MinIO API 9000, console 9001, credentials helix/helix_dev_secret" | stale | docs-site/docs/operator/deployment-setup.md:31-37 conflicts with docker-compose.yml:58-62, which maps host ports 19000/19002 and configures MinIO credentials separately.
The accepted ruling is fix-doc; the Compose host mappings and credentials are the supported local setup.
## Approach
- Preserve T008's port repair and replace obsolete MinIO host endpoints and credentials with the Compose values; do not change Compose or add a credential feature.
## Interface contract
- None
## Intent coverage
- None
## Acceptance criteria
1. Deployment guidance identifies MinIO host API/console ports 19000/19002 and the checked-in credential configuration, not the obsolete values.
## Verify
```bash
powershell -NoProfile -Command "$p='docs-site/docs/operator/deployment-setup.md'; $c=Get-Content -Raw $p; if($c -match 'helix_dev_secret|console 9001' -or $c -notmatch '19000' -or $c -notmatch '19002'){throw 'MinIO claim not aligned'}" # docs-site/docs/operator/deployment-setup.md
```
Heavy: no
## Log
- 2026-10-28 — created by planner
