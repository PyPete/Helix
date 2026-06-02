---
phase: "7"
generated: "2026-06-02"
---

# Phase 7: Site-Level React Dashboard — Pattern Map

**Mapped:** 2026-06-02
**Files analyzed:** 16 new/modified files
**Analogs found:** 15 / 16

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `helix-dashboard/package.json` | config | — | `helix-desktop/package.json` | exact |
| `helix-dashboard/vite.config.ts` | config | — | `helix-desktop/vite.config.ts` | exact |
| `helix-dashboard/vitest.config.ts` | config | — | `helix-desktop/vitest.config.ts` | exact |
| `helix-dashboard/tsconfig.json` | config | — | `helix-desktop/tsconfig.json` | exact |
| `helix-dashboard/tsconfig.node.json` | config | — | `helix-desktop/tsconfig.node.json` | exact |
| `helix-dashboard/index.html` | config | — | `helix-desktop/index.html` | exact |
| `helix-dashboard/src/main.tsx` | config | request-response | `helix-desktop/src/main.tsx` | exact |
| `helix-dashboard/src/App.tsx` | component | request-response | `helix-desktop/src/App.tsx` | role-match |
| `helix-dashboard/src/pages/*.tsx` (4 pages) | component | request-response | `helix-desktop/src/components/ExecutionPanel/ExecutionPanel.tsx` | role-match |
| `helix-dashboard/src/components/SciChartLine.tsx` | component | streaming | `helix-desktop/src/components/charts/PlaceholderChart.tsx` + `ScalarTrendChart.tsx` | role-match |
| `helix-dashboard/src/hooks/useTrendWebSocket.ts` | hook | streaming | `helix-desktop/src/hooks/useAgentWebSocket.ts` | exact |
| `helix-dashboard/src/types/dashboard.ts` | model | — | `helix-desktop/src/types/agent.ts` | exact |
| `helix-dashboard/src/styles/theme.css` | config | — | `.claude/skills/sketch-findings-Helix/sources/themes/default.css` | exact |
| `helix-dashboard/src/test/setup.ts` | config | — | `helix-desktop/src/test/setup.ts` | exact |
| `Helix.Orchestration.SiteServer/Endpoints/TrendWsEndpoint.cs` | middleware | streaming | `Helix.Orchestration.Agent/Program.cs` (ws middleware) + `AgentService.cs` | role-match |
| `Helix.Orchestration.SiteServer/Mqtt/MqttTrendBridge.cs` (extend) | service | event-driven | existing file — add WebSocket fan-out | modify-existing |

---

## Pattern Assignments

### `helix-dashboard/package.json` (config)

**Analog:** `helix-desktop/package.json` (lines 1–37)

**What to copy:** `"type": "module"`, scripts block (`dev`, `build`, `preview`, `test`), devDependencies (`@vitejs/plugin-react`, `@types/react`, `@types/react-dom`, `typescript`, `vite`, `vitest`, `@vitest/coverage-v8`, `jsdom`, `@testing-library/react`, `@testing-library/jest-dom`, `@testing-library/user-event`).

**What to change:**
- `"name": "helix-dashboard"` instead of `helix-desktop`
- Drop all `@tauri-apps/*` dependencies — this is a plain browser SPA
- Drop `recharts`, `allotment`, `@xyflow/react` — not used in dashboard
- Add `react-router-dom` to `dependencies`
- Add `scichart` to `dependencies` (commercial; requires `VITE_SCICHART_KEY` env var)
- Drop `"tauri"` script

**Core pattern** (`helix-desktop/package.json` lines 1–37):
```json
{
  "name": "helix-dashboard",
  "private": true,
  "version": "0.1.0",
  "type": "module",
  "scripts": {
    "dev": "vite",
    "build": "tsc && vite build",
    "preview": "vite preview",
    "test": "vitest"
  },
  "dependencies": {
    "react": "^18.3.0",
    "react-dom": "^18.3.0",
    "react-router-dom": "^6.x",
    "scichart": "^3.x"
  },
  "devDependencies": {
    "@testing-library/jest-dom": "^6.9.1",
    "@testing-library/react": "^16.3.2",
    "@testing-library/user-event": "^14.6.1",
    "@types/react": "^18.3.0",
    "@types/react-dom": "^18.3.0",
    "@vitejs/plugin-react": "^4.3.0",
    "@vitest/coverage-v8": "^4.1.7",
    "jsdom": "^29.1.1",
    "typescript": "^5.5.0",
    "vite": "^6.0.0",
    "vitest": "^4.1.7"
  }
}
```

**Anti-pattern to avoid:** Do not copy the `TAURI_ENV_*` envPrefix or any Tauri build targets.

---

### `helix-dashboard/vite.config.ts` (config)

**Analog:** `helix-desktop/vite.config.ts` (lines 1–29)

**What to copy:** `defineConfig`, `@vitejs/plugin-react` plugin, `server.port` and `strictPort` pattern.

**What to change:**
- Remove the `async` wrapper — it was only there for Tauri env detection
- Replace `envPrefix: ["VITE_", "TAURI_ENV_*"]` with just `envPrefix: ["VITE_"]`
- Remove `server.watch.ignored: ["**/src-tauri/**"]`
- Remove Tauri-conditional `build.target` — use `"es2020"` unconditionally
- Use a different port (e.g. `5174`) to avoid clash with helix-desktop dev server
- No need for `clearScreen: false` (Tauri-specific)

**Core pattern** (`helix-desktop/vite.config.ts` lines 1–29):
```typescript
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5174,
    strictPort: true,
  },
  envPrefix: ["VITE_"],
  build: {
    target: "es2020",
    minify: "esbuild",
    sourcemap: false,
  },
});
```

---

### `helix-dashboard/vitest.config.ts` (config)

**Analog:** `helix-desktop/vitest.config.ts` (lines 1–11) — copy verbatim, only change `setupFiles` path if needed.

**Copy verbatim** (`helix-desktop/vitest.config.ts` lines 1–11):
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

---

### `helix-dashboard/tsconfig.json` + `tsconfig.node.json` (config)

**Analog:** `helix-desktop/tsconfig.json` (lines 1–20) and `helix-desktop/tsconfig.node.json` (lines 1–17).

**Copy tsconfig.json verbatim** — all flags are correct for a browser SPA: `"target": "ES2020"`, `"lib": ["ES2020", "DOM", "DOM.Iterable"]`, `"strict": true`, `"noUnusedLocals": true`, `"noUnusedParameters": true`.

**Copy tsconfig.node.json verbatim** — update `"include"` to include `["vite.config.ts", "vitest.config.ts"]` (already correct).

---

### `helix-dashboard/index.html` (config)

**Analog:** `helix-desktop/index.html`

**What to copy:** Entire structure. Change `<title>` to `"Helix Dashboard"`. Keep `<div id="root">` and `<script type="module" src="/src/main.tsx">`.

---

### `helix-dashboard/src/main.tsx` (app entry, request-response)

**Analog:** `helix-desktop/src/main.tsx` (lines 1–10)

**What to copy:** React 18 `createRoot` pattern, `React.StrictMode` wrapper, CSS import order.

**What to change:** Import dashboard theme CSS instead of `style.css`. Wrap `<App />` in `<BrowserRouter>` from `react-router-dom`. Add SciChart licence init call **before** `createRoot`.

**Core pattern** (`helix-desktop/src/main.tsx` lines 1–10):
```typescript
import React from "react";
import ReactDOM from "react-dom/client";
import App from "./App";
import "./styles/theme.css";

// SciChart licence must be set before any chart renders
import { SciChartSurface } from "scichart";
SciChartSurface.setRuntimeLicenseKey(import.meta.env.VITE_SCICHART_KEY ?? "");

ReactDOM.createRoot(document.getElementById("root") as HTMLElement).render(
  <React.StrictMode>
    <BrowserRouter>
      <App />
    </BrowserRouter>
  </React.StrictMode>
);
```

**Anti-pattern to avoid:** Do not call SciChart licence init inside a component or after `createRoot` — it must be top-level module code.

---

### `helix-dashboard/src/App.tsx` (top-level layout, request-response)

**Analog:** `helix-desktop/src/App.tsx` (lines 1–112)

**What to copy:** Overall function-component pattern, CSS module imports, `useXxx` hooks wired at the top level and props drilled to pages.

**What to change:**
- Replace tab-panel pattern with `<Routes>` / `<Route>` from `react-router-dom`
- Use a persistent `<nav>` with `<NavLink>` for top navbar (not Ribbon — web-native)
- Use `useTrendWebSocket` hook (new) instead of `useAgentWebSocket`
- No `ConnectionConfig` component — SiteServer URL comes from `import.meta.env.VITE_SITE_SERVER_URL`

**Core structural pattern** (`helix-desktop/src/App.tsx` lines 15–111):
```typescript
export default function App() {
  const baseUrl = import.meta.env.VITE_SITE_SERVER_URL ?? "http://localhost:5090";
  const { trendValues, connected } = useTrendWebSocket(baseUrl);

  return (
    <div className="app">
      <header className="app-navbar">
        <span className="app-brand">Helix</span>
        <nav>
          <NavLink to="/">Overview</NavLink>
          <NavLink to="/campaigns">Campaigns</NavLink>
          <NavLink to="/history">History</NavLink>
          <NavLink to="/trends">Trends</NavLink>
        </nav>
        <span className={connected ? "status-healthy" : "status-unhealthy"}>
          {connected ? "Live" : "Offline"}
        </span>
      </header>
      <main>
        <Routes>
          <Route path="/" element={<SiteOverviewPage />} />
          <Route path="/campaigns" element={<CampaignsPage baseUrl={baseUrl} />} />
          <Route path="/history" element={<HistoryPage baseUrl={baseUrl} />} />
          <Route path="/trends" element={<TrendsPage trendValues={trendValues} />} />
        </Routes>
      </main>
    </div>
  );
}
```

---

### `helix-dashboard/src/pages/*.tsx` — 4 page components (component, request-response)

**Analog:** `helix-desktop/src/components/ExecutionPanel/ExecutionPanel.tsx` (lines 1–242)

**What to copy:**
- Named function export with typed props interface at top
- `useState` + `useEffect` for fetch-based data loading
- `try/catch` with `setError` error state
- `data-testid` on root element and key interactive elements
- `aria-label` on `<section>` wrapper
- `role="alert"` on error paragraph

**Core fetch pattern** (`ExecutionPanel.tsx` lines 85–113):
```typescript
const handleStart = async () => {
  setSubmitting(true);
  setError(null);
  try {
    const res = await fetch(baseUrl + "/api/campaigns", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    });
    if (!res.ok) {
      setError("Request failed: " + res.statusText);
      return;
    }
    const data = await res.json();
    // update local state with data
  } catch (e) {
    setError((e as Error).message);
  } finally {
    setSubmitting(false);
  }
};
```

**Error display pattern** (`ExecutionPanel.tsx` lines 156–161):
```typescript
{error && (
  <p role="alert" className="error">
    {error}
  </p>
)}
```

**Anti-pattern to avoid:** Do not inline hardcoded hex colors for status — use CSS custom properties (`var(--color-success)`, `var(--color-danger)`) from the shared theme. ExecutionPanel uses hardcoded `"#27ae60"` strings; the dashboard must use theme variables instead.

---

### `helix-dashboard/src/components/SciChartLine.tsx` (component, streaming)

**Analog:** `helix-desktop/src/components/charts/PlaceholderChart.tsx` (lines 1–11) + `ScalarTrendChart.tsx` (lines 1–39)

**What to copy from PlaceholderChart:**
- Fallback render when chart cannot initialise (`data-testid="chart-unavailable"`)
- Props interface shape (`trendName`, `data`)

**What to copy from ScalarTrendChart:**
- Props interface: `trendName: string`, `data: Array<{t: number; v: number}>`, `unit?: string`
- `<section aria-label>` + `data-testid` wrapper pattern
- `isAnimationActive={false}` concept — SciChart equivalent is real-time mode, no transitions

**What to change:**
- Replace recharts `<LineChart>` with `SciChartReact` (or manual `SciChartSurface` init via `useEffect`)
- Return `<PlaceholderChart>` if `SciChartSurface` init fails or key is missing
- Chart container needs a fixed pixel `height` — SciChart canvas requires explicit height

**Scaffold pattern** (`ScalarTrendChart.tsx` lines 17–39 adapted):
```typescript
interface SciChartLineProps {
  trendName: string;
  data: Array<{ t: number; v: number }>;
  unit?: string;
}

export function SciChartLine({ trendName, data, unit }: SciChartLineProps) {
  const rootRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!rootRef.current) return;
    let surface: SciChartSurface | undefined;
    SciChartSurface.create(rootRef.current).then(({ sciChartSurface }) => {
      surface = sciChartSurface;
      // add axes, data series, modifiers here
    });
    return () => surface?.delete();
  }, []);

  return (
    <section aria-label={`Trend: ${trendName}`} data-testid="scichart-line">
      <h3>{trendName}{unit ? ` (${unit})` : ""}</h3>
      <div ref={rootRef} style={{ height: 200, width: "100%" }} />
    </section>
  );
}
```

**Anti-pattern to avoid:** Do not call `SciChartSurface.create` at module scope or in a component body (outside `useEffect`) — it must be deferred until the DOM element is mounted.

---

### `helix-dashboard/src/hooks/useTrendWebSocket.ts` (hook, streaming)

**Analog:** `helix-desktop/src/hooks/useAgentWebSocket.ts` (lines 1–50) — nearest exact match in the codebase.

**What to copy verbatim:**
- `useState` + `useEffect` structure (lines 14–47)
- `ws.onopen`, `ws.onclose`, `ws.onerror` → `setConnected` pattern (lines 23–25)
- JSON parse with silent `catch {}` for malformed frames (lines 27–43)
- Ring-buffer slice pattern `prev.slice(-200)` for bounded state (line 31)
- Cleanup `return () => ws.close()` (line 46)

**What to change:**
- URL: `baseUrl.replace(/^http/, "ws") + "/api/trends/ws"` (different path from Agent's `/ws`)
- State: only `trendValues: TrendValue[]` and `connected: boolean` — no `runspaceEvents`, `safetyEvents`, `deviceStatusEvents`
- Frame dispatch: only `frame.type === "trend-value"` branch needed
- Import `TrendValue` from `../types/dashboard` (local copy, not from helix-desktop)

**Core pattern** (`useAgentWebSocket.ts` lines 1–50):
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

    ws.onopen = () => setConnected(true);
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

---

### `helix-dashboard/src/types/dashboard.ts` (model)

**Analog:** `helix-desktop/src/types/agent.ts` (lines 1–114)

**What to copy:** `TrendValue` interface (lines 65–71) exactly — same JSON frame shape used by both Agent and SiteServer WebSocket.

**What to also copy:** `RunspaceEvent` (lines 57–63) if any page needs it. `AgentConnection` / `DEFAULT_CONNECTION` pattern for `SiteServerConnection` type.

**What to add (new):** `FumehoodStatus` type for placeholder fumehood cards:
```typescript
export interface FumehoodStatus {
  id: string;
  name: string;
  status: "idle" | "running" | "faulted" | "offline";
  activeExperimentName?: string;
  lastSeenAt: string; // ISO timestamp
}
```

**TrendValue to copy verbatim** (`agent.ts` lines 65–71):
```typescript
export interface TrendValue {
  type: "trend-value";
  trendName: string;
  value: number;
  unit: string;
  timestamp: string;
}
```

---

### `helix-dashboard/src/styles/theme.css` (config)

**Analog:** `.claude/skills/sketch-findings-Helix/sources/themes/default.css` (lines 1–65)

**Copy the entire file verbatim.** This is the authoritative dark IDE palette. All dashboard components use `var(--color-*)`, `var(--space-*)`, `var(--text-*)` from this file.

**Import in:** `main.tsx` (before App import), `index.css` (if created).

**Full custom property list** (all must be present):
```
--color-bg, --color-surface, --color-surface-raised, --color-surface-overlay
--color-border, --color-border-strong
--color-text, --color-text-muted, --color-text-dim
--color-primary, --color-primary-hover, --color-primary-dim
--color-success, --color-warning, --color-danger, --color-info
--color-online, --color-offline, --color-busy
--color-ribbon-bg, --color-ribbon-tab, --color-ribbon-active
--font-sans, --font-mono
--text-xs through --text-xl
--space-1 through --space-8
--radius-sm, --radius-md, --radius-lg
--shadow-sm, --shadow-md, --shadow-lg, --shadow-panel
```

**Anti-pattern to avoid:** Do not re-declare theme variables in component-level CSS. Use `var()` references only. The helix-desktop `App.css` uses hardcoded hex literals (`#1a1a2e`, `#9d7fff`) — the dashboard must use CSS variables instead.

---

### `helix-dashboard/src/test/setup.ts` (config)

**Analog:** `helix-desktop/src/test/setup.ts` (line 1) — copy verbatim:
```typescript
import "@testing-library/jest-dom";
```

---

### Hook test: `useTrendWebSocket.test.ts` (test, streaming)

**Analog:** `helix-desktop/src/hooks/useAgentWebSocket.test.ts` (lines 1–134) — nearest exact match.

**What to copy verbatim:**
- `vi.hoisted` pattern for `mockClose` (lines 6–8)
- Manual `lastWsInstance` tracking with `vi.stubGlobal("WebSocket", ...)` (lines 10–33)
- `beforeEach`/`afterEach` teardown with `vi.restoreAllMocks()` + `mockClose.mockReset()` (lines 35–39)
- `renderHook` + `waitFor` + `act` import pattern (line 1)

**What to change:**
- Only test `trend-value` frame dispatch and `connected` state — remove `safety-event` / `runspaceEvent` test cases
- URL suffix is `/api/trends/ws` not `/ws`

**Mock WebSocket pattern** (`useAgentWebSocket.test.ts` lines 6–39):
```typescript
const { mockClose } = vi.hoisted(() => ({ mockClose: vi.fn() }));

let lastWsInstance: { ... } | null = null;

describe("useTrendWebSocket", () => {
  beforeEach(() => {
    lastWsInstance = null;
    vi.stubGlobal("WebSocket", vi.fn(function (this: typeof lastWsInstance) {
      this!.onopen = null;
      this!.onclose = null;
      this!.onerror = null;
      this!.onmessage = null;
      this!.close = mockClose;
      lastWsInstance = this as any;
    }));
  });
  afterEach(() => {
    vi.restoreAllMocks();
    mockClose.mockReset();
  });
  // ... test cases
});
```

---

### Component test pattern for page components (test, request-response)

**Analog:** `helix-desktop/src/components/TrendCharts/ScalarTrendChart.test.tsx` (lines 1–39)

**What to copy:**
- `vi.mock(...)` for heavy dependencies (SciChart, recharts) — return lightweight stubs
- `render` + `screen.getByTestId` assertion pattern
- Smoke-test: render with minimal/empty props must not throw

**Module mock pattern** (`ScalarTrendChart.test.tsx` lines 6–17):
```typescript
const { mockLineChart } = vi.hoisted(() => ({
  mockLineChart: vi.fn(() => <div data-testid="mock-chart" />),
}));

vi.mock("scichart", () => ({
  SciChartSurface: { create: vi.fn(), setRuntimeLicenseKey: vi.fn() },
}));
```

---

### `Helix.Orchestration.SiteServer/Endpoints/TrendWsEndpoint.cs` (middleware, streaming)

**Analog:** `Helix.Orchestration.Agent/Program.cs` lines 270–289 (WebSocket middleware) + `AgentService.cs` lines 60–100 (`HandleClientAsync`) + lines 106–142 (`BroadcastLoopAsync`)

**What to copy from Agent/Program.cs (lines 270–289) — the WebSocket upgrade middleware:**
```csharp
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
    else
    {
        await next(context);
    }
});
```

**What to copy from AgentService.cs — client management and broadcast:**
- `ConcurrentDictionary<string, WebSocket> _clients` pattern (line 27)
- `HandleClientAsync` receive loop with `WebSocketMessageType.Close` break and `finally` cleanup (lines 60–100)
- `ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct)` per-client send with `_clients.TryRemove` on failure (lines 119–133)
- `OperationCanceledException` silent swallow in broadcast loop (lines 138–140)

**What to change vs. AgentService:**
- New class name: `TrendWebSocketManager` (singleton, registered in DI)
- Not a `BackgroundService` — it is a passive fan-out target called by `MqttTrendBridge`
- Expose `BroadcastTrendValueAsync(TrendValue value)` method instead of a channel-based broadcast loop
- Register: `builder.Services.AddSingleton<TrendWebSocketManager>()` in Program.cs
- Add to `app.UseWebSockets()` call in SiteServer Program.cs (currently absent — must be added)
- Add CORS origin `http://localhost:5174` alongside existing `5173`

**SiteServer Program.cs changes needed** (analog: Agent/Program.cs lines 252–264):
```csharp
// Add before var app = builder.Build();
builder.Services.AddSingleton<TrendWebSocketManager>();
builder.Services.AddWebSockets(options =>
{
    options.AllowedOrigins.Add("http://localhost:5174");
});
// Add after app.UseHttpsRedirection();
app.UseWebSockets();
```

---

### `Helix.Orchestration.SiteServer/Mqtt/MqttTrendBridge.cs` — extend (service, event-driven)

**Analog:** existing file (lines 1–153) — modify, do not replace.

**What to add:**
- Inject `TrendWebSocketManager` via constructor parameter (alongside existing `ITrendProvider`, `IOptions<MqttBridgeOptions>`, `ILogger`)
- In `PublishTrendValue` (line 84), after the fire-and-forget MQTT publish, call `await trendWsManager.BroadcastTrendValueAsync(trendValue, ct)` — or use a `_ = trendWsManager.BroadcastAsync(...)` fire-and-forget matching the existing MQTT publish style (line 111)

**Pattern to preserve** (MqttTrendBridge.cs lines 25–33 — constructor injection):
```csharp
public MqttTrendBridge(
    ITrendProvider trendProvider,
    TrendWebSocketManager trendWsManager,   // add this
    IOptions<MqttBridgeOptions> options,
    ILogger<MqttTrendBridge> logger)
{
    _trendProvider = trendProvider;
    _trendWsManager = trendWsManager;       // add this
    _options = options;
    _logger = logger;
}
```

**Fire-and-forget broadcast pattern** (matching MqttTrendBridge.cs line 111):
```csharp
_ = _trendWsManager.BroadcastAsync(trendValueDto, ct);
```

---

## Shared Patterns

### CSS Theme Variables
**Source:** `.claude/skills/sketch-findings-Helix/sources/themes/default.css`
**Apply to:** Every component CSS file in helix-dashboard
**Rule:** Never use raw hex colors in component CSS. Always `var(--color-*)`. The exceptions in helix-desktop (`App.css` lines 29–32 using `#1a1a2e`, `#9d7fff`) are tech debt — do not replicate.

### Data-testid + aria-label on root element
**Source:** `helix-desktop/src/components/ExecutionPanel/ExecutionPanel.tsx` line 156, `ScalarTrendChart.tsx` line 19
**Apply to:** All page and component files in helix-dashboard
```typescript
<section aria-label="Section Name" data-testid="kebab-name">
```

### Error state pattern
**Source:** `helix-desktop/src/components/ExecutionPanel/ExecutionPanel.tsx` lines 36, 156–161
**Apply to:** All page components that make fetch calls
```typescript
const [error, setError] = useState<string | null>(null);
// ...
{error && <p role="alert" className="error">{error}</p>}
```

### Minimal API endpoint style (C#)
**Source:** `Helix.Orchestration.SiteServer/Endpoints/TrendEndpoints.cs` + `CampaignEndpoints.cs`
**Apply to:** Any new SiteServer endpoints in Phase 7
```csharp
public static class XxxEndpoints
{
    public static void MapXxxEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/xxx").WithTags("Xxx");
        group.MapGet("/", IResult (IService svc) => { ... })
             .WithName("...").WithSummary("...")
             .Produces<T>(200).ProducesProblem(500);
    }
}
// Register in Program.cs: app.MapXxxEndpoints();
```

### WebSocket client dictionary management (C#)
**Source:** `Helix.Orchestration.Agent/AgentService.cs` lines 27, 63–99, 119–135
**Apply to:** `TrendWebSocketManager`
- Use `ConcurrentDictionary<string, WebSocket>` keyed by `Guid.NewGuid().ToString("N")`
- Remove client on `WebSocketState != Open` or on `SendAsync` exception
- Best-effort close in `finally` block: `await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None)`

### Vitest mock isolation
**Source:** `helix-desktop/src/hooks/useAgentWebSocket.test.ts` lines 6–8
**Apply to:** All helix-dashboard test files that stub globals
```typescript
// Use vi.hoisted for mocks referenced in vi.stubGlobal factory:
const { mockFn } = vi.hoisted(() => ({ mockFn: vi.fn() }));
```

---

## No Analog Found

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| `helix-dashboard/src/components/SciChartLine.tsx` (SciChart-specific) | component | streaming | No SciChart usage yet in codebase — helix-desktop uses `PlaceholderChart` stub only. Use SciChart docs for `SciChartSurface.create` + `useEffect` init pattern. The `charts/index.ts` router and `types.ts` in helix-desktop define the props contract to match. |

---

## Metadata

**Analog search scope:** `helix-desktop/src/`, `Helix.Orchestration.Agent/`, `Helix.Orchestration.SiteServer/`, `.claude/skills/sketch-findings-Helix/`
**Files scanned:** ~30
**Pattern extraction date:** 2026-06-02
