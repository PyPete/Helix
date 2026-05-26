# Phase 3 Handover — WSL Compiler, Capability Routing, and Execution Path Closure

## Summary

Phase 3 closes the core execution path from compiled WSL workflows through agent capability routing to adapter invocation, and hardens lease lifecycle cleanup with background expiry management.

## Implemented in this phase

### WSL compiler and device resolution
- Added `Helix.Orchestration.Core/Devices/IDeviceRegistry.cs`.
- Added `Helix.Orchestration.Core/Devices/InMemoryDeviceRegistry.cs`.
- Added `Helix.Orchestration.Core/WSL/IWslCompiler.cs`.
- Added `Helix.Orchestration.Core/WSL/WslCompiler.cs`.
- Updated `Helix.Orchestration.Core/WSL/WslNode.cs` with optional `Device` property for logical device binding.

### Agent capability routing
- Added `Helix.Orchestration.Agent/Adapters/AdapterCapabilityInvoker.cs` to bridge runtime capability calls into `AdapterRegistry` / `AdapterProxy`.
- Added `Helix.Orchestration.Agent/Adapters/DeviceRegistryLoader.cs` to hydrate device mappings from `Configuration/device-registry.json`.
- Updated `Helix.Orchestration.Agent/Program.cs` DI wiring for compiler, registry, and capability invoker runtime path.

### gRPC hosting and control path
- Replaced `Helix.Orchestration.Agent/gRPC/Server/GrpcServerHost.cs` scaffold with concrete gRPC host setup and endpoint mapping for `AgentControlService`.

### Lease expiry cleanup
- Extended `Helix.Orchestration.Core/Leasing/InMemoryDeviceLeasingService.cs` with `PurgeExpiredAsync`.
- Added `Helix.Orchestration.Agent/Leasing/LeaseExpiryWorker.cs` as a `BackgroundService` for periodic TTL cleanup.
- Registered `LeaseExpiryWorker` in `Program.cs`.

### Tests
- Added/updated compiler and registry tests:
  - `tests/Helix.Orchestration.Core.Tests/WSL/WslCompilerTests.cs`
  - `tests/Helix.Orchestration.Core.Tests/Devices/InMemoryDeviceRegistryTests.cs`
- Added capability routing tests:
  - `tests/Helix.Orchestration.Agent.Tests/Adapters/AdapterCapabilityInvokerTests.cs`
- Added lease expiry tests:
  - `tests/Helix.Orchestration.Agent.Tests/Leasing/LeaseExpiryWorkerTests.cs`

## Outcome

The execution path now supports:
1. Device-aware compilation from WSL node metadata.
2. Adapter-targeted capability invocation in the Agent runtime.
3. Hosted gRPC control endpoints for runspace commands.
4. Deterministic cleanup of expired leases during runtime.

## Remaining gaps / next actions

1. **Durability hardening**
   - Replace in-memory runspace execution internals with full Cleipnir-backed persistence and resume semantics.
2. **Trend model implementations**
   - Implement concrete `ISpectralTrend`, `IDiodeArrayTrend`, `IImageTrend`, and `IVideoTrend` providers.
3. **Campaign orchestration**
   - Implement Site-owned campaign manager with persistent campaign state.
4. **Site Server scaffold**
   - Add Site host project for workflow compile API, campaign API, registry, and telemetry aggregation.
5. **Integration harness**
   - Add end-to-end integration tests with live gRPC adapter process and MQTT broker.
