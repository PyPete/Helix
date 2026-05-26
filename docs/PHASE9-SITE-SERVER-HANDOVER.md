# Phase 9 — Site Server API Host + MQTT-over-WebSocket Real-Time Foundation — Handover

**Phase**: 9  
**Date**: 2026-05-25  
**Status**: ✅ Complete  

---

## Executive Summary

Phase 9 establishes a simulation-first, technology-agnostic integration surface for Helix Orchestration:

- **Control plane**: REST API with Swagger/OpenAPI via ASP.NET Minimal API (`Helix.Orchestration.SiteServer`)
- **Real-time plane**: MQTT pub/sub bridge (`MqttTrendBridge`) publishing trend updates
- **Simulation layer**: in-memory trend provider and background simulated devices (`Helix.Orchestration.Simulation`)
- **Agent seam**: `IAgentProxy` + in-process `SimAgentProxy` to preserve a future swap path to gRPC without UI contract changes

This enables React, Tauri, or other non-.NET clients to:
1. submit and monitor experiments via REST, and
2. consume real-time trend data through MQTT (including WebSocket clients).

---

## Delivered Scope

### 1) Simulation project and runtime registration

Added and wired `Helix.Orchestration.Simulation` with:

- `SimulatedTrendProvider` implementing `ITrendProvider`
- `SimReactor` (`BackgroundService`) with temperature/pressure scalar trends
- `SimPump` (`BackgroundService`) with flow scalar trend
- `SimSpectrometer` (`BackgroundService`) emitting `Spectrum1D` scans
- `SimCapabilityInvoker` implementing `ICapabilityInvoker` (routes `SetTemperature`)
- `SimulationServiceExtensions.AddHelixSimulation()` DI extension

### 2) Site Server API host

Added and wired `Helix.Orchestration.SiteServer` with:

- `Program.cs` service registration and endpoint mapping
- Swagger/OpenAPI UI in development
- `IAgentProxy` contract and `SimAgentProxy` in-process implementation
- Endpoint groups:
  - `ExperimentEndpoints`
  - `DeviceEndpoints`
  - `TrendEndpoints`

### 3) MQTT trend bridge

Implemented `MqttTrendBridge` (`BackgroundService`) to:

- connect to configured broker host/port,
- subscribe to `ITrend.Values` for all trends,
- publish JSON trend payloads to topic pattern:

`helix/{siteId}/{fumehoodId}/{experimentId}/trends/{kind}/{trendId}`

### 4) Local broker configuration for dev

Added local broker artifacts:

- `docker-compose.yml`
- `mosquitto/mosquitto.conf`

Configured listeners:
- `1883` (MQTT/TCP)
- `9001` (MQTT over WebSocket)

### 5) Test suite for Site Server phase scope

Added `Helix.Orchestration.SiteServer.Tests` sources covering:

- Simulation provider behavior (`SimulatedTrendProviderTests`)
- Sim reactor behavior (`SimReactorTests`)
- Agent proxy behavior (`SimAgentProxyTests`)
- API integration tests for experiments/devices/trends using `WebApplicationFactory`

A shared `HelixWebApplicationFactory` replaces hosted services with a no-op hosted service to avoid requiring a live broker during integration tests.

---

## API Surface (Phase 9)

### Experiments

- `POST /api/experiments` — submit an experiment; returns `RunspaceId`
- `GET /api/experiments/{id}` — retrieve runspace status
- `DELETE /api/experiments/{id}` — abort runspace

### Devices

- `GET /api/devices` — list registered simulated devices and capabilities

### Trends

- `GET /api/trends` — list trend descriptors
- `GET /api/trends/{id}/history` — fetch scalar trend history (double)

---

## Technical Notes

### Minimal API compatibility adjustments

During completion, endpoint implementations were aligned to current project/runtime constraints by:

- removing unavailable `WithOpenApi()` endpoint-group usage,
- removing MVC binding attributes in minimal API delegates,
- using explicit `IResult` / `Task<IResult>` delegate signatures,
- aligning history retrieval with `TrendWindow<T>.GetValues()`.

### Simulation trend interface alignment

`SimulatedTrendProvider` wrapper types were updated to satisfy current Core trend interfaces (`IDiodeArrayTrend`, `IImageTrend`, `IVideoTrend`, `IDerivedTrend`) with explicit Phase-9 stub implementations where full simulation behavior is not yet required.

---

## Validation Results

### Build

- `Helix.Orchestration.SiteServer` builds successfully
- `Helix.Orchestration.SiteServer.Tests` builds successfully

### Tests

Executed test project:
- **Project**: `Helix.Orchestration.SiteServer.Tests`
- **Result**: **30 Passed / 0 Failed**

---

## How to Run (Local)

### Start broker

From `Helix.Orchestration` directory:

- `docker compose up -d`

### Run Site Server

- Run `Helix.Orchestration.SiteServer`
- Open Swagger UI in development environment
- API base path uses `/api/*` routes documented above

### Subscribe to real-time trends (example topic wildcard)

- `helix/+/+/+/trends/#`

---

## Key Files Added/Updated

### Added

- `Helix.Orchestration/docker-compose.yml`
- `Helix.Orchestration/mosquitto/mosquitto.conf`
- `Helix.Orchestration/docs/PHASE9-SITE-SERVER-HANDOVER.md`
- `tests/Helix.Orchestration.SiteServer.Tests/HelixWebApplicationFactory.cs`
- `tests/Helix.Orchestration.SiteServer.Tests/Agent/SimAgentProxyTests.cs`
- `tests/Helix.Orchestration.SiteServer.Tests/Simulation/SimulatedTrendProviderTests.cs`
- `tests/Helix.Orchestration.SiteServer.Tests/Simulation/SimReactorTests.cs`
- `tests/Helix.Orchestration.SiteServer.Tests/Endpoints/ExperimentEndpointsTests.cs`
- `tests/Helix.Orchestration.SiteServer.Tests/Endpoints/DeviceEndpointsTests.cs`
- `tests/Helix.Orchestration.SiteServer.Tests/Endpoints/TrendEndpointsTests.cs`

### Updated

- `Helix.Orchestration.SiteServer/Program.cs`
- `Helix.Orchestration.SiteServer/Endpoints/ExperimentEndpoints.cs`
- `Helix.Orchestration.SiteServer/Endpoints/DeviceEndpoints.cs`
- `Helix.Orchestration.SiteServer/Endpoints/TrendEndpoints.cs`
- `Helix.Orchestration.Simulation/SimulatedTrendProvider.cs`
- `tests/Helix.Orchestration.SiteServer.Tests/Helix.Orchestration.SiteServer.Tests.csproj`

---

## Known Phase-9 Limits

- `IAgentProxy` is still simulation/in-process only (no real remote Agent transport yet)
- Derived/diode-array/image/video trends are interface-complete but simulation behavior is still stubbed
- MQTT bridge tests currently avoid external broker dependencies; full end-to-end broker integration test coverage is deferred

---

## Recommended Next Phase

1. Replace `SimAgentProxy` with real transport (gRPC/protobuf seam already planned).
2. Introduce typed capability dispatch pipeline beyond simulation stubs.
3. Add broker-backed integration tests (containerized broker lifecycle in test harness).
4. Expand trend history/query endpoints for non-scalar and typed analytical retrieval patterns.
