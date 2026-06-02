---
id: grpc-reference
title: gRPC Reference
sidebar_position: 6
---

## Overview

Helix exposes two gRPC services defined in `Helix.Orchestration.Contracts/Protos/`. Both services are used for internal Agent ↔ SiteServer communication (via `IAgentProxy`) and for direct client connections from external tools or test harnesses.

| Proto file | Package | C# namespace |
|-----------|---------|--------------|
| `agent_control.proto` | `helix.control` | `Helix.Orchestration.Contracts.Control` |
| `adapter.proto` | `helix.adapter` | `Helix.Orchestration.Contracts.Adapters` |

## AgentControl service (`agent_control.proto`)

The `AgentControl` service is the primary control plane interface between the SiteServer and the Agent.

**Package:** `helix.control`  
**C# namespace:** `Helix.Orchestration.Contracts.Control`  
**Generated client class:** `AgentControl.AgentControlClient`

### RPCs

| RPC | Request | Response | Description |
|-----|---------|----------|-------------|
| `SubmitExperiment` | `ExperimentSubmission` | `SubmissionAck` | Submit a serialised `RunspaceDefinition` JSON for execution |
| `PauseRunspace` | `RunspaceCommand` | `CommandAck` | Pause a running runspace by ID |
| `ResumeRunspace` | `RunspaceCommand` | `CommandAck` | Resume a paused runspace |
| `AbortRunspace` | `RunspaceCommand` | `CommandAck` | Abort a running or paused runspace |
| `GetRunspaceStatus` | `RunspaceQuery` | `RunspaceStatusResponse` | Get the current status of a runspace |

### Messages

**`ExperimentSubmission`**

| Field | Type | Field # | Description |
|-------|------|---------|-------------|
| `runspace_definition_json` | `string` | 1 | Serialised `RunspaceDefinition` JSON |

**`SubmissionAck`**

| Field | Type | Field # | Description |
|-------|------|---------|-------------|
| `accepted` | `bool` | 1 | Whether the submission was accepted |
| `runspace_id` | `string` | 2 | Assigned runspace identifier |
| `message` | `string` | 3 | Human-readable status or error message |

**`RunspaceCommand`**

| Field | Type | Field # | Description |
|-------|------|---------|-------------|
| `runspace_id` | `string` | 1 | Target runspace identifier |

**`CommandAck`**

| Field | Type | Field # | Description |
|-------|------|---------|-------------|
| `success` | `bool` | 1 | Whether the command succeeded |
| `message` | `string` | 2 | Human-readable status or error detail |

**`RunspaceQuery`**

| Field | Type | Field # | Description |
|-------|------|---------|-------------|
| `runspace_id` | `string` | 1 | Runspace to query |

**`RunspaceStatusResponse`**

| Field | Type | Field # | Description |
|-------|------|---------|-------------|
| `runspace_id` | `string` | 1 | Runspace identifier |
| `status` | `string` | 2 | Current status (e.g. `Running`, `Paused`, `Completed`, `Aborted`) |
| `message` | `string` | 3 | Additional status detail |

## DeviceAdapter service (`adapter.proto`)

**Package:** `helix.adapter`  
**C# namespace:** `Helix.Orchestration.Contracts.Adapters`

### RPCs

| RPC | Request | Response | Streaming | Description |
|-----|---------|----------|-----------|-------------|
| `GetCapabilities` | `Empty` | `CapabilitiesResponse` | Unary | List all capability names registered on this adapter |
| `InvokeCapability` | `CapabilityRequest` | `CapabilityResponse` | Unary | Invoke a named capability with JSON parameters |
| `StreamTrends` | `Empty` | `TrendValue` | Server streaming | Stream live trend samples from the adapter continuously |
| `HealthCheck` | `Empty` | `HealthStatus` | Unary | Check adapter connectivity and health |

### Messages

**`Empty`** — no fields. Used as a placeholder request or response where no parameters are required.

**`CapabilitiesResponse`**

| Field | Type | Field # | Description |
|-------|------|---------|-------------|
| `capabilities` | `repeated string` | 1 | List of registered capability names |

**`CapabilityRequest`**

| Field | Type | Field # | Description |
|-------|------|---------|-------------|
| `capability_name` | `string` | 1 | Name of the capability to invoke |
| `parameters_json` | `string` | 2 | JSON-encoded parameter object |
| `lease_id` | `string` | 3 | Optional lease identifier for exclusive device access |

**`CapabilityResponse`**

| Field | Type | Field # | Description |
|-------|------|---------|-------------|
| `success` | `bool` | 1 | Whether invocation succeeded |
| `result_json` | `string` | 2 | JSON-encoded result value (if any) |
| `error_message` | `string` | 3 | Error detail on failure |

**`TrendValue`**

| Field | Type | Field # | Description |
|-------|------|---------|-------------|
| `trend_name` | `string` | 1 | Trend identifier (e.g. `vapourtec.temperature.A`) |
| `value_json` | `string` | 2 | JSON-encoded measurement value |
| `type_tag` | `string` | 3 | Type hint (e.g. `scalar`, `spectrum`) |
| `timestamp_utc_ticks` | `int64` | 4 | UTC timestamp in .NET Ticks |

**`HealthStatus`**

| Field | Type | Field # | Description |
|-------|------|---------|-------------|
| `healthy` | `bool` | 1 | `true` if the adapter transport is connected |
| `message` | `string` | 2 | Status description |

## Connecting a gRPC client (.NET)

Add the generated gRPC client stubs from `Helix.Orchestration.Contracts` to your project, then create a channel:

```csharp
using Grpc.Net.Client;
using Helix.Orchestration.Contracts.Control;

// Default Agent gRPC port — configurable via Kestrel config or ASPNETCORE_URLS
var channel = GrpcChannel.ForAddress("http://localhost:5050");
var client = new AgentControl.AgentControlClient(channel);

var result = await client.GetRunspaceStatusAsync(new RunspaceQuery
{
    RunspaceId = "my-runspace-id"
});
Console.WriteLine($"Status: {result.Status}");
```

For the DeviceAdapter service, connect directly to the adapter's gRPC endpoint (typically port 5051 on the adapter host):

```csharp
using Helix.Orchestration.Contracts.Adapters;

var adapterChannel = GrpcChannel.ForAddress("http://adapter-host:5051");
var adapterClient = new DeviceAdapter.DeviceAdapterClient(adapterChannel);

var caps = await adapterClient.GetCapabilitiesAsync(new Empty());
Console.WriteLine($"Capabilities: {string.Join(", ", caps.Capabilities)}");
```
