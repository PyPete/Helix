# Phase 14 — Recorder Service (Standalone Worker + JSONL/MinIO) Handover

## Status
- Phase: **Completed**
- Scope: **Infrastructure-only recorder** (no Core/Agent integration contract in this phase)
- Runtime target: **.NET 10**

## Objective
Deliver a standalone recorder process that captures MQTT trend traffic and persists full-fidelity event data to object storage as JSONL batches.

## Delivered Components

### 1) Standalone recorder host
- New worker project: `Helix.Orchestration.Recorder`
- Uses `BackgroundService` implementations for ingestion and writing pipelines
- Structured logging via Serilog

### 2) Object storage abstraction in Infrastructure
- `IObjectStorageSink` added in `Helix.Orchestration.Infrastructure/Storage`
- `MinioObjectStorageSink` implemented with bucket auto-create check
- `MinioOptions` added for configuration binding
- DI extension added: `AddMinioStorage(IConfiguration)`

### 3) MQTT ingestion pipeline
- `MqttRecorderService : BackgroundService`
- Connects to configurable broker host/port
- Subscribes to configurable topic filters (`Recorder:TopicFilters`)
- Parses topic/payload into `RecordedEvent`
- Writes to bounded `Channel<RecordedEvent>`
- Drop-on-full behavior with warning logs

### 4) Batch writer pipeline
- `RecorderWriterService : BackgroundService`
- Reads from channel and batches events
- Flushes on either condition:
  - `FlushAfterRecordCount`
  - `FlushIntervalSeconds`
- Groups by `(ExperimentId, TrendId)` per flush
- Serializes one JSON object per line (JSONL)
- Writes each grouped object via `IObjectStorageSink`

### 5) Recorder configuration model
- `RecorderOptions` with defaults:
  - `BrokerHost = localhost`
  - `BrokerPort = 1883`
  - `TopicFilters = ["helix/#"]`
  - `ChannelCapacity = 4096`
  - `FlushAfterRecordCount = 100`
  - `FlushIntervalSeconds = 30`

### 6) Local infrastructure support
- `docker-compose.yml` updated with MinIO service
  - API: `9000`
  - Console: `9001`
  - Volume: `minio_data`
  - Liveness healthcheck enabled

### 7) Tests
- New test project: `tests/Helix.Orchestration.Recorder.Tests`
- xUnit + Moq
- Coverage includes:
  - `RecordedEvent` parsing behavior
  - Writer flush behavior (count/time)
  - Grouped object writes by `(ExperimentId, TrendId)`
  - Empty-channel no-write behavior
  - JSONL output line-count validation

## Data Path

```
MQTT (helix/#)
  -> MqttRecorderService
  -> Channel<RecordedEvent>
  -> RecorderWriterService (batch + group)
  -> IObjectStorageSink
  -> MinIO bucket (JSONL objects)
```

## Object Key Scheme

Objects are written under:

`experiments/{experimentId}/trends/{yyyy-MM-dd}/{trendId}/{yyyyMMddTHHmmssfffZ}.jsonl`

This supports partitioning by experiment, day, and trend stream.

## Configuration Reference

### Recorder section
- `Recorder:BrokerHost`
- `Recorder:BrokerPort`
- `Recorder:TopicFilters[]`
- `Recorder:ChannelCapacity`
- `Recorder:FlushAfterRecordCount`
- `Recorder:FlushIntervalSeconds`

### Minio section
- `Minio:Endpoint`
- `Minio:AccessKey`
- `Minio:SecretKey`
- `Minio:BucketName`
- `Minio:UseSsl`

## Operational Notes
- Recorder is intentionally decoupled from `Helix.Orchestration.Core` and Agent execution contracts in this phase.
- Current persistence format is JSONL-first to prioritize observability and replay readiness.
- `IObjectStorageSink` keeps storage backend and future format evolution swappable.

## Known Gaps / Next Phase Candidates
1. Parquet writer path (columnar analytics optimization)
2. MinIO/S3 auth hardening and TLS defaults for production
3. Replay consumer service (JSONL/Parquet -> timed publish)
4. Optional dead-letter / disk spill strategy for prolonged backpressure

## Files Added / Updated (Phase 14)
- `Helix.Orchestration.Infrastructure/Storage/IObjectStorageSink.cs`
- `Helix.Orchestration.Infrastructure/Storage/MinioOptions.cs`
- `Helix.Orchestration.Infrastructure/Storage/MinioObjectStorageSink.cs`
- `Helix.Orchestration.Infrastructure/Persistence/InfrastructureServiceExtensions.cs`
- `Helix.Orchestration.Recorder/*`
- `tests/Helix.Orchestration.Recorder.Tests/*`
- `docker-compose.yml`
- `Helix.Orchestration.slnx`

## Summary
Phase 14 introduces a production-aligned recorder foundation: standalone worker hosting, configurable MQTT capture, batched JSONL object persistence to MinIO via Infrastructure abstraction, and baseline unit-test coverage. The implementation is intentionally isolated from Core runtime contracts to keep orchestration execution unchanged while enabling full-fidelity recording for replay and analytics paths.