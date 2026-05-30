---
created: 2026-05-30T19:00:00Z
title: Python as a First-Class Workflow Interface
area: api
files:
  - Helix.Orchestration.Agent/Api/WorkflowEndpoints.cs
  - Helix.Orchestration.Agent/Api/RunspaceEndpoints.cs
  - Helix.Orchestration.Agent/Events/RunspaceEvent.cs
---

## Problem

The current workflow authoring surface is the Tauri desktop UI. Computational scientists (chemists running optimisation experiments, data scientists building automated screening campaigns) want to drive Helix programmatically from Python — building workflows, submitting them, monitoring execution, and feeding results into optimisation loops — without touching the GUI.

## Solution

**Python as a first-class workflow interface.** Three authoring paths, one execution path.

---

### Path 1: WSL JSON Generation (raw)

Direct Python dict/dataclass construction that produces a valid `WslDocument`.

```python
from helix import WslDocument, StepNode, Edge

doc = WslDocument(
    document_id="my-workflow",
    nodes=[
        StepNode(id="heat", capability="vapourtec_r4.set_temperature", params={"value": 80}),
        StepNode(id="wait", capability="delay", params={"seconds": 300}),
        StepNode(id="sample", capability="knauer_azura.inject", params={"volume_ul": 10}),
    ],
    edges=[Edge("heat", "wait"), Edge("wait", "sample")]
)
response = helix.submit(doc)
```

Lowest-level path. Full control. No magic.

---

### Path 2: Python DSL

Fluent builder that feels native to Python scientists. Generates WSL JSON internally.

```python
from helix.dsl import workflow, step, delay

@workflow("screen-conditions")
def my_workflow(temperature: float, flow_rate: float):
    step("vapourtec_r4.set_temperature", value=temperature)
    delay(seconds=300)
    step("knauer_azura.inject", volume_ul=10)
    step("uv_detector.acquire", duration_s=60)

# Submit with parameter bindings
run = my_workflow.submit(temperature=80, flow_rate=1.5)
run.wait()
results = run.trends("uv_detector.absorbance_280nm")
```

**Key properties:**
- Decorator-based: `@workflow` wraps a Python function and captures `step()` / `delay()` / `branch()` calls
- Parameters injected at submit time → generates a concrete `WslDocument` per run
- `run.wait()` blocks until the runspace reaches `Completed` or `Faulted` state
- `run.trends(name)` returns a pandas DataFrame of recorded trend values

---

### Path 3: Templated Workflows

For teams that define canonical protocols as YAML/JSON templates and instantiate them with parameters.

```python
from helix import WorkflowTemplate

template = WorkflowTemplate.load("protocols/screen_temperature.json")
run = template.submit(temperature=80, flow_rate=1.5, solvent="MeOH")
```

Templates are stored as parameterised WSL JSON with `{{temperature}}` placeholders. The Python client resolves substitutions before submitting. Templates can be version-controlled alongside the lab protocol SOPs.

---

### Execution Interface (common to all paths)

All three paths submit to the same `POST /workflows/compile` → `POST /runspaces` sequence.

```python
# Submit
run = helix.submit(doc)          # → POST /compile → POST /runspaces

# Monitor
run.status                       # polls GET /runspaces/{id}
run.events                       # list of RunspaceEvents received so far
run.wait(timeout=600)            # blocks until Completed/Faulted

# Results
run.trends("*")                  # all trend series as dict[str, DataFrame]
run.trends("uv.*")               # glob filter
run.result                       # final ExperimentContext (if available)

# Control
run.pause()                      # POST /runspaces/{id}/pause
run.resume()                     # POST /runspaces/{id}/resume
run.abort()                      # POST /runspaces/{id}/abort
```

The Python client subscribes to the WebSocket `/ws` for live events and trends — same stream as the desktop UI.

---

### Optimisation Loops (Ax / BoTorch)

The primary motivation for Python-first authoring is closing the loop between experiment results and the next experiment's parameters.

```python
from ax.service.ax_client import AxClient
import helix

ax = AxClient()
ax.create_experiment(
    parameters=[
        {"name": "temperature", "type": "range", "bounds": [60, 120]},
        {"name": "flow_rate",   "type": "range", "bounds": [0.5, 3.0]},
    ],
    objective_name="yield",
    minimize=False,
)

for _ in range(20):
    params, trial_index = ax.get_next_trial()

    run = my_workflow.submit(**params)
    run.wait(timeout=1200)

    yield_value = run.trends("uv_detector.absorbance_280nm").mean()["value"]
    ax.complete_trial(trial_index=trial_index, raw_data={"yield": (yield_value, None)})

best_params, best_values = ax.get_best_parameters()
```

**BoTorch path:** same loop, using BoTorch's `optimize_acqf` directly when Ax's built-in models are insufficient for custom acquisition functions.

**Key contract:** `run.wait()` must be reliable. A hung runspace or a network hiccup must surface as a Python exception — not a silent hang. Timeout + abort-on-timeout is required.

---

### Python Client Package (`helix-client`)

Separate Python package, installable via `pip install helix-client`.

**Public API surface:**
- `helix.connect(host, port)` → `HelixClient`
- `HelixClient.devices()` → list of `DeviceInfo` (from `GET /devices`)
- `HelixClient.submit(doc)` → `RunHandle`
- `HelixClient.runs()` → list of past runs (from experiment history)
- `RunHandle.wait(timeout)`, `.trends(pattern)`, `.events`, `.status`

**Transport:** pure HTTP + WebSocket (no gRPC required from Python client). Connects to the Agent REST API directly, same as the Tauri desktop app in standalone mode.

**Async support:** `asyncio`-native (`await run.wait_async()`), with sync wrappers for Jupyter notebooks.

---

## Implementation hints

**Phase A — Python client package (`helix-client`):**
- `HelixClient`: connect, devices, submit (calls compile + runspaces endpoints)
- `RunHandle`: status polling, wait, abort
- WebSocket subscriber for live trend streaming
- pip-installable; no Agent changes required

**Phase B — DSL:**
- `@workflow` decorator + `step()` / `delay()` / `branch()` context managers
- DSL-to-WslDocument compiler (pure Python)
- Parameter binding at submit time

**Phase C — Optimisation integration:**
- `run.trends()` returning pandas DataFrames
- Ax / BoTorch example notebooks in `docs/python-examples/`
- Campaign management: submit N runs with parameter sets, collect results in bulk

**Key dependency:** Python client connects to the Phase 6 Agent REST API (`POST /workflows/compile`, `POST /runspaces`, `GET /runspaces/{id}`, WebSocket `/ws`) — no additional Agent changes required for Phase A. Phase B/C are pure Python.

**Cross-link:** Python sidecar in Real-Time Trend System todo (`2026-05-30-real-time-trend-system.md`) is a separate concern — the sidecar processes trend data server-side; the Python client drives workflows client-side.
