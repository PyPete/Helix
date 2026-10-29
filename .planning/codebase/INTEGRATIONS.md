---
last_mapped_commit: 25fb557bd430bea160370af41b8deb877170076b
last_mapped_at: 2026-10-29
---
# External Integrations

**Analysis Date:** 2026-10-29

## APIs & External Services

**Laboratory messaging:**
- MQTT broker - carries live trends and recorded fumehood events.
  - SDK/Client: `MQTTnet` 4.x in `Helix.Orchestration.Agent/Trends/MqttTrendIngestionService.cs`, `Helix.Orchestration.Recorder/Services/MqttRecorderService.cs`, and `Helix.Orchestration.SiteServer/Mqtt/MqttTrendBridge.cs`.
  - Auth: broker host, port, and topic settings are configuration-backed (`Mqtt` in `Helix.Orchestration.Agent/appsettings.json`, `MqttBridge` in `Helix.Orchestration.SiteServer/appsettings.json`, and recorder options); no broker credential configuration is detected.

**Service-to-service control:**
- gRPC Agent control API - SiteServer selects a simulator or real gRPC proxy to manage an Agent.
  - SDK/Client: `Grpc.AspNetCore`, `Grpc.Net.Client`, `Google.Protobuf`; contracts live in `Helix.Orchestration.Contracts/`.
  - Auth: no gRPC authentication or authorization provider is detected; endpoint address is configured by `Helix.Orchestration.SiteServer/Agent/AgentProxyOptions.cs`.

**Laboratory equipment:**
- OPC UA, Modbus RTU, serial, and TCP - device protocol adapters execute through `Helix.Orchestration.Devices/` and `Helix.Orchestration.Agent/Transport/`.
  - SDK/Client: `OPCFoundation.NetStandard.Opc.Ua.Client`, `FluentModbus`, and `System.IO.Ports`.
  - Auth: configuration/device-specific; no shared identity provider is detected.

## Data Storage

**Databases:**
- PostgreSQL - stores campaign/domain state and durable-function state.
   - Connection: the Agent requires `ConnectionStrings:HelixOrchestration` (or hierarchical environment key `ConnectionStrings__HelixOrchestration`) in `Helix.Orchestration.Agent/Program.cs`; SiteServer also binds a standard `ConnectionStrings` section in `Helix.Orchestration.SiteServer/appsettings.Development.json`.
  - Client: `Npgsql`, `Dapper`, and `Cleipnir.ResilientFunctions.PostgreSQL` through `Helix.Orchestration.Infrastructure/Persistence/`.

**File Storage:**
- S3-compatible object storage (MinIO or AWS S3) receives recorded events through `Helix.Orchestration.Infrastructure/Storage/MinioObjectStorageSink.cs`.
  - Client: `AWSSDK.S3`.
   - Configuration model: `Minio` section with service URL, access key, secret key, region, bucket, and path-style options in `Helix.Orchestration.Infrastructure/Storage/MinioOptions.cs`; Recorder binds this section in `Helix.Orchestration.Recorder/Program.cs`.

**Caching:**
- None detected.

## Authentication & Identity

**Auth Provider:**
- No external identity provider, ASP.NET authentication middleware, authorization policy, or token validation configuration is detected in the service entry points `Helix.Orchestration.Agent/Program.cs` and `Helix.Orchestration.SiteServer/Program.cs`.
  - Implementation: Not applicable.

## Monitoring & Observability

**Error Tracking:**
- No external error-tracking service is detected.

**Logs:**
- Serilog writes structured compact logs to console/file sinks in Agent and console in Recorder, configured in `Helix.Orchestration.Agent/Program.cs` and `Helix.Orchestration.Recorder/Program.cs`.
- OpenTelemetry primitives and hosting registration instrument Agent at `Helix.Orchestration.Agent/Telemetry/HelixTelemetry.cs` and `Helix.Orchestration.Agent/Program.cs`; no exporter endpoint is detected.

## CI/CD & Deployment

**Hosting:**
- Docker deployment assets are `Helix.Orchestration.Agent/Dockerfile`, `Helix.Orchestration.Recorder/Dockerfile`, `Helix.Orchestration.SiteServer/Dockerfile`, and root `docker-compose.yml`.

**CI Pipeline:**
- No CI workflow directory is detected at `.github/workflows/`.

## Environment Configuration

**Required env vars:**
- No required environment-variable names are explicitly detected. Use standard ASP.NET Core configuration binding to supply values corresponding to the `ConnectionStrings`, MQTT/MQTT bridge, storage, and Agent proxy configuration sections in the application settings files.

**Secrets location:**
- No dedicated secrets-store integration is detected. No `.env` files are detected by this analysis; do not place credentials in committed `appsettings*.json` files.

## Webhooks & Callbacks

**Incoming:**
- HTTP REST endpoints and Swagger are exposed by SiteServer (`Helix.Orchestration.SiteServer/Program.cs`); Agent exposes HTTP endpoints plus gRPC (`Helix.Orchestration.Agent/Program.cs`).
- WebSocket live-trend endpoints are `/api/trends/ws` in `Helix.Orchestration.SiteServer/Program.cs` and `/ws` in `Helix.Orchestration.Agent/Program.cs`, handled by `Helix.Orchestration.SiteServer/WebSockets/TrendWebSocketManager.cs` and `Helix.Orchestration.Agent/AgentService.cs`.

**Outgoing:**
- SiteServer publishes trends to MQTT via `Helix.Orchestration.SiteServer/Mqtt/MqttTrendBridge.cs`.
- Recorder writes MQTT-derived events to S3-compatible storage through `Helix.Orchestration.Recorder/` and `Helix.Orchestration.Infrastructure/Storage/MinioObjectStorageSink.cs`.

---

*Integration audit: 2026-10-29*
