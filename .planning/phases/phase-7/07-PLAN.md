---
phase: "7"
status: planned
waves: 3
tasks: 26
requirements: [E1, E2, E3, E4, E5, E6]
---

# Phase 7: Site-Level React Dashboard — Plan

## Phase Goal

A site manager can open a web browser, navigate four pages, manage campaigns, browse
experiment history, and watch live trend charts without installing any desktop software.

---

## Source Coverage Audit

| ID   | Source  | Description                                           | Covered by |
|------|---------|-------------------------------------------------------|------------|
| E1   | ROADMAP | React + TypeScript SPA scaffold (Vite), static files  | 07-0-001, 07-0-002, 07-0-003 |
| E2   | ROADMAP | Site overview: fumehood list, status, active count    | 07-2-001, 07-3-001 |
| E3   | ROADMAP | Campaign management: create, start, monitor, abort    | 07-2-002, 07-3-002 |
| E4   | ROADMAP | Experiment history browser                            | 07-0-009, 07-0-010, 07-2-003, 07-3-003 |
| E5   | ROADMAP | Live trend visualisation via WebSocket                | 07-0-007, 07-0-008, 07-1-002, 07-2-004, 07-3-004 |
| E6   | ROADMAP | Analytical data viewer (spectrum/DAD placeholder)     | 07-2-004, 07-3-004 |
| D-01 | CONTEXT | Chart: SciChart.js 5.2.32 + scichart-react, licence wiring | 07-0-002, 07-1-004 |
| D-02 | CONTEXT | Live trends: raw WebSocket on /api/trends/ws          | 07-0-007, 07-0-008, 07-1-002 |
| D-03 | CONTEXT | Navigation: React Router v7 (react-router), 4 routes  | 07-0-002, 07-1-003, 07-1-004 |
| D-04 | CONTEXT | Chrome: dark IDE CSS vars + top navbar (not Ribbon)   | 07-0-004, 07-1-003 |
| D-05 | CONTEXT | Fumehood data: placeholder/simulated in Phase 7       | 07-2-001 |
| D-06 | CONTEXT | Types: copy TrendValue + RunspaceEvent from agent.ts  | 07-1-001 |
| D-07 | CONTEXT | Test setup: Vitest 4.1.7 + Testing Library            | 07-0-003, 07-0-005, 07-0-006 |
| API1 | RESEARCH| GET /api/experiments missing — add GetAllAsync        | 07-0-009, 07-0-010 |
| API2 | RESEARCH| GET /api/trends/ws missing — WebSocket relay          | 07-0-007, 07-0-008 |

No deferred ideas or out-of-scope items are planned.

---

## STRIDE Threat Register

| Threat ID | Category | Component | Disposition | Mitigation |
|-----------|----------|-----------|-------------|------------|
| T-7-01 | Spoofing | Campaign/Experiment name fields (POST body) | mitigate | React escapes by default; never dangerouslySetInnerHTML |
| T-7-02 | Tampering | WebSocket message dispatch (useTrendWebSocket) | mitigate | Validate frame.type === "trend-value" before dispatch; ignore unknown types |
| T-7-03 | Information Disclosure | SiteServer WebSocket origin exposure | mitigate | Add "http://localhost:5174" to AllowedOrigins; do not surface stack traces in UI |
| T-7-04 | Denial of Service | WebSocket client dictionary (_clients) growth | mitigate | TrendWebSocketManager removes dead sockets on every BroadcastAsync |
| T-7-05 | Tampering | npm install (scichart, scichart-react, react-router) | mitigate | All packages Approved in RESEARCH.md Package Legitimacy Audit; no checkpoint required |

---

## Wave 0 — Backend Prerequisites + Scaffold

Wave 0 tasks must all complete before Wave 1 begins. Three parallel tracks:
- Track A: frontend scaffold (07-0-001 through 07-0-006)
- Track B: WebSocket backend (07-0-007, 07-0-008)
- Track C: experiment list backend (07-0-009, 07-0-010)

All tracks have zero C# / TypeScript file overlap and can execute simultaneously.

---

### Task 07-0-001 — Scaffold helix-dashboard/ Vite project

**ID:** `07-0-001`
**Files:**
- `helix-dashboard/` (new directory via `npm create vite@latest`)
- `helix-dashboard/package.json`
- `helix-dashboard/tsconfig.json`
- `helix-dashboard/tsconfig.node.json`
- `helix-dashboard/index.html`

**Dependencies:** none

**Steps:**

1. From the repo root run:
   `npm create vite@latest helix-dashboard -- --template react-ts`
   Accept all prompts.

2. Open `helix-dashboard/package.json`. Set `"name": "helix-dashboard"`.
   Add to scripts: `"copy:wasm": "node scripts/copy-wasm.js"`
   Add to scripts: `"postinstall": "npm run copy:wasm"`

3. Open `helix-dashboard/tsconfig.json`. Confirm these flags match helix-desktop:
   `"target": "ES2020"`, `"strict": true`, `"noUnusedLocals": true`,
   `"noUnusedParameters": true`. The react-ts template produces them; adjust only if different.

4. Open `helix-dashboard/index.html`. Change `<title>` to `Helix Dashboard`.
   Keep `<div id="root">` and `<script type="module" src="/src/main.tsx">` unchanged.

5. Delete scaffold boilerplate that will be replaced:
   `src/App.css`, `src/index.css`, `src/App.tsx`,
   `src/assets/react.svg`, `src/main.tsx`.

**Acceptance criteria:**
- `package.json` has `"name": "helix-dashboard"` (grep)
- `package.json` has `"postinstall"` script (grep)
- `index.html` title is `Helix Dashboard` (grep)
- `tsconfig.json` has `"strict": true` (grep)

**Test command:**
`grep -c '"name": "helix-dashboard"' "$dir/../../../helix-dashboard/package.json" 2>/dev/null || echo "file not yet created"`

---

### Task 07-0-002 — Install npm dependencies

**ID:** `07-0-002`
**Files:** `helix-dashboard/package.json` (updated), `helix-dashboard/package-lock.json` (generated)

**Dependencies:** `07-0-001`

**Steps:**

1. `cd helix-dashboard`

2. Install runtime deps (pin exact versions from RESEARCH.md):
   `npm install --ignore-scripts scichart@5.2.32 scichart-react@1.0.0 react-router@7.16.0`
   (`--ignore-scripts` skips postinstall until the copy-wasm script exists)

3. Install devDeps (mirror helix-desktop exactly):
   `npm install --save-dev vitest@^4.1.7 @vitest/coverage-v8@^4.1.7 @testing-library/react@^16.3.2 @testing-library/user-event@^14.6.1 @testing-library/jest-dom@^6.9.1 jsdom@^29.1.1`

4. Verify: `ls node_modules/scichart/_wasm/`
   Expect: `scichart2d.wasm`, `scichart2d-nosimd.wasm`, `scichart2d.js`

**Acceptance criteria:**
- `node_modules/scichart/_wasm/scichart2d.wasm` exists
- `package.json` dependencies contains `"scichart": "5.2.32"` (grep)
- `package.json` dependencies contains `"react-router": "7.16.0"` (grep)
- `package.json` devDependencies contains `"vitest"` (grep)

**Test command:**
`cd helix-dashboard && node -e "require('./node_modules/scichart/package.json')" && echo OK`

---

### Task 07-0-003 — WASM copy script + vite.config.ts + vitest.config.ts + test/setup.ts

**ID:** `07-0-003`
**Files:**
- `helix-dashboard/scripts/copy-wasm.js` (new)
- `helix-dashboard/vite.config.ts` (replace scaffold)
- `helix-dashboard/vitest.config.ts` (new)
- `helix-dashboard/src/test/setup.ts` (new)

**Dependencies:** `07-0-002`

**Steps:**

1. Create `helix-dashboard/scripts/copy-wasm.js`:

```javascript
const fs = require("fs");
const path = require("path");
const src  = path.join(__dirname, "..", "node_modules", "scichart", "_wasm");
const dest = path.join(__dirname, "..", "public");
fs.mkdirSync(dest, { recursive: true });
["scichart2d.wasm", "scichart2d-nosimd.wasm", "scichart2d.js"].forEach((f) => {
  fs.copyFileSync(path.join(src, f), path.join(dest, f));
  console.log("Copied", f);
});
```

2. Run `node scripts/copy-wasm.js` from `helix-dashboard/`.
   Verify `public/scichart2d.wasm` exists.

3. Replace `helix-dashboard/vite.config.ts` with:

```typescript
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5174,
    strictPort: true,
    proxy: {
      // WS entry MUST precede generic /api (first-match wins in Vite proxy)
      "/api/trends/ws": { target: "ws://localhost:5000", ws: true, changeOrigin: true },
      "/api": { target: "http://localhost:5000", changeOrigin: true },
    },
  },
  envPrefix: ["VITE_"],
  build: { target: "es2020", minify: "esbuild", sourcemap: false },
});
```
Note: verify SiteServer dev port from `launchSettings.json`; update proxy target if not 5000.

4. Create `helix-dashboard/vitest.config.ts` (copy verbatim from helix-desktop):

```typescript
import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  test: {
    globals: true,
    environment: "jsdom",
    setupFiles: ["./src/test/setup.ts"],
  },
});
```

5. Create `helix-dashboard/src/test/setup.ts` (SciChart needs WebGL+WASM — mock both for jsdom):

```typescript
import "@testing-library/jest-dom";
import React from "react";

vi.mock("scichart", () => ({
  SciChartSurface: {
    setRuntimeLicenseKey: vi.fn(),
    create: vi.fn().mockResolvedValue({
      sciChartSurface: {
        xAxes: { add: vi.fn() },
        yAxes: { add: vi.fn() },
        renderableSeries: { add: vi.fn() },
        delete: vi.fn(),
      },
      wasmContext: {},
    }),
    loadWasmFromCDN: vi.fn(),
  },
  NumericAxis: vi.fn(),
  FastLineRenderableSeries: vi.fn(),
  XyDataSeries: vi.fn(() => ({ append: vi.fn() })),
  SciChartJsNavyTheme: vi.fn(),
}));

vi.mock("scichart-react", () => ({
  SciChartReact: () =>
    React.createElement("div", { "data-testid": "scichart-surface" }),
}));
```

6. Run `npm test -- --run`. Expect 0 test files found, exit 0.

**Acceptance criteria:**
- `public/scichart2d.wasm` exists after running copy-wasm.js
- `vitest.config.ts` contains `environment: "jsdom"` (grep)
- `src/test/setup.ts` contains `vi.mock("scichart"` (grep)
- `vite.config.ts` has `/api/trends/ws` proxy before `/api` (line-order grep)
- `npm test -- --run` exits 0

**Test command:** `cd helix-dashboard && npm test -- --run`

---

### Task 07-0-004 — Copy CSS theme to helix-dashboard

**ID:** `07-0-004`
**Files:** `helix-dashboard/src/themes/default.css` (new)

**Dependencies:** `07-0-001`

**Steps:**

1. Create `helix-dashboard/src/themes/` directory.

2. Copy verbatim:
   Source:      `.claude/skills/sketch-findings-Helix/sources/themes/default.css`
   Destination: `helix-dashboard/src/themes/default.css`
   Do NOT modify any variable names or values.

3. Verify representative properties:
   `grep -c "--color-bg" helix-dashboard/src/themes/default.css` (must return 1)
   `grep -c "--space-4" helix-dashboard/src/themes/default.css` (must return 1)

**Acceptance criteria:**
- File exists with `--color-bg:` (grep)
- File exists with `--color-primary:` (grep)
- File is byte-identical to the skill source (diff exits 0)

**Test command (bash):**
`diff ".claude/skills/sketch-findings-Helix/sources/themes/default.css" "helix-dashboard/src/themes/default.css" && echo IDENTICAL`

---

### Task 07-0-005 — App smoke test skeleton (RED)

**ID:** `07-0-005`
**Files:** `helix-dashboard/src/App.test.tsx` (new)

**Dependencies:** `07-0-003`

**Note:** Intentionally RED — fails until 07-1-004 creates App.tsx. TDD gate: write contract first.

**Steps:**

1. Create `helix-dashboard/src/App.test.tsx`:

```typescript
import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { App } from "./App";

describe("App", () => {
  it("renders without crashing and shows navbar", () => {
    render(
      <MemoryRouter initialEntries={["/"]}>
        <App />
      </MemoryRouter>
    );
    expect(screen.getByRole("navigation")).toBeInTheDocument();
  });

  it("renders the Overview nav link", () => {
    render(
      <MemoryRouter initialEntries={["/"]}>
        <App />
      </MemoryRouter>
    );
    expect(screen.getByRole("link", { name: /overview/i })).toBeInTheDocument();
  });
});
```

2. Run `npm test -- --run`. Two tests FAIL (module not found). That is the expected RED state.

**Acceptance criteria:**
- `src/App.test.tsx` exists
- `npm test -- --run` exits non-zero with the two App tests failing

**Test command:** `cd helix-dashboard && npm test -- --run 2>&1 | grep -c "FAIL\|Cannot find"`

---

### Task 07-0-006 — useTrendWebSocket test skeleton (RED)

**ID:** `07-0-006`
**Files:** `helix-dashboard/src/hooks/useTrendWebSocket.test.ts` (new)

**Dependencies:** `07-0-003`

**Note:** Intentionally RED — fails until 07-1-002 creates the hook.

**Steps:**

1. Create `helix-dashboard/src/hooks/` directory.

2. Create `helix-dashboard/src/hooks/useTrendWebSocket.test.ts`
   (adapted from `helix-desktop/src/hooks/useAgentWebSocket.test.ts` — same vi.hoisted
   WebSocket mock, narrowed to trend-value frames, URL suffix /api/trends/ws):

```typescript
import { describe, it, expect, beforeEach, afterEach, vi } from "vitest";
import { renderHook, waitFor, act } from "@testing-library/react";
import { useTrendWebSocket } from "./useTrendWebSocket";

const { mockClose } = vi.hoisted(() => ({ mockClose: vi.fn() }));

interface MockWs {
  onopen: (() => void) | null;
  onclose: (() => void) | null;
  onerror: (() => void) | null;
  onmessage: ((e: { data: string }) => void) | null;
  close: typeof mockClose;
}

let lastWs: MockWs | null = null;

describe("useTrendWebSocket", () => {
  beforeEach(() => {
    lastWs = null;
    vi.stubGlobal("WebSocket", vi.fn(function (this: MockWs) {
      this.onopen = null;
      this.onclose = null;
      this.onerror = null;
      this.onmessage = null;
      this.close = mockClose;
      lastWs = this;
    }));
  });

  afterEach(() => {
    vi.restoreAllMocks();
    mockClose.mockReset();
  });

  it("starts disconnected", () => {
    const { result } = renderHook(() => useTrendWebSocket("http://localhost:5000"));
    expect(result.current.connected).toBe(false);
  });

  it("sets connected=true on open", async () => {
    const { result } = renderHook(() => useTrendWebSocket("http://localhost:5000"));
    act(() => lastWs!.onopen?.());
    await waitFor(() => expect(result.current.connected).toBe(true));
  });

  it("appends trend-value frame to trendValues", async () => {
    const { result } = renderHook(() => useTrendWebSocket("http://localhost:5000"));
    act(() => lastWs!.onopen?.());
    act(() => lastWs!.onmessage?.({
      data: JSON.stringify({
        type: "trend-value", trendName: "Temp",
        value: 37.5, unit: "C", timestamp: "2026-06-02T00:00:00Z",
      }),
    }));
    await waitFor(() => expect(result.current.trendValues).toHaveLength(1));
    expect(result.current.trendValues[0].trendName).toBe("Temp");
  });

  it("ignores unknown frame types", async () => {
    const { result } = renderHook(() => useTrendWebSocket("http://localhost:5000"));
    act(() => lastWs!.onopen?.());
    act(() => lastWs!.onmessage?.({ data: JSON.stringify({ type: "unknown" }) }));
    await waitFor(() => expect(result.current.trendValues).toHaveLength(0));
  });

  it("calls ws.close on unmount", () => {
    const { unmount } = renderHook(() => useTrendWebSocket("http://localhost:5000"));
    unmount();
    expect(mockClose).toHaveBeenCalledOnce();
  });

  it("constructs URL with ws:// and /api/trends/ws path", () => {
    renderHook(() => useTrendWebSocket("http://localhost:5000"));
    expect(vi.mocked(WebSocket)).toHaveBeenCalledWith("ws://localhost:5000/api/trends/ws");
  });
});
```

3. Run `npm test -- --run`. All 6 tests FAIL (module not found). Expected RED.

**Acceptance criteria:**
- `src/hooks/useTrendWebSocket.test.ts` exists with `"/api/trends/ws"` string (grep)
- `npm test -- --run` reports 6 failing tests for this file

**Test command:** `cd helix-dashboard && npm test -- --run 2>&1 | grep "useTrendWebSocket"`

---

### Task 07-0-007 — TrendWebSocketManager singleton

**ID:** `07-0-007`
**Files:** `Helix.Orchestration.SiteServer/WebSockets/TrendWebSocketManager.cs` (new)

**Dependencies:** none (parallel with frontend scaffold)

**Steps:**

1. Create directory `Helix.Orchestration.SiteServer/WebSockets/`.

2. Create `TrendWebSocketManager.cs` (modelled on AgentService.cs ConcurrentDictionary
   pattern, lines 27/60-99/118-135; passive singleton, not a BackgroundService):

```csharp
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Core.Trends;

namespace Helix.Orchestration.SiteServer.WebSockets;

/// <summary>
/// Singleton WebSocket relay for live trend streaming to dashboard clients.
/// MqttTrendBridge calls BroadcastAsync on each MQTT tick.
/// </summary>
public sealed class TrendWebSocketManager
{
    private readonly ConcurrentDictionary<string, WebSocket> _clients = new();

    public void Register(string clientId, WebSocket socket) =>
        _clients[clientId] = socket;

    public void Remove(string clientId) =>
        _clients.TryRemove(clientId, out _);

    public async Task HandleClientAsync(WebSocket ws, CancellationToken ct)
    {
        var clientId = Guid.NewGuid().ToString("N");
        Register(clientId, ws);
        try
        {
            var buffer = new byte[256];
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(
                        WebSocketCloseStatus.NormalClosure, "done",
                        CancellationToken.None);
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            Remove(clientId);
        }
    }

    public async Task BroadcastAsync(TrendValue value, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        });
        var bytes = Encoding.UTF8.GetBytes(json);

        foreach (var (clientId, ws) in _clients.ToArray())
        {
            if (ws.State != WebSocketState.Open) { Remove(clientId); continue; }
            try
            {
                await ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
            }
            catch
            {
                Remove(clientId);
            }
        }
    }
}
```

3. `dotnet build Helix.Orchestration.SiteServer` exits 0.

**Acceptance criteria:**
- File exists with `HandleClientAsync` and `BroadcastAsync` public methods (grep)
- `dotnet build` exits 0

**Test command:**
`dotnet build Helix.Orchestration.SiteServer/Helix.Orchestration.SiteServer.csproj --no-restore -v q`

---

### Task 07-0-008 — Wire WebSocket in Program.cs + extend MqttTrendBridge

**ID:** `07-0-008`
**Files:**
- `Helix.Orchestration.SiteServer/Program.cs` (modify)
- `Helix.Orchestration.SiteServer/Mqtt/MqttTrendBridge.cs` (modify)

**Dependencies:** `07-0-007`

**Steps:**

1. Open `Program.cs`. Add after `AddHostedService<MqttTrendBridge>()`:

```csharp
builder.Services.AddSingleton<TrendWebSocketManager>();
builder.Services.AddWebSockets(options =>
{
    options.AllowedOrigins.Add("http://localhost:5174");
    options.KeepAliveInterval = TimeSpan.FromSeconds(30);
});
```
Add `using Helix.Orchestration.SiteServer.WebSockets;` at the top.

2. After `app.UseHttpsRedirection();` and BEFORE all `app.Map*` calls:
   `app.UseWebSockets();`

3. After `app.MapCampaignEndpoints();` add the WS handler:

```csharp
var trendWsManager = app.Services.GetRequiredService<TrendWebSocketManager>();
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/api/trends/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            using var ws = await context.WebSockets.AcceptWebSocketAsync();
            await trendWsManager.HandleClientAsync(ws, context.RequestAborted);
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
    else { await next(context); }
});
```

4. Open `MqttTrendBridge.cs`. Add field and constructor parameter:

```csharp
private readonly TrendWebSocketManager _trendWsManager;

public MqttTrendBridge(
    ITrendProvider trendProvider,
    TrendWebSocketManager trendWsManager,   // add
    IOptions<MqttBridgeOptions> options,
    ILogger<MqttTrendBridge> logger)
{
    _trendProvider  = trendProvider;
    _trendWsManager = trendWsManager;       // add
    _options        = options;
    _logger         = logger;
}
```

5. In `PublishTrendValue`, after the fire-and-forget MQTT publish line, add:
   `_ = _trendWsManager.BroadcastAsync(value, ct);`
   (`value` is the TrendValue parameter already in scope.)

6. `dotnet build Helix.Orchestration.SiteServer` exits 0.

**Acceptance criteria:**
- `Program.cs` contains `app.UseWebSockets()` (grep)
- `Program.cs` contains `AddSingleton<TrendWebSocketManager>()` (grep)
- `MqttTrendBridge.cs` constructor accepts `TrendWebSocketManager` (grep)
- `MqttTrendBridge.cs` calls `_trendWsManager.BroadcastAsync` (grep)
- `dotnet build` exits 0

**Test command:**
`dotnet build Helix.Orchestration.SiteServer/Helix.Orchestration.SiteServer.csproj --no-restore -v q`

---

### Task 07-0-009 — IExperimentRepository.GetAllAsync + implementation

**ID:** `07-0-009`
**Files:**
- `Helix.Orchestration.Infrastructure/Persistence/IExperimentRepository.cs` (modify)
- `Helix.Orchestration.Infrastructure/Persistence/PostgresExperimentRepository.cs` (modify)

**Dependencies:** none (parallel with frontend and WS backend)

**Steps:**

1. Open `IExperimentRepository.cs`. Add one method:
   `Task<IReadOnlyList<ExperimentRecord>> GetAllAsync(CancellationToken ct = default);`

2. Open `PostgresExperimentRepository.cs`. Add after `GetByCampaignIdAsync`:

```csharp
public async Task<IReadOnlyList<ExperimentRecord>> GetAllAsync(CancellationToken ct = default)
{
    const string sql = """
        SELECT
            runspace_id    AS RunspaceId,
            campaign_id    AS CampaignId,
            definition_id  AS DefinitionId,
            name           AS Name,
            status         AS Status,
            created_at     AS CreatedAt,
            started_at     AS StartedAt,
            ended_at       AS EndedAt,
            fault_message  AS FaultMessage,
            definition_json AS DefinitionJson
        FROM experiments
        ORDER BY created_at DESC;
        """;

    await using var connection = connectionFactory.Create();
    await connection.OpenAsync(ct);
    var rows = await connection.QueryAsync<ExperimentRecord>(
        new CommandDefinition(sql, cancellationToken: ct));
    return rows.ToList();
}
```

3. `dotnet build Helix.Orchestration.Infrastructure` exits 0.

**Acceptance criteria:**
- Interface contains `GetAllAsync` (grep)
- Implementation has `ORDER BY created_at DESC` (grep)
- `dotnet build` exits 0

**Test command:**
`dotnet build Helix.Orchestration.Infrastructure/Helix.Orchestration.Infrastructure.csproj --no-restore -v q`

---

### Task 07-0-010 — GET /api/experiments endpoint

**ID:** `07-0-010`
**Files:**
- `Helix.Orchestration.SiteServer/Endpoints/ExperimentEndpoints.cs` (modify)
- `Helix.Orchestration.SiteServer/Contracts/ExperimentContracts.cs` (modify)

**Dependencies:** `07-0-009`

**Steps:**

1. Open `ExperimentContracts.cs`. Add after `ExperimentStatusResponse`:

```csharp
/// <summary>Summary DTO for experiment history list (GET /api/experiments).</summary>
public sealed record ExperimentSummaryDto
{
    public required string RunspaceId   { get; init; }
    public string?         CampaignId   { get; init; }
    public required string Name         { get; init; }
    public required string Status       { get; init; }
    public required DateTimeOffset CreatedAt  { get; init; }
    public DateTimeOffset? StartedAt    { get; init; }
    public DateTimeOffset? EndedAt      { get; init; }
    public string?         FaultMessage { get; init; }
}
```

2. Open `ExperimentEndpoints.cs`. Add `using Helix.Orchestration.Infrastructure.Persistence;`
   if not present.

3. Add `MapGet("/", ...)` BEFORE the existing `MapPost("/", ...)`:

```csharp
group.MapGet("/", async Task<IResult> (
    IExperimentRepository experimentRepository,
    CancellationToken ct) =>
{
    try
    {
        var records = await experimentRepository.GetAllAsync(ct);
        var dtos = records.Select(r => new ExperimentSummaryDto
        {
            RunspaceId   = r.RunspaceId,
            CampaignId   = r.CampaignId,
            Name         = r.Name,
            Status       = r.Status,
            CreatedAt    = r.CreatedAt,
            StartedAt    = r.StartedAt,
            EndedAt      = r.EndedAt,
            FaultMessage = r.FaultMessage,
        });
        return Results.Ok(dtos);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Failed to retrieve experiment list",
            detail: ex.Message, statusCode: 500);
    }
})
.WithName("GetAllExperiments")
.WithSummary("List all experiments (history browser)")
.Produces<IEnumerable<ExperimentSummaryDto>>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status500InternalServerError);
```

4. `dotnet build Helix.Orchestration.SiteServer` exits 0.

**Acceptance criteria:**
- `ExperimentEndpoints.cs` contains `MapGet("/", async Task<IResult>` (grep)
- `ExperimentEndpoints.cs` calls `experimentRepository.GetAllAsync` (grep)
- `ExperimentContracts.cs` defines `ExperimentSummaryDto` (grep)
- `dotnet build` exits 0

**Test command:**
`dotnet build Helix.Orchestration.SiteServer/Helix.Orchestration.SiteServer.csproj --no-restore -v q`

---

## Wave 1 — App Shell + Types + Hook

All Wave 0 tasks must be green before Wave 1 begins.
Tasks 07-1-001, 07-1-002, and 07-1-003 are independent and can run in parallel.
Task 07-1-004 depends on all three.

---

### Task 07-1-001 — src/types/dashboard.ts

**ID:** `07-1-001`
**Files:** `helix-dashboard/src/types/dashboard.ts` (new)

**Dependencies:** `07-0-001`

**Steps:**

1. Create `helix-dashboard/src/types/` directory.

2. Create `helix-dashboard/src/types/dashboard.ts`.
   Copy `TrendValue` and `RunspaceEvent` verbatim from
   `helix-desktop/src/types/agent.ts` (lines 57-71). Add new types:

```typescript
// Copied verbatim from helix-desktop/src/types/agent.ts — per D-06
export interface RunspaceEvent {
  type: string;
  runspaceId: string;
  timestamp: string;
  estimatedDurationMs?: number;
  [key: string]: unknown;
}

export interface TrendValue {
  type: "trend-value";
  trendName: string;
  value: number;
  unit: string;
  timestamp: string;
}

/** Placeholder fumehood status — hardcoded in Phase 7, no API endpoint yet (D-05). */
export interface FumehoodStatus {
  id: string;
  name: string;
  status: "idle" | "running" | "faulted" | "offline";
  activeExperimentName?: string;
  lastSeenAt: string; // ISO 8601
}

export interface SiteServerConnection {
  host: string;
  port: number;
  protocol: "http" | "https";
}

export const DEFAULT_SITE_CONNECTION: SiteServerConnection = {
  host: "localhost",
  port: 5000,
  protocol: "http",
};
```

**Acceptance criteria:**
- File exports `TrendValue`, `RunspaceEvent`, `FumehoodStatus`,
  `SiteServerConnection`, `DEFAULT_SITE_CONNECTION` (grep each)
- `TrendValue.type` literal is `"trend-value"` (grep)
- `FumehoodStatus.status` union contains `"faulted"` (grep)

**Test command:** `cd helix-dashboard && npm test -- --run`

---

### Task 07-1-002 — src/hooks/useTrendWebSocket.ts

**ID:** `07-1-002`
**Files:** `helix-dashboard/src/hooks/useTrendWebSocket.ts` (new)

**Dependencies:** `07-1-001`

**Steps:**

Adapt from `helix-desktop/src/hooks/useAgentWebSocket.ts` — same useState+useEffect
structure, narrowed to trend-value frames, URL suffix /api/trends/ws:

```typescript
import { useState, useEffect } from "react";
import type { TrendValue } from "../types/dashboard";

export interface TrendWebSocketState {
  trendValues: TrendValue[];
  connected: boolean;
}

export function useTrendWebSocket(baseUrl: string): TrendWebSocketState {
  const [trendValues, setTrendValues] = useState<TrendValue[]>([]);
  const [connected, setConnected] = useState(false);

  useEffect(() => {
    const wsUrl = baseUrl.replace(/^http/, "ws") + "/api/trends/ws";
    const ws = new WebSocket(wsUrl);

    ws.onopen  = () => setConnected(true);
    ws.onclose = () => setConnected(false);
    ws.onerror = () => setConnected(false);
    ws.onmessage = (msg) => {
      try {
        const frame = JSON.parse(msg.data as string);
        if (frame.type === "trend-value") {
          setTrendValues((prev) => [...prev.slice(-200), frame as TrendValue]);
        }
      } catch {
        // ignore malformed frames
      }
    };

    return () => ws.close();
  }, [baseUrl]);

  return { trendValues, connected };
}
```

Run `npm test -- --run src/hooks/useTrendWebSocket.test.ts`. All 6 must be GREEN.

**Acceptance criteria:**
- File exports `useTrendWebSocket` (grep)
- URL suffix is `/api/trends/ws` (grep)
- Ring-buffer slice `prev.slice(-200)` present (grep)
- All 6 tests in `useTrendWebSocket.test.ts` PASS

**Test command:**
`cd helix-dashboard && npm test -- --run src/hooks/useTrendWebSocket.test.ts`

---

### Task 07-1-003 — src/components/Navbar.tsx + Layout.tsx

**ID:** `07-1-003`
**Files:**
- `helix-dashboard/src/components/Navbar.tsx` (new)
- `helix-dashboard/src/components/Layout.tsx` (new)

**Dependencies:** `07-0-004` (theme CSS must exist for var() references)

**Steps:**

1. Create `helix-dashboard/src/components/Navbar.tsx` (per D-04 — top navbar, NOT Ribbon):

```typescript
import { NavLink } from "react-router";

interface NavbarProps { connected?: boolean; }

export function Navbar({ connected = false }: NavbarProps) {
  return (
    <nav role="navigation" aria-label="Site navigation"
      style={{
        background: "var(--color-ribbon-bg)",
        borderBottom: "1px solid var(--color-border)",
        padding: "0 var(--space-4)",
        display: "flex", alignItems: "center",
        gap: "var(--space-4)", height: "40px",
      }}
    >
      <span style={{
        color: "var(--color-primary)", fontWeight: 600,
        fontSize: "var(--text-lg)", marginRight: "var(--space-6)",
      }}>Helix</span>
      <NavLink to="/">Overview</NavLink>
      <NavLink to="/campaigns">Campaigns</NavLink>
      <NavLink to="/history">History</NavLink>
      <NavLink to="/trends">Trends</NavLink>
      <span data-testid="ws-status" style={{
        marginLeft: "auto", fontSize: "var(--text-xs)",
        color: connected ? "var(--color-online)" : "var(--color-offline)",
      }}>
        {connected ? "Live" : "Offline"}
      </span>
    </nav>
  );
}
```

2. Create `helix-dashboard/src/components/Layout.tsx`:

```typescript
import { Outlet } from "react-router";
import { Navbar } from "./Navbar";

interface LayoutProps { connected?: boolean; }

export function Layout({ connected }: LayoutProps) {
  return (
    <div data-testid="app-layout" style={{
      minHeight: "100vh",
      background: "var(--color-bg)",
      color: "var(--color-text)",
      fontFamily: "var(--font-sans)",
    }}>
      <Navbar connected={connected} />
      <main style={{ padding: "var(--space-4)" }}><Outlet /></main>
    </div>
  );
}
```

3. Verify zero raw hex literals in both files:
   `grep -n "#[0-9a-fA-F]\{3,6\}" src/components/Navbar.tsx src/components/Layout.tsx`
   Must return 0 results.

**Acceptance criteria:**
- `Navbar.tsx` has `role="navigation"` and 4 NavLinks (grep)
- `Layout.tsx` has `data-testid="app-layout"` (grep)
- 0 raw hex literals in both files

**Test command:** `cd helix-dashboard && npm test -- --run`

---

### Task 07-1-004 — src/main.tsx + src/App.tsx

**ID:** `07-1-004`
**Files:**
- `helix-dashboard/src/main.tsx` (new — scaffold deleted in 07-0-001)
- `helix-dashboard/src/App.tsx` (new)

**Dependencies:** `07-1-001`, `07-1-002`, `07-1-003`

**Note:** Wave 2 page components do not exist yet. Use inline placeholder components
so App.tsx compiles and smoke tests pass. Each Wave 2 task replaces its placeholder.

**Steps:**

1. Create `helix-dashboard/src/main.tsx` (per D-01: licence init BEFORE createRoot):

```typescript
import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router";
import { SciChartSurface } from "scichart";
import { App } from "./App";
import "./themes/default.css";

// D-01: must be called once before any SciChartSurface is instantiated.
// Empty string = community mode (watermark). Set VITE_SCICHART_KEY for production.
SciChartSurface.setRuntimeLicenseKey(import.meta.env.VITE_SCICHART_KEY ?? "");

ReactDOM.createRoot(document.getElementById("root") as HTMLElement).render(
  <React.StrictMode>
    <BrowserRouter>
      <App />
    </BrowserRouter>
  </React.StrictMode>
);
```

2. Create `helix-dashboard/src/App.tsx`:

```typescript
import { Routes, Route } from "react-router";
import { Layout } from "./components/Layout";
import { useTrendWebSocket } from "./hooks/useTrendWebSocket";

// Wave 2 placeholders — replaced one-by-one as tasks 07-2-* complete
const SiteOverviewPage = () => <div data-testid="site-overview-page">Overview (placeholder)</div>;
const CampaignsPage = () => <div data-testid="campaigns-page">Campaigns (placeholder)</div>;
const HistoryPage = () => <div data-testid="history-page">History (placeholder)</div>;
const TrendsPage = () => <div data-testid="trends-page">Trends (placeholder)</div>;

export function App() {
  const baseUrl = import.meta.env.VITE_SITE_SERVER_URL ?? "http://localhost:5000";
  const { connected } = useTrendWebSocket(baseUrl);

  return (
    <Routes>
      <Route element={<Layout connected={connected} />}>
        <Route index element={<SiteOverviewPage />} />
        <Route path="campaigns" element={<CampaignsPage />} />
        <Route path="history" element={<HistoryPage />} />
        <Route path="trends" element={<TrendsPage />} />
      </Route>
    </Routes>
  );
}
```

3. Run `npm test -- --run`. Both App.test.tsx tests must be GREEN.
4. Run `npm run build`. Must exit 0.

**Acceptance criteria:**
- `main.tsx` calls `setRuntimeLicenseKey` before `createRoot` (line-number check)
- `App.tsx` exports named `App` with 4 `<Route>` entries (grep)
- Both App.test.tsx tests PASS
- `npm run build` exits 0

**Test command:**
`cd helix-dashboard && npm test -- --run src/App.test.tsx`

---

## Wave 2 — Page Components

All Wave 1 tasks must be green. Pages are independent; run in parallel.
Each task replaces its inline placeholder in App.tsx.

**Replacement pattern:** Delete the inline const, add an import from
`./pages/XxxPage`, update the Route element to pass required props.

---

### Task 07-2-001 — src/pages/SiteOverviewPage.tsx

**ID:** `07-2-001`
**Files:**
- `helix-dashboard/src/pages/SiteOverviewPage.tsx` (new)
- `helix-dashboard/src/App.tsx` (modify: replace placeholder)

**Dependencies:** `07-1-004`

**Steps:**

1. Create `helix-dashboard/src/pages/` directory.

2. Create `SiteOverviewPage.tsx` (per D-05 — hardcoded placeholder, no API call):

```typescript
import type { FumehoodStatus } from "../types/dashboard";

const PLACEHOLDER_FUMEHOODS: FumehoodStatus[] = [
  { id: "fh-001", name: "Fumehood A", status: "running",
    activeExperimentName: "Vapourtec R4 Test", lastSeenAt: new Date().toISOString() },
  { id: "fh-002", name: "Fumehood B", status: "idle",
    lastSeenAt: new Date().toISOString() },
  { id: "fh-003", name: "Fumehood C", status: "faulted",
    lastSeenAt: new Date(Date.now() - 300_000).toISOString() },
];

const STATUS_COLOR: Record<FumehoodStatus["status"], string> = {
  running: "var(--color-busy)",
  idle:    "var(--color-online)",
  faulted: "var(--color-danger)",
  offline: "var(--color-offline)",
};

export function SiteOverviewPage() {
  return (
    <section aria-label="Site Overview" data-testid="site-overview-page">
      <h1 style={{ color: "var(--color-text)", fontSize: "var(--text-xl)" }}>
        Site Overview
      </h1>
      <div style={{
        display: "grid",
        gridTemplateColumns: "repeat(auto-fill, minmax(220px, 1fr))",
        gap: "var(--space-4)", marginTop: "var(--space-4)",
      }}>
        {PLACEHOLDER_FUMEHOODS.map((fh) => (
          <div key={fh.id} data-testid="fumehood-card"
            style={{
              background: "var(--color-surface)",
              border: "1px solid var(--color-border)",
              borderRadius: "var(--radius-md)",
              padding: "var(--space-4)",
            }}
          >
            <div style={{ fontWeight: 600, color: "var(--color-text)" }}>{fh.name}</div>
            <div data-testid="fumehood-status"
              style={{ color: STATUS_COLOR[fh.status], fontSize: "var(--text-sm)" }}>
              {fh.status}
            </div>
            {fh.activeExperimentName && (
              <div style={{ color: "var(--color-text-muted)", fontSize: "var(--text-xs)" }}>
                {fh.activeExperimentName}
              </div>
            )}
          </div>
        ))}
      </div>
    </section>
  );
}
```

3. In `App.tsx`: delete inline `SiteOverviewPage` const, add import from `./pages/SiteOverviewPage`.

**Acceptance criteria:**
- File has `data-testid="site-overview-page"` (grep)
- `PLACEHOLDER_FUMEHOODS` has `"running"` and `"faulted"` entries (grep)
- 0 raw hex literals in the file
- `npm test -- --run` exits 0

**Test command:** `cd helix-dashboard && npm test -- --run`

---

### Task 07-2-002 — src/pages/CampaignsPage.tsx

**ID:** `07-2-002`
**Files:**
- `helix-dashboard/src/pages/CampaignsPage.tsx` (new)
- `helix-dashboard/src/App.tsx` (modify: pass baseUrl, replace placeholder)

**Dependencies:** `07-1-004`

**Steps:**

1. Create `CampaignsPage.tsx`. Model fetch/error pattern on
   `ExecutionPanel.tsx` lines 85-161.

   Props: `{ baseUrl: string }`

   State: `campaigns: CampaignDto[]`, `loading`, `error`, `submitting`, `newCampaignName`

   Local DTO: `interface CampaignDto { campaignId: string; name: string; status: string; }`

   Load via `useEffect([baseUrl])`: `fetch(baseUrl + "/api/campaigns")`

   Create form: `<input data-testid="campaign-name-input">` + `<button data-testid="create-campaign-btn">`
   On submit: `POST /api/campaigns` with `{ name: newCampaignName }`, reload on success.

   Per-row buttons: Start (`POST /api/campaigns/${id}/start`), Delete (`DELETE /api/campaigns/${id}`).

   Error: `{error && <p role="alert">{error}</p>}`

   Root: `<section aria-label="Campaign Management" data-testid="campaigns-page">`

   Do NOT use `dangerouslySetInnerHTML`.

2. In `App.tsx`: delete inline const, import from `./pages/CampaignsPage`,
   route: `<CampaignsPage baseUrl={baseUrl} />`

**Acceptance criteria:**
- File has `data-testid="campaigns-page"` (grep)
- File calls `fetch(baseUrl + "/api/campaigns"` (grep)
- File uses `/start` in URL path (grep)
- Error uses `role="alert"` (grep)
- `npm test -- --run` exits 0

**Test command:** `cd helix-dashboard && npm test -- --run`

---

### Task 07-2-003 — src/pages/HistoryPage.tsx

**ID:** `07-2-003`
**Files:**
- `helix-dashboard/src/pages/HistoryPage.tsx` (new)
- `helix-dashboard/src/App.tsx` (modify: pass baseUrl, replace placeholder)

**Dependencies:** `07-1-004`, `07-0-010`

**Steps:**

1. Create `HistoryPage.tsx`. Calls `GET /api/experiments` (from 07-0-010).

   Props: `{ baseUrl: string }`

   Local DTO (camelCase, matches ExperimentSummaryDto):
```typescript
interface ExperimentSummaryDto {
  runspaceId: string; campaignId?: string; name: string; status: string;
  createdAt: string; startedAt?: string; endedAt?: string; faultMessage?: string;
}
```

   Load via `useEffect([baseUrl])`: `fetch(baseUrl + "/api/experiments")`

   Display each record with `data-testid="experiment-row"`.

   Empty state: `<p>No experiments found.</p>` when array is empty.

   Error: `{error && <p role="alert">{error}</p>}`

   Root: `<section aria-label="Experiment History" data-testid="history-page">`

2. In `App.tsx`: delete inline const, import from `./pages/HistoryPage`,
   route: `<HistoryPage baseUrl={baseUrl} />`

**Acceptance criteria:**
- File has `data-testid="history-page"` (grep)
- File calls `fetch(baseUrl + "/api/experiments"` (grep)
- File renders `data-testid="experiment-row"` (grep)
- Empty state text matches `/no experiments/i` (grep)
- `npm test -- --run` exits 0

**Test command:** `cd helix-dashboard && npm test -- --run`

---

### Task 07-2-004 — src/pages/TrendsPage.tsx + src/components/SciChartLine.tsx

**ID:** `07-2-004`
**Files:**
- `helix-dashboard/src/components/SciChartLine.tsx` (new)
- `helix-dashboard/src/pages/TrendsPage.tsx` (new)
- `helix-dashboard/src/App.tsx` (modify: pass trendValues, replace placeholder)

**Dependencies:** `07-1-002`, `07-1-004`

**Steps:**

1. Create `SciChartLine.tsx`. Rules from RESEARCH.md:
   - `initChart` MUST use `useCallback(fn, [])` — stable reference prevents StrictMode double-init
   - `fifoCapacity: 500` on XyDataSeries (per D-01, Pattern 3)
   - Feed data via `seriesRef.current.append(x, y)` in `useEffect([trendValues])`, NOT React state

```typescript
import { useRef, useEffect, useCallback } from "react";
import { SciChartReact } from "scichart-react";
import {
  SciChartSurface, NumericAxis, FastLineRenderableSeries,
  XyDataSeries, SciChartJsNavyTheme,
} from "scichart";
import type { TrendValue } from "../types/dashboard";

interface SciChartLineProps {
  trendName?: string;
  trendValues: TrendValue[];
}

export function SciChartLine({ trendName = "Trend", trendValues }: SciChartLineProps) {
  const seriesRef = useRef<{ append: (x: number, y: number) => void } | null>(null);

  useEffect(() => {
    if (!seriesRef.current || !trendValues.length) return;
    const last = trendValues[trendValues.length - 1];
    seriesRef.current.append(new Date(last.timestamp).getTime(), last.value);
  }, [trendValues]);

  const initChart = useCallback(async (rootElement: HTMLDivElement) => {
    const { sciChartSurface, wasmContext } = await SciChartSurface.create(
      rootElement, { theme: new SciChartJsNavyTheme() }
    );
    sciChartSurface.xAxes.add(new NumericAxis(wasmContext));
    sciChartSurface.yAxes.add(new NumericAxis(wasmContext));
    const dataSeries = new XyDataSeries(wasmContext, { fifoCapacity: 500 });
    seriesRef.current = dataSeries;
    sciChartSurface.renderableSeries.add(
      new FastLineRenderableSeries(wasmContext, { dataSeries, strokeThickness: 2 })
    );
    return { sciChartSurface };
  }, []);

  return (
    <section aria-label={`Trend chart: ${trendName}`} data-testid="scichart-line"
      style={{ marginBottom: "var(--space-6)" }}>
      <h3 style={{ color: "var(--color-text)", fontSize: "var(--text-base)" }}>{trendName}</h3>
      <SciChartReact style={{ width: "100%", height: 300 }} initChart={initChart} />
    </section>
  );
}
```

2. Create `TrendsPage.tsx`:

```typescript
import type { TrendValue } from "../types/dashboard";
import { SciChartLine } from "../components/SciChartLine";

interface TrendsPageProps { trendValues: TrendValue[]; }

export function TrendsPage({ trendValues }: TrendsPageProps) {
  const byName = trendValues.reduce<Record<string, TrendValue[]>>((acc, v) => {
    (acc[v.trendName] ??= []).push(v);
    return acc;
  }, {});
  const names = Object.keys(byName);

  return (
    <section aria-label="Live Trends" data-testid="trends-page">
      <h1 style={{ color: "var(--color-text)", fontSize: "var(--text-xl)" }}>Live Trends</h1>
      {names.length === 0 ? (
        <p data-testid="trends-empty" style={{ color: "var(--color-text-muted)" }}>
          Waiting for trend data…
        </p>
      ) : (
        names.map((name) => (
          <SciChartLine key={name} trendName={name} trendValues={byName[name]} />
        ))
      )}
    </section>
  );
}
```

3. In `App.tsx`: delete inline `TrendsPage` const, import from `./pages/TrendsPage`,
   destructure `trendValues` from `useTrendWebSocket`,
   route: `<TrendsPage trendValues={trendValues} />`

**Acceptance criteria:**
- `SciChartLine.tsx` uses `useCallback` with `[]` deps (grep)
- `SciChartLine.tsx` has `fifoCapacity: 500` (grep)
- `TrendsPage.tsx` has `data-testid="trends-empty"` (grep)
- `npm test -- --run` exits 0

**Test command:** `cd helix-dashboard && npm test -- --run`

---

## Wave 3 — Tests + Build Gate

All Wave 2 tasks must be green. Tasks 07-3-001 through 07-3-004 are independent.
Task 07-3-005 is the final phase gate.

---

### Task 07-3-001 — SiteOverviewPage unit test

**ID:** `07-3-001`
**Files:** `helix-dashboard/src/pages/SiteOverviewPage.test.tsx` (new)

**Dependencies:** `07-2-001`

```typescript
import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { SiteOverviewPage } from "./SiteOverviewPage";

describe("SiteOverviewPage", () => {
  it("renders with correct testid", () => {
    render(<SiteOverviewPage />);
    expect(screen.getByTestId("site-overview-page")).toBeInTheDocument();
  });

  it("renders at least 3 fumehood cards", () => {
    render(<SiteOverviewPage />);
    expect(screen.getAllByTestId("fumehood-card").length).toBeGreaterThanOrEqual(3);
  });

  it("shows a running status badge", () => {
    render(<SiteOverviewPage />);
    const badges = screen.getAllByTestId("fumehood-status");
    expect(badges.some((el) => el.textContent === "running")).toBe(true);
  });

  it("shows a faulted status badge", () => {
    render(<SiteOverviewPage />);
    const badges = screen.getAllByTestId("fumehood-status");
    expect(badges.some((el) => el.textContent === "faulted")).toBe(true);
  });

  it("has accessible aria-label on section", () => {
    render(<SiteOverviewPage />);
    expect(screen.getByRole("region", { name: /site overview/i })).toBeInTheDocument();
  });
});
```

**Acceptance criteria:** All 5 tests GREEN. Full suite still exits 0.

**Test command:**
`cd helix-dashboard && npm test -- --run src/pages/SiteOverviewPage.test.tsx`

---

### Task 07-3-002 — CampaignsPage unit test

**ID:** `07-3-002`
**Files:** `helix-dashboard/src/pages/CampaignsPage.test.tsx` (new)

**Dependencies:** `07-2-002`

```typescript
import { describe, it, expect, beforeEach, afterEach, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { CampaignsPage } from "./CampaignsPage";

const BASE = "http://localhost:5000";

describe("CampaignsPage", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({
      ok: true,
      json: async () => [{ campaignId: "c-001", name: "Campaign Alpha", status: "Idle" }],
    }));
  });
  afterEach(() => vi.restoreAllMocks());

  it("renders with correct testid", () => {
    render(<CampaignsPage baseUrl={BASE} />);
    expect(screen.getByTestId("campaigns-page")).toBeInTheDocument();
  });

  it("loads and displays campaigns", async () => {
    render(<CampaignsPage baseUrl={BASE} />);
    await waitFor(() => expect(screen.getByText("Campaign Alpha")).toBeInTheDocument());
  });

  it("renders name input and create button", () => {
    render(<CampaignsPage baseUrl={BASE} />);
    expect(screen.getByTestId("campaign-name-input")).toBeInTheDocument();
    expect(screen.getByTestId("create-campaign-btn")).toBeInTheDocument();
  });

  it("calls POST /api/campaigns on create", async () => {
    const user = userEvent.setup();
    render(<CampaignsPage baseUrl={BASE} />);
    await user.type(screen.getByTestId("campaign-name-input"), "New Run");
    await user.click(screen.getByTestId("create-campaign-btn"));
    await waitFor(() =>
      expect(vi.mocked(fetch)).toHaveBeenCalledWith(
        expect.stringContaining("/api/campaigns"),
        expect.objectContaining({ method: "POST" })
      )
    );
  });

  it("shows error alert on fetch failure", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({
      ok: false, statusText: "Server Error",
    }));
    render(<CampaignsPage baseUrl={BASE} />);
    await waitFor(() => expect(screen.getByRole("alert")).toBeInTheDocument());
  });
});
```

**Acceptance criteria:** All 5 tests GREEN. Full suite still exits 0.

**Test command:**
`cd helix-dashboard && npm test -- --run src/pages/CampaignsPage.test.tsx`

---

### Task 07-3-003 — HistoryPage unit test

**ID:** `07-3-003`
**Files:** `helix-dashboard/src/pages/HistoryPage.test.tsx` (new)

**Dependencies:** `07-2-003`

```typescript
import { describe, it, expect, beforeEach, afterEach, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { HistoryPage } from "./HistoryPage";

const BASE = "http://localhost:5000";

const MOCK = [
  { runspaceId: "r-001", name: "Vapourtec Run 1",
    status: "Completed", createdAt: "2026-06-01T10:00:00Z" },
  { runspaceId: "r-002", name: "Knauer HPLC Calibration",
    status: "Running",   createdAt: "2026-06-02T08:00:00Z" },
];

describe("HistoryPage", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({
      ok: true, json: async () => MOCK,
    }));
  });
  afterEach(() => vi.restoreAllMocks());

  it("renders with correct testid", () => {
    render(<HistoryPage baseUrl={BASE} />);
    expect(screen.getByTestId("history-page")).toBeInTheDocument();
  });

  it("displays 2 experiment rows", async () => {
    render(<HistoryPage baseUrl={BASE} />);
    await waitFor(() =>
      expect(screen.getAllByTestId("experiment-row")).toHaveLength(2)
    );
  });

  it("shows experiment name", async () => {
    render(<HistoryPage baseUrl={BASE} />);
    await waitFor(() =>
      expect(screen.getByText("Vapourtec Run 1")).toBeInTheDocument()
    );
  });

  it("shows empty state when no experiments", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({
      ok: true, json: async () => [],
    }));
    render(<HistoryPage baseUrl={BASE} />);
    await waitFor(() =>
      expect(screen.getByText(/no experiments/i)).toBeInTheDocument()
    );
  });

  it("shows error alert on fetch failure", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({
      ok: false, statusText: "Internal Server Error",
    }));
    render(<HistoryPage baseUrl={BASE} />);
    await waitFor(() => expect(screen.getByRole("alert")).toBeInTheDocument());
  });
});
```

**Acceptance criteria:** All 5 tests GREEN. Full suite still exits 0.

**Test command:**
`cd helix-dashboard && npm test -- --run src/pages/HistoryPage.test.tsx`

---

### Task 07-3-004 — TrendsPage unit test

**ID:** `07-3-004`
**Files:** `helix-dashboard/src/pages/TrendsPage.test.tsx` (new)

**Dependencies:** `07-2-004`

Note: `scichart` and `scichart-react` are mocked in `src/test/setup.ts`.
`SciChartReact` renders `<div data-testid="scichart-surface">`.

```typescript
import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { TrendsPage } from "./TrendsPage";
import type { TrendValue } from "../types/dashboard";

const MOCK_TRENDS: TrendValue[] = [
  { type: "trend-value", trendName: "Temperature",
    value: 25.3, unit: "C", timestamp: "2026-06-02T00:00:00Z" },
  { type: "trend-value", trendName: "Pressure",
    value: 1.02, unit: "bar", timestamp: "2026-06-02T00:00:01Z" },
  { type: "trend-value", trendName: "Temperature",
    value: 25.5, unit: "C", timestamp: "2026-06-02T00:00:02Z" },
];

describe("TrendsPage", () => {
  it("renders with correct testid", () => {
    render(<TrendsPage trendValues={[]} />);
    expect(screen.getByTestId("trends-page")).toBeInTheDocument();
  });

  it("shows empty state when no trend values", () => {
    render(<TrendsPage trendValues={[]} />);
    expect(screen.getByTestId("trends-empty")).toBeInTheDocument();
  });

  it("renders one chart per unique trendName", () => {
    render(<TrendsPage trendValues={MOCK_TRENDS} />);
    expect(screen.getAllByTestId("scichart-line")).toHaveLength(2);
  });

  it("renders Temperature heading", () => {
    render(<TrendsPage trendValues={MOCK_TRENDS} />);
    expect(screen.getByText("Temperature")).toBeInTheDocument();
  });

  it("does not render empty state when trends present", () => {
    render(<TrendsPage trendValues={MOCK_TRENDS} />);
    expect(screen.queryByTestId("trends-empty")).not.toBeInTheDocument();
  });
});
```

**Acceptance criteria:** All 5 tests GREEN. Full suite still exits 0.

**Test command:**
`cd helix-dashboard && npm test -- --run src/pages/TrendsPage.test.tsx`

---

### Task 07-3-005 — Full suite gate

**ID:** `07-3-005`
**Files:** none (validation only)

**Dependencies:** `07-3-001`, `07-3-002`, `07-3-003`, `07-3-004`

**Steps:**

1. `cd helix-dashboard && npm test -- --run --coverage`
   All tests must pass. Record total count and coverage %.

2. `cd helix-dashboard && npm run build`
   Must exit 0. Fix any TypeScript errors before marking complete.

3. `dotnet build Helix.Orchestration.SiteServer/Helix.Orchestration.SiteServer.csproj --no-restore -v q`
   Must exit 0.

4. Verify Vite proxy order (WS before generic /api):
   `grep -n "trends/ws\|\"/api\"" helix-dashboard/vite.config.ts`
   The `/api/trends/ws` line number must be lower than the `"/api"` line number.

5. Verify zero raw hex literals in page/component files:
   `grep -rn "#[0-9a-fA-F]\{3,6\}" helix-dashboard/src/pages/ helix-dashboard/src/components/`
   Must return 0 results.

6. Verify SciChart licence call precedes createRoot:
   `grep -n "setRuntimeLicenseKey\|createRoot" helix-dashboard/src/main.tsx`
   `setRuntimeLicenseKey` line number must be less than `createRoot` line number.

**Acceptance criteria:**
- All Vitest tests GREEN (0 failures)
- `npm run build` exits 0
- `dotnet build` SiteServer exits 0
- Vite proxy `/api/trends/ws` appears before `/api` (line order)
- 0 raw hex literals in src/pages/ and src/components/
- `setRuntimeLicenseKey` line < `createRoot` line in main.tsx

**Test command:**
`cd helix-dashboard && npm test -- --run --coverage && npm run build`

---

## Dependency Graph

```
Wave 0 — parallel tracks:

  Track A (frontend scaffold):
    07-0-001
      |-- 07-0-002 --> 07-0-003 --> 07-0-005
      |                         --> 07-0-006
      +-- 07-0-004

  Track B (WebSocket backend):
    07-0-007 --> 07-0-008

  Track C (experiment list backend):
    07-0-009 --> 07-0-010

Wave 1 (all Wave 0 green first):
  07-1-001 \
  07-1-003  +--> 07-1-004
  07-1-002 /
  (07-1-002 also depends on 07-1-001)

Wave 2 (all Wave 1 green; pages parallel):
  07-2-001  (SiteOverview)
  07-2-002  (Campaigns)
  07-2-003  (History — also 07-0-010)
  07-2-004  (Trends — also 07-1-002)

Wave 3 (all Wave 2 green; tests parallel):
  07-3-001 --> |
  07-3-002 --> +--> 07-3-005 (gate)
  07-3-003 --> |
  07-3-004 --> |
```

---

## Phase Completion Checklist

- [ ] `helix-dashboard/` exists and `npm run build` exits 0
- [ ] `helix-dashboard/public/scichart2d.wasm` exists (WASM copy ran)
- [ ] All 4 routes render without crashing (App.test.tsx green)
- [ ] `GET /api/experiments` returns 200 (dotnet build confirms; verify via Swagger)
- [ ] `GET /api/trends/ws` upgrades to WebSocket (`app.UseWebSockets()` wired)
- [ ] `MqttTrendBridge` calls `_trendWsManager.BroadcastAsync` (grep + dotnet build)
- [ ] `setRuntimeLicenseKey` called before `createRoot` in main.tsx (line-number check)
- [ ] All Vitest tests pass (`npm test -- --run --coverage` exits 0)
- [ ] 0 raw hex literals in src/pages/ and src/components/
- [ ] `dotnet build Helix.Orchestration.SiteServer` exits 0
