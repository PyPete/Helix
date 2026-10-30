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
- Copied and applied every numbered PostgreSQL migration in the Infrastructure
  test fixture. Fixture startup now throws rather than silently converting an
  unavailable database or missing migration into a passing test run.
- Added real PostgreSQL ledger coverage for concurrent duplicate command
  identities (one persisted evidence row/outcome) and conflicting immutable
  command content (throws while preserving the original fact).
- Added a barrier-controlled Agent dispatch-race seam test that proves
  competing dispatch starts record one durable in-flight transition and
  competing terminal transitions leave one legal terminal result.

## Verification

- `dotnet test tests/Helix.Orchestration.Infrastructure.Tests/Helix.Orchestration.Infrastructure.Tests.csproj --no-restore --filter FullyQualifiedName~CommandLedgerTests --verbosity minimal`
  passed: 2 tests, including a disposable real PostgreSQL container.
- `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter FullyQualifiedName~DispatchRaceTests --verbosity minimal`
  passed: 1 test.

The Agent test build emitted pre-existing package/platform warnings (OPC UA,
System.Reactive, and Windows-only adapters); neither focused test suite failed.
