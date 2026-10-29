---
phase: 01-agent-safety-boundary
plan: "08"
status: blocked
---

# Plan 01-08 summary

## Delivered source changes

- Locked the Agent default listener to IPv4 loopback and added startup rejection
  of wildcard/non-loopback `ASPNETCORE_URLS`, `urls`, and Kestrel overrides.
- Replaced the deployment compose profile with loopback-published PostgreSQL and
  MQTT only; container Agent/Site Server are not supported by this profile.
- Made the old Linux unit and Windows service installer explicit fail-closed
  unsupported paths, with same-user interactive launch guidance.
- Added a blocking migration/grant script, native unauthenticated-health smoke
  binary, smoke checks for listener/anonymous routes/wildcard refusal, and the
  operational runbook.

## Verification

`dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter FullyQualifiedName~E2eWorkflowExecutionTests --verbosity minimal`
compiled the Agent and test project but failed: 0 passed / 2 failed. Both existing
tests call the deliberately disabled legacy `DurableRunspace.StartAsync` path and
throw `NotSupportedException: Legacy DurableRunspace execution is disabled. Use
canonical dispatch with a persisted cursor.`

The real migration and full smoke gates were not run: this environment has not
provided the required separate migration/runtime PostgreSQL identities, nor a
documented deployed scientist-account inspection. Per plan, their absence is a
blocking condition rather than a skipped pass. No full build, desktop build, or
native suite is claimed.

## Blocking follow-up

Replace the stale legacy E2E fixtures with the already-selected canonical
persisted-cursor composition, then run against a disposable database with
`HELIX_MIGRATION_USER` and `HELIX_RUNTIME_USER` set to distinct identities.
