# Phase 2 Handover — Agent Runtime with Language-Agnostic Adapter Discovery

## Summary

Phase 2 foundation has been scaffolded for a production-oriented Agent runtime centered on gRPC adapter integration, runspace orchestration, and MQTT trend routing.

## Implemented in this phase

### Contracts
- Added `Helix.Orchestration.Contracts` project.
- Added `Protos/adapter.proto` (DeviceAdapter contract).
- Added `Protos/agent_control.proto` (AgentControl contract).
- Enabled C# gRPC code generation in the contracts project.

### Agent runtime
- Added `Helix.Orchestration.Agent` project.
- Added host bootstrap in `Program.cs` with DI and configuration loading.
- Added `AgentService` startup orchestration.

### Configuration and registry
- Added `Configuration/AgentConfiguration.cs`.
- Added `Configuration/DeviceRegistry.cs`.
- Added `Configuration/agent-config.json` and `Configuration/device-registry.json`.

### Adapter layer
- Added config-driven adapter discovery (`AdapterDiscoveryService`).
- Added in-memory adapter map (`AdapterRegistry`).
- Added gRPC client wrapper (`AdapterProxy`) with invoke/stream/health operations.
- Added periodic health monitor (`AdapterHealthMonitor`).

### Runspace layer
- Added `DurableRunspace` implementing `IRunspace` lifecycle.
- Added `DefinitionCompiler` for node-driven execution delegates.
- Added `RunspaceManager` for submit/pause/resume/abort/status management.

### Streaming layer
- Added `MqttTrendPublisher` for topic publishing.
- Added `TrendAggregator` to bridge adapter streams to MQTT publisher.
- Added `TrendBuffer` queue for transient buffering.

### Control plane and health
- Added `gRPC/AgentControlService.cs` skeleton.
- Added `gRPC/Server/GrpcServerHost.cs` host placeholder.
- Added health checks (`AgentHealthCheck`, `AdapterHealthCheck`).

### Python adapter assets
- Added simulated pump adapter: `adapters/python/pump_simulator/pump_adapter.py`.
- Added Linux service unit: `pump-adapter.service`.
- Added Windows NSSM script: `install-windows-service.ps1`.

## Solution updates
- Added projects to `Helix.Orchestration.slnx`:
  - `Helix.Orchestration.Contracts`
  - `Helix.Orchestration.Agent`
  - `Helix.Orchestration.Agent.Tests`

## Notes and known gaps
- Current gRPC server hosting is scaffold-level; endpoint wiring can be expanded in the next iteration.
- Durable persistence internals (Cleipnir store selection/policy details) are staged for follow-up hardening.
- Integration tests with live Python adapter and MQTT broker are not yet fully implemented in this commit set.

## Recommended next actions
1. Complete gRPC endpoint hosting and network configuration in Agent host.
2. Add robust retry/reconnect policies in `AdapterProxy` and monitor loops.
3. Implement full integration test harness with environment guards.
4. Add structured telemetry and production logging sinks.
