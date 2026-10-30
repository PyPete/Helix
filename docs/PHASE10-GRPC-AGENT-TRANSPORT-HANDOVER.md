# Phase 10 — gRPC Agent Transport Handover

## Executive Summary

Phase 10 replaces the Site Server's hard-coded in-process agent coupling with a configuration-driven transport seam that supports either simulation mode (`SimAgentProxy`) or real remote gRPC mode (`GrpcAgentProxy`). This preserves the technology-independent control-plane boundary while enabling progressive rollout toward real Agent execution without breaking Phase 9 simulation workflows.

## Delivered Scope

- Updated `Helix.Orchestration.Contracts/Helix.Orchestration.Contracts.csproj`
  - Enabled client generation for `agent_control.proto` (`GrpcServices="Both"`)
- Updated `Helix.Orchestration.SiteServer/Helix.Orchestration.SiteServer.csproj`
  - Added `Grpc.Net.Client`
  - Added `Google.Protobuf`
- Added `Helix.Orchestration.SiteServer/Agent/AgentProxyOptions.cs`
  - `AgentProxy:Mode` and `AgentProxy:GrpcAddress`
- Added `Helix.Orchestration.SiteServer/Agent/AgentProxyServiceExtensions.cs`
  - `AddHelixAgentProxy(configuration)` with mode-based DI selection
- Added `Helix.Orchestration.SiteServer/Agent/IAgentControlClient.cs`
  - Testable abstraction over gRPC control calls
- Added `Helix.Orchestration.SiteServer/Agent/GrpcAgentControlClient.cs`
  - Concrete gRPC client adapter around generated `AgentControlClient`
- Added `Helix.Orchestration.SiteServer/Agent/GrpcAgentProxy.cs`
  - `IAgentProxy` implementation for remote control-plane calls
- Updated `Helix.Orchestration.SiteServer/Program.cs`
  - Replaced direct `SimAgentProxy` registration with `AddHelixAgentProxy(...)`
- Updated `Helix.Orchestration.SiteServer/appsettings.json`
  - Added `AgentProxy` configuration section
- Updated `tests/Helix.Orchestration.SiteServer.Tests/Helix.Orchestration.SiteServer.Tests.csproj`
  - Added `Moq`
- Added `tests/Helix.Orchestration.SiteServer.Tests/Agent/GrpcAgentProxyTests.cs`
  - Submission, status, and abort mapping tests

## API / Contract Surface

### Configuration

```json
"AgentProxy": {
  "Mode": "sim",
  "GrpcAddress": "http://localhost:5080"
}
```

- `Mode = sim` keeps Phase 9 behavior (`SimAgentProxy`)
- `Mode = grpc` enables remote Agent transport (`GrpcAgentProxy`)

### Transport Mapping

`GrpcAgentProxy` maps `IAgentProxy` methods to `AgentControl` RPCs:

- `SubmitExperimentAsync(RunspaceDefinition)` → `SubmitExperiment(ExperimentSubmission)`
  - Payload uses `runspace_definition_json`
- `GetRunspaceStatusAsync(runspaceId)` → `GetRunspaceStatus(RunspaceQuery)`
- `AbortRunspaceAsync(runspaceId)` → `AbortRunspace(RunspaceCommand)`

Error translation rules:

- RPC not-found semantics and `Status = NotFound` map to `KeyNotFoundException`
- Negative acknowledgements map to `InvalidOperationException`

## Validation Results

- `dotnet build Helix.Orchestration.SiteServer.csproj` succeeded
- `dotnet build tests/Helix.Orchestration.SiteServer.Tests.csproj` succeeded
- SiteServer test run:
  - **35 passed, 0 failed**
  - Includes new `GrpcAgentProxyTests` suite

## Architectural Notes

- Site Server retains a stable application-facing seam (`IAgentProxy`) while switching transport beneath it.
- A dedicated `IAgentControlClient` adapter was introduced to avoid tight coupling to generated gRPC client internals and to keep unit tests deterministic.
- `GetRegisteredDevices()` currently returns an empty list in gRPC mode because `agent_control.proto` does not yet expose device discovery RPCs; simulation mode still returns simulated devices.
- `RunspaceStatusDto` timing fields (`CreatedAt`, `StartedAt`, `EndedAt`) remain limited in gRPC mode until those fields are added to the proto response.

## Recommended Next Phase

Proceed to **Phase 11 — Campaign Manager**:

- Add Site Server-owned campaign orchestration (`ICampaignManager`)
- Introduce campaign iteration strategy abstraction
- Add campaign REST endpoints
- Keep campaign state in the Site Server, delegating experiment execution only to `IAgentProxy`
