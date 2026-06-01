---
phase: "7"
slug: site-level-react-dashboard
padded: "07"
status: discussed
updated: "2026-06-01"
---

# Phase 7 Context: Site-Level React Dashboard

## Domain

A Vite + TypeScript SPA (`helix-dashboard/`) that a site manager opens in a browser to see fumehood
status, manage campaigns, browse experiment history, and watch live trend charts. Separate from the
Tauri desktop app — no install required, served as static files from the SiteServer or CDN.

## Decisions

### Fumehood List & Status Source

- **Placeholder data in Phase 7.** Fumehood entries are hardcoded/simulated in the frontend.
  No new SiteServer `/api/fumehoods` endpoint is required for this phase.
- Each fumehood card shows: name + status badge + active experiment name (if available) + last-seen timestamp.

### Navigation & Page Structure

- **React Router with URL-based routes.**
  Pages: `/` (Site Overview), `/campaigns`, `/history`, `/trends`.
  Add `react-router-dom` dependency.
- **Chrome:** Same dark IDE palette (`#1e1e2e`, `#252535`, `#9d7fff`) from sketch-findings-Helix,
  but with a **top navbar** (not VS-Ribbon). Web-native — the Ribbon shell is Tauri desktop only.

### Chart Library

- **SciChart.js** — WebGL-accelerated charts. Full licence wiring now.
  Planner must add licence initialisation at app entry point.
  Note: SciChart.js is a commercial library — a valid runtime key must be available at build time.

### Live Trend Connection

- **Raw WebSocket endpoint on SiteServer** — `GET /api/trends/ws` (or similar).
  `MqttTrendBridge` already ingests from MQTT in-process; add a WebSocket relay.
  No SignalR, no extra npm dependencies in the dashboard.
- **Message format:** Reuse the same JSON frame types as the Agent WebSocket
  (`TrendValue`, `RunspaceEvent` etc. defined in `helix-desktop/src/types/agent.ts`).
  Dashboard shares or copies those type definitions.

## Canonical Refs

- `.planning/ROADMAP.md` — Phase 7 deliverables and success criteria (Requirements E1–E6)
- `.planning/PROJECT.md` — Core constraints (standalone mode, cross-platform, safety)
- `.claude/skills/sketch-findings-Helix/SKILL.md` — Dark IDE palette, CSS variables, layout patterns
- `Helix.Orchestration.SiteServer/Program.cs` — DI wiring, existing endpoints, MqttTrendBridge registration
- `Helix.Orchestration.SiteServer/Endpoints/` — CampaignEndpoints, ExperimentEndpoints, TrendEndpoints (existing API surface)
- `helix-desktop/src/types/agent.ts` — WebSocket frame types to reuse in dashboard

## Code Context

### Reusable Assets from helix-desktop

- CSS variables: `--color-bg`, `--color-surface`, `--color-primary`, `--text-xs`, `--space-*`, etc. — copy/share from sketch-findings theme.
- `TrendValue` / `RunspaceEvent` frame types — copy or extract to shared package.
- Vitest + Testing Library test patterns — established across 76+ tests, same setup for dashboard.

### SiteServer Existing Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/campaigns` | GET, POST | List / create campaigns |
| `/api/campaigns/{id}` | GET | Get campaign by id |
| `/api/campaigns/{id}/start` | POST | Start campaign |
| `/api/campaigns/{id}` | DELETE | Delete campaign |
| `/api/experiments` | POST | Submit experiment |
| `/api/experiments/{id}` | GET | Experiment status |
| `/api/experiments/{id}` | DELETE | Abort experiment |
| `/api/trends` | GET | List trends |
| `/api/trends/{id}/history` | GET | Scalar trend history |

### What Needs to Be Added to SiteServer

- WebSocket endpoint for live trend streaming (Phase 7 scope — SiteServer side).
- `MqttTrendBridge` relay into connected WebSocket clients.

## Deferred Ideas

- Real `/api/fumehoods` endpoint backed by Agent health aggregation — deferred to a later phase.
- SciChart.js licence key management / rotation — operational concern, post-Phase 7.
