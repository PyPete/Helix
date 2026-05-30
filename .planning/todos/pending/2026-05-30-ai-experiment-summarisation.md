---
created: 2026-05-30T19:30:00Z
title: AI Experiment Summarisation Engine
area: api
files:
  - Helix.Orchestration.Agent/Api/RunspaceEndpoints.cs
  - Helix.Orchestration.Infrastructure/Repositories/IExperimentRepository.cs
  - helix-desktop/src/App.tsx
---

## Problem

After an experiment completes, scientists must manually correlate planned protocols, event logs, trend data, and device interactions to understand what happened. This is time-consuming, error-prone, and produces inconsistent records across different operators. There is no structured, reproducible way to generate a scientific summary from the raw experiment data, and no machine-learning-ready feature extraction for optimisation loops.

## Solution

An **AI-powered experiment summarisation engine** that consumes the unified experiment timeline (from the Record/Replay system) and generates structured scientific summaries with ML feature vectors.

The engine is a prompt-driven LLM call with a well-defined input schema and output format. It is deterministic in structure (always produces the same sections) but contextually rich in content (tailored to experiment type, trend patterns, and deviations).

---

### System Prompt

The following is the validated system prompt for the summarisation engine:

```
You are an expert scientific summarisation engine. Your task is to generate a clear,
accurate, and structured summary of an experiment based on the data provided.

The experiment is defined by:
- experimentId: <ID>
- experimentType: <crystallisation | reaction | filtration | distillation | analysis | etc>
- phases: preExperiment, mainExperiment, postExperiment (each optional except main)
- plannedProtocol: the intended workflow steps
- eventProtocol: the actual executed steps as a chronological event stream
- trendData: time-series data for raw and derived trends
- customTrends: user-defined trends (local maths or Python-based)
- devices: devices used, their capabilities, and any offline/online transitions
- metadata: timestamps, user, environment, parameters, batch info

Your job is to:
1. Identify the experiment type and describe the scientific intent.
2. Summarise the pre-experiment phase (if present):
   - purpose (e.g., cleaning, calibration, charging reagents)
   - what was planned vs what occurred
   - any deviations or warnings
3. Summarise the main experiment phase:
   - high-level description of what was done
   - key steps from the planned protocol
   - what actually happened in the event protocol
   - important device actions and capability invocations
   - loop iterations, branch decisions, retries, or safety triggers
   - any deviations from the plan
4. Summarise the post-experiment phase (if present):
   - quenching, cleaning, shutdown, analysis, etc
   - planned vs executed
5. Use trend data to describe:
   - important changes in temperature, pressure, flow, mass, spectra, etc
   - any custom trends (e.g., ratios, PCA scores, baseline-corrected signals)
   - anomalies, spikes, plateaus, or unexpected behaviour
6. Highlight deviations:
   - differences between planned and executed protocol
   - device offline events
   - missing data
   - safety envelope triggers
7. Provide a concise narrative of the full experiment timeline.
8. Provide a short "Outcome" section:
   - final state
   - success/failure
   - yield or endpoint (if available)
   - recommended next steps or follow-up experiments
9. Provide a "Machine-Learning Features" section:
   - key numeric values
   - durations
   - peak values
   - derived metrics
   - anything useful for optimisation or modelling
10. Ensure the summary is factual, chronological, and scientifically grounded.

Input data will be provided in the following structure:
{
  "experimentId": "...",
  "experimentType": "...",
  "preExperiment": {...},
  "mainExperiment": {...},
  "postExperiment": {...},
  "plannedProtocol": [...],
  "eventProtocol": [...],
  "trendData": {...},
  "customTrends": {...},
  "devices": {...},
  "metadata": {...}
}

Output format:
- Title
- Experiment Type
- Summary of Pre-Experiment Phase
- Summary of Main Experiment Phase
- Summary of Post-Experiment Phase
- Trend Interpretation
- Deviations & Issues
- Final Outcome
- Machine-Learning Features (bullet list)
- Short Narrative Summary (3-5 sentences)

Be concise, accurate, and scientifically rigorous.
```

---

### Input Schema

The summarisation engine receives a single JSON object assembled from the Record/Replay timeline and protocol documents:

```typescript
interface SummarisationInput {
  experimentId: string;
  experimentType: string;            // crystallisation | reaction | filtration | distillation | analysis | ...

  preExperiment?: ExperimentPhase;   // optional
  mainExperiment: ExperimentPhase;   // required
  postExperiment?: ExperimentPhase;  // optional

  plannedProtocol: PlannedProtocol;  // from protocol standardisation model
  eventProtocol: EventProtocol;      // from record/replay timeline

  trendData: {
    [trendName: string]: TrendSeries;  // raw + derived trends; validated names
  };
  customTrends: {
    [trendName: string]: TrendSeries;  // custom/ and python/ namespace trends
  };

  devices: {
    [deviceId: string]: {
      capabilities: string[];
      statusHistory: DeviceStatusEvent[];  // online/offline transitions
    };
  };

  metadata: {
    operator: string;
    projectCode: string;
    startedAt: string;
    completedAt: string;
    agentVersion: string;
    deviceFirmwareVersions: Record<string, string>;
    batchInfo?: Record<string, string>;
    environmentContext?: Record<string, string>;
  };
}
```

### Output Schema

```typescript
interface SummarisationOutput {
  title: string;
  experimentType: string;
  preExperimentSummary?: string;
  mainExperimentSummary: string;
  postExperimentSummary?: string;
  trendInterpretation: string;
  deviationsAndIssues: string;
  finalOutcome: {
    state: "completed" | "faulted" | "aborted" | "partial";
    success: boolean;
    yield?: string;
    endpoint?: string;
    nextSteps?: string;
  };
  mlFeatures: MLFeature[];
  narrativeSummary: string;          // 3-5 sentences
  generatedAt: string;
  modelVersion: string;
}

interface MLFeature {
  name: string;
  value: number | string;
  unit?: string;
  source: "trend" | "protocol" | "derived";
}
```

---

### Assembly Pipeline (Agent → LLM → Storage)

```
Runspace completes
  ↓
EventProtocolService.BuildSummarisationInput(runspaceId)
  ├── Load PlannedProtocol from runspace creation record
  ├── Load EventProtocol from DurableRunspace
  ├── Load TrendData from TrendRegistry (aggregated statistics per step)
  ├── Load DeviceStatusHistory from AgentService log
  └── Load Metadata from ExperimentContext
  ↓
SummarisationService.SummariseAsync(input)
  ├── Serialise input to JSON
  ├── Call LLM API (Claude) with system prompt + input
  ├── Parse structured output
  └── Validate: all required sections present, MLFeatures non-empty
  ↓
Store summary:
  ├── PostgreSQL: ExperimentSummary table (structured fields + full JSON)
  ├── MinIO: experiments/{runspaceId}/summary.json
  └── Publish event: SummaryReadyEvent via IRunspaceEventEmitter
```

**Trigger options:**
- Automatic on runspace completion (default)
- Manual: `POST /experiments/{runspaceId}/summarise` (re-run if model improves)
- Batch: `POST /experiments/summarise-batch` with list of runspace IDs

---

### UI Integration

**Experiment history browser** (Phase 7 Site Dashboard):
- Summary card per experiment: narrative summary + outcome badge (✓ / ✗) + ML feature table
- "View full summary" expands to all sections
- Export as PDF or structured JSON

**Desktop app** (Phase 6 extension):
- Summary panel appears after runspace completes
- Shows narrative summary + outcome immediately
- "Open in browser" links to full summary page in Site Dashboard

**Python client:**
```python
run.summary()           # SummarisationOutput for this run
run.ml_features()       # dict[str, float] — ML feature vector
client.summaries(projectCode="HEL-042")  # bulk retrieve summaries for a project
```

---

### ML Features — Design Intent

The ML Features section is the primary interface to the Python optimisation loop. It extracts everything an Ax/BoTorch objective function needs:

**Standard features extracted for all experiment types:**
- `duration_total_s` — total experiment duration
- `duration_main_s` — main phase duration only
- `fault_count` — number of step faults
- `deviation_count` — number of planned vs. actual deviations
- `loop_iterations_actual` — actual loop count vs. planned

**Trend-derived features (per trend, per step):**
- `{trendName}.mean_during_{stepId}` — mean value during a specific step
- `{trendName}.max_overall` — global maximum across entire experiment
- `{trendName}.time_to_setpoint_s` — time from step start to first setpoint value
- `{trendName}.stability_s` — duration within ±5% of setpoint (configurable tolerance)

**Custom trend features:**
- All `custom/` and `python/` namespace trends included as features automatically
- e.g., `custom/conversion_rate.mean`, `python/pls_concentration.max`

**Outcome features:**
- `yield_percent` (if present in metadata or custom trends)
- `endpoint_reached` (bool → 0/1)
- `safety_triggers` (count)

---

### Prompt Engineering Notes

- System prompt is versioned and stored in `prompts/summarisation-v1.md`
- Prompt version is recorded in `SummarisationOutput.modelVersion`
- Temperature: 0.2 (low — we want factual, structured output, not creative variation)
- Max tokens: 2000 (covers all sections with room for detail)
- If LLM output fails schema validation: retry once with explicit schema reminder; if still fails, store raw output with `parsed: false` flag and surface to user for manual review
- Structured output mode (JSON mode) enforced where supported by the model API

---

## Implementation hints

**Phase A — Input assembly:**
- `EventProtocolService.BuildSummarisationInput()` — pure function assembling input from existing data sources
- No LLM call yet; outputs structured JSON for testing the schema
- Unit-testable: given a known timeline, assert the input JSON structure

**Phase B — LLM integration:**
- `ISummarisationService` interface + `ClaudeSummarisationService` implementation
- Prompt versioning: load from file, not hardcoded string
- Output parsing + schema validation
- Retry logic for schema failures

**Phase C — Storage + REST API:**
- PostgreSQL `experiment_summaries` table
- MinIO storage
- `POST /experiments/{runspaceId}/summarise` endpoint
- `GET /experiments/{runspaceId}/summary` endpoint

**Phase D — UI:**
- Summary panel in desktop app (post-completion)
- Experiment history browser in Site Dashboard (Phase 7)
- PDF export

**Cross-links:**
- Record/Replay: `2026-05-30-record-replay-system.md` (timeline is the data source)
- Protocol model: `2026-05-30-protocol-standardisation-trend-naming.md` (PlannedProtocol + EventProtocol are inputs)
- Trend naming: `2026-05-30-protocol-standardisation-trend-naming.md` (trendData keys use validated names; ML features inherit the namespace)
- Python client: `2026-05-30-python-workflow-interface.md` (run.summary(), run.ml_features(), optimisation loop consumes ML features)
- Real-time trends: `2026-05-30-real-time-trend-system.md` (custom/ and python/ namespace trends appear in customTrends input field)
