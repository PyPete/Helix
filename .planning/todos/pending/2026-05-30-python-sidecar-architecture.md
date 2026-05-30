---
created: 2026-05-30T19:10:00Z
title: Python Sidecar Architecture (Multi-Environment)
area: api
files:
  - Helix.Orchestration.Agent/Events/RunspaceEvent.cs
  - Helix.Orchestration.Agent/AgentService.cs
---

## Problem

A single shared Python environment for the sidecar is unworkable in practice: different labs have different scientific packages (chemometrics vs. ML vs. spectroscopy), package versions conflict, and IT departments cannot easily maintain a monolithic Python install across all use cases. Users need to bring their own libraries and virtual environments, and the sidecar must execute their `compute()` functions safely without one user's script crashing or monopolising resources for another.

## Solution

**Multi-environment Python sidecar** — a microservice that dispatches to isolated virtual environments per trend definition, with sandboxing at the function level.

---

### Sidecar as a Script Dispatcher

The sidecar's job is narrow and explicit:

1. **Load user scripts** — watch a `scripts/` directory; hot-reload on file change
2. **Execute `compute()` functions** — each script exposes a single `compute(inputs: dict) -> dict` entry point
3. **Support user-provided libraries** — each trend definition declares a `venv` path; the sidecar activates it before executing the script
4. **Support user-provided virtual environments** — pre-created by the user, referenced by path; the sidecar does not manage venv creation
5. **Return results to the Agent** — flat `{ "value": float, "unit": str }` or `{ "series": list[float] }` dict

**The sidecar is stateless per call.** It does not retain state between `compute()` invocations. Accumulation (e.g. sliding windows) is the Agent's responsibility.

---

### Script Contract

Every user script must expose exactly one function:

```python
def compute(inputs: dict) -> dict:
    """
    inputs: { "trend_name": float | list[float], ... }
    returns: { "value": float } | { "series": list[float] }
    """
    absorbance = inputs["uv_detector.absorbance_280nm"]
    concentration = calibration_curve(absorbance)
    return {"value": concentration, "unit": "mM"}
```

**Rules enforced by the sidecar loader:**
- Must define `compute`
- `compute` must accept exactly one positional argument (`inputs`)
- `compute` must return a dict with at least a `"value"` or `"series"` key
- Module-level code runs at load time (imports, model loading) — this is intentional and allowed
- Side effects (writing files, network calls) are permitted at load time; blocked at `compute()` call time

---

### Multi-Environment: Per-Trend venv Declaration

Each trend definition declares which virtual environment to use:

```json
{
  "id": "pls-concentration",
  "name": "PLS Concentration Model",
  "script": "scripts/pls_model.py",
  "inputs": ["uv_detector.spectrum"],
  "engine": "python",
  "python_env": {
    "venv": "/opt/helix/envs/chemometrics",
    "python": "/opt/helix/envs/chemometrics/bin/python"
  }
}
```

**How the sidecar handles multiple environments:**

```
sidecar process
├── Environment router
│   ├── "chemometrics" venv  →  subprocess worker pool (2 workers)
│   ├── "ml-inference"  venv →  subprocess worker pool (1 worker)
│   └── "base"          venv →  subprocess worker pool (4 workers)
└── gRPC / HTTP server
```

Each venv gets its own pool of worker subprocesses, pre-loaded with the scripts that declared that venv. The sidecar dispatcher routes `compute()` calls to the correct pool.

**Worker process lifecycle:**
- Pre-fork at sidecar startup: one worker process per venv that has at least one script
- Worker loads all scripts for its venv at startup (module-level import cost paid once)
- Each `compute()` call is dispatched to an idle worker; result returned to dispatcher
- Worker crash → dispatcher restarts the worker and marks in-flight calls as failed; Agent receives an error frame, not a hang

---

### Sandboxing

Three layers of isolation:

**1. Subprocess isolation (mandatory)**
- Each worker runs in a separate OS process
- A worker crash cannot affect the sidecar dispatcher or the Agent
- Worker memory is reclaimed by the OS on crash

**2. Import restrictions (configurable)**
- Restricted import list applied at Python level via `sys.meta_path` hook
- Default blocked: `subprocess`, `socket`, `os.system`, `ctypes`, `shutil`
- Default allowed: `numpy`, `scipy`, `sklearn`, `pandas`, `torch`, `onnxruntime` + anything in the declared venv
- User can add to allowed list in `sidecar-config.json`; cannot remove from blocked list without admin override

**3. Resource limits (per call)**
- Timeout: configurable per trend definition (default 5s, max 30s)
- Memory: RSS limit via `resource.setrlimit` (Linux) or Job Objects (Windows); default 512 MB per worker
- CPU: no hard limit (process scheduling provides natural isolation); watchdog kills workers exceeding 10s wall-clock per call

**Sandbox enforcement contract:**
```json
{
  "id": "pls-concentration",
  "sandbox": {
    "timeout_ms": 2000,
    "memory_mb": 256,
    "allowed_imports": ["sklearn", "numpy"],
    "blocked_network": true
  }
}
```

If `blocked_network: true`, `socket` is blocked at import hook level for that worker process.

---

### Failure Modes and Agent Contract

| Failure | Sidecar behaviour | Agent behaviour |
|---|---|---|
| `compute()` timeout | Kill worker, restart it | Publish `TrendValueEvent` with `error: "timeout"` flag; mark trend stale |
| `compute()` exception | Catch, return error dict | Same as timeout — publish error frame, not crash |
| Worker OOM | OS kills worker process | Dispatcher detects exit, restarts worker, returns error for in-flight call |
| Script syntax error at load | Log error, skip script | Trend marked `unavailable` at Agent; no crash |
| Sidecar process crash | Agent health check fails | All Python-engine trends marked unavailable; Local Engine trends unaffected |
| venv path missing | Log error at startup | Trends referencing that venv marked unavailable at startup |

**Staleness guard:** The Agent tracks the last successful value and timestamp for each Python trend. If no successful update arrives within `2 × polling_interval`, the trend is published as stale to the WebSocket with `{ "stale": true }`. The UI renders stale values with a visual indicator, not an error.

---

### Hot-reload

- Script files in `scripts/` are watched with `watchdog` (Python)
- On file change: old workers for that script are drained (finish in-flight calls), then replaced with a new pre-loaded worker
- No sidecar restart required for script updates
- venv changes require worker pool restart for the affected venv only (not full sidecar restart)

---

### Sidecar API (internal, Agent-facing)

**Protocol:** local gRPC (preferred) or HTTP/JSON over loopback. The Agent never exposes the sidecar externally.

```protobuf
service PythonSidecar {
  rpc Execute(ExecuteRequest) returns (ExecuteResponse);
  rpc Health(HealthRequest) returns (HealthResponse);
  rpc ListScripts(Empty) returns (ScriptListResponse);
}

message ExecuteRequest {
  string script_id = 1;
  map<string, double> inputs = 2;
  int32 timeout_ms = 3;
}

message ExecuteResponse {
  oneof result {
    double scalar_value = 1;
    bytes series_cbor = 2;
    string error = 3;
  }
  string unit = 4;
  bool stale = 5;
}
```

---

## Implementation hints

**Phase A — Sidecar scaffold:**
- FastAPI or gRPC server (Python 3.11+)
- Script loader with `compute()` contract validation
- Single default venv, no sandboxing yet
- Agent `PythonSidecarClient` calling `Execute` RPC

**Phase B — Multi-environment:**
- venv declaration in trend JSON
- Per-venv worker pool with pre-fork pattern
- Worker crash recovery

**Phase C — Sandboxing:**
- Import restriction hook (`sys.meta_path`)
- Timeout + memory limits
- `sandbox` block in trend definition JSON

**Phase D — Hot-reload + observability:**
- `watchdog`-based script watcher
- Worker health metrics exposed to Agent `/health` endpoint
- Sidecar logs structured JSON (consumed by Recorder MQTT pipeline)

**Cross-links:**
- Parent architecture: `2026-05-30-real-time-trend-system.md` (two-engine architecture)
- Deployment: `2026-05-30-deployment-strategy-lab.md` (Docker vs. NSSM Windows Service modes)
- Python client: `2026-05-30-python-workflow-interface.md` (client-side, separate concern)
