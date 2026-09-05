# External Integrations

**Analysis Date:** 2026-09-05

## APIs & External Services

**gRPC (internal inter-service):**
- Agent ↔ SiteServer over gRPC - Agent exposes gRPC server; SiteServer connects as client
  - SDK/Client: `Grpc.AspNetCore` 2.x (server), `Grpc.Net.Client` 2.x (client), `tonic` 0.14 (Rust client in Tauri shell)
  - Proto contracts: `Helix.Orchestration.Contracts/Protos/adapter.proto`, `Helix.Orchestration.Contracts/Protos/agent_control.proto`
  - Auth: None detected (plain HTTP/2 in dev; `AgentProxy__GrpcAddress` env var)

**NATS (internal messaging):**
- NATS.Net 2.x client in `Helix.Orchestration.Core`
- Connection: configured programmatically; no dedicated env var visible in compose (not exposed as a compose service — likely external or future)

## Data Storage

**Databases:**
- PostgreSQL 16
  - Connection string env var: `ConnectionStrings__HelixOrchestration`
  - Example value: `Host=helix-postgres;Port=5432;Database=helix_orchestration;Username=helix;Password=helix_dev_password`
  - Client: `Npgsql` 9.x (ADO.NET driver) + `Dapper` 2.x (micro-ORM) in `Helix.Orchestration.Infrastructure`
  - Durable function state: `Cleipnir.ResilientFunctions.PostgreSQL` 4.x in `Helix.Orchestration.Agent`
  - Migrations: SQL files in `postgres/migrations/` applied via Docker `docker-entrypoint-initdb.d`

**File / Object Storage:**
- MinIO (self-hosted S3-compatible)
  - API endpoint: `Minio__ServiceUrl` (default `http://helix-minio:9000`)
  - Credentials: `Minio__AccessKey` / `Minio__SecretKey` env vars; bucket `Minio__DefaultBucket` (default `helix-recordings`)
  - Client: `AWSSDK.S3` 4.x in `Helix.Orchestration.Infrastructure`
  - Used by: `Helix.Orchestration.Recorder` (stores MQTT-captured telemetry recordings)

**Caching:**
- None detected

## Authentication & Identity

**Auth Provider:**
- None detected — no OAuth, JWT middleware, or identity package references found in any `.csproj`
- SiteServer REST API is unauthenticated in current implementation (Swagger enabled, no auth middleware)

## Messaging / Real-time

**MQTT (Eclipse Mosquitto 2):**
- Broker ports: TCP 1883 (backend services), WebSocket 9001 (Tauri/React frontend)
- Client library: `MQTTnet` 4.x in `Helix.Orchestration.Core`, `Helix.Orchestration.Agent`, `Helix.Orchestration.SiteServer`, `Helix.Orchestration.Recorder`
- Config env vars: `Mqtt__BrokerHost`, `Mqtt__BrokerPort` (Agent, SiteServer); `Recorder__MqttBrokerHost`, `Recorder__MqttBrokerPort` (Recorder); `MqttBridge__BrokerHost`, `MqttBridge__BrokerPort`, `MqttBridge__SiteId`, `MqttBridge__FumehoodId` (SiteServer bridge)
- Frontend connects via `@tauri-apps/api` Tauri IPC or directly to ws://localhost:9001

## Industrial Device Protocols

**OPC UA:**
- Client: `OPCFoundation.NetStandard.Opc.Ua.Client` 1.5.374 in `Helix.Orchestration.Devices`
- Used for reading/writing process data on OPC UA-capable lab instruments

**Modbus TCP/RTU:**
- Client: `FluentModbus` 5.0.3 in `Helix.Orchestration.Devices` and `Helix.Orchestration.Agent`
- Used for direct instrument register access

**Serial (RS-232/RS-485):**
- `System.IO.Ports` 9.0.5 in `Helix.Orchestration.Devices` and `Helix.Orchestration.Agent`
- Used for serial-connected lab devices

## Monitoring & Observability

**Tracing / Metrics:**
- OpenTelemetry SDK 1.x + `OpenTelemetry.Extensions.Hosting` in `Helix.Orchestration.Agent`
- No exporter package detected — collector endpoint not yet configured

**Logging:**
- Serilog structured logging in `Helix.Orchestration.Agent` and `Helix.Orchestration.Recorder`
  - Sinks: Console (compact JSON via `Serilog.Formatting.Compact`), File (`Serilog.Sinks.File`)
- Frontend: browser `console.*` (no structured log SDK detected)

**Error Tracking:**
- None detected (no Sentry, Application Insights, or similar package)

## CI/CD & Deployment

**Hosting:**
- Docker containers orchestrated by `docker-compose.yml` (local dev and likely production)
- Tauri distributes a native desktop installer (`bundle.targets: "all"` in `helix-desktop/src-tauri/tauri.conf.json`)

**CI Pipeline:**
- `CI.md` document present in `docs/`; specific CI provider (GitHub Actions, GitLab CI, etc.) not confirmed without reading that file

## API Documentation

**REST API (SiteServer):**
- Swagger UI via `Swashbuckle.AspNetCore` 7.x in `Helix.Orchestration.SiteServer`
- Available at SiteServer base URL (port 5090) in dev

## Webhooks & Callbacks

**Incoming:**
- None detected

**Outgoing:**
- None detected

## Environment Configuration Summary

**Required env vars (backend services):**
- `ConnectionStrings__HelixOrchestration` - PostgreSQL connection string (Agent, SiteServer, Recorder)
- `Mqtt__BrokerHost` / `Mqtt__BrokerPort` - MQTT broker address (Agent)
- `MqttBridge__BrokerHost` / `MqttBridge__BrokerPort` / `MqttBridge__SiteId` / `MqttBridge__FumehoodId` - MQTT bridge config (SiteServer)
- `AgentProxy__GrpcAddress` - gRPC address of Agent service (SiteServer)
- `Recorder__MqttBrokerHost` / `Recorder__MqttBrokerPort` - MQTT broker (Recorder)
- `Minio__ServiceUrl` / `Minio__AccessKey` / `Minio__SecretKey` / `Minio__DefaultBucket` - Object storage (Recorder)

**Secrets location:**
- Dev: plaintext env vars in `docker-compose.yml` (dev passwords only; not production secrets)
- Agent service uses .NET User Secrets (`UserSecretsId` present in `Helix.Orchestration.Recorder.csproj`)

---

*Integration audit: 2026-09-05*
