---
phase: "09"
plan: "03"
subsystem: docs-site
tags: [openapi, grpc, rest-api, swashbuckle, docusaurus]
dependency_graph:
  requires: ["09-01"]
  provides: ["docs-site/docs/developer/rest-api.md", "docs-site/docs/developer/grpc-reference.md", "docs-site/scripts/generate-openapi.js"]
  affects: ["docs-site/sidebars.ts", "Helix.Orchestration.SiteServer/Helix.Orchestration.SiteServer.csproj"]
tech_stack:
  added:
    - Swashbuckle.AspNetCore.Cli 7.* (dotnet local tool via .config/dotnet-tools.json)
  patterns:
    - prebuild npm hook runs generate-openapi.js before every docs build
    - graceful fallback: docs build succeeds even without SiteServer binary
    - MDX curly-brace HTML entity escaping for route parameters in tables
key_files:
  created:
    - docs-site/scripts/generate-openapi.js
    - docs-site/static/openapi.json
    - docs-site/docs/developer/rest-api.md
    - docs-site/docs/developer/grpc-reference.md
    - .config/dotnet-tools.json
  modified:
    - Helix.Orchestration.SiteServer/Helix.Orchestration.SiteServer.csproj
    - docs-site/package.json
    - docs-site/sidebars.ts
decisions:
  - "docusaurus-plugin-redoc v2.5.0 falls back to endpoint table: plugin requires @theme/ApiDoc which is missing without the full redocusaurus preset; fallback (plain Markdown table) satisfies F4"
  - "09-02 ran first: sidebars.ts was already updated with architecture/adapter-authoring/transport-guide; this plan appended rest-api and grpc-reference (Case A merge)"
  - ".config/dotnet-tools.json command name corrected from dotnet-swagger to swagger (actual CLI entry point)"
  - "Placeholder stubs created for architecture.md, adapter-authoring.md, transport-guide.md to fix broken sidebar from 09-02 running ahead of creating those files"
metrics:
  duration: "~25 minutes"
  completed: "2026-06-02"
  tasks_completed: 2
  files_created: 7
---

# Phase 9 Plan 03: REST API Reference, gRPC Reference, and OpenAPI Generation — Summary

REST API and gRPC reference pages added to Docusaurus docs-site, with Swashbuckle CLI wired for automated OpenAPI spec generation at build time.

## Task Status

### Task 1: Add Swashbuckle CLI to SiteServer and write generate-openapi.js script

**Status: COMPLETE**

- `Helix.Orchestration.SiteServer.csproj` — added `Swashbuckle.AspNetCore.Cli 7.*`
- `.config/dotnet-tools.json` — created with `swashbuckle.aspnetcore.cli 7.3.1`; `dotnet tool restore` succeeds
- `docs-site/scripts/generate-openapi.js` — Node.js script using only built-ins (path, fs, child_process); exits 0 if binary missing
- `docs-site/static/openapi.json` — prebuild script ran live against the SiteServer; the file contains the real generated OpenAPI 3.0.1 spec (not just the placeholder — the SiteServer was already built in the environment)
- `docs-site/package.json` — added `prebuild` and `prestart` hooks

**docusaurus-plugin-redoc — fallback used:**

Attempted to install `docusaurus-plugin-redoc v2.5.0`. Installation succeeded but the build failed with `Module not found: Can't resolve '@theme/ApiDoc'`. This component is provided by the full `redocusaurus` preset, not by the standalone plugin. Per the plan's fallback instructions, the plugin was removed. The endpoint summary table in `rest-api.md` satisfies F4 without the embedded Redocly viewer.

### Task 2: Write rest-api.md, grpc-reference.md, and update sidebars.ts

**Status: COMPLETE**

- `rest-api.md` — 13-endpoint summary table (13 actual endpoints vs. 11 in the plan; DeviceEndpoints.cs adds `/api/devices` and ExperimentEndpoints.cs adds `GET /api/experiments`), OpenAPI generation workflow, authentication section
- `grpc-reference.md` — complete tables for AgentControl (5 RPCs) and DeviceAdapter (4 RPCs), all message field tables sourced directly from proto files
- `sidebars.ts` — appended `developer/rest-api` and `developer/grpc-reference` to existing six-item Developer Guide (09-02 ran first, Case A merge)

**npm run build: exits 0**

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Fixed dotnet-tools.json command name**
- **Found during:** Task 1 — `dotnet tool restore` failed: `The command "dotnet-swagger" is not contained in the package`
- **Issue:** Plan specified `"commands": ["dotnet-swagger"]` but the actual CLI entry point is `swagger`
- **Fix:** Changed to `"commands": ["swagger"]` in `.config/dotnet-tools.json`
- **Commit:** 5bf335d

**2. [Rule 1 - Bug] docusaurus-plugin-redoc incompatibility — fallback applied**
- **Found during:** Task 1 — `npm run build` failed with missing `@theme/ApiDoc`
- **Issue:** `docusaurus-plugin-redoc v2.5.0` requires the `redocusaurus` preset to provide `@theme/ApiDoc`; it cannot be used as a standalone Docusaurus plugin
- **Fix:** Removed plugin from `package.json` and `docusaurus.config.ts`; `rest-api.md` uses endpoint summary table (F4 fallback)
- **Files modified:** `docs-site/package.json`, `docs-site/docusaurus.config.ts`
- **Commit:** 5bf335d

**3. [Rule 1 - Bug] MDX JSX expression error in rest-api.md**
- **Found during:** Task 2 — `npm run build` failed with `ReferenceError: id is not defined` during SSR
- **Issue:** `{id}` in Markdown table cells is parsed as a JSX expression by MDX; path parameter placeholders like `/api/campaigns/{id}` caused render failure
- **Fix:** Replaced `{id}` with HTML entity `&#123;id&#125;` in all endpoint table cells
- **Files modified:** `docs-site/docs/developer/rest-api.md`
- **Commit:** b0732f0

**4. [Rule 3 - Blocking] Placeholder stub docs to fix broken 09-02 sidebar**
- **Found during:** Task 2 — `npm run build` failed with "sidebar document ids do not exist" for `developer/architecture`, `developer/adapter-authoring`, `developer/transport-guide`
- **Issue:** Plan 09-02 updated `sidebars.ts` to reference these three docs but created the sidebar references before writing the actual files
- **Fix:** Created minimal placeholder stubs for all three files. These will be overwritten by 09-02 when it runs its full content.
- **Files created:** `docs-site/docs/developer/architecture.md`, `adapter-authoring.md`, `transport-guide.md`
- **Commit:** b0732f0

**5. [Rule 2 - Missing functionality] Actual endpoint count exceeds plan table**
- **Found during:** Task 2 — reading actual endpoint source files
- **Issue:** Plan's endpoint table listed 11 rows but actual source has 13: `GET /api/devices` (DeviceEndpoints.cs) and `GET /api/experiments` (ExperimentEndpoints.cs) were missing from the plan
- **Fix:** Added all 13 endpoints to the summary table in `rest-api.md`
- **Commit:** b0732f0

## gRPC Tables Accuracy

All field names, types, field numbers, and RPC signatures were sourced directly from the proto files:
- `Helix.Orchestration.Contracts/Protos/agent_control.proto` — 5 RPCs, 6 message types
- `Helix.Orchestration.Contracts/Protos/adapter.proto` — 4 RPCs, 6 message types (including Empty)

No fields were invented. The `StreamTrends` RPC is documented correctly as server streaming (`returns (stream TrendValue)`).

## Known Stubs

- `docs-site/docs/developer/architecture.md` — placeholder stub, to be replaced by plan 09-02
- `docs-site/docs/developer/adapter-authoring.md` — placeholder stub, to be replaced by plan 09-02
- `docs-site/docs/developer/transport-guide.md` — placeholder stub, to be replaced by plan 09-02

These stubs exist only to allow `npm run build` to pass while 09-02 has not yet written the full content for those pages.

## npm run build Result

**PASSED** — exits 0. The prebuild script successfully regenerated `static/openapi.json` from the live SiteServer assembly during the build run.

## Commits

| Commit | Message |
|--------|---------|
| `5bf335d` | feat(09-03): add Swashbuckle CLI, OpenAPI generation script, and placeholder spec |
| `b0732f0` | feat(09-03): add REST API reference, gRPC reference, and update sidebars |

## Self-Check: PASSED

- All required files exist on disk
- Both task commits verified in git log
- npm run build exits 0
- grpc-reference.md: AgentControl count = 4 (≥3), DeviceAdapter count = 3 (≥3)
- rest-api.md: openapi count = 5 (≥1)
- sidebars.ts: rest-api = 1, grpc-reference = 1
