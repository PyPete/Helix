---
last_mapped_commit: 25fb557bd430bea160370af41b8deb877170076b
last_mapped_at: 2026-10-29
---
# Technology Stack

**Analysis Date:** 2026-10-29

## Languages

**Primary:**
- C# / .NET 10 (`net10.0`) powers the orchestration services and class libraries in `Helix.Orchestration.*/*.csproj`.
- TypeScript powers the Vite web applications in `helix-dashboard/`, `helix-desktop/`, and the Docusaurus site in `docs-site/`.

**Secondary:**
- Rust 1.77+ supports the Tauri desktop host in `helix-desktop/src-tauri/Cargo.toml`.
- Protocol Buffers defines gRPC contracts in `Helix.Orchestration.Contracts/`.
- SQL is embedded in the PostgreSQL repositories under `Helix.Orchestration.Infrastructure/Persistence/`.

## Runtime

**Environment:**
- .NET 10 SDK/runtime builds and runs the services listed by `Helix.Orchestration.slnx`.
- Node.js 20+ is explicitly required by `docs-site/package.json`; it also runs the Vite applications.
- Rust/Cargo is required to build the Tauri native wrapper in `helix-desktop/src-tauri/`.

**Package Manager:**
- NuGet restores .NET dependencies declared by each `*.csproj`; no `packages.lock.json` is detected.
- npm manages each JavaScript workspace; lockfiles are present at `helix-dashboard/package-lock.json`, `helix-desktop/package-lock.json`, and `docs-site/package-lock.json`.

## Frameworks

**Core:**
- ASP.NET Core minimal APIs / hosted services provide HTTP, gRPC, and WebSocket endpoints in `Helix.Orchestration.Agent/Program.cs` and `Helix.Orchestration.SiteServer/Program.cs`.
- React powers the dashboard (`helix-dashboard/package.json`, React 19) and desktop renderer (`helix-desktop/package.json`, React 18).
- Tauri 2 combines the Rust host and React renderer for `helix-desktop/`.
- Docusaurus 3 generates documentation from `docs-site/`.

**Testing:**
- xUnit, Microsoft.NET.Test.Sdk, coverlet, Moq/NSubstitute, and Testcontainers PostgreSQL test .NET projects in `tests/*/*.csproj`.
- Vitest, Testing Library, jsdom, and V8 coverage test the Vite applications from each frontend `package.json`.

**Build/Dev:**
- Vite compiles both React applications (`helix-dashboard/vite.config.ts`, `helix-desktop/vite.config.ts`).
- Docusaurus invokes `docs-site/scripts/generate-openapi.js` before development and production builds.
- Dockerfiles package Agent, Recorder, and SiteServer from `Helix.Orchestration.Agent/Dockerfile`, `Helix.Orchestration.Recorder/Dockerfile`, and `Helix.Orchestration.SiteServer/Dockerfile`; `docker-compose.yml` is present for local multi-service deployment.

## Key Dependencies

**Critical:**
- `Cleipnir.ResilientFunctions` 4.x and PostgreSQL persistence coordinate durable runspaces in `Helix.Orchestration.Agent/Helix.Orchestration.Agent.csproj` and `Helix.Orchestration.Core/Helix.Orchestration.Core.csproj`.
- `MQTTnet` 4.x transports live trend/event streams in Agent, Recorder, SiteServer, and Core projects.
- gRPC (`Grpc.AspNetCore`, `Grpc.Net.Client`, `Google.Protobuf`) connects SiteServer to Agent and generates contracts from `Helix.Orchestration.Contracts/`.
- `Npgsql` 9.x plus `Dapper` 2.x access PostgreSQL in `Helix.Orchestration.Infrastructure/`.
- `AWSSDK.S3` 4.0.23.4 implements S3-compatible/MinIO object storage in `Helix.Orchestration.Infrastructure/Storage/MinioObjectStorageSink.cs`.

**Infrastructure:**
- `OpenTelemetry` 1.x instruments the Agent runtime in `Helix.Orchestration.Agent/Program.cs` and `Helix.Orchestration.Agent/Telemetry/HelixTelemetry.cs`.
- Serilog hosting, compact formatting, console, and file sinks provide Agent/Recorder structured logging in their respective project files.
- `NATS.Net` 2.x is declared by `Helix.Orchestration.Core/Helix.Orchestration.Core.csproj`; no concrete NATS client use is detected.
- OPC UA, FluentModbus, `System.IO.Ports`, and TCP sockets connect laboratory hardware through `Helix.Orchestration.Devices/`.
- SciChart powers dashboard trend charts (`helix-dashboard/package.json`); React Flow, Allotment, rc-dock, and Recharts support the desktop UI (`helix-desktop/package.json`).

## Configuration

**Environment:**
- .NET uses standard app configuration from `appsettings.json`, with development overrides where present under `Helix.Orchestration.Agent/`, `Helix.Orchestration.Recorder/`, and `Helix.Orchestration.SiteServer/`.
- Configure MQTT, service listener, PostgreSQL, object-storage, device transport, and Agent proxy options through their named configuration sections; option types reside in the owning service or infrastructure project.
- `.env` files are not detected by this analysis.

**Build:**
- Solution membership is defined in `Helix.Orchestration.slnx`.
- TypeScript/Vite settings live beside each frontend in `helix-dashboard/` and `helix-desktop/`; Tauri settings are in `helix-desktop/src-tauri/tauri.conf.json`.
- Documentation configuration is `docs-site/docusaurus.config.ts` and its OpenAPI source is `docs-site/static/openapi.json`.

## Platform Requirements

**Development:**
- Install .NET 10, Node.js 20+ and npm; install Rust/Cargo for desktop builds. Device-connected services additionally require reachable serial/TCP/Modbus/OPC UA equipment as configured under `Helix.Orchestration.Devices/`.

**Production:**
- Deploy Agent, Recorder, and SiteServer as .NET services or their provided containers. Supply connectivity to PostgreSQL, an MQTT broker, S3-compatible storage, and configured laboratory devices.

---

*Stack analysis: 2026-10-29*
