---
id: T006
title: Correct the infrastructure persistence architecture claim
wave: 3
deps: []
status: pending
agent: null
base: null
worktree: null
task_branch: null
files:
  - docs-site/docs/developer/architecture.md
---
# T006 — Correct the infrastructure persistence architecture claim
## Context
"Infrastructure owns EF Core migrations" | stale | docs-site/docs/developer/architecture.md:90 says EF Core; Helix.Orchestration.Infrastructure/Helix.Orchestration.Infrastructure.csproj:12-13 references Dapper/Npgsql, and SQL migrations are under postgres/migrations.
The accepted ruling is fix-doc, so documentation follows the checked-in Dapper/Npgsql and SQL migration contract.
## Approach
- Describe Dapper/Npgsql persistence and `postgres/migrations`; do not introduce EF Core or change runtime persistence.
## Interface contract
- None
## Intent coverage
- None
## Acceptance criteria
1. Architecture documentation identifies Dapper/Npgsql and SQL migrations rather than EF Core migrations.
## Verify
```bash
powershell -NoProfile -Command "$p='docs-site/docs/developer/architecture.md'; $c=Get-Content -Raw $p; if($c -match 'EF Core migrations' -or $c -notmatch 'Dapper' -or $c -notmatch 'postgres/migrations'){throw 'persistence claim not aligned'}" # docs-site/docs/developer/architecture.md
```
Heavy: no
## Log
- 2026-10-28 — created by planner
