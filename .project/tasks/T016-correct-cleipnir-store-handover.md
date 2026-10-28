---
id: T016
title: Correct the durable execution store handover
wave: 3
deps: []
status: pending
agent: null
base: null
worktree: null
task_branch: null
files:
  - docs/PHASE4-DURABLE-EXECUTION-TREND-FOUNDATION-HANDOVER.md
---
# T016 — Correct the durable execution store handover
## Context
"Cleipnir durability store is InMemoryFunctionStore" | stale | docs/PHASE4-DURABLE-EXECUTION-TREND-FOUNDATION-HANDOVER.md:7,31; Agent/Program.cs configures PostgreSqlFunctionStore.
The accepted ruling is fix-doc: the handover must distinguish its historic snapshot from the current PostgreSQL runtime.
## Approach
- Label the in-memory statement historical or replace it with the currently configured `PostgreSqlFunctionStore`; do not alter Cleipnir runtime configuration.
## Interface contract
- None
## Intent coverage
- None
## Acceptance criteria
1. The handover does not represent `InMemoryFunctionStore` as current and identifies the Agent’s PostgreSQL function store.
## Verify
```bash
powershell -NoProfile -Command "$p='docs/PHASE4-DURABLE-EXECUTION-TREND-FOUNDATION-HANDOVER.md'; $c=Get-Content -Raw $p; if($c -match 'Cleipnir durability store is InMemoryFunctionStore' -or $c -notmatch 'PostgreSqlFunctionStore'){throw 'Cleipnir store handover not aligned'}" # docs/PHASE4-DURABLE-EXECUTION-TREND-FOUNDATION-HANDOVER.md
```
Heavy: no
## Log
- 2026-10-28 — created by planner
