---
created: 2026-05-30T19:05:00Z
title: Deployment Strategy for Lab Computers
area: tooling
files:
  - docker-compose.yml
  - Helix.Orchestration.Agent/Program.cs
  - Helix.Orchestration.Recorder/Program.cs
  - .github/workflows/ci.yml
---

## Problem

Lab computers have specific deployment constraints that differ from cloud or developer machines: they are long-running Windows workstations connected to physical instruments, managed by IT departments, expected to auto-start after power cycles, and used by scientists who are not developers. The current repo has no production deployment artifacts beyond a basic `docker-compose.yml`.

## Solution

**Layered deployment model** — each component deployed at the appropriate isolation boundary for its role and risk profile.

---

### Component Deployment Map

| Component | Mode | Why |
|---|---|---|
| Local Agent | Windows Service | Must auto-start, must survive user logoff, owns device connections |
| Device Services (per-adapter) | Optional separate Windows Services | Isolation: a buggy driver cannot crash the Agent |
| Tauri Desktop App | Per-user or per-machine MSI | Scientists log in to different workstations; per-user avoids IT escalation |
| Python Sidecar | Optional Docker container or Windows Service | Optional; isolates Python dependencies from .NET runtime |
| Recorder | Windows Service | Background MQTT consumer; no user interaction |
| Portable mode | Single-folder xcopy | R&D laptops and temporary benches; no installer required |

---

### Local Agent — Windows Service

**Install mechanism:** NSSM (Non-Sucking Service Manager) or `sc.exe` with a PowerShell wrapper.

**Requirements:**
- Auto-start on boot (`StartType=Automatic`)
- Recover from crash: restart after 30s, 60s, then alert on 3rd failure
- Run as `LocalService` or a dedicated `helix-agent` service account (not `LocalSystem`)
- Log to Windows Event Log + rolling file log in `%ProgramData%\Helix\logs\`
- Configurable via `appsettings.json` in `%ProgramData%\Helix\config\` (not next to the binary)

**Install script (`install-agent.ps1`):**
```powershell
sc.exe create HelixAgent binpath="C:\Program Files\Helix\agent\Helix.Orchestration.Agent.exe" start=auto
sc.exe failure HelixAgent reset=86400 actions=restart/30000/restart/60000/run/120000
sc.exe start HelixAgent
```

**Uninstall script:** stops service, removes binary, removes Event Log source.

---

### Device Services — Isolated per-adapter (optional)

Each device adapter can run as a separate Windows Service that communicates with the Agent over a local named pipe or gRPC.

**Rationale:** A serial port deadlock in VapourtecR4 should not require restarting the Agent (and losing all other device connections). Process isolation gives independent restart without disruption.

**Trade-off:** Adds inter-process communication complexity. Recommended only for adapters with known instability. Default deployment is all adapters in-process.

**Isolation boundary:** `IDeviceAdapterProxy` interface; in-process and out-of-process implementations share the same contract. Switch is a config flag, not a code change.

---

### Tauri Desktop App — Per-user or Per-machine Install

**Per-user MSI (recommended default):**
- No admin rights required at install time
- Installs to `%LocalAppData%\Programs\HelixDesktop\`
- Each scientist has their own copy; IT does not need to manage it
- `tauri updater` plugin for silent auto-update from a release endpoint

**Per-machine MSI (IT-managed labs):**
- Admin install to `C:\Program Files\HelixDesktop\`
- Single install for all users on a shared workstation
- Requires IT to push updates via SCCM/Intune or GPO

**Auto-update:**
- Tauri updater checks `https://releases.helix.internal/latest.json` on launch
- Differential update: downloads only changed files
- Update applied on next launch (no mid-session restart)
- Offline labs: updater endpoint is optional; falls back gracefully if unreachable

---

### Python Sidecar — Optional Microservice

**Two deployment modes:**

1. **Docker container (default for capable machines):**
   ```yaml
   # docker-compose.yml entry
   python-sidecar:
     image: helix/python-sidecar:latest
     ports: ["50052:50052"]
     volumes: ["./transforms:/app/transforms"]
     restart: unless-stopped
   ```

2. **Windows Service (Docker-free labs):**
   - Conda or embedded Python distribution
   - NSSM-managed service: `HelixPythonSidecar`
   - Activated only if `PythonSidecar.Enabled=true` in Agent `appsettings.json`

**Graceful absence:** If the sidecar is not running, Python-engine trend definitions are marked `unavailable` — they do not crash the Agent. Raw device trends and Local Engine trends continue normally.

---

### Portable Mode (R&D Machines, Temporary Benches)

For laptops and temporary setups where a formal install is undesirable.

**Contract:**
- Single folder, xcopy-deployable: `helix-portable\`
- No registry writes, no `%ProgramData%` entries, no GAC
- Config in `helix-portable\config\appsettings.json`
- Logs in `helix-portable\logs\`
- Launch: `start-helix.ps1` (starts Agent + Recorder as foreground processes with a stop script)
- Agent auto-detects portable mode: `if (File.Exists("portable.flag")) { usePortableConfig = true; }`

**Limitations vs. service mode:** Does not auto-start on boot. Stops when the terminal is closed. Not appropriate for unattended production runs.

---

### Pre-flight Check Script (`preflight.ps1`)

Validates the environment before the first run. Outputs a pass/warn/fail summary.

Checks:
- Agent service status
- PostgreSQL connection (`Server=localhost;Port=5432;Database=helix`)
- MQTT broker reachability (`localhost:1883`)
- MinIO bucket access (`http://localhost:9000/helix-data`)
- Serial port availability for each configured device adapter
- TCP reachability for Modbus/TCP and OPC UA devices
- Tauri app binary present (optional)
- Python sidecar health (optional, skip if not configured)
- Disk space: warn if `%ProgramData%\Helix\` < 10 GB free

---

### Upgrade Path

- Agent: stop service → xcopy new binary → start service (zero-database-change upgrades are hot-swappable)
- Database migrations: `dotnet ef database update` in upgrade script; run before starting new Agent binary
- Rollback: previous binary kept in `agent-prev\`; `rollback.ps1` stops service, swaps binary, starts service

---

## Implementation hints

This is the **Phase 10** deliverable (Deployment + Setup Scripts) from the ROADMAP. Key artifacts:

**Already planned in Phase 10 ROADMAP:**
- `setup.ps1` (Windows bootstrap)
- `setup.sh` (Linux/RPi bootstrap)
- `docker-compose.yml` (full-stack)
- Windows service install scripts
- systemd unit files

**Additional detail from this todo (not yet in Phase 10 scope):**
- Portable mode (`portable.flag` auto-detection, `start-helix.ps1`)
- Per-user vs. per-machine MSI distinction for Tauri app
- Tauri auto-updater integration (`tauri updater` plugin + release endpoint)
- Device Services isolation boundary (`IDeviceAdapterProxy`, config flag)
- Sidecar two-mode deployment (Docker vs. NSSM Windows Service)
- Upgrade/rollback procedure with `agent-prev\` pattern

**Cross-link:** Python Sidecar deployment details connect to `2026-05-30-real-time-trend-system.md`.
