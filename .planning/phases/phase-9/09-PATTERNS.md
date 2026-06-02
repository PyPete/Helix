# Phase 9: Docusaurus Documentation - Pattern Map

**Mapped:** 2026-06-02
**Files analyzed:** 12 new/modified files across docs-site scaffold, CI job, and OpenAPI export
**Analogs found:** 9 / 12

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `docs-site/package.json` | config | N/A | `helix-dashboard/package.json` | role-match |
| `docs-site/docusaurus.config.ts` | config | N/A | `helix-dashboard/vite.config.ts` | partial (same toolchain tier) |
| `docs-site/tsconfig.json` | config | N/A | `helix-desktop/tsconfig.json` | role-match |
| `docs-site/sidebars.ts` | config | N/A | none | no analog |
| `docs-site/docs/architecture/overview.md` | doc | N/A | `docs/CONCEPT.md` | exact |
| `docs-site/docs/architecture/adapter-guide.md` | doc | N/A | `docs/HANDOVER.md` §6 | exact |
| `docs-site/docs/api/rest-reference.md` | doc | request-response | `Helix.Orchestration.SiteServer/Endpoints/ExperimentEndpoints.cs` | role-match |
| `docs-site/docs/api/grpc-reference.md` | doc | request-response | `Helix.Orchestration.Contracts/Protos/agent_control.proto` | exact |
| `docs-site/docs/operator/workflow-guide.md` | doc | N/A | `docs/CONCEPT.md` §WSL | partial |
| `docs-site/static/openapi.json` | config/artifact | request-response | `Helix.Orchestration.SiteServer/Program.cs` (Swashbuckle) | exact |
| `.github/workflows/ci.yml` (new `docs` job) | config | N/A | `.github/workflows/ci.yml` `dashboard` job | exact |
| `docs-site/scripts/export-openapi.sh` | utility | N/A | none | no analog |

---

## Pattern Assignments

### `docs-site/package.json` (config)

**Analog:** `helix-dashboard/package.json` (lines 1-42)

**Toolchain convention pattern:**
```json
{
  "name": "helix-docs",
  "private": true,
  "version": "0.0.0",
  "type": "module",
  "scripts": {
    "start":  "docusaurus start",
    "build":  "docusaurus build",
    "serve":  "docusaurus serve",
    "clear":  "docusaurus clear"
  }
}
```

Key conventions from existing front-end projects:
- `"private": true` — all internal packages use this
- `"type": "module"` — both `helix-desktop` and `helix-dashboard` use ES module type
- Node.js version in CI is pinned to `20` (see `.github/workflows/ci.yml` line 100)
- `npm ci` (not `npm install`) used in all CI jobs — requires a committed `package-lock.json`
- TypeScript version: `helix-dashboard` uses `~6.0.2`; `helix-desktop` uses `^5.5.0` — use `^5.5.0` for Docusaurus 3 compatibility

**Docusaurus 3 dependencies to add:**
```json
{
  "dependencies": {
    "@docusaurus/core": "^3.7.0",
    "@docusaurus/preset-classic": "^3.7.0",
    "docusaurus-plugin-redoc": "^1.0.0",
    "redocusaurus": "^2.1.0",
    "react": "^18.3.0",
    "react-dom": "^18.3.0"
  },
  "devDependencies": {
    "@docusaurus/module-type-aliases": "^3.7.0",
    "@docusaurus/tsconfig": "^3.7.0",
    "@docusaurus/types": "^3.7.0",
    "typescript": "^5.5.0"
  }
}
```

Note: Docusaurus 3 pins React 18, not 19. Do not copy React 19 from `helix-dashboard`.

---

### `docs-site/docusaurus.config.ts` (config)

**Analog:** `helix-dashboard/vite.config.ts` (lines 1-17) — same toolchain config tier

**TypeScript config file convention:**
```typescript
// helix-dashboard/vite.config.ts lines 1-4
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
```

Docusaurus uses the same named-export pattern:
```typescript
import type { Config } from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'Helix Orchestration',
  tagline: 'Distributed lab automation platform',
  url: 'https://helix.internal',
  baseUrl: '/',
  organizationName: 'helix',
  projectName: 'helix-orchestration',

  presets: [
    [
      'classic',
      {
        docs: { sidebarPath: './sidebars.ts' },
        theme: { customCss: './src/css/custom.css' },
      } satisfies Preset.Options,
    ],
  ],
};

export default config;
```

**OpenAPI plugin wiring** (add to `plugins` array in docusaurus.config.ts):
```typescript
plugins: [
  [
    'redocusaurus',
    {
      specs: [{ spec: 'static/openapi.json', route: '/api/rest/' }],
      theme: { primaryColor: '#1a73e8' },
    },
  ],
],
```

---

### `docs-site/tsconfig.json` (config)

**Analog:** `helix-desktop/tsconfig.json` (lines 1-20)

**Copy base pattern from helix-desktop:**
```json
{
  "compilerOptions": {
    "target": "ES2020",
    "lib": ["ES2020"],
    "module": "ESNext",
    "skipLibCheck": true,
    "moduleResolution": "bundler",
    "strict": true,
    "noUnusedLocals": false,
    "noUnusedParameters": false
  },
  "include": ["src", "docusaurus.config.ts", "sidebars.ts"]
}
```

Note: `noUnusedLocals`/`noUnusedParameters` should be `false` for doc config files (Docusaurus generates some unused symbols in auto-generated scaffolding). Both existing tsconfigs in the project enable `strict: true` — keep that.

---

### `docs-site/docs/architecture/overview.md` (doc)

**Analog:** `docs/CONCEPT.md` (lines 1-586) — direct migration source

**Content to migrate / adapt:**
- Architecture ASCII diagram (lines 40-73 of CONCEPT.md) — copy verbatim
- Naming model: Site > Fumehood > Agent > Device > Capabilities (lines 79-84)
- Design principles table (lines 89-100)
- Communication protocols table (lines 326-339)
- Technology stack table (lines 542-554)

**Markdown convention from existing docs:**
- H1 title, then horizontal rules (`---`) between major sections
- Tables use pipe syntax (consistent in all existing docs)
- Code blocks for architecture diagrams use triple-backtick with no language tag (plain ASCII art)
- Internal cross-references use relative links: `[PHASE10](../PHASE10-GRPC-AGENT-TRANSPORT-HANDOVER.md)`

**Section structure of existing docs:**
```markdown
# Title

---

## Section

Content.

---

## Section

Content.
```

---

### `docs-site/docs/architecture/adapter-guide.md` (doc)

**Analog:** `docs/HANDOVER.md` section 6 "Device Layer — Adapters and Capabilities" (lines 141-180)

**Key content to incorporate:**

From `docs/HANDOVER.md` lines 141-179:
```markdown
## Device Adapters and Capabilities

Each device has an adapter translating vendor protocols into capabilities:

| Device | Protocol | Adapter language |
|---|---|---|
| Reactor | OPC UA | C# |
| Pump | Modbus/serial | Python or C# |
| Agilent HPLC | ICF + file-drop | C# + file watcher |
| Camera | SDK / RTSP | Node.js or C# |
| Spectrometer | Vendor SDK | Python |

Workflows never reference OPC UA nodes, SiLA 2 features, COM objects, serial commands,
or vendor SDKs. They only call:

```csharp
await ctx.Capabilities.InvokeAsync("SetTemperature", new { Value = 75 });
```
```

**Proto contract for adapter guide** — from `Helix.Orchestration.Contracts/Protos/adapter.proto` (line 1):
```protobuf
syntax = "proto3";
option csharp_namespace = "Helix.Orchestration.Contracts.Adapters";
package helix.adapter;

service DeviceAdapter {
  rpc GetCapabilities (Empty) returns (CapabilitiesResponse);
  rpc InvokeCapability (CapabilityRequest) returns (CapabilityResponse);
  rpc StreamTrends (Empty) returns (stream TrendValue);
  rpc HealthCheck (Empty) returns (HealthStatus);
}
```

The adapter guide must document:
1. How to implement `DeviceAdapter` gRPC service
2. How to register adapter capabilities with `CapabilityRegistry`
3. The `adapter.proto` contract reproduced verbatim
4. Python adapter example from `adapters/python/pump_simulator/pump_adapter.py`

---

### `docs-site/docs/api/rest-reference.md` (doc)

**Analog:** `Helix.Orchestration.SiteServer/Endpoints/ExperimentEndpoints.cs` (lines 1-153), `TrendEndpoints.cs` (lines 1-99)

**API surface to document** (extracted from endpoint files and Phase 9 handover):

From `Helix.Orchestration.SiteServer/Endpoints/ExperimentEndpoints.cs`:
- `GET  /api/experiments` — list all experiments (line 19: `group.MapGet("/", ...)`)
- `POST /api/experiments` — submit experiment, returns `RunspaceId` (line 52)
- `GET  /api/experiments/{id}` — retrieve runspace status (line 85)
- `DELETE /api/experiments/{id}` — abort runspace (line 125)

From `TrendEndpoints.cs`:
- `GET /api/trends` — list trend descriptors (line 17)
- `GET /api/trends/{id}/history?maxPoints=N` — scalar trend history (line 50)
- `GET /api/trends/ws` — WebSocket endpoint for live trend streaming (Program.cs line 64)

From `Helix.Orchestration.SiteServer/Endpoints/DeviceEndpoints.cs`:
- `GET /api/devices` — list registered devices and capabilities

From `Helix.Orchestration.SiteServer/Endpoints/CampaignEndpoints.cs`:
- Campaign CRUD endpoints (added in Phase 11)

**Swashbuckle wiring** — `Program.cs` lines 13-21:
```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Helix Site Server API",
        Version = "v1",
        Description = "REST API for Helix lab automation control plane + MQTT-over-WebSocket real-time trends"
    });
});
```

Swagger UI exposed at `/swagger` when `app.Environment.IsDevelopment()` (Program.cs lines 52-56).

**Note on auto-generation:** The REST reference page in Docusaurus will be generated by the `redocusaurus` plugin consuming `static/openapi.json`. The handwritten `rest-reference.md` is a fallback narrative; the authoritative rendered spec is the Redoc UI page.

---

### `docs-site/docs/api/grpc-reference.md` (doc)

**Analog:** `Helix.Orchestration.Contracts/Protos/agent_control.proto` (line 1) and `adapter.proto` (line 1)

**Full proto content for agent_control.proto** (to embed verbatim):
```protobuf
syntax = "proto3";
option csharp_namespace = "Helix.Orchestration.Contracts.Control";
package helix.control;

message ExperimentSubmission { string runspace_definition_json = 1; }
message SubmissionAck { bool accepted = 1; string runspace_id = 2; string message = 3; }
message RunspaceCommand { string runspace_id = 1; }
message CommandAck { bool success = 1; string message = 2; }
message RunspaceQuery { string runspace_id = 1; }
message RunspaceStatusResponse { string runspace_id = 1; string status = 2; string message = 3; }

service AgentControl {
  rpc SubmitExperiment (ExperimentSubmission) returns (SubmissionAck);
  rpc PauseRunspace (RunspaceCommand) returns (CommandAck);
  rpc ResumeRunspace (RunspaceCommand) returns (CommandAck);
  rpc AbortRunspace (RunspaceCommand) returns (CommandAck);
  rpc GetRunspaceStatus (RunspaceQuery) returns (RunspaceStatusResponse);
}
```

**Full proto content for adapter.proto** (to embed verbatim):
```protobuf
syntax = "proto3";
option csharp_namespace = "Helix.Orchestration.Contracts.Adapters";
package helix.adapter;

message Empty {}
message CapabilitiesResponse { repeated string capabilities = 1; }
message CapabilityRequest { string capability_name = 1; string parameters_json = 2; string lease_id = 3; }
message CapabilityResponse { bool success = 1; string result_json = 2; string error_message = 3; }
message TrendValue { string trend_name = 1; string value_json = 2; string type_tag = 3; int64 timestamp_utc_ticks = 4; }
message HealthStatus { bool healthy = 1; string message = 2; }

service DeviceAdapter {
  rpc GetCapabilities (Empty) returns (CapabilitiesResponse);
  rpc InvokeCapability (CapabilityRequest) returns (CapabilityResponse);
  rpc StreamTrends (Empty) returns (stream TrendValue);
  rpc HealthCheck (Empty) returns (HealthStatus);
}
```

**Transport mapping** — from `docs/PHASE10-GRPC-AGENT-TRANSPORT-HANDOVER.md` lines 40-56:
- `Mode = sim` keeps simulation/in-process (`SimAgentProxy`)
- `Mode = grpc` routes to real Agent (`GrpcAgentProxy`) at `AgentProxy:GrpcAddress`
- Error translation: RPC `NotFound` → `KeyNotFoundException`; negative ack → `InvalidOperationException`

---

### `docs-site/docs/operator/workflow-guide.md` (doc)

**Analog:** `docs/CONCEPT.md` §WSL (lines 249-283) and `docs/HANDOVER.md` §13 (lines 386-457)

**WSL structure to document** (from CONCEPT.md lines 255-263):
- `version`, `name`, `mode` fields
- `nodes[]` with declarative step definitions
- `edges[]` for sequencing
- Logical device references resolved by Site Server Device Registry

**Minimal WSL example** — from HANDOVER.md lines 427-457:
```json
{
  "version": "1.0",
  "name": "CSTR Stage 1",
  "mode": "continuous",
  "nodes": [
    { "id": "setFlow",    "type": "SetFlowRate",      "device": "pump01",    "parameters": { "flowRate": 1.2 } },
    { "id": "setTemp",    "type": "SetTemperature",   "device": "reactor01", "parameters": { "temperature": 60 } },
    { "id": "waitSteady", "type": "WaitForSteadyState","device": "reactor01", "parameters": { "window": "5m" } }
  ],
  "edges": [
    { "from": "setFlow",  "to": "setTemp" },
    { "from": "setTemp",  "to": "waitSteady" }
  ]
}
```

**LLM generation rules** — from HANDOVER.md lines 459-465:
- Valid JSON only
- Declarative intent only (no imperative code)
- `type` must map to a known workflow primitive
- `device` must use logical device name
- `edges` must reference valid node ids

**Execution flow** — from CONCEPT.md lines 266-272:
```
WSL (JSON) → Workflow Compiler (Site Server) → RunspaceDefinition (JSON) → Agent Runspace Execution
```

---

### `docs-site/static/openapi.json` (artifact)

**Analog:** `Helix.Orchestration.SiteServer/Program.cs` Swashbuckle wiring (lines 13-21, 52-56)

**How to export the spec at build time:**

Swashbuckle CLI can generate `openapi.json` from the compiled assembly without running the server:

```bash
# Install once
dotnet tool install -g Swashbuckle.AspNetCore.Cli

# Export (run from solution root)
dotnet swagger tofile \
  --output docs-site/static/openapi.json \
  Helix.Orchestration.SiteServer/bin/Release/net10.0/Helix.Orchestration.SiteServer.dll \
  v1
```

The spec document name (`"v1"`) matches the `SwaggerDoc("v1", ...)` call in `Program.cs` line 14.

**NuGet package already present:** `Swashbuckle.AspNetCore` version `7.*` is already in `Helix.Orchestration.SiteServer.csproj` line 10 — no new package needed.

**`GenerateDocumentationFile` flag:** `Helix.Orchestration.Contracts.csproj` line 7 already has `<GenerateDocumentationFile>true</GenerateDocumentationFile>`. Add the same to `Helix.Orchestration.SiteServer.csproj` so XML doc comments flow into the Swagger spec.

**Recommended build order in CI:**
1. `dotnet build Helix.Orchestration.SiteServer.csproj -c Release`
2. `dotnet swagger tofile --output docs-site/static/openapi.json ... v1`
3. `npm ci && npm run build` in `docs-site/`

---

### `.github/workflows/ci.yml` — new `docs` job (config)

**Analog:** `.github/workflows/ci.yml` `dashboard` job (lines 91-117) — same pattern: checkout → setup-node → npm ci → npm run build

**Copy the `dashboard` job pattern exactly, adapting `working-directory`:**

```yaml
docs:
  name: Docs Build
  runs-on: ubuntu-latest
  needs: build-and-test          # wait for OpenAPI export from .NET build

  steps:
    - name: Checkout
      uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 10.0.x

    - name: Build SiteServer (for OpenAPI export)
      run: dotnet build Helix.Orchestration.SiteServer/Helix.Orchestration.SiteServer.csproj -c Release --no-restore

    - name: Export OpenAPI spec
      run: |
        dotnet tool install -g Swashbuckle.AspNetCore.Cli
        dotnet swagger tofile \
          --output docs-site/static/openapi.json \
          Helix.Orchestration.SiteServer/bin/Release/net10.0/Helix.Orchestration.SiteServer.dll \
          v1

    - name: Setup Node.js
      uses: actions/setup-node@v4
      with:
        node-version: 20
        cache: npm
        cache-dependency-path: docs-site/package-lock.json

    - name: Install dependencies
      working-directory: docs-site
      run: npm ci

    - name: Build docs
      working-directory: docs-site
      run: npm run build

    - name: Upload docs artifact
      uses: actions/upload-artifact@v4
      with:
        name: helix-docs
        path: docs-site/build/
        retention-days: 30
```

**Key CI conventions from existing jobs:**
- Node version: `20` (lines 100, 186 in ci.yml — consistent across all front-end jobs)
- Cache key uses `cache-dependency-path` pointing to `package-lock.json` (line 104 pattern)
- `npm ci` not `npm install` (lines 108, 219)
- `working-directory` at each npm step, not a global `defaults.run.working-directory`
- `actions/upload-artifact@v4` with `retention-days` set (7 for test results, 30 for publish artifacts — use 30 for docs)
- `needs: build-and-test` ensures .NET restore cache is already populated

---

### `docs-site/scripts/export-openapi.sh` (utility)

**No analog in codebase.** Closest reference is the version-stamping scripts in `helix-desktop/scripts/`:
```
helix-desktop/scripts/stamp-version.js
helix-desktop/scripts/stamp-cargo-version.js
```

Those scripts are Node.js (`node scripts/stamp-version.js <arg>`). The OpenAPI export script can follow the same pattern or be a plain shell script run via `package.json` scripts entry.

**Recommended approach** — add a `package.json` scripts entry rather than a separate file:
```json
"scripts": {
  "export-openapi": "dotnet swagger tofile --output static/openapi.json ../Helix.Orchestration.SiteServer/bin/Release/net10.0/Helix.Orchestration.SiteServer.dll v1"
}
```

---

## Shared Patterns

### npm / Node toolchain conventions
**Source:** `helix-dashboard/package.json` lines 1-42, `helix-desktop/package.json` lines 1-37, `.github/workflows/ci.yml` lines 91-117

**Apply to:** `docs-site/package.json`, docs CI job

```
- "private": true on all internal packages
- "type": "module" for ES module projects
- Node.js 20 in CI (actions/setup-node@v4 with node-version: 20)
- npm ci in CI (not npm install)
- package-lock.json must be committed for CI cache to work
- cache-dependency-path: <subdir>/package-lock.json pattern for npm cache
```

### TypeScript strictness
**Source:** `helix-desktop/tsconfig.json` lines 12-17

**Apply to:** `docs-site/tsconfig.json`

```json
"strict": true,
"noFallthroughCasesInSwitch": true,
"skipLibCheck": true,
"moduleResolution": "bundler"
```

### CI job structure
**Source:** `.github/workflows/ci.yml` lines 91-117 (`dashboard` job)

**Apply to:** `docs` CI job

Pattern:
1. `actions/checkout@v4`
2. `actions/setup-node@v4` with `node-version: 20` and `cache: npm`
3. `npm ci` (not install)
4. build step
5. `actions/upload-artifact@v4` with `retention-days`

### Markdown documentation style
**Source:** `docs/CONCEPT.md`, `docs/HANDOVER.md`, `docs/PHASE9-SITE-SERVER-HANDOVER.md`

**Apply to:** all `.md` files under `docs-site/docs/`

```
- H1 title at top, no front-matter (existing docs have no YAML front-matter)
- Horizontal rule (---) between major sections
- Tables use GFM pipe syntax with header separator row
- Code blocks with language tag: ```csharp, ```json, ```protobuf, ```bash
- ASCII architecture diagrams in plain ``` blocks (no language tag)
- Bold for inline emphasis on key terms
- Relative links between docs files
```

Note: Docusaurus uses its own front-matter system. Add front-matter (`title`, `sidebar_position`) to migrated content even though existing docs don't have it — required for sidebar ordering.

### Error response pattern (for REST reference)
**Source:** `Helix.Orchestration.SiteServer/Endpoints/ExperimentEndpoints.cs` lines 39-45

**Apply to:** REST API reference documentation

```csharp
return Results.Problem(
    title: "Failed to retrieve experiment list",
    detail: ex.Message,
    statusCode: 500);
```

All endpoints use RFC 7807 `ProblemDetails` (`Results.Problem(...)`) for error responses. Document this as the uniform error shape in the API reference.

---

## Existing Docs Inventory (Migration Sources)

All files under `docs/` are candidates for migration or cross-referencing from `docs-site/`:

| File | Content | Target in docs-site |
|---|---|---|
| `docs/CONCEPT.md` | Full architecture, data model, WSL, subsystems | `docs/architecture/overview.md` (base content) |
| `docs/HANDOVER.md` | Infrastructure decisions, adapter guide, technology stack | `docs/architecture/adapter-guide.md` + `docs/architecture/decisions.md` |
| `docs/CI.md` | Branch model, CI job descriptions | `docs/contributing/ci.md` |
| `docs/PHASE9-SITE-SERVER-HANDOVER.md` | Site server API surface, MQTT bridge, how to run | `docs/architecture/site-server.md` |
| `docs/PHASE10-GRPC-AGENT-TRANSPORT-HANDOVER.md` | gRPC transport, `IAgentProxy` seam | `docs/architecture/agent-transport.md` |
| `docs/PHASE13-DEVICE-INTEGRATION-LAYER-HANDOVER.md` | Device integration, transport hierarchy | `docs/architecture/device-integration.md` |
| `docs/PHASE14-RECORDER-SERVICE-HANDOVER.md` | Recorder service | `docs/architecture/recorder.md` |
| `docs/README.md` | Index listing all handover docs | Can be deleted (replaced by sidebar) |

---

## No Analog Found

| File | Role | Data Flow | Reason |
|---|---|---|---|
| `docs-site/sidebars.ts` | config | N/A | No sidebar config pattern exists; project has no Docusaurus precedent |
| `docs-site/scripts/export-openapi.sh` | utility | N/A | No shell/Node utility scripts for API export exist; closest are version-stamp scripts in `helix-desktop/scripts/` |

---

## Metadata

**Analog search scope:** `/c/Users/u643664/source/repos/CSharpGitLab/Helix/` — all directories
**Files scanned:** 28 source files read; 6 directories listed
**Pattern extraction date:** 2026-06-02

**OpenAPI export tool:** `Swashbuckle.AspNetCore.Cli` (global dotnet tool) — not currently installed; must be added to CI and developer setup instructions.

**Proto files location:** `Helix.Orchestration.Contracts/Protos/agent_control.proto` and `adapter.proto` — both single-line formatted (all messages and service on one line each). The gRPC reference doc should reformat these with proper indentation for readability.

**Key constraint:** SiteServer targets `net10.0` (`.csproj` line 4) — Swashbuckle CLI and `dotnet swagger tofile` must be invoked with the same SDK version used in the CI `build-and-test` job (`.github/workflows/ci.yml` line 50: `dotnet-version: 10.0.x`).
