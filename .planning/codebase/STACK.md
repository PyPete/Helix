# Technology Stack

**Analysis Date:** 2026-09-05

## Languages

**Primary:**
- C# (.NET 10) - Backend orchestration services (all `.csproj` projects under repo root)
- TypeScript 5.5 - Frontend desktop UI (`helix-desktop/src/`)
- Rust (edition 2021, rust-version 1.77) - Tauri native shell (`helix-desktop/src-tauri/`)

**Secondary:**
- Protocol Buffers (proto3) - gRPC contract definitions (`Helix.Orchestration.Contracts/Protos/`)

## Runtime

**Environment:**
- .NET 10.0 - All C# backend services
- Node.js (version managed via project tooling) - Frontend build/dev

**Package Manager:**
- npm - Frontend (`helix-desktop/package.json`)
- NuGet - C# projects (`nuget.config` present at repo root)
- Cargo - Rust/Tauri shell (`helix-desktop/src-tauri/Cargo.toml`)
- Lockfile: `helix-desktop/package-lock.json` (npm), `helix-desktop/src-tauri/Cargo.lock` (Cargo)

## Frameworks

**Core (Backend):**
- ASP.NET Core 10 (Kestrel) - HTTP + gRPC hosting in `Helix.Orchestration.Agent` and `Helix.Orchestration.SiteServer`
- .NET Generic Host (Worker SDK) - Background service host in `Helix.Orchestration.Agent` and `Helix.Orchestration.Recorder`
- Cleipnir.ResilientFunctions 4.x - Durable workflow execution (`Helix.Orchestration.Core`, `Helix.Orchestration.Agent`)
- Grpc.AspNetCore 2.x - gRPC server (`Helix.Orchestration.Agent`)

**Core (Frontend):**
- React 18.3 - UI component framework (`helix-desktop/`)
- Tauri 2.0 - Native desktop shell wrapping the React frontend (`helix-desktop/src-tauri/`)
- Vite 6.0 - Dev server and build tool (`helix-desktop/vite.config.ts`)

**Testing:**
- Vitest 4.x - Unit/integration test runner (`helix-desktop/vitest` config inline in package.json scripts)
- xUnit (inferred from `.Tests` csproj structure) - C# unit tests under `tests/`
- @testing-library/react 16.x + jsdom 29 - React component testing

**Build/Dev:**
- tonic-build 0.14 + protoc - Rust-side gRPC code gen (`helix-desktop/src-tauri/build.rs`)
- Grpc.Tools 2.x - C#-side protobuf/gRPC code gen (`Helix.Orchestration.Contracts`)
- Docker Compose - Local dev infrastructure orchestration (`docker-compose.yml`)

## Key Dependencies

**Critical:**
- `Cleipnir.ResilientFunctions` 4.x - Durable execution engine backing experiment lifecycle; underpins agent workflows in `Helix.Orchestration.Core` and `Helix.Orchestration.Agent`
- `MQTTnet` 4.x - MQTT client for real-time device telemetry; used in `Helix.Orchestration.Core`, `Helix.Orchestration.Agent`, `Helix.Orchestration.SiteServer`, `Helix.Orchestration.Recorder`
- `NATS.Net` 2.x - NATS messaging client in `Helix.Orchestration.Core`
- `System.Reactive` 6.x - Rx.NET observable pipelines throughout backend and `Helix.Orchestration.Devices`
- `rc-dock` 3.3.2 - Dockable panel layout library for the desktop UI (`helix-desktop/`)
- `@xyflow/react` 12.x - Node-graph / flow diagram UI (`helix-desktop/`)
- `recharts` 3.8.1 - Chart rendering in the desktop UI (`helix-desktop/`)

**Infrastructure:**
- `Npgsql` 9.x + `Dapper` 2.x - PostgreSQL data access in `Helix.Orchestration.Infrastructure`
- `Cleipnir.ResilientFunctions.PostgreSQL` 4.x - PostgreSQL-backed durable function state (`Helix.Orchestration.Agent`)
- `AWSSDK.S3` 4.x - S3-compatible object storage client in `Helix.Orchestration.Infrastructure`
- `OPCFoundation.NetStandard.Opc.Ua.Client` 1.5.x - OPC UA device communication (`Helix.Orchestration.Devices`)
- `FluentModbus` 5.0.3 - Modbus device communication (`Helix.Orchestration.Devices`, `Helix.Orchestration.Agent`)
- `System.IO.Ports` 9.0.5 - Serial port access (`Helix.Orchestration.Devices`, `Helix.Orchestration.Agent`)
- `Serilog` (Extensions.Hosting 8.x, Formatting.Compact 2.x, Sinks.Console 5.x, Sinks.File 5.x) - Structured logging in Agent and Recorder
- `OpenTelemetry` 1.x + `OpenTelemetry.Extensions.Hosting` - Observability instrumentation in `Helix.Orchestration.Agent`
- `Swashbuckle.AspNetCore` 7.x - Swagger/OpenAPI docs for SiteServer REST API
- `Google.Protobuf` 3.x + `Grpc.Core.Api` 2.x - Protobuf contracts in `Helix.Orchestration.Contracts`
- `allotment` 1.20.5 - Resizable split-pane layout in the desktop UI
- `@tauri-apps/plugin-fs` 2.5.x + `@tauri-apps/plugin-store` 2.x - Native file system and key-value store access from React via Tauri IPC
- `tonic` 0.14 + `tokio` 1.x - Async Rust runtime and gRPC client in Tauri shell

## Configuration

**Environment:**
- Backend services configured via ASP.NET Core environment variables and `appsettings.json` (connection strings, MQTT broker host/port, gRPC address)
- Key env vars at runtime: `Mqtt__BrokerHost`, `Mqtt__BrokerPort`, `ConnectionStrings__HelixOrchestration`, `AgentProxy__GrpcAddress`, `Minio__ServiceUrl`, `Minio__AccessKey`, `Minio__SecretKey`
- Frontend build: `VITE_*` and `TAURI_ENV_*` prefixes respected by `helix-desktop/vite.config.ts`

**Build:**
- `helix-desktop/vite.config.ts` - Vite build config; targets Chrome 105 on Windows, Safari 13 on others
- `helix-desktop/src-tauri/tauri.conf.json` - Tauri app identity (`com.helix.desktop`), window config (1280x800), bundle targets
- `helix-desktop/src-tauri/Cargo.toml` - Rust crate manifest
- `nuget.config` - NuGet feed configuration (repo root)

## Platform Requirements

**Development:**
- .NET 10 SDK
- Node.js + npm
- Rust toolchain (1.77+) with Tauri CLI v2
- Docker + Docker Compose (for local infrastructure: Postgres, MQTT, MinIO)

**Production:**
- Containerised: each backend service has a `Dockerfile`; orchestrated via `docker-compose.yml`
- Desktop: Tauri bundles a native installer for Windows (primary target per vite config); macOS/Linux also supported by Tauri
- Infrastructure services: PostgreSQL 16, Eclipse Mosquitto 2, MinIO (self-hosted)

---

*Stack analysis: 2026-09-05*
