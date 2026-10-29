# Plan 01-09 Summary

## Implemented

- Added an additive `004_agent_ledger_constraints.sql` migration that guards
  command intents and evidence from updates/deletes, enforces unique canonical
  sequence values per execution, and removes public mutation privileges.
- Made admission idempotency race-safe by serializing same-command requests
  with a transaction-scoped PostgreSQL advisory lock and rejecting a command ID
  whose immutable intent content differs from its recorded content.
- Re-evaluate admission inside the durable dispatch-start control gate so a
  changed safety, health, lease, or policy context cannot authorize an effect
  based only on earlier proposal feedback.

## Verification

- `dotnet test tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj --no-restore --filter FullyQualifiedName~CommandLedgerTests --verbosity minimal`
  built successfully, but reported no matching `CommandLedgerTests` tests.
- `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter FullyQualifiedName~DispatchRaceTests --verbosity minimal`
  built successfully, but reported no matching `DispatchRaceTests` tests.

Focused real-PostgreSQL and dispatcher-race test classes specified by the plan
are not present in this workspace; consequently those acceptance tests remain
unverified.
