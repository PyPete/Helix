# Technology Stack

**Analysis Date:** 2026-05-26

## Language & Runtime

**Primary:**
- C# (latest LangVersion; `AllowUnsafeBlocks` enabled in `Helix.Orchestration.Devices`) — all projects
- Runtime: .NET 10.0 (`net10.0`) — all projects in `Helix.Orchestration/`

**Secondary:**
- SQL — PostgreSQL migration scripts in `Helix.Orchestration/postgres/migrations/`
- Protocol Buffers (proto3) — gRPC service definitions in `Helix.Orchestration/Helix.Orchestration.Contracts/Protos/`

## Runtime

**Environment:**
- .NET 10.0 — confirmed across all `.csproj` files

**Package Manager:**
- NuGet (dotnet CLI)
- Lockfile: not present (no `packages.lock.json` detected; CI uses hash of `*.csproj` files for cache key)

## Frameworks

**Core:**
- `Microsoft.NET.Sdk.Worker` — `Helix.Orchestration.Agent`, `Helix.Orchestration.Recorder` (background Worker Services)
- `Microsoft.NET.Sdk.Web` — `Helix.Orchestration.SiteServer` (ASP.NET Core Web API + gRPC host)
- `Microsoft.NET.Sdk` — all library projects (`Core`, `Devices`, `Infrastructure`, `Contracts`, `Simulation`)
- ASP.NET Core (via `Microsoft.NET.Sdk.Web`) 10.0 — HTTP API + health check endpoint (`/health`) in SiteServer and Agent

**Testing:**
- xUnit 2.9.x — all test projects
- Microsoft.NET.Test.Sdk 17.x — all test projects
- Testcontainers.PostgreSql 4.x — `Helix.Orchestration.Infrastructure.Tests` (integration tests against real Postgres)
- Microsoft.AspNetCore.Mvc.Testing 10.x — `Helix.Orchestration.SiteServer.Tests`

**Build/Dev:**
- dotnet CLI — build, restore, test, publish
- `dotnet format` — formatting check (informational, `continue-on-error: true` in CI)

## Key NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `Cleipnir.ResilientFunctions` | 4.0.0 | Durable workflow execution; `InMemoryFunctionStore` + `FunctionsRegistry` in Agent |
| `FluentModbus` | 5.0.3 | Modbus RTU client (`ModbusRtuClient`) in Agent transport and Devices project |
| `Grpc.AspNetCore` | 2.* | gRPC server hosting in Agent |
| `Grpc.Net.Client` | 2.* | gRPC client in Agent and SiteServer |
| `Grpc.Core.Api` | 2.* | gRPC core API in Contracts |
| `Grpc.Tools` | 2.* | Protobuf code generation (build-time only) in Contracts |
| `Google.Protobuf` | 3.* | Protobuf serialization in Contracts and SiteServer |
| `MQTTnet` | 4.* | MQTT client (trend ingestion) in Agent, SiteServer, Recorder |
| `NATS.Net` | 2.* | NATS messaging client in Core |
| `Opc.UaFx.Advanced` | 2.9.1 | OPC-UA client (commercial SDK) in Agent (`OpcTransport`) |
| `OPCFoundation.NetStandard.Opc.Ua.Client` | 1.5.374.86 | OPC Foundation open-source OPC-UA client in Devices (`OpcFoundationSession`) |
| `System.IO.Ports` | 9.0.5 | Serial port (RS-232/RS-485) access in Agent and Devices |
| `System.Reactive` | 6.x | Rx.NET observables; used for transport status streams, trend streams throughout |
| `Npgsql` | 9.* | PostgreSQL ADO.NET driver in Infrastructure |
| `Dapper` | 2.* | Lightweight ORM / SQL mapping in Infrastructure |
| `AWSSDK.S3` | 4.0.23.4 | S3-compatible object storage client (used for MinIO) in Infrastructure |
| `Serilog.Extensions.Hosting` | 8.* | Serilog DI integration in Agent and Recorder |
| `Serilog.Formatting.Compact` | 2.* | CLEF (compact JSON) log formatting |
| `Serilog.Sinks.Console` | 5.* | Console log sink |
| `Serilog.Sinks.File` | 5.* | Rolling file log sink (`logs/helix-agent-.log`) |
| `OpenTelemetry` | 1.* | Tracing and metrics instrumentation in Agent |
| `OpenTelemetry.Extensions.Hosting` | 1.* | OTel DI integration in Agent |
| `Swashbuckle.AspNetCore` | 7.* | OpenAPI / Swagger docs in SiteServer |
| `Microsoft.Extensions.Hosting` | 10.0.8 | Generic host in Recorder |
| `NSubstitute` | 5.x | Test mocking in Agent.Tests and Devices.Tests |
| `Moq` | 4.20.x | Test mocking in Agent.Tests, SiteServer.Tests, Recorder.Tests |
| `coverlet.collector` | 6.x | Code coverage collection in test projects |

## Tooling

**Build:**
- dotnet CLI (`dotnet restore`, `dotnet build`, `dotnet publish`)
- Solution file: `Helix.Orchestration/Helix.Orchestration.slnx` (new `.slnx` format)

**Test:**
- xUnit via `dotnet test`
- Test results output: TRX format (`--logger trx --results-directory test-results`)

**Containerization:**
- Docker Compose — local dev infrastructure only (no application Dockerfile found)
  - `Helix.Orchestration/docker-compose.yml` — Mosquitto MQTT broker + PostgreSQL 16
  - `docker-compose.yml` (root) — Mosquitto + MinIO

**CI/CD:**
- GitHub Actions — `.github/workflows/ci.yml`
- Triggers: push/PR to `dev`, `stage`, `main`
- Runner: `ubuntu-latest`
- Jobs: `build-and-test` → `code-quality` (parallel) → `publish` (on push to `main` only)
- Publish artifact: `Helix.Orchestration.Core` → `helix-orchestration-core-publish` (retained 30 days)
- NuGet cache keyed on `packages.lock.json` + `*.csproj` hashes

## Configuration

**Environment:**
- `appsettings.json` + `appsettings.Development.json` per service
- User Secrets: `Helix.Orchestration.Recorder` has `UserSecretsId` configured
- Connection string key: `HelixOrchestration` (PostgreSQL) — read by `InfrastructureServiceExtensions`
- MinIO config section: `Minio` (ServiceUrl, AccessKey, SecretKey, Region, DefaultBucket, ForcePathStyle)
- MQTT config section: `Mqtt` (BrokerHost, BrokerPort, TopicFilters)
- AgentProxy config section: `AgentProxy` (Mode: `sim`/live, GrpcAddress)
- MqttBridge config section: `MqttBridge` (BrokerHost, BrokerPort, SiteId, FumehoodId)

**Build:**
- Default configuration: `Release` in CI; no global `.props` or `Directory.Build.props` detected

## Platform Requirements

**Development:**
- .NET 10 SDK
- Docker (for local Mosquitto + PostgreSQL via Compose)
- Windows preferred (CDllDeviceTransport uses `win-x64/win-x86/win-arm64/win-arm` runtime paths; `NativeLibrary` P/Invoke)

**Production:**
- Deployment target: not defined (no Dockerfile or cloud config found)
- CI publishes `Helix.Orchestration.Core` as a GitHub Actions artifact on `main` push

---

*Stack analysis: 2026-05-26*
