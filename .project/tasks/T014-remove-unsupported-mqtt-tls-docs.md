---
id: T014
title: Remove unsupported MQTT TLS configuration advice
wave: 3
deps: []
status: pending
agent: null
base: null
worktree: null
task_branch: null
files:
  - docs-site/docs/operator/production-hardening.md
---
# T014 — Remove unsupported MQTT TLS configuration advice
## Context
"Mqtt:UseTls and Mqtt:CaFile are supported settings" | stale | docs-site/docs/operator/production-hardening.md:62-63; no such settings are defined by the shipped MQTT options/configuration.
The accepted ruling is fix-doc; unsupported configuration must not remain advice.
## Approach
- Remove or clearly label as unsupported the nonexistent MQTT TLS settings; do not add MQTT TLS implementation in this milestone.
## Interface contract
- None
## Intent coverage
- None
## Acceptance criteria
1. Production hardening does not advertise `Mqtt:UseTls` or `Mqtt:CaFile` as shipped settings.
## Verify
```bash
powershell -NoProfile -Command "$p='docs-site/docs/operator/production-hardening.md'; if((Get-Content -Raw $p) -match 'Mqtt:UseTls|Mqtt:CaFile'){throw 'unsupported MQTT setting remains'}" # docs-site/docs/operator/production-hardening.md
```
Heavy: no
## Log
- 2026-10-28 — created by planner
