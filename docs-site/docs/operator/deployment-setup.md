---
id: deployment-setup
title: "Deployment Guide"
sidebar_position: 5
---

# Deployment Guide

This guide covers local and development deployment. Production hardening (TLS, credential rotation, Windows Service / systemd installers) is deferred to Phase 10.

## Prerequisites

- **Docker Desktop** (Windows) or **Docker + Docker Compose** (Linux) — required for the infrastructure stack.
- **.NET 10 SDK** — required for running the Agent and SiteServer via `dotnet run`. Not required if you use Docker images exclusively.
- **Node.js v20** — required for building the React dashboard and the Tauri desktop app.

## Starting the Infrastructure Stack

The root `docker-compose.yml` starts the supporting services: PostgreSQL, Mosquitto MQTT broker, and MinIO object storage.

```bash
docker-compose up -d
```

Default service ports:

| Service | Port |
|---------|------|
| PostgreSQL | 5432 |
| Mosquitto MQTT | 1883 |
| MinIO API | 9000 |
| MinIO Console | 9001 |

Default development credentials (do not use in production):

- PostgreSQL: user `helix`, password `helix_dev_password`, database `helix_orchestration`
- MinIO: access key `helix`, secret key `helix_dev_secret`

:::warning
These credentials are for local development only. Rotate all secrets before deploying to a shared or internet-accessible environment. Production hardening is covered in the Phase 10 release.
:::

## Starting the Agent

The Agent connects to hardware devices and exposes REST and gRPC endpoints. From the monorepo root:

```bash
dotnet run --project Helix.Orchestration.Agent
```

The Agent listens on:

- HTTP port **5000** (REST API and WebSocket)
- gRPC port **5050** (configurable via `appsettings.json` or the `ASPNETCORE_URLS` environment variable)

Key configuration options in `appsettings.json` (or override via environment variables):

| Key | Description |
|-----|-------------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string for the Cleipnir durability store |
| `MqttBridge__Host` | Hostname of the Mosquitto broker (default: `localhost`) |
| `SerialTransportOptions__PortName` | Serial port for the VapourtecR4 adapter (e.g. `COM3` on Windows, `/dev/ttyUSB0` on Linux) |

## Starting the SiteServer

The SiteServer aggregates Agents and serves the React dashboard API. From the monorepo root:

```bash
dotnet run --project Helix.Orchestration.SiteServer
```

The SiteServer starts on HTTP port **7020** (default dev port).

Configure Agent connections in `appsettings.json` under the `AgentProxy` section. For each fumehood, set `UseSimulation: false` and provide the Agent's gRPC address:

```json
"AgentProxy": {
  "Agents": [
    {
      "FumeHoodId": "fh-01",
      "UseSimulation": false,
      "GrpcAddress": "http://localhost:5050"
    }
  ]
}
```

## Standalone Mode (Desktop App Only)

For single-fumehood operation without a SiteServer, open the Helix desktop app (`helix-desktop/`) and configure the Agent connection directly:

- **Host:** `localhost`
- **Port:** `5000`

The app connects to the Agent REST and WebSocket endpoints and operates without a SiteServer. Trend data, execution status, and manual device controls are all available in standalone mode.

## React Dashboard

Build and serve the React dashboard:

```bash
cd helix-dashboard
npm ci
npm run build
```

Serve the `dist/` directory from any static file server (nginx, Caddy, or the Node.js `serve` package). The dashboard reads `VITE_SITE_SERVER_URL` from `.env` to locate the SiteServer:

```
VITE_SITE_SERVER_URL=http://localhost:7020
```

## Phase 10 Note

Phase 10 will add a single-command setup script (`setup.ps1` on Windows, `setup.sh` on Linux), Windows Service and systemd unit installers, and a hardware pre-flight check utility. See the Phase 10 release notes for the production-ready deployment path.
