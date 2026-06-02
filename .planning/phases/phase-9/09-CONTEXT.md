---
phase: "9"
slug: docusaurus-documentation
padded: "09"
status: discussed
updated: "2026-06-02"
---

# Phase 9 Context: Docusaurus Documentation

## Domain

A Docusaurus 3 static documentation site at `docs-site/` that serves two audiences:
- **Developers** — understand the architecture, write new device adapters, navigate the transport layer, and use the REST + gRPC APIs.
- **Scientists / operators** — author workflows in the Tauri app, understand device capabilities, and set up experiments.

No new runtime code required. This phase is purely documentation + tooling to generate API references from existing artefacts.

## Decisions

### Docusaurus 3 Site Structure

- `docs-site/` at the monorepo root (alongside `helix-desktop/`, `helix-dashboard/`).
- GitHub Pages deployment via `docusaurus.config.ts` `url` + `baseUrl` — configured for the repo's GH Pages URL.
- Two sidebar sections: `Developer Guide` and `Operator Guide`.

### Developer Section Content

| Doc | Source |
|-----|--------|
| Architecture overview | Describe two-tier design (Agent + SiteServer), data flow diagram in Mermaid, key concepts |
| Adapter authoring guide | Step-by-step with VapourtecR4 as worked example |
| Transport guide | Serial, TCP, OPC UA transports; connection lifecycle |
| REST API reference | Auto-generated from SiteServer OpenAPI/Swagger |
| gRPC reference | Rendered from `agent_control.proto` + `adapter.proto` |

### Scientist / Operator Section Content

| Doc | Source |
|-----|--------|
| Workflow authoring guide | Screenshots from Tauri desktop app (Phase 6) |
| Device capability reference | Describes CapabilityDescriptor schema, how to invoke capabilities |
| Experiment setup guide | Campaign → Experiment → Runspace flow |
| Deployment setup guide | Mirrors Phase 10 scripts with explanatory prose |

### API Reference Generation

- **REST:** Use **Redocly CLI** (`npx @redocly/cli build-docs`) to render the SiteServer OpenAPI spec into a static HTML page embedded in the Docusaurus site via an `iframe` or `docusaurus-plugin-redoc`. SiteServer already uses Swashbuckle — generate JSON spec at build time with `dotnet swagger tofile`.
- **gRPC:** Use `protoc-gen-doc` (or simple manual Markdown pages) to produce reference docs from `agent_control.proto` and `adapter.proto` — both are small and fully readable as inline Markdown tables.

### Build Requirements

- `npm run build` in `docs-site/` must exit 0 with no broken internal links.
- No gRPC toolchain required at build time for documentation generation — proto content rendered as static Markdown.

## Canonical Refs

- `.planning/ROADMAP.md` — Phase 9 deliverables and success criteria (Requirements F1–F5)
- `.planning/PROJECT.md` — Core constraints, architecture decisions
- `Helix.Orchestration.Contracts/Protos/agent_control.proto` — gRPC service: SubmitExperiment, PauseRunspace, ResumeRunspace, AbortRunspace, GetRunspaceStatus
- `Helix.Orchestration.Contracts/Protos/adapter.proto` — gRPC service: GetCapabilities, InvokeCapability, StreamTrends, HealthCheck
- `Helix.Orchestration.SiteServer/Program.cs` — Swashbuckle wiring (UseSwagger / MapSwagger)
- `Helix.Orchestration.SiteServer/Endpoints/` — CampaignEndpoints, ExperimentEndpoints, TrendEndpoints
- `Helix.Orchestration.Core/Capabilities/CapabilityDescriptor.cs` — schema types for capability reference
- `.planning/phases/phase-2/` — VapourtecR4 implementation (worked example source)
- `helix-desktop/` — Tauri app screenshots source (Phase 6 workflow builder, execution panel, trends)

## Code Context

### Proto Services

**agent_control.proto (AgentControl service):**
- `SubmitExperiment(ExperimentSubmission) → SubmissionAck`
- `PauseRunspace(RunspaceCommand) → CommandAck`
- `ResumeRunspace(RunspaceCommand) → CommandAck`
- `AbortRunspace(RunspaceCommand) → CommandAck`
- `GetRunspaceStatus(RunspaceQuery) → RunspaceStatusResponse`

**adapter.proto (DeviceAdapter service):**
- `GetCapabilities(Empty) → CapabilitiesResponse`
- `InvokeCapability(CapabilityRequest) → CapabilityResponse`
- `StreamTrends(Empty) → stream TrendValue`
- `HealthCheck(Empty) → HealthStatus`

### SiteServer REST Endpoints

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
| `/api/trends/ws` | WS | Live trend streaming |

### Docusaurus 3 Known Constraints

- Use `@docusaurus/preset-classic` v3.x.
- MDX 3 — MDX components must use `.mdx` extension or configure `format: "detect"`.
- `npm run build` runs link checking internally — all `[text](href)` links must resolve.
- Mermaid diagrams: use `@docusaurus/theme-mermaid` (built in to `@docusaurus/preset-classic` v3.3+).

## Deferred Ideas

- Live API reference from running SiteServer (Swagger UI embed) — static Redocly rendering is sufficient for Phase 9.
- Versioned docs — single version for v1.0 milestone.
- i18n / translations — out of scope v1.
