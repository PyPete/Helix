# Phase 7: Site-Level React Dashboard - Research

**Researched:** 2026-06-02
**Domain:** Vite + React + TypeScript SPA, SciChart.js WebGL charts, ASP.NET Core WebSocket relay
**Confidence:** HIGH

---

## User Constraints (from CONTEXT.md)

### Locked Decisions

- **Chart library:** SciChart.js (WebGL-accelerated). Full licence wiring at app entry.
- **Live trends:** Raw WebSocket endpoint GET /api/trends/ws on SiteServer. No SignalR.
- **Navigation:** React Router, 4 pages: /, /campaigns, /history, /trends.
- **Chrome:** Dark IDE palette (#1e1e2e, #252535, #9d7fff) + top navbar (not VS-Ribbon).
- **Fumehood data:** Placeholder/simulated only in Phase 7.
- **Type definitions:** Copy TrendValue, RunspaceEvent from helix-desktop/src/types/agent.ts.
- **Test setup:** Vitest + Testing Library (same as helix-desktop).
- **Location:** helix-dashboard/ at repo root alongside helix-desktop/.

### Deferred Ideas (OUT OF SCOPE)

- Real /api/fumehoods endpoint -- later phase.
- SciChart.js licence key rotation -- post-Phase 7.

---

## Phase Requirements

| ID  | Description | Research Support |
|-----|-------------|------------------|
| E1  | React app scaffold (Vite + TypeScript) deployable as static web app | npm create vite@latest helix-dashboard -- --template react-ts |
| E2  | Site overview: fumehood list with status, active experiment count | Placeholder data locked; /api/campaigns GET for active count |
| E3  | Campaign management: create, start, monitor, abort | All endpoints exist: POST/GET/DELETE /api/campaigns |
| E4  | Experiment history browser | **API gap confirmed:** GET /api/experiments missing -- needs new endpoint + repo method |
| E5  | Live trend visualisation via WebSocket from Site Server | WebSocket relay must be added to MqttTrendBridge + Program.cs |
| E6  | Analytical data viewer: spectrum charts, DAD chromatogram 2D | SciChart.js (same library); placeholder data in Phase 7 |

---

## Summary

Phase 7 builds a brand-new Vite + React + TypeScript SPA at helix-dashboard/.
It is structurally identical to helix-desktop/ in toolchain (Vite 6, Vitest + Testing Library,
dark IDE CSS variables) but without Tauri and with react-router for URL-based navigation.

The two novel technical problems are:

1. **SciChart.js WASM bootstrapping in Vite** -- Vite does not bundle .wasm files. WASM files
   must be copied to public/ so SciChart's JS loader finds them at the root URL path.
   SciChartSurface.loadWasmFromCDN() is the low-friction dev-time alternative.

2. **WebSocket fan-out in ASP.NET Core Minimal API** -- A new WebSocketRelay singleton holds a
   ConcurrentDictionary<Guid, WebSocket> of clients. MqttTrendBridge calls BroadcastAsync
   on each MQTT tick. Program.cs adds UseWebSockets() and maps the endpoint.

**Confirmed API gap (E4):** GET /api/experiments (list all) does not exist.
IExperimentRepository has no GetAllAsync method. Must add: interface method,
PostgresExperimentRepository implementation, and MapGet('/') in ExperimentEndpoints.

**Primary recommendation:** Scaffold helix-dashboard/ via react-ts Vite template, add postinstall
WASM copy, install scichart scichart-react react-router, wire WebSocket relay in SiteServer,
add missing experiments list endpoint -- then build page components.

---

## Architectural Responsibility Map
| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|----------|
| Fumehood status (Phase 7) | Browser / Client | -- | Hardcoded placeholder per locked decision |
| Campaign CRUD UI | Browser / Client | API / Backend (existing) | UI calls /api/campaigns endpoints |
| Experiment history list | API / Backend | Browser / Client | Needs new GET /api/experiments + DB query |
| Live trend stream | API / Backend | Browser / Client | SiteServer WS relay; client subscribes |
| Chart rendering (SciChart.js) | Browser / Client (WebGL) | -- | Client-side WebGL; WASM at page init |
| Navigation / routing | Browser / Client | -- | React Router declarative mode, SPA |
| CSS / theme | Browser / Client | -- | Copy CSS variables from helix-desktop theme |
| WebSocket relay (fan-out) | API / Backend | -- | MqttTrendBridge broadcasts to all clients |

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|-------------- |
| vite | ^6.0.0 | Build tool / dev server | Already in helix-desktop |
| @vitejs/plugin-react | ^6.0.2 | Babel JSX transform | Already in helix-desktop |
| react + react-dom | ^18.3.0 | UI framework | Already in helix-desktop |
| typescript | ^5.5.0 | Type safety | Already in helix-desktop |
| scichart | 5.2.32 | WebGL chart engine + WASM runtime | Locked decision; latest stable 2026-06-02 |
| scichart-react | 1.0.0 | React wrapper for SciChart surfaces | Official wrapper; handles StrictMode + cleanup |
| react-router | 7.16.0 | Client-side routing | Locked decision; v7 declarative mode |

[VERIFIED: npm registry] -- all packages confirmed via npm view and npm downloads API 2026-06-02.
[CITED: scichart.com/licensing-scichart-js]
[CITED: reactrouter.com/start/declarative/installation]

### Supporting (dev only)

| Library | Version | Purpose |
|---------|---------|--------|
| vitest | ^4.1.7 | Test runner (same as helix-desktop) |
| @testing-library/react | ^16.3.2 | Component testing |
| @testing-library/user-event | ^14.6.1 | Interaction simulation |
| @testing-library/jest-dom | ^6.9.1 | DOM matchers |
| jsdom | ^29.1.1 | DOM environment for tests |
| @vitest/coverage-v8 | ^4.1.7 | Coverage reports |

**Installation:**

```bash
npm create vite@latest helix-dashboard -- --template react-ts
cd helix-dashboard
npm install scichart scichart-react react-router
npm install --save-dev vitest @vitest/coverage-v8 @testing-library/react @testing-library/user-event @testing-library/jest-dom jsdom
```

---

## Package Legitimacy Audit

> slopcheck checks PyPI only -- these are npm packages. False positives guaranteed.
> All packages verified directly on the npm registry.

| Package | Registry | Age | Downloads/wk | Source Repo | Disposition |
|---------|----------|-----|--------------|-------------|-------------|
| scichart | npm | ~6 yrs (2020-02) | 26,250 | github.com/abtsoftware/scichart.js.examples | Approved |
| scichart-react | npm | ~2.5 yrs (2023-11) | 6,771 | github.com/ABTSoftware/scichart-react | Approved |
| react-router | npm | ~10 yrs (2016-12) | 40.4M | github.com/remix-run/react-router | Approved |
| @vitejs/plugin-react | npm | ~4.5 yrs (2021-09) | 56M | github.com/vitejs/vite-plugin-react | Approved |

No postinstall scripts on scichart or scichart-react.
[VERIFIED: npm registry] via npm view + npm downloads API on 2026-06-02.

---

## Architecture Patterns

### System Architecture Diagram

```
Browser (helix-dashboard SPA)
|
+-- React Router (BrowserRouter)
|   +-- /              --> SiteOverviewPage  (placeholder fumehood cards)
|   +-- /campaigns     --> CampaignsPage    (REST: GET/POST /api/campaigns)
|   +-- /history       --> HistoryPage      (REST: GET /api/experiments  <- NEW)
|   +-- /trends        --> TrendsPage
|                           |
|                           +-- new WebSocket(".../api/trends/ws")
|                                 |
|                                 v
|                           SciChartSurface (WebGL)
|                           XyDataSeries.append(x, y) [fifoCapacity=500]
|
+-- Layout: top Navbar (dark IDE palette) + Outlet

SiteServer (ASP.NET Core Minimal API)
|
+-- app.UseWebSockets()                      <- add to Program.cs
+-- MapGet("/api/trends/ws", ...)            <- new WebSocket endpoint
|       |
|       +-- WebSocketRelay (new singleton)
|               +-- ConcurrentDictionary<Guid, WebSocket> _clients
|               +-- BroadcastAsync(json)     <- called from MqttTrendBridge
|
+-- MqttTrendBridge (existing BackgroundService)
|       +-- on each TrendValue: relay.BroadcastAsync(payload)  <- add
|
+-- GET  /api/campaigns             (existing)
+-- POST /api/campaigns             (existing)
+-- GET  /api/campaigns/{id}        (existing)
+-- POST /api/campaigns/{id}/start  (existing)
+-- DELETE /api/campaigns/{id}      (existing)
+-- GET  /api/experiments/{id}      (existing)
+-- GET  /api/experiments           <- NEW (IExperimentRepository.GetAllAsync)
+-- GET  /api/trends                (existing)
+-- GET  /api/trends/{id}/history   (existing)
```

### Recommended Project Structure

```
helix-dashboard/
+-- public/
|   +-- scichart2d.wasm            # from node_modules/scichart/_wasm/
|   +-- scichart2d-nosimd.wasm
|   +-- scichart2d.js
+-- scripts/
|   +-- copy-wasm.js
+-- src/
|   +-- main.tsx                   # entry: setRuntimeLicenseKey + BrowserRouter
|   +-- App.tsx                    # Routes definition
|   +-- components/
|   |   +-- Navbar.tsx
|   |   +-- Layout.tsx             # Navbar + Outlet
|   +-- pages/
|   |   +-- SiteOverviewPage.tsx
|   |   +-- CampaignsPage.tsx
|   |   +-- HistoryPage.tsx
|   |   +-- TrendsPage.tsx
|   +-- hooks/
|   |   +-- useCampaigns.ts
|   |   +-- useExperiments.ts
|   |   +-- useTrendWebSocket.ts
|   +-- types/
|   |   +-- agent.ts               # copied from helix-desktop/src/types/agent.ts
|   +-- themes/
|   |   +-- default.css            # copied from sketch-findings default.css
|   +-- test/
|       +-- setup.ts
+-- index.html
+-- vite.config.ts
+-- vitest.config.ts
+-- tsconfig.json
+-- package.json
```

### Pattern 1: SciChart WASM Bootstrapping in Vite

Confirmed WASM files (npm pack scichart@5.2.32 --dry-run) [VERIFIED: npm registry]:
- _wasm/scichart2d.wasm (1.3 MB) -- main 2D WASM module
- _wasm/scichart2d-nosimd.wasm (1.3 MB) -- SIMD fallback
- _wasm/scichart2d.js (161 kB) -- JS loader

Vite does NOT bundle .wasm files. Place in public/ via postinstall:

```json
{
  "scripts": {
    "copy:wasm": "node scripts/copy-wasm.js",
    "postinstall": "npm run copy:wasm"
  }
}
```

```javascript
// scripts/copy-wasm.js
const fs = require("fs");
const files = ["scichart2d.wasm", "scichart2d-nosimd.wasm", "scichart2d.js"];
for (const f of files) {
  fs.copyFileSync(`node_modules/scichart/_wasm/${f}`, `public/${f}`);
  console.log("Copied", f);
}
```

Alternative for local dev only (do NOT use in CI/production):

```typescript
SciChartSurface.loadWasmFromCDN(); // before ReactDOM.createRoot
```
[CITED: github.com/ABTSoftware/scichart-react README]

### Pattern 2: License Key Initialization

[CITED: scichart.com/licensing-scichart-js -- "must be called once before any SciChartSurface is instantiated"]

```typescript
// src/main.tsx
import { SciChartSurface } from "scichart";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router";
import { App } from "./App";
import "./themes/default.css";

// Community edition: empty string = watermark but fully functional.
SciChartSurface.setRuntimeLicenseKey(import.meta.env.VITE_SCICHART_KEY ?? "");

ReactDOM.createRoot(document.getElementById("root")!).render(
  <BrowserRouter><App /></BrowserRouter>
);
```

### Pattern 3: SciChart React Component -- FIFO Streaming Data

[ASSUMED] derived from scichart-react README + Tutorial_02 pattern.
[CITED: scichart.com/documentation/js/v5/.../realtime-updates/ -- fifoCapacity (v3.2+)]

Key rules:
- initChart MUST be stable: use useCallback(fn, [])
- fifoCapacity: 500 auto-discards oldest points -- no manual clearing needed
- Feed data via seriesRef.current.append() in useEffect, NOT via React state

```typescript
// src/components/LiveTrendChart.tsx
import { SciChartReact } from "scichart-react";
import {
  SciChartSurface, NumericAxis, FastLineRenderableSeries,
  XyDataSeries, SciChartJsNavyTheme,
} from "scichart";
import { useRef, useEffect, useCallback } from "react";

export function LiveTrendChart({ trendValues }: { trendValues: {x:number,y:number}[] }) {
  const seriesRef = useRef<XyDataSeries | null>(null);

  useEffect(() => {
    if (!seriesRef.current || !trendValues.length) return;
    const last = trendValues[trendValues.length - 1];
    seriesRef.current.append(last.x, last.y);
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

  return <SciChartReact style={{ width: "100%", height: 300 }} initChart={initChart} />;
}
```

### Pattern 4: React Router v7 Declarative Layout

In React Router v7, install react-router (not react-router-dom).
[CITED: reactrouter.com/upgrading/v6 -- "react-router-dom is no longer needed in v7"]
[CITED: reactrouter.com/start/declarative/routing -- layout routes, Outlet]

```typescript
// src/App.tsx
import { Routes, Route } from "react-router";

export function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<SiteOverviewPage />} />
        <Route path="campaigns" element={<CampaignsPage />} />
        <Route path="history" element={<HistoryPage />} />
        <Route path="trends" element={<TrendsPage />} />
      </Route>
    </Routes>
  );
}

// src/components/Layout.tsx
import { Outlet } from "react-router";
export function Layout() {
  return (
    <div style={{ minHeight: "100vh", background: "var(--color-bg)" }}>
      <Navbar />
      <main style={{ padding: "var(--space-4)" }}><Outlet /></main>
    </div>
  );
}
```

### Pattern 5: ASP.NET Core WebSocket Fan-Out

[CITED: learn.microsoft.com/aspnet/core/fundamentals/websockets -- UseWebSockets, AcceptWebSocketAsync, receive loop]

```csharp
// WebSocketRelay.cs -- new singleton service
public sealed class WebSocketRelay
{
    private readonly ConcurrentDictionary<Guid, WebSocket> _clients = new();
    public void Register(Guid id, WebSocket socket) => _clients[id] = socket;
    public void Remove(Guid id) => _clients.TryRemove(id, out _);

    public async Task BroadcastAsync(string json, CancellationToken ct = default)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        var dead = new List<Guid>();
        foreach (var (id, ws) in _clients)
        {
            if (ws.State != WebSocketState.Open) { dead.Add(id); continue; }
            try { await ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct); }
            catch { dead.Add(id); }
        }
        foreach (var id in dead) _clients.TryRemove(id, out _);
    }
}

// Program.cs additions (BEFORE app.Map* calls)
builder.Services.AddSingleton<WebSocketRelay>();
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });
app.MapGet("/api/trends/ws", async (HttpContext ctx, WebSocketRelay relay, CancellationToken ct) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest) { ctx.Response.StatusCode = 400; return; }
    using var ws = await ctx.WebSockets.AcceptWebSocketAsync();
    var id = Guid.NewGuid();
    relay.Register(id, ws);
    try {
        var buf = new byte[256];
        while (ws.State == WebSocketState.Open) {
            var r = await ws.ReceiveAsync(buf, ct);
            if (r.MessageType == WebSocketMessageType.Close)
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", ct);
        }
    } finally { relay.Remove(id); }
});

// MqttTrendBridge: inject _relay; after each publish:
_ = _relay.BroadcastAsync(payload, CancellationToken.None);
```

### Pattern 6: vite.config.ts

[ASSUMED] -- mirrors helix-desktop minus Tauri settings

```typescript
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5174,   // different from helix-desktop (5173)
    strictPort: true,
    proxy: {
      // WS entry MUST come before generic /api (first-match wins)
      "/api/trends/ws": { target: "ws://localhost:5000", ws: true, changeOrigin: true },
      "/api": { target: "http://localhost:5000", changeOrigin: true },
    },
  },
  build: { target: "es2020", sourcemap: true },
});
```

### CSS Sharing

Copy sketch-findings-Helix/sources/themes/default.css to helix-dashboard/src/themes/default.css.
Import in main.tsx. All 60 CSS custom properties are identical to helix-desktop.
No monorepo tooling needed for 2 apps -- extract to workspace package if a third app appears.

### Anti-Patterns to Avoid

- WASM via Vite asset pipeline: Do NOT import wasmUrl from "./scichart2d.wasm". Use public/ placement.
- setRuntimeLicenseKey inside a component: Call once in main.tsx before ReactDOM.createRoot.
- Inline anonymous initChart in JSX: Must be stable. Use useCallback(fn, []).
- Chart data in React state: Use dataSeries.append() via ref, NOT useState.
- UseWebSockets() after Map* calls: Middleware must be registered before endpoint mappings.
- Generic /api proxy before /api/trends/ws: Vite proxy first-match; WS entry must precede /api.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| SciChart React lifecycle | Custom useEffect mount/unmount | scichart-react SciChartReact | Handles StrictMode, cleanup, group context |
| FIFO scrolling time series | Manual array slice + clear + appendRange | XyDataSeries fifoCapacity option | Built-in, O(1) memory |
| WebSocket reconnect | Complex retry wrapper | ws.onclose = () => setTimeout(connect, 2000) | Sufficient for Phase 7 |

---

## API Gap Analysis

### Confirmed Existing Endpoints (codebase inspection)

| Endpoint | Method | Status |
|----------|--------|--------|
| /api/campaigns | GET | EXISTS -- CampaignEndpoints.cs |
| /api/campaigns | POST | EXISTS |
| /api/campaigns/{id} | GET | EXISTS |
| /api/campaigns/{id}/start | POST | EXISTS |
| /api/campaigns/{id} | DELETE | EXISTS |
| /api/experiments | POST | EXISTS -- ExperimentEndpoints.cs |
| /api/experiments/{id} | GET | EXISTS |
| /api/experiments/{id} | DELETE | EXISTS |
| /api/trends | GET | EXISTS -- TrendEndpoints.cs |
| /api/trends/{id}/history | GET | EXISTS |

### Gaps Phase 7 Must Close

| Endpoint | Method | Required By | Gap Detail |
|----------|--------|-------------|------------|
| /api/experiments | GET | E4 | MISSING. IExperimentRepository has no GetAllAsync. Add: interface method, PostgresExperimentRepository impl, MapGet("/") in ExperimentEndpoints. |
| /api/trends/ws | WebSocket GET | E5 | MISSING. Add WebSocketRelay singleton + UseWebSockets() in Program.cs + MapGet handler + fan-out in MqttTrendBridge. |

No other endpoints are needed for the four pages (fumehood data is placeholder).

---

## Common Pitfalls

### Pitfall 1: SciChart WASM 404 at Runtime
What: Charts blank; console 404 for scichart2d.wasm.
Why: Vite does not auto-bundle .wasm.
Prevention: postinstall copy script (Pattern 1), or loadWasmFromCDN() for local dev.

### Pitfall 2: StrictMode Double-Invoke
What: In dev, charts fail to init or error on "surface already exists".
Why: StrictMode mounts -> unmounts -> remounts. Raw useEffect does not handle this.
Prevention: Always use scichart-react SciChartReact component.

### Pitfall 3: WebSocket Pipeline Closes Immediately
What: /api/trends/ws disconnects immediately after connect.
Why: Minimal API handler returns, ending the pipeline.
Prevention: Handler must await the receive loop. Never return until socket closes.
[CITED: learn.microsoft.com/aspnet/core/fundamentals/websockets]

### Pitfall 4: GET /api/experiments Returns 404
What: History page 404 on load.
Why: Only GET /api/experiments/{id} exists. IExperimentRepository has no GetAllAsync.
Prevention: Add GetAllAsync, implement, add endpoint BEFORE building HistoryPage.

### Pitfall 5: React Router v7 Import Confusion
What: Mixing react-router-dom and react-router imports.
Prevention: Install react-router only. Import from "react-router".

### Pitfall 6: Vite Proxy Misses WebSocket Upgrades
What: REST calls work but WebSocket fails CORS or wrong port.
Prevention: Add /api/trends/ws entry with ws: true BEFORE generic /api (Pattern 6).

### Pitfall 7: loadWasmFromCDN() Breaks Offline/CI
What: CI or offline lab builds fail when CDN unreachable.
Prevention: CDN loading for local dev only. Copy WASM to public/ for CI and production.

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Vitest 4.1.7 |
| Config file | helix-dashboard/vitest.config.ts (Wave 0) |
| Quick run | cd helix-dashboard && npm test -- --run |
| Full suite | cd helix-dashboard && npm test -- --coverage |

### SciChart Mock Strategy

SciChart requires WebGL + WASM -- not available in jsdom. Mock both packages:

```typescript
// src/test/setup.ts
import "@testing-library/jest-dom";
import React from "react";

vi.mock("scichart", () => ({
  SciChartSurface: { setRuntimeLicenseKey: vi.fn(), create: vi.fn(), loadWasmFromCDN: vi.fn() },
  NumericAxis: vi.fn(),
  FastLineRenderableSeries: vi.fn(),
  XyDataSeries: vi.fn(() => ({ append: vi.fn() })),
  SciChartJsNavyTheme: vi.fn(),
}));

vi.mock("scichart-react", () => ({
  SciChartReact: () => React.createElement("div", { "data-testid": "scichart-surface" }),
}));
```

### Phase Requirements to Test Map

| Req ID | Behavior | Test Type | File |
|--------|----------|-----------|------|
| E1 | App renders without crash | smoke | src/App.test.tsx (Wave 0) |
| E2 | SiteOverviewPage renders fumehood placeholder cards | unit | src/pages/SiteOverviewPage.test.tsx (Wave 0) |
| E3 | CampaignsPage renders list, POST on create | unit | src/pages/CampaignsPage.test.tsx (Wave 0) |
| E4 | HistoryPage renders experiment list from mocked GET | unit | src/pages/HistoryPage.test.tsx (Wave 0) |
| E5 | useTrendWebSocket receives messages, updates state | unit | src/hooks/useTrendWebSocket.test.ts (Wave 0) |
| E5 | TrendsPage renders with mocked SciChartReact | unit | src/pages/TrendsPage.test.tsx (Wave 0) |
| E6 | AnalyticsPage renders spectrum placeholder | unit | src/pages/AnalyticsPage.test.tsx (Wave 0) |

Sampling rate:
- Per task commit: cd helix-dashboard && npm test -- --run
- Per wave merge: cd helix-dashboard && npm test -- --coverage
- Phase gate: Full suite green before /gsd:verify-work

### Wave 0 Gaps

- [ ] helix-dashboard/vitest.config.ts -- mirrors helix-desktop version
- [ ] helix-dashboard/src/test/setup.ts -- jest-dom + SciChart mocks
- [ ] helix-dashboard/src/App.test.tsx -- smoke test
- [ ] Page tests: SiteOverviewPage, CampaignsPage, HistoryPage, TrendsPage, AnalyticsPage
- [ ] helix-dashboard/src/hooks/useTrendWebSocket.test.ts
- [ ] npm install from helix-dashboard/ to trigger postinstall WASM copy

---

## Security Domain

security_enforcement not set in .planning/config.json -- treating as enabled.

| ASVS Category | Applies | Standard Control |
|---------------|---------|------------------|
| V2 Authentication | No | Unauthenticated internal tool (lab intranet) in Phase 7 |
| V3 Session Management | No | No sessions in Phase 7 |
| V4 Access Control | No | Not in Phase 7 scope |
| V5 Input Validation | Yes | Campaign name/description -- validate before POST |
| V6 Cryptography | No | No crypto operations |
| V7 Error Handling | Yes | Do not expose SiteServer stack traces in dashboard UI |

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| XSS via experiment/campaign names | Spoofing | React escapes by default; never dangerouslySetInnerHTML |
| WebSocket message injection | Tampering | Validate msg.type before dispatch |
| CORS / origin exposure | Information Disclosure | Set WebSocketOptions.AllowedOrigins for production |

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Node.js / npm | helix-dashboard build | Yes | 24.12.0 | -- |
| .NET SDK (SiteServer) | WebSocket backend | Yes (solution exists) | inferred from .csproj | -- |
| scichart npm package | E5, E6 | Not yet installed | 5.2.32 | loadWasmFromCDN() for dev |
| react-router npm package | Navigation | Not yet installed | 7.16.0 | -- |
| SiteServer running locally | Dev integration | Manual start | -- | Mock fetch in tests |
| helix-dashboard/ directory | All | Does NOT exist yet | -- | npm create vite@latest |
| Docker Compose (postgres) | E4 real data | Manual start | -- | In-memory repo for dev |

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| import from react-router-dom | import from react-router | React Router v7 (2024) | react-router-dom is a re-export; new projects use react-router |
| Webpack CopyPlugin for WASM | Vite public/ + postinstall script | Vite ecosystem (~2021) | No plugin needed |
| Manual useEffect SciChart setup | scichart-react SciChartReact component | scichart-react (2023) | Handles StrictMode, cleanup, group context |
| SciChartSurface.configure() | SciChartSurface.setRuntimeLicenseKey() | SciChart v3.2+ | Old method deprecated |
| No community edition | Free community watermark mode | SciChart v3.2+ | Empty key = community mode |

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | SiteServer development port is 5000 for Vite proxy | Pattern 6 | Fix proxy target -- trivial change |
| A2 | PostgresExperimentRepository.GetAllAsync uses "experiments" table | API Gap | Verify schema at implementation |
| A3 | SciChartSurface.loadWasmFromCDN() available in scichart v5.2.32 | Pattern 1 | If absent, public/ copy is the only approach |
| A4 | XyDataSeries fifoCapacity constructor param present in v5.2.32 | Pattern 3 | Documented for v3.2+; verify at implementation |
| A5 | Vite dev proxy handles WS upgrades with ws: true | Pattern 6 | Documented Vite feature; not tested in this setup |
| A6 | SciChartJsNavyTheme exported from scichart v5 | Pattern 3 | Fallback: new SciChartJsDarkTheme() |
| A7 | IExperimentRepository DI is live (Phase 1 merged) | API Gap | Verify AddHelixInfrastructure is active in Program.cs |

---

## Open Questions

1. SciChart licence key -- Does team have a paid key? Store as VITE_SCICHART_KEY env var;
   default "" (community) for Phase 7.

2. SiteServer development port -- Not set in Program.cs. Check appsettings.Development.json
   at implementation; update Vite proxy target.

3. Vite WS proxy forwarding -- Documented Vite feature but not tested against ASP.NET Core.
   Verify in Wave 1 smoke test.

---

## Sources

### Primary (HIGH confidence)

- npm registry (npm view) + npm downloads API -- 2026-06-02 [VERIFIED: npm registry]
- npm pack scichart@5.2.32 --dry-run -- confirmed WASM filenames [VERIFIED: npm registry]
- scichart.com/licensing-scichart-js -- setRuntimeLicenseKey() API [CITED]
- github.com/ABTSoftware/scichart-react README -- SciChartReact props, WASM paths, loadWasmFromCDN() [CITED]
- learn.microsoft.com/aspnet/core/fundamentals/websockets -- UseWebSockets, AcceptWebSocketAsync [CITED]
- reactrouter.com/start/declarative/installation + routing -- BrowserRouter, Routes, Route, Outlet [CITED]
- reactrouter.com/upgrading/v6 -- v7 import path changes [CITED]
- scichart.com/documentation/js/v5/2d-charts/.../realtime-updates/ -- fifoCapacity, FIFO modes [CITED]
- Codebase inspection of Helix.Orchestration.SiteServer/ -- endpoints confirmed, API gaps confirmed [VERIFIED: codebase]
- helix-desktop toolchain files -- versions to mirror [VERIFIED: codebase]

### Secondary (MEDIUM confidence)

- scichart.com tutorials-react tutorial-01 -- Webpack tutorial; WASM approach adapted to Vite
- github.com/ABTSoftware/SciChart.JS.Examples Tutorial_02 -- initChart pattern confirmed

### Tertiary (LOW confidence -- [ASSUMED])

- Vite proxy WebSocket upgrade handling -- documented, not tested in this setup
- PostgresExperimentRepository table/column names -- inferred from existing methods

---

## Metadata

Confidence breakdown:
- Standard Stack: HIGH -- all packages verified on npm registry
- SciChart WASM/Vite: MEDIUM -- WASM filenames confirmed; public/ approach from scichart-react README
- Architecture / API gaps: HIGH -- confirmed by direct codebase inspection
- WebSocket relay: HIGH -- cited from official ASP.NET Core docs
- React Router v7: HIGH -- cited from official reactrouter.com docs

Research date: 2026-06-02
Valid until: 2026-09-01
