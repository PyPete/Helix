# External Integrations

**Analysis Date:** 2026-05-26

## Hardware / Protocols

**OPC-UA (dual-SDK):**
- Used by: `Helix.Orchestration.Agent` (via `Opc.UaFx.Advanced` 2.9.1) and `Helix.Orchestration.Devices` (via `OPCFoundation.NetStandard.Opc.Ua.Client` 1.5.374.86)
- Agent implementation: `Helix.Orchestration/Helix.Orchestration.Agent/Transport/OpcTransport.cs` — synchronous `OpcClient` from Opc.UaFx; supports Connect, Read, Write, BrowseChildren
- Devices implementation: `Helix.Orchestration/Helix.Orchestration.Devices/Transports/Opc/OpcFoundationSession.cs` — OPC Foundation client wrapping `IOpcSession`
- Abstraction: `Helix.Orchestration/Helix.Orchestration.Devices/Transport/IOpcTransport.cs`, `Helix.Orchestration/Helix.Orchestration.Devices/Transports/Opc/OpcTransportOptions.cs`
- Note: Two different OPC-UA SDKs are in use simultaneously (commercial Opc.UaFx in Agent, open-source OPC Foundation in Devices)

**Modbus RTU:**
- Used by: `Helix.Orchestration.Agent` and `Helix.Orchestration.Devices`
- Agent implementation: `Helix.Orchestration/Helix.Orchestration.Agent/Transport/ModbusTransport.cs` — `ModbusRtuClient` from FluentModbus; serial port, big-endian
- Devices implementation: `Helix.Orchestration/Helix.Orchestration.Devices/Transports/Modbus/ModbusDeviceTransport.cs`
- Abstraction: `Helix.Orchestration/Helix.Orchestration.Devices/Transport/IModbusTransport.cs`, `Helix.Orchestration/Helix.Orchestration.Devices/Transports/Modbus/ModbusTransportOptions.cs`
- SDK: `FluentModbus` 5.0.3

**Serial (RS-232 / RS-485):**
- Used by: `Helix.Orchestration.Agent` and `Helix.Orchestration.Devices`
- Agent implementation: `Helix.Orchestration/Helix.Orchestration.Agent/Transport/SerialTransport.cs`
- Devices implementation: `Helix.Orchestration/Helix.Orchestration.Devices/Transports/Serial/SerialDeviceTransport.cs`
- Abstraction: `Helix.Orchestration/Helix.Orchestration.Devices/Transport/ISerialTransport.cs`, `Helix.Orchestration/Helix.Orchestration.Devices/Transports/Serial/SerialTransportOptions.cs`
- SDK: `System.IO.Ports` 9.0.5

**TCP (raw socket):**
- Used by: `Helix.Orchestration.Agent` and `Helix.Orchestration.Devices`
- Agent implementation: `Helix.Orchestration/Helix.Orchestration.Agent/Transport/TcpTransport.cs`
- Devices implementation: `Helix.Orchestration/Helix.Orchestration.Devices/Transports/Tcp/TcpDeviceTransport.cs`
- Abstraction: `Helix.Orchestration/Helix.Orchestration.Devices/Transport/ITcpTransport.cs`, `Helix.Orchestration/Helix.Orchestration.Devices/Transports/Tcp/TcpTransportOptions.cs`

**Native C DLL (vendor libraries):**
- Used by: `Helix.Orchestration.Devices`
- Implementation: `Helix.Orchestration/Helix.Orchestration.Devices/Transports/CDll/CDllDeviceTransport.cs`
- Loads vendor-supplied native DLLs at runtime via `NativeLibrary.Load`; invokes exported functions through unmanaged delegates (`P/Invoke`)
- Windows-only: resolves architecture subfolder (`win-x64`, `win-x86`, `win-arm64`, `win-arm`)
- Options: `Helix.Orchestration/Helix.Orchestration.Devices/Transports/CDll/CDllTransportOptions.cs`

**Named device drivers (deleted from working tree, referenced in git history):**
- Knauer Azura (HPLC) — `Helix.Orchestration.Devices/Drivers/KnauerAzura.cs`
- Vapourtec R4 (flow chemistry reactor) — `Helix.Orchestration.Devices/Drivers/VapourtecR4.cs`
- Systag OPC-UA client — `Helix.Orchestration.Devices/Drivers/SystagOpcUaClient.cs`
- Device interfaces still present: `IHplc`, `IReactionPlatform`, `IVapourtec`, `ISetPointDevice` in `Helix.Orchestration/Helix.Orchestration.Devices/Interfaces/`

## Messaging / Event Bus

**MQTT:**
- Broker: Eclipse Mosquitto 2.0/2 (Docker container `helix-mosquitto`)
  - Plain TCP port: 1883
  - WebSocket port: 9001
  - Compose file: `Helix.Orchestration/docker-compose.yml`
- Client SDK: `MQTTnet` 4.x
- Producer/Consumer:
  - Agent: `Helix.Orchestration/Helix.Orchestration.Agent/Trends/MqttTrendIngestionService.cs` — subscribes to `helix/trends/#`; routes `spectral` and `diodearray` trend payloads
  - SiteServer: MqttBridge (config section `MqttBridge`) — connects to broker for site/fumehood telemetry
  - Recorder: `Helix.Orchestration/Helix.Orchestration.Recorder/Helix.Orchestration.Recorder.csproj` — MQTTnet dependency declared
- Topic convention: `helix/trends/{trendKind}/{trendName}` (e.g., `helix/trends/spectral/uv1`)
- Config keys: `Mqtt:BrokerHost`, `Mqtt:BrokerPort`, `Mqtt:TopicFilters`

**NATS:**
- SDK: `NATS.Net` 2.x
- Used by: `Helix.Orchestration.Core`
- Implementation files: not yet located in source (dependency declared in `Helix.Orchestration.Core.csproj`)

## Inter-Service Communication

**gRPC:**
- Proto definitions: `Helix.Orchestration/Helix.Orchestration.Contracts/Protos/`
  - `adapter.proto` — `DeviceAdapter` service: GetCapabilities, InvokeCapability, StreamTrends (server-side streaming), HealthCheck
  - `agent_control.proto` — `AgentControl` service: SubmitExperiment, PauseRunspace, ResumeRunspace, AbortRunspace, GetRunspaceStatus
- Server: `Helix.Orchestration.Agent` (`Grpc.AspNetCore` 2.x)
- Client: `Helix.Orchestration.SiteServer` (`Grpc.Net.Client` 2.x; configured via `AgentProxy:GrpcAddress`, default `http://localhost:5080`)
- Code generation: `Grpc.Tools` + `Google.Protobuf` in `Helix.Orchestration.Contracts`

## Databases / Storage

**PostgreSQL 16:**
- Purpose: Persistent store for campaigns, campaign iterations, and experiments
- Docker container: `helix-postgres` (database `helix_orchestration`, user `helix`)
- Compose file: `Helix.Orchestration/docker-compose.yml` (port 5432)
- Schema: `Helix.Orchestration/postgres/migrations/001_initial.sql`
  - Tables: `campaigns`, `campaign_iterations`, `experiments` (with JSONB columns)
- Connection string key: `ConnectionStrings:HelixOrchestration`
- Driver: `Npgsql` 9.x (`DbConnectionFactory` wraps `NpgsqlConnection`)
- ORM: `Dapper` 2.x
- Repositories:
  - `Helix.Orchestration/Helix.Orchestration.Infrastructure/Persistence/PostgresCampaignRepository.cs`
  - `Helix.Orchestration/Helix.Orchestration.Infrastructure/Persistence/PostgresExperimentRepository.cs`
- Registration: `InfrastructureServiceExtensions.AddHelixInfrastructure()` in `Helix.Orchestration/Helix.Orchestration.Infrastructure/Persistence/InfrastructureServiceExtensions.cs`
- Integration tests: `Testcontainers.PostgreSql` 4.x spins up real Postgres in `Helix.Orchestration.Infrastructure.Tests`

**Object Storage (MinIO / S3-compatible):**
- Purpose: Experiment data objects (binary blobs)
- Default bucket: `helix-experiments`
- Docker container: `helix-minio` (MinIO latest) in root `docker-compose.yml` (ports 9000/9001)
- SDK: `AWSSDK.S3` 4.0.23.4 — `AmazonS3Client` with `ForcePathStyle = true` for MinIO
- Implementation: `Helix.Orchestration/Helix.Orchestration.Infrastructure/Storage/MinioObjectStorageSink.cs`
- Interface: `Helix.Orchestration/Helix.Orchestration.Infrastructure/Storage/IObjectStorageSink.cs`
- Config section: `Minio` (ServiceUrl, AccessKey, SecretKey, Region, DefaultBucket, ForcePathStyle)
- Also compatible with AWS S3, Wasabi, and other S3-compatible services (comment in `MinioObjectStorageSink`)
- Registration: `InfrastructureServiceExtensions.AddMinioStorage()` in `Helix.Orchestration/Helix.Orchestration.Infrastructure/Persistence/InfrastructureServiceExtensions.cs`

**In-Memory (runtime only):**
- `InMemoryFunctionStore` (Cleipnir) — durable workflow state during Agent process lifetime
- `InMemoryTrendProvider` — active trend ring buffers in Agent
- `InMemoryDeviceLeasingService` — device lease registry in Agent
- `InMemoryGlobalStateStore` — global cross-workflow state in Agent

## Observability

**Structured Logging:**
- Framework: Serilog
- Format: CLEF (Compact Log Event Format JSON) via `Serilog.Formatting.Compact`
- Sinks: Console + rolling daily file (`logs/helix-agent-.log`)
- Integration: `Serilog.Extensions.Hosting` for DI-aware configuration
- Used by: `Helix.Orchestration.Agent`, `Helix.Orchestration.Recorder`

**OpenTelemetry:**
- SDK: `OpenTelemetry` 1.x + `OpenTelemetry.Extensions.Hosting` 1.x
- Used by: `Helix.Orchestration.Agent`
- Instrumented in: `Helix.Orchestration/Helix.Orchestration.Agent/Telemetry/HelixTelemetry.cs`
- Activity source name: `Helix.Orchestration.Agent`
- Custom metrics:
  - `helix.agent.trends.published` (counter)
  - `helix.agent.capability.invocations` (counter)
  - `helix.agent.runspace.completions` (counter)
  - `helix.agent.runspace.faults` (counter)
  - `helix.agent.adapters.health.failures` (counter)
- No exporter configured in source (OTLP exporter would be added via config)

## External APIs / Web

**REST / OpenAPI:**
- SiteServer exposes HTTP API documented via Swagger (`Swashbuckle.AspNetCore` 7.x)
- Agent exposes health endpoint at `GET /health` (ASP.NET Core health checks)
- No outbound HTTP client integrations detected in source

## WSL (Windows Subsystem for Linux)

**WSL Execution Adapter:**
- `Helix.Orchestration/Helix.Orchestration.Agent/Execution/WslExecutionAdapter.cs`
- Compiles and executes `WslDocument` (Workflow Specification Language) documents as durable runspaces
- Internal abstraction; not an external service but a named subsystem requiring WSL on the host

## CI/CD Infrastructure

**GitHub Actions:**
- Workflow: `.github/workflows/ci.yml`
- No external secrets or service connections configured in the workflow file
- NuGet packages restored from public nuget.org feed

---

*Integration audit: 2026-05-26*
