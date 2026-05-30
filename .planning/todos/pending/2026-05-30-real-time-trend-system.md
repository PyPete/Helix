---
created: 2026-05-30T18:55:00Z
title: Real-Time Trend System (Local + Python Engines)
area: ui
files:
  - Helix.Orchestration.Agent/Streaming/TrendAggregator.cs
  - Helix.Orchestration.Agent/Trends/InMemoryTrendProvider.cs
  - Helix.Orchestration.Agent/Events/RunspaceEvent.cs
  - Helix.Orchestration.Agent/AgentService.cs
  - helix-desktop/src/components/TrendCharts/ScalarTrendChart.tsx
  - helix-desktop/src/components/TrendCharts/SpectralChart.tsx
  - helix-desktop/src/hooks/useAgentWebSocket.ts
---

## Problem

The current trend system (Phase 6) streams raw device measurements as scalar `TrendValueEvent` frames. There is no way to define custom derived trends (e.g. concentration from absorbance, conversion rate, temperature delta), no support for complex transforms (ML inference, PCA, spectral deconvolution), and no mechanism to use custom trends as workflow inputs or safety interlock values.

## Solution

**Two-engine architecture** — both engines produce first-class trend values that are indistinguishable at the subscription layer.

---

### Engine 1: Local Agent Trend Engine

Runs inside the Agent process. Handles simple maths that must be fast, safe, and synchronous with workflow execution.

**Capabilities:**
- Arithmetic expressions over raw device values: `(A - B) / C`, running average, rate-of-change
- Threshold alerts as trend-derived safety values
- Moving window statistics (min/max/stddev over N samples)
- Unitless dimensionless ratios

**Definition format (JSON):**
```json
{
  "id": "conversion-rate",
  "name": "Conversion Rate",
  "expression": "(feed_flow - product_flow) / feed_flow * 100",
  "inputs": ["vapourtec_r4.flow_feed", "vapourtec_r4.flow_product"],
  "unit": "%",
  "engine": "local"
}
```

**Integration:**
- Trend definitions loaded from `trends/` directory or via REST `POST /trends`
- `TrendAggregator` evaluates expressions on each raw value update
- Output published as `TrendValueEvent` on WebSocket — same frame as raw device trends
- Usable in WSL workflow steps as capability inputs
- Subscribable by safety engine for interlock rules

---

### Engine 2: Python Sidecar

Separate process (Docker container or local subprocess) for complex transforms that would be unsafe or too slow to run in the Agent process.

**Capabilities:**
- Chemometrics (PCA, PLS, calibration curves)
- UV/Vis spectral deconvolution and peak detection
- ML inference (scikit-learn, ONNX models)
- Custom Python functions with numpy/scipy

**Communication protocol:**
- Agent → Sidecar: raw data frames over local gRPC or named pipe
- Sidecar → Agent: derived trend frames, same JSON schema as Engine 1 output
- Health: Agent polls `/health` on the sidecar; marks sidecar trends unavailable if sidecar is offline

**Definition format (JSON):**
```json
{
  "id": "uv-pca-score",
  "name": "UV PCA Score 1",
  "script": "transforms/uv_pca.py",
  "inputs": ["uv_detector.spectrum"],
  "output_unit": "AU",
  "engine": "python"
}
```

**Sidecar contract:**
- Stateless per-call: no side effects beyond returning a value
- Agent retains full authority over logging, workflow integration, and safety evaluation
- Python code cannot block the Agent event loop

---

### Custom Trends as First-Class Citizens

Both engine outputs are **indistinguishable** from raw device trends at the subscription layer:

| Property | Raw Device Trend | Local Engine Trend | Python Sidecar Trend |
|---|---|---|---|
| WebSocket frame | `TrendValueEvent` | `TrendValueEvent` | `TrendValueEvent` |
| Subscribable in workflows | Yes | Yes | Yes |
| Loggable to MQTT/MinIO | Yes | Yes | Yes |
| Usable in safety interlocks | Yes | Yes | Yes (with staleness guard) |
| UI chart | Yes | Yes | Yes |
| Trend library browse | Yes | Yes | Yes |

**Workflow integration:** a WSL `WaitFor` step can block on `{ "trend": "conversion-rate", "condition": ">= 95" }` — works for both local and Python-derived trends.

---

### Trend Definition Storage & Orchestration

- Definitions stored as JSON files in `trends/` (Agent project directory) or in a `trend_definitions` Postgres table
- Agent orchestrates evaluation: subscribes to required raw inputs, evaluates on update, publishes output
- REST API:
  - `GET /trends` — list all defined trends (raw + derived) with last-known value
  - `POST /trends` — register a new trend definition at runtime
  - `DELETE /trends/{id}` — remove a trend definition
  - `GET /trends/{id}/history` — time-series data from Recorder

---

## Implementation hints

Multi-phase work. Suggested split:

**Phase A (Agent-side, Local Engine):**
- `ITrendDefinition` interface + JSON deserializer
- Expression evaluator (use `NCalc` or `MathParser` NuGet, or a simple hand-rolled evaluator for safety)
- `DerivedTrendService` subscribing to `TrendAggregator` output and publishing derived values
- REST endpoints for `GET /trends` and `POST /trends`

**Phase B (Python Sidecar):**
- Sidecar scaffold (FastAPI or gRPC server)
- Agent `PythonSidecarClient` bridging local Engine 1 results to the sidecar
- Docker Compose service definition

**Phase C (UI):**
- Trend library browser in helix-desktop (list all trends, toggle visibility on charts)
- Custom trend builder UI (expression editor for Local Engine)
- Sidecar script upload/management

**Key dependency:** Requires `CapabilityRegistry` to expose raw device trend names so the expression evaluator knows valid input identifiers. See todo: `2026-05-30-multi-mode-workflow-authoring.md` (CapabilityRegistry schema introspection gap).
