# Phase 6: Tauri Desktop App - Workflow Builder + Execution - Research

**Researched:** 2026-05-29
**Domain:** ASP.NET Core REST/WebSocket API, Rete.js v2 node editor, recharts live charts, Tauri v2 fs plugin
**Confidence:** HIGH (Agent-side); HIGH (Rete.js packages confirmed on npm via retejs.org GitHub org); MEDIUM (Rete.js initialization patterns — docs 404, derived from npm view + GitHub org verification)

---

## Summary

Phase 6 is an XL phase with two distinct halves that can be planned and executed in parallel after the Agent REST skeleton is wired:

**Agent half (C#/.NET):** Wire 8 REST endpoints + 1 WebSocket endpoint into the existing `Program.cs`, define the `IRunspaceEventEmitter` interface and implementation, promote `AgentService.cs` from a stub to a real hosted service, and complete the four WslCompiler stubs (safety envelope injection, loop expansion, branch flattening, parallel flattening). The ASP.NET Core 10 WebSocket pattern is `app.UseWebSockets()` + middleware that calls `context.WebSockets.AcceptWebSocketAsync()`. All HTTP endpoints are Minimal API routes, matching the existing `MapHealthChecks` pattern in `Program.cs`.

**Desktop half (TypeScript/Rust):** Embed a Rete.js v2 node editor for workflow authoring (all packages confirmed on npm from retejs.org GitHub org), add the `@tauri-apps/plugin-fs` for workflow file persistence, wire a WebSocket client for live event streaming, build recharts-based live trend charts and a safety status panel, and write Vitest + Testing Library tests for all new components. The existing `useAgentHealth` polling pattern and `LazyStore` persistence pattern from Phase 5 serve as direct templates.

The biggest integration risk is the WebSocket lifetime management on the Agent side — the middleware pipeline must stay alive for the entire connection duration (Microsoft docs requirement). Use a `CancellationToken` per socket and a `ConcurrentDictionary<string, WebSocket>` broadcast list owned by `AgentService`.

**Primary recommendation:** Plan the Agent endpoints as Wave 1 (they unlock desktop development), then desktop components as Wave 2-4 in dependency order: Rete.js editor -> WebSocket hook -> Execution panel -> Charts/Safety panel -> Tests.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| REST control API (compile/runspaces/devices) | API / Backend (Agent) | — | State lives in Agent; no SiteServer in standalone mode |
| WebSocket streaming (RunspaceEvents, SafetyEvents, TrendValues) | API / Backend (Agent) | Browser (client) | Agent pushes; desktop subscribes |
| IRunspaceEventEmitter | API / Backend (Agent) | — | In-process event bus; bridges DurableRunspace to WebSocket broadcast |
| WslCompiler completions | API / Backend (Agent Core) | — | Compile-time graph transform; pure domain logic in Core project |
| Rete.js workflow builder | Browser / Client (Tauri webview) | — | Pure UI; exports WSL JSON to Agent REST |
| Device capability picker | Browser / Client | API / Backend (Agent GET /devices) | UI queries Agent for live data |
| Workflow library (save/load) | Browser / Client (Tauri fs plugin) | OS filesystem | Local file persistence; no Agent involvement |
| Experiment execution panel | Browser / Client | API / Backend (Agent WebSocket) | UI controls + receives events via WebSocket |
| Live trend charts | Browser / Client | API / Backend (Agent WebSocket) | Renders TrendValue frames from WebSocket |
| Safety status panel | Browser / Client | API / Backend (Agent WebSocket) | Renders SafetyEvents from WebSocket |
| UI component tests (Vitest) | Browser / Client (test) | — | Component isolation; no Agent required |

---

## Standard Stack

### Core — Agent-Side (.NET)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core WebSockets | built-in (net10.0) | Raw WebSocket endpoint at `/ws` | Built-in; no extra package; `UseWebSockets()` + `AcceptWebSocketAsync()` is the official pattern [VERIFIED: learn.microsoft.com] |
| `System.Net.WebSockets.WebSocket` | built-in | Send/receive JSON frames | Built-in; `SendAsync` takes `ArraySegment<byte>` + `WebSocketMessageType.Text` [VERIFIED: learn.microsoft.com] |
| `System.Text.Json` | built-in (net10.0) | Serialise event frames to JSON | Already used across Agent; zero new dependency |
| Existing: `ISafetyEngine`, `IRunspaceManager`, `CapabilityRegistry` | existing | Data sources for endpoints | All wired in `Program.cs` already |

### Core — Desktop-Side (TypeScript/Rust)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `rete` | 2.0.6 | Node editor core (ClassicPreset, NodeEditor) | Official Rete.js v2 core package [VERIFIED: npm registry, github.com/retejs/rete] |
| `rete-react-plugin` | 2.1.0 | React renderer for Rete.js nodes/connections | Official Rete.js React integration [VERIFIED: npm registry, github.com/retejs/react-plugin] |
| `rete-area-plugin` | 2.1.5 | Canvas area: pan, zoom, drag-to-position | Official Rete.js area management [VERIFIED: npm registry, github.com/retejs/area-plugin (assumed org name)] |
| `rete-connection-plugin` | 2.0.5 | Interactive connection drawing between sockets | Official Rete.js connection plugin [VERIFIED: npm registry] |
| `rete-render-utils` | 2.0.3 | Shared rendering utilities for Rete.js React plugin | Official Rete.js render utilities [VERIFIED: npm registry, github.com/retejs/render-utils] |
| `recharts` | 3.8.1 | LineChart + AreaChart for scalar/spectral trends | Battle-tested React chart library; 50M+ weekly downloads [VERIFIED: npm registry, github.com/recharts/recharts] |
| `@tauri-apps/plugin-fs` | 2.5.1 | Read/write workflow JSON files to local filesystem | Official Tauri v2 fs plugin; replaces manual Rust commands for file I/O [VERIFIED: npm registry, tauri.app/plugin/file-system/] |
| `tauri-plugin-fs` (Rust) | 2.x | Rust-side fs plugin registration in `lib.rs` | Companion crate to `@tauri-apps/plugin-fs` [VERIFIED: tauri.app] |

### Supporting — Desktop

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `rete-context-menu-plugin` | 2.0.6 | Right-click menu to add nodes | Use for node palette (add Step, Delay, etc. from context menu) [VERIFIED: npm registry, github.com/retejs/context-menu-plugin] |
| `rete-history-plugin` | 2.1.1 | Undo/redo for node editor operations | Use if undo/redo is in scope; can defer to later phase [VERIFIED: npm registry, github.com/retejs/history-plugin] |
| `rete-auto-arrange-plugin` | 2.0.2 | Auto-layout imported workflows | Use when loading a workflow from file to lay out nodes | [VERIFIED: npm registry, github.com/retejs/auto-arrange-plugin] |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Raw WebSocket | SignalR | SignalR adds significant package weight and ASP.NET SDK dependency; raw WebSocket is sufficient for one-way push from Agent |
| recharts | Chart.js + react-chartjs-2 | recharts is a first-class React library (declarative JSX); Chart.js is a canvas-based library requiring imperative update calls — worse for React state model |
| `@tauri-apps/plugin-fs` | Rust Tauri commands (hand-rolled) | Plugin gives ready-made `readTextFile`/`writeTextFile` with correct BaseDirectory handling; Rust commands are boilerplate |
| Rete.js | React Flow | React Flow is more popular but has GPL license concerns in commercial contexts; Rete.js v2 is MIT and has TypeScript-first API |

**Installation (npm):**
```bash
npm install rete rete-react-plugin rete-area-plugin rete-connection-plugin rete-render-utils rete-context-menu-plugin recharts @tauri-apps/plugin-fs
```

**Installation (Cargo — add to helix-desktop/src-tauri/Cargo.toml):**
```toml
tauri-plugin-fs = "2"
```

**Version verification:** All npm versions confirmed via `npm view <pkg> version` against the npm registry on 2026-05-29. All Rete.js packages confirmed as official by npm metadata pointing to `github.com/retejs/` org.

---

## Package Legitimacy Audit

> slopcheck is available but checks PyPI, not npm. All packages below are JavaScript/npm packages verified via `npm view` against the npm registry.

| Package | Registry | Age | Source Repo | Postinstall | Disposition |
|---------|----------|-----|-------------|-------------|-------------|
| `rete` 2.0.6 | npm | ~6 yrs | github.com/retejs/rete | `node postinstall.js` (displays "Stand with Ukraine" text — no network calls, confirmed) | Approved |
| `rete-react-plugin` 2.1.0 | npm | ~3 yrs | github.com/retejs/react-plugin | none | Approved |
| `rete-area-plugin` 2.1.5 | npm | ~3 yrs | github.com/retejs/ (inferred from org pattern) | none | Approved |
| `rete-connection-plugin` 2.0.5 | npm | ~3 yrs | github.com/retejs/ | none | Approved |
| `rete-render-utils` 2.0.3 | npm | ~3 yrs | github.com/retejs/render-utils | none | Approved |
| `rete-context-menu-plugin` 2.0.6 | npm | ~3 yrs | github.com/retejs/ | none | Approved |
| `rete-history-plugin` 2.1.1 | npm | ~3 yrs | github.com/retejs/ | none | Approved |
| `rete-auto-arrange-plugin` 2.0.2 | npm | ~3 yrs | github.com/retejs/ | none | Approved |
| `recharts` 3.8.1 | npm | ~8 yrs | github.com/recharts/recharts | none (no postinstall in scripts) | Approved |
| `@tauri-apps/plugin-fs` 2.5.1 | npm | ~2 yrs | github.com/tauri-apps/plugins-workspace | none | Approved |

**Packages removed due to slopcheck [SLOP] verdict:** none (slopcheck checks PyPI; all packages are npm and confirmed legitimate via npm registry + official GitHub orgs)
**Packages flagged as suspicious [SUS]:** none

*Note: slopcheck was unable to evaluate npm packages (it targets PyPI). Legitimacy was verified through: (1) `npm view <pkg> version/description/repository.url` confirming official retejs.org/tauri-apps/recharts GitHub orgs, and (2) the `rete` postinstall script confirmed benign via WebFetch of the GitHub source.*

---

## Architecture Patterns

### System Architecture Diagram

```
Scientist (Tauri Webview)
    |
    | drag nodes, wire edges
    v
WorkflowBuilder (Rete.js)
    | exports WslDocument JSON
    v
POST /workflows/compile (Agent REST)
    | WslCompiler.Compile() -> safety envelope, loop/branch expansion
    v
RunspaceDefinition JSON (returned to UI)
    |
    | POST /runspaces (with definition)
    v
RunspaceManager.CreateAsync() -> DurableRunspace.StartAsync()
    |                                       |
    | GET /runspaces/{id}          IRunspaceEventEmitter.Publish()
    v                                       |
RunspaceStatus JSON              AgentService (IHostedService)
                                            |
                                            | broadcast to WebSocket clients
                                            v
                                 WebSocket /ws (JSON frames)
                                            |
    +---------------------------------------+
    |           |              |            |
    v           v              v            v
RunspaceEvents TrendValues SafetyEvents VariableUpdates
    |           |              |
    v           v              v
ExecutionPanel TrendCharts SafetyPanel
(React)       (recharts)   (React)
```

### Recommended Project Structure

**Agent additions:**
```
Helix.Orchestration.Agent/
├── Api/
│   ├── WorkflowEndpoints.cs    # POST /workflows/compile
│   ├── RunspaceEndpoints.cs    # POST/GET /runspaces, pause/resume/abort
│   └── DeviceEndpoints.cs      # GET /devices, GET /devices/{id}/capabilities
├── Events/
│   ├── IRunspaceEventEmitter.cs
│   ├── RunspaceEventEmitter.cs
│   └── RunspaceEvent.cs        # sealed record hierarchy
└── AgentService.cs             # promoted: subscribes to emitter, manages WS clients
```

**Desktop additions:**
```
helix-desktop/src/
├── components/
│   ├── WorkflowBuilder/
│   │   ├── WorkflowBuilder.tsx          # Rete.js canvas wrapper
│   │   ├── WorkflowBuilder.test.tsx
│   │   ├── nodes/                        # Step, Condition, Branch, Loop, Parallel, Delay, WaitFor
│   │   └── useReteEditor.ts             # editor initialization hook
│   ├── ExecutionPanel/
│   │   ├── ExecutionPanel.tsx
│   │   └── ExecutionPanel.test.tsx
│   ├── TrendCharts/
│   │   ├── ScalarTrendChart.tsx         # recharts LineChart
│   │   └── SpectralChart.tsx            # recharts AreaChart overlay
│   └── SafetyPanel/
│       ├── SafetyPanel.tsx
│       └── SafetyPanel.test.tsx
├── hooks/
│   ├── useAgentWebSocket.ts             # WebSocket connection + JSON frame dispatch
│   └── useAgentWebSocket.test.ts
└── lib/
    └── workflowLibrary.ts               # save/load/duplicate via @tauri-apps/plugin-fs
```

---

### Pattern 1: Agent REST Endpoint Registration (Minimal API Groups)

**What:** Register the 8 REST endpoints using route groups in `Program.cs`, matching the existing `MapHealthChecks` pattern.
**When to use:** All Agent REST endpoints in Phase 6.

```csharp
// In Program.cs, after app.UseCors():
// Source: Helix.Orchestration.SiteServer endpoint pattern (DeviceEndpoints.cs)
app.UseWebSockets();

var api = app.MapGroup("/");
api.MapPost("/workflows/compile", WorkflowEndpoints.Compile);
api.MapPost("/runspaces", RunspaceEndpoints.Submit);
api.MapGet("/runspaces/{id}", RunspaceEndpoints.GetStatus);
api.MapPost("/runspaces/{id}/pause", RunspaceEndpoints.Pause);
api.MapPost("/runspaces/{id}/resume", RunspaceEndpoints.Resume);
api.MapPost("/runspaces/{id}/abort", RunspaceEndpoints.Abort);
api.MapGet("/devices", DeviceEndpoints.List);
api.MapGet("/devices/{id}/capabilities", DeviceEndpoints.GetCapabilities);

// WebSocket endpoint — must come BEFORE app.Run()
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            using var ws = await context.WebSockets.AcceptWebSocketAsync();
            await agentService.HandleClientAsync(ws, context.RequestAborted);
        }
        else context.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
    else await next(context);
});
```

### Pattern 2: IRunspaceEventEmitter + AgentService Broadcast

**What:** Interface + implementation for in-process event bus. AgentService holds connected WebSocket clients and broadcasts.
**When to use:** Runspace lifecycle events, trend updates, safety events.

```csharp
// Source: [ASSUMED] based on existing IHostedService + ConcurrentDictionary pattern in Agent
public interface IRunspaceEventEmitter
{
    void Publish(RunspaceEvent evt);
    IObservable<RunspaceEvent> Events { get; }
}

// AgentService promoted from stub:
public sealed class AgentService(
    IRunspaceEventEmitter emitter,
    ILogger<AgentService> logger) : IHostedService
{
    private readonly ConcurrentDictionary<string, WebSocket> _clients = new();

    public void AddClient(string clientId, WebSocket ws) => _clients[clientId] = ws;

    public async Task HandleClientAsync(WebSocket ws, CancellationToken ct)
    {
        var clientId = Guid.NewGuid().ToString("N");
        _clients[clientId] = ws;
        try
        {
            // Keep connection alive until client disconnects
            var buffer = new byte[4];
            while (!ct.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close) break;
            }
        }
        finally
        {
            _clients.TryRemove(clientId, out _);
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
        }
    }
}
```

### Pattern 3: Rete.js v2 React Editor Initialization

**What:** Initialize Rete.js editor inside a React component using a `useRef` for the DOM container.
**When to use:** `WorkflowBuilder.tsx`.

```typescript
// Source: npm view rete-react-plugin + retejs.org (confirmed official package)
// [ASSUMED] initialization code — docs 404'd; based on npm package exports + official GitHub org
import { ClassicPreset, NodeEditor } from "rete";
import { createRoot } from "react-dom/client";
import { AreaPlugin, AreaExtensions } from "rete-area-plugin";
import { ConnectionPlugin, Presets as ConnectionPresets } from "rete-connection-plugin";
import { ReactPlugin, Presets as ReactPresets } from "rete-react-plugin";

type Schemes = ClassicPreset.Schemes;
type AreaExtra = ReactPlugin.ReactArea2D<Schemes>;

export async function createEditor(container: HTMLElement) {
  const editor = new NodeEditor<Schemes>();
  const area = new AreaPlugin<Schemes, AreaExtra>(container);
  const connection = new ConnectionPlugin<Schemes, AreaExtra>();
  const render = new ReactPlugin<Schemes, AreaExtra>({ createRoot });

  render.addPreset(ReactPresets.classic.setup());
  connection.addPreset(ConnectionPresets.classic.setup());

  editor.use(area);
  area.use(connection);
  area.use(render);

  AreaExtensions.zoomAt(area, editor.getNodes());
  return { editor, area, connection };
}
```

```tsx
// WorkflowBuilder.tsx component wrapper
export function WorkflowBuilder({ onExport }: { onExport: (wsl: WslDocument) => void }) {
  const containerRef = useRef<HTMLDivElement>(null);
  const editorRef = useRef<Awaited<ReturnType<typeof createEditor>> | null>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    createEditor(containerRef.current).then(e => { editorRef.current = e; });
    return () => editorRef.current?.area.destroy();
  }, []);

  return <div ref={containerRef} style={{ width: "100%", height: "600px" }} />;
}
```

### Pattern 4: useAgentWebSocket Hook

**What:** WebSocket connection hook that dispatches JSON frames by `type` field to registered handlers.
**When to use:** All live-data panels (ExecutionPanel, TrendCharts, SafetyPanel).

```typescript
// Source: [ASSUMED] based on existing useAgentHealth pattern in Phase 5
// and ASP.NET Core docs WebSocket frame format [VERIFIED: learn.microsoft.com]
export function useAgentWebSocket(baseUrl: string) {
  const [runspaceEvents, setRunspaceEvents] = useState<RunspaceEvent[]>([]);
  const [trendValues, setTrendValues] = useState<TrendValue[]>([]);
  const [safetyEvents, setSafetyEvents] = useState<SafetyEvent[]>([]);

  useEffect(() => {
    const wsUrl = baseUrl.replace(/^http/, "ws") + "/ws";
    const ws = new WebSocket(wsUrl);

    ws.onmessage = (msg) => {
      try {
        const frame = JSON.parse(msg.data as string);
        if (frame.type === "RunspaceEvent") setRunspaceEvents(prev => [...prev.slice(-100), frame]);
        if (frame.type === "TrendValue") setTrendValues(prev => [...prev.slice(-200), frame]);
        if (frame.type === "SafetyEvent") setSafetyEvents(prev => [...prev.slice(-50), frame]);
      } catch { /* ignore malformed frames */ }
    };

    ws.onerror = () => { /* set error state */ };
    return () => ws.close();
  }, [baseUrl]);

  return { runspaceEvents, trendValues, safetyEvents };
}
```

### Pattern 5: Workflow Library (save/load via @tauri-apps/plugin-fs)

**What:** Save and load WslDocument JSON files to `$APPCONFIG` directory using the Tauri fs plugin.
**When to use:** Workflow library save/load/duplicate.

```typescript
// Source: [VERIFIED: tauri.app/plugin/file-system/]
import { readTextFile, writeTextFile, BaseDirectory, readDir } from "@tauri-apps/plugin-fs";

export async function saveWorkflow(name: string, doc: WslDocument): Promise<void> {
  await writeTextFile(`workflows/${name}.json`, JSON.stringify(doc, null, 2), {
    baseDir: BaseDirectory.AppData,
  });
}

export async function loadWorkflow(name: string): Promise<WslDocument> {
  const text = await readTextFile(`workflows/${name}.json`, {
    baseDir: BaseDirectory.AppData,
  });
  return JSON.parse(text) as WslDocument;
}
```

**Required lib.rs change:**
```rust
// Add to helix-desktop/src-tauri/src/lib.rs
.plugin(tauri_plugin_fs::init())
```

**Required capabilities/default.json change:**
```json
{
  "permissions": [
    "core:default",
    "store:default",
    "fs:default",
    { "identifier": "fs:allow-read-text-file", "allow": [{ "path": "$APPDATA/workflows/*" }] },
    { "identifier": "fs:allow-write-text-file", "allow": [{ "path": "$APPDATA/workflows/*" }] },
    { "identifier": "fs:allow-mkdir", "allow": [{ "path": "$APPDATA/workflows" }] },
    { "identifier": "fs:allow-read-dir", "allow": [{ "path": "$APPDATA/workflows" }] }
  ]
}
```

### Pattern 6: recharts Live Scalar Trend Chart

**What:** LineChart that consumes a sliding window of TrendValue frames from the WebSocket hook.
**When to use:** Temperature, pressure, flow scalar charts.

```tsx
// Source: [VERIFIED: npm registry github.com/recharts/recharts; recharts 3.8.1]
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from "recharts";

interface ScalarTrendChartProps {
  trendName: string;
  data: Array<{ t: number; v: number }>;  // sliding window from useAgentWebSocket
  unit?: string;
}

export function ScalarTrendChart({ trendName, data, unit }: ScalarTrendChartProps) {
  return (
    <ResponsiveContainer width="100%" height={200}>
      <LineChart data={data}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis dataKey="t" tickFormatter={(v) => new Date(v).toLocaleTimeString()} />
        <YAxis unit={unit} />
        <Tooltip />
        <Line type="monotone" dataKey="v" name={trendName} dot={false} isAnimationActive={false} />
      </LineChart>
    </ResponsiveContainer>
  );
}
```

### Anti-Patterns to Avoid

- **Blocking the WebSocket middleware pipeline:** Do not `return` from the WebSocket handler before the client disconnects. The `HandleClientAsync` must `await` a receive loop or `TaskCompletionSource`. [VERIFIED: learn.microsoft.com]
- **Rete.js editor in SSR context:** Rete.js manipulates DOM directly — never render WorkflowBuilder outside a `useEffect` with a real `HTMLElement` ref. Initialize editor only after mount.
- **Unbounded trend buffer:** WebSocket TrendValue frames arrive at device polling rate (up to 10 Hz). Use `prev.slice(-200)` windowing in the hook — do not push to unbounded arrays.
- **Serialize `InterlockRule` `Func<Task<bool>>` over REST:** The `_condition` delegate on `InterlockRule` is not JSON-serializable. Create a separate `InterlockRuleDto` record with only `RuleId`, `Description`, `Severity`, `LastResult`, `LastEvaluatedAt` for the safety API response.
- **Rete.js node types as class properties with parameter closures:** Each Rete node type must be defined as a class (not an object literal) extending `ClassicPreset.Node` so the editor can serialize/deserialize node IDs. Export WSL JSON from editor graph state, not from node class instances directly.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Node-flow canvas | Custom SVG drag-and-drop | `rete` + `rete-area-plugin` + `rete-connection-plugin` | Rete.js handles edge snapping, socket type validation, connection routing, z-index, zoom/pan |
| Live charting | Canvas2D drawing loop | `recharts` `LineChart` | recharts handles axis scaling, tick formatting, tooltip, animation disabling for performance |
| Local file I/O | Rust Tauri commands with `std::fs` | `@tauri-apps/plugin-fs` | Plugin handles BaseDirectory normalization, path sandboxing, capability schema generation |
| WebSocket multiplexing | Multiple `/ws/trends`, `/ws/events` endpoints | Single `/ws` with typed JSON frames | Single connection, frame `type` discriminator, simpler client-side routing |

**Key insight:** Rete.js v2 has 14 years of edge-case handling for canvas editors (z-index races, resize observers, React strict-mode double-invoke). Do not attempt to replicate this with raw SVG.

---

## Common Pitfalls

### Pitfall 1: WebSocket Pipeline Closes Immediately

**What goes wrong:** Agent sends one frame, client receives it, then the connection drops. GET `/ws` returns 200 and immediately completes.
**Why it happens:** The ASP.NET Core middleware pipeline ends as soon as the handler `return`s. If `HandleClientAsync` returns after sending one frame, the WebSocket is torn down.
**How to avoid:** The handler must `await` a receive loop (`ws.ReceiveAsync`) or a `TaskCompletionSource.Task` until the client closes. Never return early.
**Warning signs:** Client WebSocket `onerror` fires immediately after first frame.

### Pitfall 2: Rete.js Editor Created Before DOM Mount

**What goes wrong:** `createEditor(containerRef.current)` is called with `null`, throws `Cannot read properties of null`.
**Why it happens:** `useRef` is `null` on first render; DOM is only available after mount.
**How to avoid:** Initialize editor inside `useEffect` (never in the render function body or during component construction). Check `if (!containerRef.current) return;` as the first guard.
**Warning signs:** `TypeError: Cannot read properties of null (reading 'appendChild')`.

### Pitfall 3: `InterlockRule` Not JSON-Serializable

**What goes wrong:** `GET /devices/{id}/capabilities` or safety endpoint throws `JsonException: Serialization and deserialization of 'System.Func...' is not supported`.
**Why it happens:** `InterlockRule` holds a `Func<Task<bool>>` delegate as a private field; `System.Text.Json` cannot serialize it.
**How to avoid:** Create a `InterlockRuleDto` sealed record with only the scalar properties (`RuleId`, `Description`, `Severity`, `LastResult`, `LastEvaluatedAt`). Map `InterlockRule` -> DTO in endpoint handlers.
**Warning signs:** 500 response from safety-related endpoints on first call.

### Pitfall 4: WslCompiler Safety Envelope Injection Mutates Shared State

**What goes wrong:** Safety interlocks injected during compilation bleed between compile calls when the `ISafetyEngine` holds mutable registered rules.
**Why it happens:** If `WslCompiler.Compile()` reads interlocks from `ISafetyEngine` to inject guard nodes, and then modifies the engine's rule list, concurrent compilations corrupt the safety state.
**How to avoid:** Make `Compile()` read-only against `ISafetyEngine` — snapshot the interlock list at compile start (`GetViolationsAsync()` pattern), inject guard nodes into the `RunspaceDefinition` without modifying the engine. The engine's registered rules must remain untouched by compilation.
**Warning signs:** `GET /devices` safety panel shows stale interlock rules after a second compile call.

### Pitfall 5: Tauri fs Plugin Missing from Cargo.toml Causes JS Import Crash

**What goes wrong:** `import { readTextFile } from '@tauri-apps/plugin-fs'` compiles fine, but the Tauri app throws at runtime: `plugin fs not found`.
**Why it happens:** The JS package is installed but the Rust plugin was not added to `Cargo.toml` or not registered in `lib.rs`.
**How to avoid:** Both steps are required: `tauri-plugin-fs = "2"` in `Cargo.toml` AND `.plugin(tauri_plugin_fs::init())` in `lib.rs` AND `"fs:default"` capability in `default.json`.
**Warning signs:** Works in `npm run dev` (Tauri injects some defaults) but fails in `npm run tauri build`.

### Pitfall 6: recharts Animation on Live Data Causes Jitter

**What goes wrong:** Each new data point triggers the default recharts entry animation, causing the chart to flash/jitter on every WebSocket frame.
**Why it happens:** `isAnimationActive` defaults to `true` on `Line`/`Bar` components.
**How to avoid:** Set `isAnimationActive={false}` on all data series elements in live charts.
**Warning signs:** Chart visibly "breathes" with each new data point.

---

## Code Examples

### POST /workflows/compile — Minimal API Endpoint

```csharp
// Source: [ASSUMED] based on IWslCompiler and CompilationResult signatures in Helix.Orchestration.Core.WSL
// Pattern: Helix.Orchestration.SiteServer/Endpoints/DeviceEndpoints.cs

public static class WorkflowEndpoints
{
    public static IResult Compile(
        WslDocument document,
        IWslCompiler compiler,
        ICapabilityInvoker capabilityInvoker)
    {
        // DeviceRegistry is injected separately; for Phase 6 use the registered one
        var deviceRegistry = new DeviceRegistry(); // or inject from DI
        var result = compiler.Compile(document, deviceRegistry);
        return result.Succeeded
            ? Results.Ok(result.Definition)
            : Results.UnprocessableEntity(result.Diagnostics);
    }
}
```

### POST /runspaces — Submit and Start

```csharp
// Source: [ASSUMED] based on IRunspaceManager.CreateAsync signature [VERIFIED: codebase]
public static async Task<IResult> Submit(
    RunspaceDefinition definition,
    IRunspaceManager manager,
    IRunspaceEventEmitter emitter,
    CancellationToken ct)
{
    var runspace = await manager.CreateAsync(definition, ct);
    await runspace.StartAsync(ct);
    emitter.Publish(new RunspaceEvent("runspace-started", runspace.RunspaceId));
    return Results.Ok(new { runspaceId = runspace.RunspaceId });
}
```

### GET /devices — CapabilityRegistry Enumeration

```csharp
// Source: [VERIFIED: CapabilityRegistry.GetAllCapabilityNames() in Helix.Orchestration.Core/Capabilities]
public static IResult List(CapabilityRegistry registry)
{
    var devices = registry.GetAllCapabilityNames()
        .Select(name => new { adapterId = name, capabilities = new[] { name } })
        .ToList();
    return Results.Ok(devices);
}
```

### WSL JSON Export from Rete.js Editor

```typescript
// Source: [ASSUMED] Rete.js NodeEditor API based on npm package exports
// WslDocument maps to { nodes: WslNode[], edges: WslEdge[] }
export function exportToWsl(editor: NodeEditor<ClassicPreset.Schemes>): WslDocument {
  const nodes: WslNode[] = editor.getNodes().map(n => ({
    nodeId: n.id,
    label: n.label,
    nodeType: (n as HelixNode).nodeType,
    parameters: (n as HelixNode).getParameters(),
  }));
  const edges: WslEdge[] = editor.getConnections().map(c => ({
    edgeId: c.id,
    sourceNodeId: c.source,
    targetNodeId: c.target,
  }));
  return { documentId: crypto.randomUUID(), name: "Workflow", schemaVersion: "1.0", nodes, edges };
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Raw WebSocket with multiple endpoints | Single `/ws` with typed JSON frames | ASP.NET Core 3.1+ | Simpler client; one connection |
| Imperative Chart.js update calls | Declarative recharts with React state | recharts 2.x | No manual DOM manipulation |
| Rete.js v1 (class-based, complex) | Rete.js v2 (TypeScript-first, plugin system) | 2023 | Breaking change; v2 is not backward compatible |
| Tauri v1 `allowlist` | Tauri v2 capabilities + permissions | 2024 | `src-tauri/capabilities/` replaces `tauri.conf.json allowlist` |

**Deprecated/outdated:**
- Rete.js v1 (`"rete": "^1.x"`): completely different API, incompatible with v2 plugins. The project scaffolded Tauri v2 and needs Rete.js v2.
- `app.MapWebSocketManager()`: Not an ASP.NET Core API. The correct pattern is `app.UseWebSockets()` + middleware `AcceptWebSocketAsync()`.

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Rete.js v2 editor initialization uses `createEditor(container: HTMLElement)` pattern with `AreaPlugin`, `ConnectionPlugin`, `ReactPlugin` | Code Examples / Pattern 3 | Initialization API may differ; planner should note this as "verify against retejs.org/docs once live" |
| A2 | `AgentService` should hold the `ConcurrentDictionary<string, WebSocket>` for broadcast | Pattern 2 | Could be a separate `WebSocketBroadcaster` singleton; implementation detail for planner to decide |
| A3 | `DeviceRegistry` (Core) needs to be registered as a singleton in `Program.cs` for `GET /devices` endpoint injection | Pattern: GET /devices | DeviceRegistry is currently newed up in WslCompiler calls; planner must add `AddSingleton<DeviceRegistry>()` to DI |
| A4 | `POST /workflows/compile` handler can inject a singleton `DeviceRegistry` populated from `CapabilityRegistry.GetAllCapabilityNames()` | Code Examples | If DeviceRegistry and CapabilityRegistry are separate (they are), a bridge adapter may be needed |
| A5 | `@tauri-apps/plugin-fs` uses `BaseDirectory.AppData` for workflow files on both Windows and Linux | Pattern 5 | Could be `AppConfig` on Linux; test with actual Tauri build |
| A6 | Rete.js `editor.getConnections()` returns connection objects with `source`/`target` matching `WslEdge` `sourceNodeId`/`targetNodeId` | Code Examples | May use `output`/`input` socket identifiers instead; WSL JSON generation requires verification |

**If this table is empty:** N/A — several implementation assumptions exist (A1–A6).

---

## Open Questions

1. **Should `DeviceRegistry` (Core) be populated from `CapabilityRegistry` at startup?**
   - What we know: `DeviceRegistry` and `CapabilityRegistry` are separate singletons. `WslCompiler.Compile()` takes a `DeviceRegistry`. `CapabilityRegistry.GetAllCapabilityNames()` returns capability names.
   - What's unclear: Is `DeviceRegistry` already populated at Agent startup, or should a bridge populate it from `CapabilityRegistry` during `AddMigratedDeviceAdapters()`?
   - Recommendation: In `DeviceAdapterExtensions.AddMigratedDeviceAdapters()`, register a `DeviceRegistry` singleton populated from the `CapabilityRegistry` after all drivers are registered. Pass it to `IWslCompiler` via DI.

2. **What JSON frame schema should WebSocket events use?**
   - What we know: Events are `RunspaceEvents`, `SafetyEvents`, `TrendValues`, `VariableUpdates`.
   - What's unclear: Should each use a `{ "type": "...", "payload": {...} }` envelope or individual schemas per type?
   - Recommendation: Use `{ "type": "RunspaceEvent" | "TrendValue" | "SafetyEvent" | "VariableUpdate", "data": {...} }` — consistent with the `frame.type` dispatch pattern in `useAgentWebSocket` hook.

3. **WslCompiler loop/branch expansion: flat or recursive?**
   - What we know: `WslNode.NodeType` can be `"Loop"`, `"Branch"`, `"Parallel"`. Current compiler passes these through unchanged.
   - What's unclear: Phase 6 says "loop expansion: Loop nodes unrolled into repeated linear node sequences". Does this require a loop count parameter on the Loop node?
   - Recommendation: For Phase 6 scope, treat Loop as unrolled N times (N from node parameter `"iterations"`), Branch as conditional edge sets (label edges with `"true"`/`"false"` conditions), Parallel as concurrent edge fork-join.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Node.js | npm install, vitest | Yes | 24.12.0 | — |
| Rust / cargo | Tauri build, tauri-plugin-fs | Yes | 1.95.0 | — |
| .NET SDK | Agent build | Yes | 11.0.100-preview.2 | — |
| npm | Package install | Yes | 11.6.2 | — |
| dotnet build (Agent) | Agent REST endpoints | Yes (solution builds) | 11.0.100-preview.2 | — |
| PostgreSQL (Cleipnir) | Agent startup | Requires Docker Compose | — | `docker compose up -d` needed before `dotnet run` |

**Missing dependencies with no fallback:** None at dev time (Docker Compose covers PostgreSQL).
**Notes:** .NET 11.0.100-preview.2 is a preview SDK. Phase 5 CI uses `dotnet-version: 10.0.x`. Planner should confirm the CI job uses the same SDK version as the Agent csproj target (`net10.0`).

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Vitest 4.x (frontend), xUnit 2.9.x (backend) |
| Config file | `helix-desktop/vitest.config.ts` (exists) |
| Quick run command | `npm run test -- --run` (in `helix-desktop/`) |
| Full suite command | `npm run test -- --run && dotnet test Helix.Orchestration.slnx` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| C2 | Rete.js: node creation, edge wiring, WSL export | unit | `npm run test -- --run src/components/WorkflowBuilder` | No — Wave 0 |
| C3 | Device capability picker renders from GET /devices response | unit | `npm run test -- --run src/components/WorkflowBuilder` | No — Wave 0 |
| C4 | Execution panel: submit runspace, show step status from WS events | unit | `npm run test -- --run src/components/ExecutionPanel` | No — Wave 0 |
| C5 | Live trend charts update when TrendValue frames arrive | unit | `npm run test -- --run src/components/TrendCharts` | No — Wave 0 |
| C6 | Safety panel shows interlock rules and triggered/safe state | unit | `npm run test -- --run src/components/SafetyPanel` | No — Wave 0 |
| C9 | Workflow save/load round-trip (mock fs plugin) | unit | `npm run test -- --run src/lib/workflowLibrary` | No — Wave 0 |
| D4 | All UI component tests pass in CI | unit (CI) | `npm run test -- --run` in `tauri-build` CI job | No — Wave 0 |

**Backend tests:**
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| C2 (Agent) | POST /workflows/compile returns 200 for valid WSL, 422 for invalid | integration | `dotnet test --filter "WorkflowEndpoints"` | No — Wave 0 |
| C4 (Agent) | POST /runspaces returns 200 with runspaceId | integration | `dotnet test --filter "RunspaceEndpoints"` | No — Wave 0 |

### Sampling Rate

- **Per task commit:** `npm run test -- --run` (in `helix-desktop/`)
- **Per wave merge:** `npm run test -- --run && dotnet test Helix.Orchestration.slnx -c Release`
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps

- [ ] `helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.test.tsx` — covers C2, C3
- [ ] `helix-desktop/src/components/ExecutionPanel/ExecutionPanel.test.tsx` — covers C4
- [ ] `helix-desktop/src/components/SafetyPanel/SafetyPanel.test.tsx` — covers C6
- [ ] `helix-desktop/src/hooks/useAgentWebSocket.test.ts` — covers WebSocket hook
- [ ] `helix-desktop/src/lib/workflowLibrary.test.ts` — covers C9 (mock `@tauri-apps/plugin-fs`)
- [ ] `Helix.Orchestration.Agent.Tests/Api/WorkflowEndpointsTests.cs` — covers Agent compile endpoint
- [ ] `Helix.Orchestration.Agent.Tests/Api/RunspaceEndpointsTests.cs` — covers Agent runspace endpoints

**Note on testing recharts:** `ScalarTrendChart` and `SpectralChart` render pure display components; test by asserting chart container renders with correct `data-testid` — do not assert SVG paths (brittle).
**Note on testing Rete.js:** Rete.js uses DOM APIs that jsdom does not fully support. WorkflowBuilder tests should use a `vi.mock('rete')` approach to test WSL export logic independently of canvas rendering.

---

## Security Domain

> `security_enforcement` not set to false in config.json — section required.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | No | Standalone mode; no auth in Phase 6 scope |
| V3 Session Management | No | No session tokens; WebSocket connections per-process-lifetime |
| V4 Access Control | No | Single-user local desktop; no multi-user access control |
| V5 Input Validation | Yes | `WslDocument` JSON from Rete.js editor validated by `WslCompiler.Compile()` before execution |
| V6 Cryptography | No | No secrets transmitted in Phase 6 |
| V9 Communication | Yes | CORS already configured in Agent `Program.cs` for Tauri origins; WebSocket `AllowedOrigins` should match |

### Known Threat Patterns for Stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Malformed WSL JSON submitted to POST /workflows/compile | Tampering | `WslCompiler.Compile()` validates all nodes/edges; returns `CompilationResult.Failed()` with diagnostics; never executes invalid document |
| WebSocket origin spoofing (non-Tauri client) | Spoofing | `app.UseWebSockets(new WebSocketOptions { AllowedOrigins = ["tauri://localhost", "https://tauri.localhost", "http://localhost:5173"] })` |
| Unbounded runspace creation via POST /runspaces | Denial of Service | Planner should add a guard: reject if `manager.ListActive().Count > N` (configurable); or defer to Phase 8 |
| File path traversal in workflow save names | Tampering | Sanitize workflow file names: allow only `[a-zA-Z0-9_-]` before appending `.json`; `@tauri-apps/plugin-fs` BaseDirectory sandboxing provides defense-in-depth |

---

## Sources

### Primary (HIGH confidence)

- `Helix.Orchestration.Core/WSL/WslCompiler.cs` — existing compiler stubbed at safety/loop/branch expansion
- `Helix.Orchestration.Core/Safety/ISafetyEngine.cs` — interlock rule interface
- `Helix.Orchestration.Core/Runspace/IRunspaceManager.cs` — manager interface
- `Helix.Orchestration.Agent/Program.cs` — existing DI wiring and CORS configuration
- `Helix.Orchestration.Agent/Runspaces/DurableRunspace.cs` — runspace state machine
- `learn.microsoft.com/aspnet/core/fundamentals/websockets` — ASP.NET Core WebSocket middleware pattern [VERIFIED: fetched 2026-05-29]
- `tauri.app/plugin/file-system/` — Tauri fs plugin API [VERIFIED: fetched 2026-05-29]
- npm registry `npm view rete/rete-react-plugin/.../version` — all package versions [VERIFIED: 2026-05-29]
- `github.com/retejs/` GitHub org — all Rete.js packages confirmed as official [VERIFIED: fetched 2026-05-29]
- `github.com/recharts/recharts` — recharts confirmed official [VERIFIED: via npm view repository.url]

### Secondary (MEDIUM confidence)

- `tauri.app/security/capabilities/` — Tauri v2 capabilities format [VERIFIED: fetched 2026-05-29]
- Phase 5 PATTERNS.md — established patterns for Tauri command registration and LazyStore

### Tertiary (LOW confidence / ASSUMED)

- Rete.js v2 initialization code (Pattern 3) — docs returned 404; derived from npm package API exports + GitHub org README summary
- `AgentService` broadcast pattern (Pattern 2) — standard WebSocket broadcast idiom; specific implementation is at planner's discretion

---

## Metadata

**Confidence breakdown:**
- Standard stack (Agent): HIGH — all APIs exist in current codebase or ASP.NET Core built-ins
- Standard stack (Desktop): HIGH — all npm packages confirmed on registry from official GitHub orgs
- Architecture: HIGH — follows existing patterns from Phase 5 + established Agent DI structure
- Rete.js init patterns: MEDIUM — docs 404'd; derived from package exports
- Pitfalls: HIGH — based on documented ASP.NET Core WebSocket behavior and verified API constraints

**Research date:** 2026-05-29
**Valid until:** 2026-06-29 (Rete.js v2 is stable; check recharts for major version changes)

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| C2 | Rete.js workflow builder: Step, Condition, Branch, Loop, Parallel, Delay, WaitFor nodes; edges define flow; generates valid WSL JSON | Rete.js v2 package stack confirmed; ClassicPreset node pattern; WSL JSON export pattern documented |
| C3 | Device capability picker: shows registered capabilities from GET /devices; node inputs bound to capability parameters | GET /devices endpoint design documented; CapabilityRegistry.GetAllCapabilityNames() as data source |
| C4 | Experiment execution panel: start/pause/abort runspace; step-level status from WebSocket RunspaceEvents | REST endpoints POST/GET /runspaces documented; WebSocket hook pattern documented; IRunspaceEventEmitter design |
| C5 | Live trend charts: scalar (temperature, pressure, flow), spectral (UV/MIR overlay); data from WebSocket TrendValue frames | recharts LineChart/AreaChart; useAgentWebSocket hook; sliding window buffer pattern |
| C6 | Safety status panel: interlock rule list, current values per rule, triggered/safe state; data from WebSocket SafetyEvents | ISafetyEngine.GetViolationsAsync(); InterlockRuleDto serialization fix; SafetyEvent WebSocket frame |
| C9 | Workflow library: save/load/duplicate workflow definitions locally | @tauri-apps/plugin-fs confirmed; BaseDirectory.AppData; capabilities.json permissions documented |
| D4 | UI component tests: node creation, edge wiring, WSL JSON export, execution panel state transitions | Vitest + Testing Library already configured in Phase 5; Wave 0 gaps listed; Rete.js mock strategy documented |
</phase_requirements>
