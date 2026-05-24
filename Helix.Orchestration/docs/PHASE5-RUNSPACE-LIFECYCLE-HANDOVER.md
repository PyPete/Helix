# Phase 5 — Runspace Lifecycle Management & WSL Execution Wiring Handover

## Delivered

- Added lease renewal support to Core leasing contracts:
  - `IDeviceLeasingService.RenewAsync(...)`
  - `InMemoryDeviceLeasingService.RenewAsync(...)` implementation with expiry validation and updated lease snapshots.
- Added runspace lifecycle manager in Core:
  - `Runspace/IRunspaceManager`
  - `Runspace/RunspaceManager` (`ConcurrentDictionary` tracking, create/get/list/abort/dispose semantics)
  - `IRunspaceManager.Track(IRunspace)` for externally-created runspace instances.
- Extended workflow execution context:
  - `WorkflowContext.Leasing` (`IDeviceLeasingService?`) to expose optional lease coordination to workflows.
- Added Agent execution bridge from WSL to runtime runspaces:
  - `Agent/Execution/WslExecutionAdapter`
  - Compiles `WslDocument`, acquires device leases from node `deviceId` parameters, creates `DurableRunspace`, tracks it with `IRunspaceManager`, and starts execution.
- Updated Agent DI (`Agent/Program.cs`):
  - Registered `IRunspaceManager` / `RunspaceManager`
  - Registered `WslExecutionAdapter`
  - Updated `WorkflowContext` factory to include `Leasing`

## Validation

- Added Core tests:
  - `Leasing/DeviceLeasingRenewalTests` (renewal extension and expired lease rejection)
  - `Runspace/RunspaceManagerTests` (creation/tracking, abort/removal, duplicate runspace ID guard)
- Added Agent tests:
  - `Execution/WslExecutionAdapterTests` (compile+execute path and invalid document rejection)

## Remaining Gaps / Follow-On

- Lease heartbeat policy is not yet centralized as a background renew worker; renew API is now available.
- WSL `deviceId` extraction currently relies on node parameter conventions (`Parameters["deviceId"]`).
- `WslExecutionAdapter` currently acquires leases but does not yet auto-release them on successful runspace completion.
- No external API surface (gRPC/HTTP) is yet wired to invoke `WslExecutionAdapter`; current scope is internal runtime wiring.
