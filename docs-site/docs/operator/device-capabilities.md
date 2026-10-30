---
id: device-capabilities
title: "Device Capability Reference"
sidebar_position: 3
---

# Device Capability Reference

## CapabilityDescriptor Schema

`CapabilityDescriptor` (namespace `Helix.Orchestration.Core.Capabilities`) is the record type that describes a single capability verb a device adapter exposes. The workflow builder reads these descriptors at runtime to auto-generate the properties panel.

| Field | Type | Description |
|-------|------|-------------|
| VerbName | string | Machine-readable capability name matching the CapabilityRegistry key (e.g. "SetTemperature") |
| DisplayName | string | Human-readable label shown in the workflow builder UI |
| Description | string | Short description of what the capability does |
| Category | CapabilityCategory | Functional category: Control, Measurement, Status, or Utility |
| Parameters | ParameterSchema[] | Ordered list of accepted parameter schemas |
| IsDestructive | bool | True if invoking this capability has irreversible side effects |
| RequiresActiveRunspace | bool | True if the capability may only be invoked during an active runspace |
| EstimatedDurationMs | int? | Optional estimated execution duration in milliseconds |
| Returns | ParameterSchema[]? | Optional schema describing returned values |

## ParameterSchema Fields

Each entry in `Parameters` or `Returns` is a `ParameterSchema` record:

| Field | Type | Description |
|-------|------|-------------|
| Name | string | Machine-readable parameter name (e.g. "targetCelsius") |
| DisplayName | string | Human-readable label |
| Type | ParameterType | Numeric, Boolean, Enum, String, or Duration |
| Unit | string | Engineering unit (e.g. "celsius", "rpm", "ml/min") |
| Min | double? | Minimum value for Numeric parameters |
| Max | double? | Maximum value for Numeric parameters |
| Step | double? | Increment step for Numeric parameters |
| Options | string[]? | Allowed values for Enum parameters |
| DefaultValue | object? | Default value presented in the UI |

## Capability Namespacing

Capabilities in the CapabilityRegistry are namespaced by device prefix to prevent collision when multiple adapters register identically-named capabilities. The namespace prefix is the device name as registered in the Agent's DI container.

Examples:

- `vapourtec.SetTemperature` — SetTemperature on the VapourtecR4 adapter
- `vapourtec.SetFlowRate` — SetFlowRate on the VapourtecR4 adapter
- `knauer.RunMethod` — RunMethod on the KnauerAzura adapter
- `systag.SetTemperature` — SetTemperature on the SystagOpcUaClient adapter

In the workflow builder, the device picker shows the prefix (device name) separately from the capability name so you can select vapourtec + SetTemperature without knowing the full namespaced key.

## Example: Invoking SetTemperature via REST

The Agent exposes `POST /devices/&#123;id&#125;/execute` to invoke any registered capability directly (useful for testing outside a workflow).

```http
POST http://localhost:5000/devices/vapourtec/execute
Content-Type: application/json

{
  "capabilityName": "SetTemperature",
  "parameters": { "reactor": 0, "targetCelsius": 80 }
}
```

Expected response: HTTP 200 with result JSON. On failure the response body contains an error detail object with a `message` field.

## VapourtecR4 Capabilities Quick Reference

The VapourtecR4 adapter registers the following capabilities. See the [Adapter Authoring Guide](../developer/adapter-authoring) for full protocol detail.

| Capability | Parameters | Returns |
|-----------|------------|---------|
| Start | none | — |
| Stop | none | — |
| SetFlowRate | pump: int (0–3), rateMlMin: number (0–9.99) | — |
| SetTemperature | reactor: int, targetCelsius: int (0–150) | — |
| SelectReagent | channel: int (0–3) | — |
| SelectSolvent | channel: int (0–3) | — |
| SelectLoopFill | loop: int (0–3) | — |
| SelectLoopInject | loop: int (0–3) | — |
| GetFirmwareVersion | none | version: string |
| GetTemperatures | none | \{ "A": double, "B": double, ... \} |

All temperature values are in degrees Celsius. Flow rate parameters are in ml/min. The pump and reactor index values are zero-based.
