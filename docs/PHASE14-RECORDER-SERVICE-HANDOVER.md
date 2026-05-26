# Phase 14 — Recorder Service Handover

**Status**: Implemented  
**Date**: 2025-05-26  
**Phase Goal**: Provide standalone Worker Service for full-fidelity MQTT event capture to object storage (MinIO) in JSONL format.

---

## Scope

The **Recorder Service** is an optional infrastructure component that:

- Connects to the MQTT broker (Mosquitto) and subscribes to configurable topic filters (e.g., `helix/#`).
- Parses received messages into normalized `RecordedEvent` records.
- Batches events in-memory and flushes them to object storage (MinIO via S3-compatible API) as JSONL files.
- Runs as a standalone .NET 10 Worker Service, decoupled from Core and Agent.
- Enables future analytics, replay, and experiment reconstruction workflows without modifying the orchestration engine.

---

## Architecture

```
┌──────────────────┐
│  MQTT Broker     │
│  (Mosquitto)     │
└────────┬─────────┘
         │ helix/site-01/fumehood-02/exp-123/trends/Scalar/trend-456
         │ (MQTT messages on subscribed topics)
         v
┌──────────────────────────────────────────────────────────────────┐
│  Helix.Orchestration.Recorder (Worker Service)                   │
│                                                                    │
│  ┌────────────────────┐                                           │
│  │ MqttRecorderService│  (BackgroundService)                      │
│  │  - Connects to MQTT                                            │
│  │  - Subscribes to TopicFilters                                  │
│  │  - Parses to RecordedEvent                                     │
│  │  - Writes to Channel<RecordedEvent>                            │
│  │    (bounded, DropWrite on full)                                │
│  └────────┬───────────┘                                           │
│           │                                                        │
│           v                                                        │
│  ┌────────────────────┐                                           │
│  │RecorderWriterService│ (BackgroundService)                      │
│  │  - Reads from Channel                                          │
│  │  - Batches events                                              │
│  │  - Flushes to IObjectStorageSink                               │
│  │    (groups by experimentId, trendId)                           │
│  │  - Serializes as JSONL                                         │
│  └────────┬───────────┘                                           │
│           │                                                        │
│           v                                                        │
│  ┌────────────────────┐                                           │
│  │ IObjectStorageSink │ (MinioObjectStorageSink)                  │
│  │  - Infrastructure abstraction                                  │
│  │  - Writes to MinIO bucket                                      │
│  └────────────────────┘                                           │
└──────────────────────────────────────────────────────────────────┘
         │
         v
┌──────────────────┐
│  MinIO           │
│  (S3-compatible) │
│  Bucket:         │
│  helix-recorder  │
└──────────────────┘
```

---

## Configuration Reference

### appsettings.json

```json
{
  "Recorder": {
    "BrokerHost": "localhost",
    "BrokerPort": 1883,
    "TopicFilters": ["helix/#"],
    "ChannelCapacity": 4096,
    "FlushAfterRecordCount": 100,
    "FlushIntervalSeconds": 30
  },
  "Minio": {
    "Endpoint": "localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "BucketName": "helix-recorder",
    "UseSsl": false
  }
}
```

### docker-compose.yml

MinIO service added:

```yaml
minio:
  image: minio/minio:latest
  container_name: helix-minio
  command: server /data --console-address ":9001"
  ports:
    - "9000:9000"   # MinIO API
    - "9001:9001"   # MinIO Console
  environment:
    MINIO_ROOT_USER: minioadmin
    MINIO_ROOT_PASSWORD: minioadmin
  volumes:
    - minio_data:/data
  healthcheck:
    test: ["CMD", "curl", "-f", "http://localhost:9000/minio/health/live"]
    interval: 30s
    timeout: 10s
    retries: 3
  restart: unless-stopped
```

---

## Object Key Scheme

Flushed JSONL files follow this hierarchical structure:

```
experiments/{experimentId}/trends/{date:yyyy-MM-dd}/{trendId}/{batchTimestampUtc:yyyyMMddTHHmmssfffZ}.jsonl
```

**Example**:

```
experiments/exp-123/trends/2025-05-26/trend-456/20250526T143052123Z.jsonl
```

**Benefits**:

- **Partitioned by experiment**: Enables per-experiment analytics and replay.
- **Partitioned by date**: Supports efficient time-range queries and retention policies.
- **Partitioned by trend**: Groups related measurements for correlation analysis.
- **Timestamped batches**: Preserves flush order and enables incremental replay.

---

## Flush Policy

Events are batched in-memory and flushed when **either** condition is met:

1. **Record count threshold**: `FlushAfterRecordCount` events accumulated (default: 100).
2. **Time interval**: `FlushIntervalSeconds` elapsed since last flush (default: 30s).

**On flush**:

- Events are grouped by `(experimentId, trendId)`.
- Each group produces one JSONL object.
- JSONL format: one JSON object per line (newline-delimited JSON).

**On shutdown**:

- RecorderWriterService flushes any remaining events before stopping.

---

## JSONL Format Example

Each line is a complete `RecordedEvent` JSON object:

```jsonl
{"Topic":"helix/site-01/fumehood-02/exp-123/trends/Scalar/trend-456","ExperimentId":"exp-123","TrendId":"trend-456","TrendKind":"Scalar","ReceivedAt":"2025-05-26T14:30:52.123Z","PayloadJson":"{\"value\": 42.5, \"timestamp\": \"2025-05-26T14:30:52Z\"}"}
{"Topic":"helix/site-01/fumehood-02/exp-123/trends/Scalar/trend-456","ExperimentId":"exp-123","TrendId":"trend-456","TrendKind":"Scalar","ReceivedAt":"2025-05-26T14:30:53.456Z","PayloadJson":"{\"value\": 43.2, \"timestamp\": \"2025-05-26T14:30:53Z\"}"}
```

---

## Operational Notes

### Running the Recorder

```bash
# Start infrastructure (MQTT + MinIO)
docker-compose up -d

# Run Recorder service
dotnet run --project Helix.Orchestration.Recorder
```

### Accessing MinIO Console

Navigate to `http://localhost:9001` and log in with `minioadmin` / `minioadmin`.

### Verifying Recorded Data

1. Open MinIO console.
2. Browse `helix-recorder` bucket.
3. Navigate to `experiments/{experimentId}/trends/{date}/{trendId}/`.
4. Download and inspect JSONL files.

---

## Known Gaps and Future Work

### 1. Parquet Support

**Gap**: Current implementation writes JSONL only.

**Future**: Add Parquet writer behind `IObjectStorageSink` abstraction for:

- Columnar compression (10-50x smaller files).
- Native integration with analytics engines (Spark, DuckDB, Polars).
- Schema evolution support.

**Approach**:

- Introduce `ParquetObjectStorageSink` implementing `IObjectStorageSink`.
- Use Apache Arrow .NET bindings or Parquet.Net library.
- Configure via `appsettings.json` (`"StorageFormat": "Parquet"`).

---

### 2. Authentication and TLS for MinIO

**Gap**: Current configuration uses plaintext credentials and no TLS.

**Future**:

- Enable TLS for MinIO API (`UseSsl: true`).
- Use IAM roles or federated identity for production deployments.
- Rotate credentials via Azure Key Vault or AWS Secrets Manager.

**Security Recommendation**:

- Do not commit real credentials to source control.
- Use environment variables or secret management tools.

---

### 3. Replay Consumer

**Gap**: Recorder captures data but no consumer exists to replay it.

**Future**: Implement replay consumer that:

- Reads JSONL (or Parquet) from MinIO.
- Reconstructs `RecordedEvent` stream.
- Publishes back to MQTT or directly to Agent `ITrendProvider` mock.
- Supports time-shifted replay (e.g., 10x speed, specific time range).

**Use Cases**:

- Reproduce bugs from production experiments.
- Train ML models on historical data.
- Validate protocol changes against real-world sequences.

---

### 4. Authentication for MQTT Broker

**Gap**: Current Mosquitto configuration has no authentication.

**Future**:

- Enable username/password authentication in `mosquitto.conf`.
- Update `MqttRecorderService` to use credentials from configuration.
- Consider TLS for MQTT transport security.

---

### 5. Backpressure Handling

**Gap**: If MinIO writes are slow, the bounded channel may drop events.

**Mitigation**:

- Monitor `_logger.LogWarning("Channel is full...")` messages.
- Increase `ChannelCapacity` if drops occur.
- Add alerting for sustained drop rates.

**Future**:

- Implement overflow spillover to disk (local JSONL buffer).
- Retry failed MinIO writes with exponential back-off.

---

## Testing

### Unit Tests

- **RecordedEventParseTests**: Validates MQTT topic parsing and payload preservation.
- **RecorderWriterServiceTests**: Covers flush triggers, grouping, JSONL formatting, and empty-channel behavior.

### Integration Tests (Future)

- End-to-end test with real MQTT broker and MinIO container.
- Verify JSONL files are correctly written and readable.
- Test reconnect logic and shutdown flush.

---

## Dependencies

- **Helix.Orchestration.Infrastructure**: Provides `IObjectStorageSink` abstraction and `MinioObjectStorageSink` implementation.
- **MQTTnet 4.x**: MQTT client library.
- **Minio SDK 6.x**: S3-compatible client for MinIO.
- **Serilog**: Structured logging (compact JSON formatter for container environments).

---

## Related Documentation

- [CONCEPT.md](./CONCEPT.md) — Overall Helix architecture.
- [PHASE9-SITE-SERVER-HANDOVER.md](./PHASE9-SITE-SERVER-HANDOVER.md) — MQTT/NATS trend flow.
- [PHASE12-POSTGRESQL-PERSISTENCE-HANDOVER.md](./PHASE12-POSTGRESQL-PERSISTENCE-HANDOVER.md) — Persistence layer design.

---

## Summary

Phase 14 delivers a **standalone Recorder Worker Service** that captures full-fidelity MQTT events to MinIO object storage in JSONL format. The architecture is decoupled from Core/Agent, configurable, and designed for future Parquet and replay extensions. Tests validate parsing, batching, and flush behavior. Production deployments should add TLS, authentication, and monitoring for dropped events.
