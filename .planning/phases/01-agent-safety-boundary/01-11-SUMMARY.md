# Plan 01-11 Summary: Alternative Authority Closure

## Completed

- Retired the direct WSL, generic Cleipnir, durable-runspace, and adapter-bridge mutation paths. They now fail closed rather than creating replayable effects outside `ICommandDispatcher`.
- Rebuilt active Agent composition around the simulator only. Workflow steps submit canonical simulator `CommandIntent` records through the dispatcher; no active runspace receives a capability registry or device adapter.
- Routed `/devices/{id}/execute` through authenticated proposal and dispatch, with canonical parameters and actor identity. A held/rejected proposal causes no dispatch.
- Limited device discovery to simulator vocabulary, explicitly authenticated WebSocket upgrades, and made legacy experiment-event persistence projection-only. Manual actions are notifications after dispatcher completion, not duplicate command facts.
- Added boundary coverage for legacy durable execution and authenticated device dispatch/no-effect denial paths.

## Production/Alternative Path Inventory

| Path | Disposition |
| --- | --- |
| Canonical runspace step | Creates `CommandIntent` and dispatches through the Agent fence/ledger. |
| Manual device endpoint | Authenticated proposal then dispatcher invocation; simulator only. |
| WSL execution adapter | Explicitly rejected. |
| `DurableRunspace` / generic `WorkflowExecutor` | Explicitly rejected to prevent Cleipnir replay effects. |
| Adapter capability bridge | Explicitly rejects invocation; health check only. |
| Experiment persistence worker | Legacy projection only; manual canonical facts remain in command ledger. |

## Notification Proof

`DeviceEndpoints` publishes `ManualActionEvent` only after `ICommandDispatcher.DispatchAsync` returns. The Agent dispatcher confirms the observed simulator outcome in the ledger before that return. The persistence worker skips manual-action records, so it cannot overwrite or duplicate the canonical command/effect history.

## Verification

- `dotnet build Helix.Orchestration.Agent/Helix.Orchestration.Agent.csproj --no-restore --verbosity minimal` — passed (pre-existing package/platform warnings remain).
- `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter "FullyQualifiedName~DispatchBoundaryTests|FullyQualifiedName~AgentDeviceEndpointsTests|FullyQualifiedName~AgentAuthenticationTests|FullyQualifiedName~WslExecutionAdapterTests" --verbosity minimal` — passed: 12 tests.
