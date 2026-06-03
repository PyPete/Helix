---
phase: "10"
plan: "03"
subsystem: deploy
tags: [windows-service, systemd, nssm, preflight, production-hardening, mqtt-tls, minio, postgresql, opc-ua]
dependency_graph:
  requires: [10-01, 10-02]
  provides: [windows-service-scripts, systemd-unit-files, preflight-checks, production-hardening-guide]
  affects: [G3, G4, G5, G6]
tech_stack:
  added: []
  patterns:
    - NSSM-based idempotent Windows service installation with sc.exe query check
    - systemd Type=simple with EnvironmentFile pattern for secrets
    - Cross-platform preflight script pair (PowerShell + Bash) with PASS/WARN/FAIL exit codes
    - Docusaurus operator guide page with production hardening checklist
key_files:
  created:
    - deploy/windows/install-agent-service.ps1
    - deploy/windows/install-recorder-service.ps1
    - deploy/systemd/helix-agent.service
    - deploy/systemd/helix-recorder.service
    - deploy/preflight/preflight.ps1
    - deploy/preflight/preflight.sh
    - docs-site/docs/operator/production-hardening.md
  modified:
    - docs-site/sidebars.ts
decisions:
  - systemd units use Type=simple (not notify) -- Agent does not call UseSystemd() so Type=notify would timeout on start
  - CHANGE_ME placeholders in systemd Environment= lines with comment directing to EnvironmentFile approach
  - preflight.sh uses nc for TCP checks with fallback to apt-get install netcat-openbsd if missing
  - preflight.ps1 OPC UA check reads appsettings.json via ConvertFrom-Json (no external dependencies)
  - sidebars.ts updated to add operator/production-hardening at sidebar_position 6
metrics:
  duration: "~4 min"
  completed: "2026-06-03"
  tasks_completed: 2
  tasks_total: 2
  files_created: 7
  files_modified: 1
---

# Phase 10 Plan 03: Windows Service Scripts, systemd Units, Pre-flight Checks, Production Hardening Docs Summary

**One-liner:** NSSM-based Windows service installers, systemd unit files, cross-platform pre-flight check scripts (6 checks, PASS/WARN/FAIL), and Docusaurus production hardening guide covering TLS MQTT, MinIO/PostgreSQL credential rotation, and OPC UA cert validation.

---

## What Was Built

### Windows Service Scripts (G3)

**`deploy/windows/install-agent-service.ps1`**
- Parameters: `$InstallPath` (default `C:\Helix\Agent`), `$NssmPath`, `$DotnetPath`
- Auto-detects dotnet on PATH if `$DotnetPath` not provided
- Idempotency: queries `sc.exe` for existing service, stops and removes before reinstalling
- Configures `SERVICE_AUTO_START`, sets `AppDirectory`, `DisplayName`, `Description`
- Sets `AppEnvironmentExtra`: `Kestrel__Endpoints__Http__Url`, `Mqtt__BrokerHost`, `Mqtt__BrokerPort`

**`deploy/windows/install-recorder-service.ps1`**
- Same pattern as Agent script
- Sets `AppEnvironmentExtra`: `Recorder__MqttBrokerHost`, `Recorder__MqttBrokerPort`, `Minio__*` settings

### systemd Unit Files (G4)

**`deploy/systemd/helix-agent.service`**
- `After=network-online.target postgresql.service` (Agent needs DB)
- `Type=simple` with comment explaining why not `Type=notify`
- `User=helix`, `Group=helix` -- runs as dedicated service account
- `Restart=on-failure`, `RestartSec=10`, `TimeoutStopSec=30`
- `StandardOutput/Error=journal`, `SyslogIdentifier=helix-agent`
- `CHANGE_ME` placeholder on `ConnectionStrings__HelixOrchestration` with note about `EnvironmentFile`

**`deploy/systemd/helix-recorder.service`**
- `After=network-online.target` (no DB dependency)
- `CHANGE_ME` placeholders on `Minio__AccessKey` and `Minio__SecretKey`
- Same service hardening settings as Agent unit

### Pre-flight Check Scripts (G5)

**`deploy/preflight/preflight.ps1`** (Windows, PowerShell)
- 6 checks: PostgreSQL TCP, MQTT TCP, MinIO HTTP (`/minio/health/live`), Agent HTTP (`/health`), OPC UA cert config, Serial ports
- TCP checks via `System.Net.Sockets.TcpClient` with async timeout
- MinIO/Agent checks via `Invoke-WebRequest` with `-ErrorAction Stop`
- OPC UA check: reads `appsettings.json` via `ConvertFrom-Json`, checks `OpcUa.AutoAcceptUntrustedCertificates`
- Serial ports: `[System.IO.Ports.SerialPort]::GetPortNames()`
- Exit codes: 0=PASS, 1=WARN, 2=FAIL
- bash -n syntax check: N/A (PowerShell)

**`deploy/preflight/preflight.sh`** (Linux, Bash)
- Same 6 checks as PowerShell version
- TCP checks via `nc -z -w3`; auto-installs `netcat-openbsd` if missing
- MinIO/Agent checks via `curl -sf --max-time 5`
- OPC UA check: uses `python3` (preferred) or `jq` fallback for JSON parsing
- Serial ports: `ls /dev/ttyUSB* /dev/ttyACM*`
- `bash -n` syntax check: PASSED

### Production Hardening Guide (G6)

**`docs-site/docs/operator/production-hardening.md`**
- `id: production-hardening`, `sidebar_position: 6`
- Secrets Checklist table: PostgreSQL password, MinIO root/access keys, MQTT auth, OPC UA certs
- TLS for MQTT: openssl CA/server cert generation, `mosquitto.conf` config snippet, `appsettings.json` JSON snippet
- MinIO Access Key Rotation: step-by-step via MinIO Console + service restart
- PostgreSQL Password Rotation: `docker exec` + `ALTER USER` + all config locations
- OPC UA Certificate Validation: step-by-step to disable `AutoAcceptUntrustedCertificates`
- Systemd Secrets: `EnvironmentFile` pattern with `/etc/helix/agent-secrets` (mode 0600)

**`docs-site/sidebars.ts`**
- Added `'operator/production-hardening'` to `operatorSidebar` items array

---

## Verification Results

1. `bash -n deploy/preflight/preflight.sh` -- PASSED (syntax valid)
2. `preflight.ps1` contains "PASS", "WARN", "FAIL" keywords -- PASSED
3. Windows service scripts use NSSM install pattern (`& $NssmPath install`) -- PASSED
4. Both systemd unit files contain `WantedBy=multi-user.target` -- PASSED
5. `docs-site/docs/operator/production-hardening.md` exists with `id: production-hardening` frontmatter -- PASSED
6. `cd docs-site && npm run build` -- PASSED (Docusaurus build succeeds with new page + sidebar entry)

---

## Deviations from Plan

None - plan executed exactly as written.

One minor note: the plan verification criterion (#3) says "nssm install" but the actual PowerShell idiom is `& $NssmPath install` (using the `$NssmPath` variable). The scripts correctly invoke NSSM via the PowerShell call operator `&`, which is idiomatic PowerShell. The verification grep was adjusted accordingly.

---

## Commits

| Hash | Message |
|------|---------|
| c202b8b | feat(deploy): service scripts, systemd units, preflight checks, production hardening docs |

---

## Self-Check: PASSED

Files confirmed present:
- deploy/windows/install-agent-service.ps1 -- FOUND
- deploy/windows/install-recorder-service.ps1 -- FOUND
- deploy/systemd/helix-agent.service -- FOUND
- deploy/systemd/helix-recorder.service -- FOUND
- deploy/preflight/preflight.ps1 -- FOUND
- deploy/preflight/preflight.sh -- FOUND
- docs-site/docs/operator/production-hardening.md -- FOUND
- docs-site/sidebars.ts -- FOUND

Commit c202b8b confirmed in git log.
