---
phase: phase-10
plan: "10-01"
subsystem: deployment
tags: [docker, dotnet, compose, infrastructure]
dependency_graph:
  requires: []
  provides: [docker-compose-full-stack, agent-dockerfile, siteserver-dockerfile, recorder-dockerfile]
  affects: [docker-compose.yml]
tech_stack:
  added: [multi-stage-dockerfiles, dotnet-sdk-10, dotnet-aspnet-10, dotnet-runtime-10]
  patterns: [multi-stage-build, health-checks, depends-on-condition]
key_files:
  created:
    - Helix.Orchestration.Agent/Dockerfile
    - Helix.Orchestration.SiteServer/Dockerfile
    - Helix.Orchestration.Recorder/Dockerfile
  modified:
    - docker-compose.yml
decisions:
  - depends_on uses service names (mosquitto/postgres/minio) not container names (helix-*) — Docker Compose depends_on requires service names; container_name provides DNS alias but cannot be used in depends_on
  - AgentProxy__Mode=grpc set in SiteServer environment (not 'real') — code checks StringComparison.OrdinalIgnoreCase for "grpc"
  - Recorder uses dotnet/runtime:10.0 not aspnet:10.0 — Worker-only service has no HTTP listener, smaller image
  - Agent and SiteServer use dotnet/aspnet:10.0 — has Kestrel and gRPC support
  - SiteServer ENV ASPNETCORE_URLS set at image level and also passed via compose environment — belt-and-suspenders for port binding
metrics:
  duration: ~5 min
  completed: "2026-06-03"
  tasks: 2/2
  files: 4
---

# Phase 10 Plan 01: Dockerfiles + Six-Service docker-compose.yml Summary

Multi-stage .NET 10 Dockerfiles for Agent, SiteServer, and Recorder; docker-compose.yml updated from three to six services with health checks and dependency ordering. Closes requirement G1.

## What Was Done

### Task 1: Multi-stage Dockerfiles

Three Dockerfiles created using a two-stage pattern (sdk:10.0 build + runtime):

**Agent** (`Helix.Orchestration.Agent/Dockerfile`)
- Build stage: restores Contracts, Core, Devices, Infrastructure, Agent csproj files, then publishes
- Runtime stage: `mcr.microsoft.com/dotnet/aspnet:10.0` — has Kestrel + gRPC support
- EXPOSE 5080
- ENTRYPOINT: `dotnet Helix.Orchestration.Agent.dll`

**SiteServer** (`Helix.Orchestration.SiteServer/Dockerfile`)
- Build stage: restores Contracts, Core, Infrastructure, Simulation, SiteServer csproj files, then publishes
- Runtime stage: `mcr.microsoft.com/dotnet/aspnet:10.0`
- ENV ASPNETCORE_URLS=http://+:5090
- EXPOSE 5090
- ENTRYPOINT: `dotnet Helix.Orchestration.SiteServer.dll`

**Recorder** (`Helix.Orchestration.Recorder/Dockerfile`)
- Build stage: restores only Infrastructure and Recorder csproj files (minimal dependency chain)
- Runtime stage: `mcr.microsoft.com/dotnet/runtime:10.0` — Worker-only, no HTTP
- No EXPOSE (no HTTP port)
- ENTRYPOINT: `dotnet Helix.Orchestration.Recorder.dll`

### Task 2: docker-compose.yml Update

- Added `healthcheck` to `mosquitto` service (was missing): uses `mosquitto_pub` probe
- Added `helix-agent` service: depends on postgres/mosquitto/minio (service_healthy), health check via `curl -f http://localhost:5080/health`
- Added `helix-siteserver` service: depends on helix-agent/mosquitto (service_healthy), health check via `nc -z localhost 5090`
- Added `helix-recorder` service: depends on mosquitto/minio (service_healthy), no health check (Worker with no HTTP port)
- All existing volumes kept unchanged

## Files Created/Modified

| File | Action | Notes |
|------|--------|-------|
| `Helix.Orchestration.Agent/Dockerfile` | Created | Multi-stage, aspnet:10.0 runtime, EXPOSE 5080 |
| `Helix.Orchestration.SiteServer/Dockerfile` | Created | Multi-stage, aspnet:10.0 runtime, EXPOSE 5090 |
| `Helix.Orchestration.Recorder/Dockerfile` | Created | Multi-stage, runtime:10.0, no port |
| `docker-compose.yml` | Modified | 3→6 services, mosquitto healthcheck added |

## Verification Results

1. `docker compose config --quiet` — exits 0 (valid YAML, all service references resolved)
2. All three Dockerfiles exist at correct paths
3. Each Dockerfile contains `FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build` and a runtime stage
4. Agent and SiteServer use `aspnet:10.0`; Recorder uses `runtime:10.0`

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed depends_on using container names instead of service names**
- **Found during:** Task 2 — `docker compose config --quiet` failed
- **Issue:** Plan spec used `helix-postgres`, `helix-mosquitto`, `helix-minio` in `depends_on` blocks. These are `container_name` values; Docker Compose `depends_on` requires service names (`postgres`, `mosquitto`, `minio`).
- **Fix:** Changed all `depends_on` references to use service names. Container names still work as DNS aliases for inter-service `environment` variables (e.g. `Mqtt__BrokerHost: helix-mosquitto`).
- **Files modified:** `docker-compose.yml`
- **Commit:** d0c7a04

## Self-Check: PASSED

- Helix.Orchestration.Agent/Dockerfile: EXISTS
- Helix.Orchestration.SiteServer/Dockerfile: EXISTS
- Helix.Orchestration.Recorder/Dockerfile: EXISTS
- docker-compose.yml: modified (167 insertions)
- Commit d0c7a04: EXISTS
- `docker compose config --quiet`: Exit 0
