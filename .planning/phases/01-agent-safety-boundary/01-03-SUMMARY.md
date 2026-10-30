# Plan 01-03 Summary: Durable Execution Controls and Reconciliation

## Delivered

- Added revision-checked, PostgreSQL-backed execution controls with append-only attributed control events. Pause, hold, review release, and terminal abort now share the dispatch-start fence.
- Added a durable dispatch lifecycle: record `InFlight` before a mutable simulator effect, confirm only from observed identity/value/unit/generation evidence, and never replay an in-flight command.
- Replaced the reconciliation placeholder with a startup worker that installs a visible recovery hold before observation, clears only that reason after a strict match, and retains mismatches/unreadable observations for review.
- Expanded simulator state into independently persisted parameter keys for flow, temperature, and mode. Get capabilities remain read-only; legacy `SetPumpMode` is not registered.
- Added migrations for control history and dispatch state, plus parameter-keyed simulator observations.

## Verification

- `dotnet build tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --verbosity minimal` — passed.
- `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter "FullyQualifiedName~ExecutionControlTests|FullyQualifiedName~ReconciliationTests" --verbosity minimal` — passed (4 tests).

The focused tests prove revision conflicts, restart-visible controls, terminal abort dispatch blocking, match-only recovery, and preservation of independent holds. Existing restore warnings remain for the OPC UA dependency and System.Reactive version conflict.
