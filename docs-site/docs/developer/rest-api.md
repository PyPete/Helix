---
id: rest-api
title: REST API Reference
sidebar_position: 5
---

## Overview

The SiteServer exposes a REST API at port 7020 (default dev port; configurable via `ASPNETCORE_URLS`). The API is documented via OpenAPI 3.0 (Swashbuckle). The full interactive specification is served at `/swagger` when the server is running in development mode.

A static copy of the OpenAPI spec is generated into `docs-site/static/openapi.json` during the documentation build via the `generate-openapi.js` prebuild script.

## Endpoint Summary

All endpoints are grouped by resource. The WebSocket endpoint at `/api/trends/ws` is handled outside of Minimal API via ASP.NET Core middleware.

| Method | Path | Description |
|--------|------|-------------|
| GET | /api/campaigns | List all campaigns |
| POST | /api/campaigns | Create a new campaign definition |
| GET | /api/campaigns/&#123;id&#125; | Get a campaign by ID |
| POST | /api/campaigns/&#123;id&#125;/start | Start campaign execution |
| DELETE | /api/campaigns/&#123;id&#125; | Delete a campaign |
| GET | /api/devices | List all registered devices and their capabilities |
| GET | /api/experiments | List all experiments (history browser) |
| POST | /api/experiments | Submit a new experiment for execution |
| GET | /api/experiments/&#123;id&#125; | Retrieve the status of a running or completed experiment |
| DELETE | /api/experiments/&#123;id&#125; | Abort a running experiment |
| GET | /api/trends | List all available trends and their metadata |
| GET | /api/trends/&#123;id&#125;/history | Retrieve historical values for a scalar trend |
| WS | /api/trends/ws | Live trend streaming over WebSocket (MQTT-over-WebSocket bridge) |

## Authentication

The dev server has no authentication by default. Production deployments should place the SiteServer behind a reverse proxy (nginx, Caddy, or similar) with mutual TLS or token-based authentication. See the Deployment Guide for hardening steps.

## Generating the OpenAPI Spec

The `docs-site/scripts/generate-openapi.js` script automates OpenAPI spec generation:

1. Build `Helix.Orchestration.SiteServer` in Release configuration:
   ```bash
   dotnet build Helix.Orchestration.SiteServer -c Release
   ```
2. Run the documentation build — the `prebuild` npm hook calls `generate-openapi.js` automatically:
   ```bash
   cd docs-site && npm run build
   ```
3. The script runs `dotnet swagger tofile` against the built assembly and writes the spec to `docs-site/static/openapi.json`.

If the SiteServer binary is not present, the script exits with code 0 and leaves the existing `openapi.json` in place so the documentation build always succeeds.

The `dotnet swagger` CLI tool is installed as a local .NET tool defined in `.config/dotnet-tools.json`. Run `dotnet tool restore` from the repository root before using it directly.
