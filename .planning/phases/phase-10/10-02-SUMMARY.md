---
phase: "10"
plan: "02"
subsystem: deploy
tags: [setup, bootstrap, powershell, bash, windows, linux, raspberry-pi]
dependency_graph:
  requires: []
  provides: [setup.ps1, setup.sh]
  affects: [G2]
tech_stack:
  added: []
  patterns:
    - Idempotent bootstrap scripts with per-step PASS/FAIL reporting
    - winget-based Windows package installation
    - apt-based Linux package installation with NodeSource for Node.js
    - systemd service installation guard (waits for Plan 10-03 units)
key_files:
  created:
    - setup.ps1
    - setup.sh
  modified: []
decisions:
  - setup.sh guards against missing systemd unit files with a FAIL message directing to Plan 10-03
  - setup.ps1 uses Push-Location/Pop-Location for npm steps to avoid leaving shell in wrong directory
  - Both scripts exit 1 on any failure but continue through all steps to report all failures at once
metrics:
  duration: "~2 min"
  completed: "2026-06-03"
  tasks_completed: 2
  tasks_total: 2
  files_created: 2
  files_modified: 0
---

# Phase 10 Plan 02: setup.ps1 + setup.sh Bootstrap Scripts Summary

**One-liner:** Idempotent one-command bootstrap scripts for Windows (winget) and Linux/Raspberry Pi (apt + systemd) that check prerequisites, build all projects, and start the compose stack.

---

## What Was Built

### setup.ps1 (Windows)

Six-step idempotent bootstrap script for Windows:

1. Check/install .NET 10 SDK via `winget install Microsoft.DotNet.SDK.10`
2. Check/install Node.js 20+ via `winget install OpenJS.NodeJS.LTS`
3. Verify Docker Desktop is running (no auto-install — manual step required)
4. `dotnet build Helix.Orchestration.slnx -c Release`
5. `npm ci && npm run build` in `helix-dashboard/`
6. `docker compose -f docker-compose.yml up -d --build`

Each step prints `PASS:` or `FAIL:` with an actionable message. Script accumulates failures and exits 1 at the end if any step failed.

### setup.sh (Linux/Raspberry Pi)

Seven-step idempotent bootstrap script for Debian-based Linux:

1. Check/install .NET 10 SDK via Microsoft apt package feed
2. Check/install Node.js 20+ via NodeSource `setup_20.x`
3. Check/install Docker via `docker.io` + `docker-compose-plugin`; adds user to docker group if needed
4. `dotnet build Helix.Orchestration.slnx -c Release`
5. `npm ci && npm run build` in `helix-dashboard/`
6. Install systemd unit files from `deploy/systemd/` (guarded: reports FAIL if units not yet present, directing user to complete Plan 10-03 first)
7. `docker compose -f docker-compose.yml up -d --build`

Uses `pass()`/`fail()` helpers. `FAILED` accumulator; exits 1 if any step failed.

---

## Verification Results

| Check | Result |
|-------|--------|
| setup.ps1 exists | PASS |
| setup.ps1 contains `dotnet build` | PASS |
| setup.ps1 contains `npm ci` | PASS |
| setup.ps1 contains `docker compose` | PASS |
| setup.ps1 contains `winget install` | PASS |
| setup.ps1 contains `PASS` | PASS |
| setup.ps1 contains `FAIL` | PASS |
| setup.sh exists | PASS |
| `bash -n setup.sh` syntax check | PASS |
| setup.sh contains `systemctl enable` | PASS |

---

## Deviations from Plan

None — plan executed exactly as written.

Minor implementation note: `setup.ps1` uses `Push-Location`/`Pop-Location` with explicit `Pop-Location` calls before early-returns in the npm step (rather than a bare `return`), ensuring the shell location is always restored even on failure.

---

## Threat Flags

None. These are local developer setup scripts with no network endpoints, auth paths, or schema changes.

---

## Self-Check: PASSED

- setup.ps1: FOUND at repo root
- setup.sh: FOUND at repo root
- Commit 0b1c282: FOUND in git log
