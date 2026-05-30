---
created: 2026-05-30T19:25:00Z
title: Record/Replay System (Unified Experiment Timeline)
area: api
files:
  - Helix.Orchestration.Recorder/Program.cs
  - Helix.Orchestration.Agent/Events/RunspaceEvent.cs
  - Helix.Orchestration.Agent/AgentService.cs
  - Helix.Orchestration.Agent/Streaming/TrendAggregator.cs
  - Helix.Orchestration.Infrastructure/Repositories/IExperimentRepository.cs
  - helix-desktop/src/hooks/useAgentWebSocket.ts
  - helix-desktop/src/components/TrendCharts/ScalarTrendChart.tsx
---

## Problem

Experiments currently produce disconnected data: trend values in MQTT topics, runspace events in a WebSocket stream, and workflow metadata in the Recorder JSONL pipeline. There is no way to reconstruct exactly what happened during an experiment, replay it for debugging or simulation, or generate a complete audit trail. Scientists cannot answer "what were the temperature and pressure values at the moment step 3 faulted?" without manually correlating three separate data sources.

## Solution

**Unified experiment timeline** with deterministic record and replay. Every experiment writes one timeline; replay reconstructs it exactly without touching devices.

---

### The Unified Timeline

A timeline is an ordered sequence of **TimelineEntries** with monotonic timestamps:

```
t=0ms      RunspaceStarted        { runspaceId, workflowId, operator }
t=312ms    StepStarted            { stepId="heat", capability="vapourtec_r4.set_temperature", params={value:80} }
t=850ms    TrendSample            { name="vapourtec_r4.temperature.pv", value=22.3, unit="°C" }
t=1700ms   TrendSample            { name="vapourtec_r4.temperature.pv", value=45.1, unit="°C" }
t=2100ms   DeviceInteraction      { deviceId="vapourtec_r4", command="SET_TEMPERATURE", payload="80\r", responseMs=45 }
t=312000ms StepCompleted          { stepId="heat", outcome="completed" }
t=312100ms StepStarted            { stepId="wait", capability="delay", params={seconds:300} }
...
t=624000ms RunspaceCompleted      { outcome="completed", durationMs=624000 }
```

**Five event categories recorded:**

| Category | Examples | Source |
|---|---|---|
| Continuous trends | Temperature, pressure, flow, absorbance | `TrendAggregator` at configured Hz |
| Discrete events | Step started/completed/faulted, runspace lifecycle | `RunspaceEvent` frames |
| Workflow metadata | Planned protocol, schema, operator, project code | At runspace creation |
| Device interactions | Every command sent to a device + response + latency | Device adapters (opt-in) |
| Environment context | Agent version, device firmware versions, config hash | At runspace creation |

---

### Record Mode

**Activation:** Always-on for all runspaces (not opt-in). Recording is an invariant of running an experiment.

**Storage pipeline:**

```
Agent (in-process)
  ├── TrendAggregator → TrendSample events
  ├── RunspaceEventEmitter → lifecycle events
  └── DeviceInteractionLogger → command/response pairs (opt-in per adapter)
          ↓ (all events → ITimelineWriter)
Recorder Service (out-of-process)
  ├── MQTT subscriber (existing pipeline)
  ├── TimelineSerializer → JSONL timeline file (one per runspace)
  └── TimelineIndex → PostgreSQL: (runspaceId, protocolId, startedAt, outcome, filePath)
```

**Timeline file format (JSONL):**
- One JSON object per line, chronological order
- File stored in MinIO: `timelines/{runspaceId}.jsonl.gz`
- Index entry in PostgreSQL: metadata + S3 path for retrieval

**Trend sampling rate:**
- Default: 1 Hz for all active trends
- Configurable per trend: `{ "trendId": "vapourtec_r4.temperature.pv", "recordHz": 10 }`
- Burst mode: during step execution, rate increases to configured `burstHz` (default 5 Hz)
- Idle mode: between steps, rate drops to `idleHz` (default 0.2 Hz)

**Device interaction recording:**
- Opt-in at adapter level: `RecordInteractions = true` in adapter config
- Records raw bytes sent/received + timestamp + latency
- Useful for debugging device communication issues in replay
- Not recorded by default (high volume for frequent-polling adapters)

---

### Replay Mode

**Replay reconstructs the experiment timeline deterministically. No devices are contacted.**

**Replay inputs:**
- `runspaceId` → load `timelines/{runspaceId}.jsonl.gz` from MinIO
- Optional: `startAt` and `endAt` timestamps for partial replay
- Optional: `speed` multiplier (1.0 = real-time, 10.0 = 10× faster, 0 = step-mode)

**Replay outputs (same interface as live execution):**
- WebSocket `/ws` emits the same `RunspaceEvent` and `TrendValueEvent` frames in timeline order
- The desktop UI is unmodified — it receives replay frames identically to live frames
- A replay session is a special runspace with `{ "source": "replay", "originalRunspaceId": "..." }`

**Replay engine contract:**
```csharp
public interface IReplayEngine
{
    IAsyncEnumerable<TimelineEntry> Replay(
        string runspaceId,
        ReplayOptions options,          // speed, startAt, endAt, stepMode
        CancellationToken ct);
}
```

The replay engine reads the JSONL file, applies the speed multiplier to inter-event delays, and emits entries. Consumers (WebSocket broadcaster, trend charts) receive them as if live.

**Replay does NOT:**
- Send commands to physical devices
- Write new timeline entries (replay is read-only)
- Affect the live Agent state (isolated replay session)

---

### Replay Use Cases

**1. Debugging — step through a faulted workflow**
- Load replay for a faulted runspace
- Set `speed=0` (step mode)
- Navigate forward/backward one event at a time
- Inspect trend values, device interactions, and step parameters at the moment of fault

**2. Simulation — test a new workflow against recorded device responses**
- Replay a past timeline to "simulate" what a new workflow would see
- Use recorded device interaction responses as a mock transport layer
- Compare planned vs. simulated execution without physical devices

**3. Trend scrubbing**
- UI timeline scrubber: drag to a timestamp, charts update to show trend values at that moment
- Equivalent to `git blame` for experiment data

**4. Optimisation**
- Replay N past experiments to extract trend summaries as `optim/` namespace values
- Feed into Ax/BoTorch without re-running experiments
- `run.trends(pattern, source="replay", runspaceIds=[...])` in the Python client

**5. Experiment summaries and reports**
- Replay at high speed (100×) to generate automation summary and statistical report
- Extract: step durations, trend statistics (mean/stddev/min/max per step), fault sequence, parameter deviations vs. planned

**6. Audit trail**
- Regulatory submission: export timeline as signed PDF or structured data package
- Immutable: timeline files are write-once in MinIO (bucket policy: `WORM`)

---

### Timeline Scrubber (Desktop UI)

A new UI component below the trend charts:

```
|←  ‖  →|  [══════════════●════════════════]  10:42:15 / 11:14:03
Step:  heat (set_temperature=80°C)
Trend: vapourtec_r4.temperature.pv = 79.8°C
```

- **Scrub bar:** drag to any timestamp in the timeline
- **Step indicator:** shows which workflow step was active at the scrubbed position
- **Play/pause/step-forward/step-back** controls
- **Speed selector:** 0.5×, 1×, 5×, 10×, 100×
- Trend charts update as the scrubber moves (same `ScalarTrendChart` component, different data source)

---

### Replay API (Agent REST)

```
POST /replay
  Body: { runspaceId, speed?, startAt?, endAt?, stepMode? }
  Returns: { replaySessionId }  → connects to /ws as a replay session

GET  /replay/{sessionId}/status
POST /replay/{sessionId}/step          (step-mode: advance one event)
POST /replay/{sessionId}/seek          Body: { timestamp }
POST /replay/{sessionId}/stop

GET  /experiments/{runspaceId}/timeline
  Returns: paginated TimelineEntry[] for a given runspaceId

GET  /experiments/{runspaceId}/summary
  Returns: automation summary (Compare planned vs. event protocol)
```

---

### Storage Sizing

Rule of thumb for timeline storage:
- 1 Hz trend sampling, 4 active trends, 1-hour experiment → ~14,400 TrendSample entries → ~1.4 MB uncompressed, ~150 KB gzipped
- 10 Hz burst, 10 active trends, 1-hour experiment → ~360,000 entries → ~36 MB uncompressed, ~4 MB gzipped
- Device interaction logging adds ~100 bytes per command, typically 1–5 commands per second → ~360–1800 KB/hour

MinIO lifecycle policy: compress after 24h, archive to cold storage after 90 days, delete after retention period (configurable per schema).

---

## Implementation hints

**Phase A — Record (Recorder Service + timeline writer):**
- `TimelineEntry` discriminated union type (all five categories)
- `ITimelineWriter` interface + JSONL implementation
- Recorder Service subscribes to Agent MQTT + WebSocket, writes JSONL per runspace
- MinIO upload on runspace completion; PostgreSQL index entry

**Phase B — Replay engine (Agent):**
- `IReplayEngine` + `ReplayEngine` implementation
- `POST /replay` endpoint; replay session emits via existing WebSocket broadcaster
- Step mode: replay pauses until `POST /replay/{sessionId}/step` is called

**Phase C — Desktop UI (scrubber):**
- `TimelineScrubber` component with play/pause/seek controls
- Trend charts accept a `dataSource: "live" | "replay"` prop; replay source reads from replay session WebSocket
- Step indicator overlay on charts showing step boundaries

**Phase D — Python client integration:**
- `run.replay(speed, startAt, endAt)` → `ReplayHandle` with same interface as `RunHandle`
- `HelixClient.replay(runspaceId)` → `ReplayHandle`
- Bulk replay for optimisation: `await asyncio.gather(*[client.replay(rid).trends("*") for rid in run_ids])`

**Cross-links:**
- Protocol model: `2026-05-30-protocol-standardisation-trend-naming.md` (Event Protocol is a high-level summary derived from the timeline; the timeline is the raw record)
- Trend naming: `2026-05-30-protocol-standardisation-trend-naming.md` (TrendSample entries use validated trend names; `GET /experiments/{id}/timeline` is filterable by trend name)
- Python client: `2026-05-30-python-workflow-interface.md` (replay mode for optimisation)
- Deployment: `2026-05-30-deployment-strategy-lab.md` (Recorder Service as a Windows Service; MinIO bucket WORM policy)
- Graceful degradation: `2026-05-30-graceful-device-loss-removal.md` (timeline captures device-offline events; replay correctly shows the degraded state)
