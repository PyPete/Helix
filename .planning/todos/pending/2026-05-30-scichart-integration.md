---
created: 2026-05-30T19:35:00Z
title: SciChart Integration — High-Performance Real-Time Charting
area: ui
files:
  - helix-desktop/src/components/TrendCharts/ScalarTrendChart.tsx
  - helix-desktop/src/components/TrendCharts/SpectralChart.tsx
  - helix-desktop/src/hooks/useAgentWebSocket.ts
  - helix-desktop/package.json
  - helix-desktop/src/App.tsx
  - .github/workflows/ci.yml
---

## Problem

The current charting stack (recharts, Phase 6) is adequate for low-frequency scalar trends but is not designed for the system's actual operational requirements:
- Real-time experiment monitoring at 100–1000 Hz update rates causes recharts to drop frames
- Replay scrubbing over large datasets (millions of data points for long experiments) is unusable
- Spectral charts (UV/MIR overlay) require multi-axis, multi-series rendering with sub-millisecond update latency
- recharts has no GPU acceleration; all rendering is SVG/DOM — CPU-bound and unsuitable for scientific visualisation at scale
- No consistent charting layer across Tauri desktop and the Site Server React app

## Solution

Replace recharts with **SciChart** as the unified charting backend. SciChart provides GPU-accelerated WebGL/WebAssembly rendering in the browser and a WPF variant for any future native Windows components. The integration is designed to keep the repository fully open-source and license-clean.

---

### Why SciChart

| Capability | recharts (current) | SciChart |
|---|---|---|
| Rendering engine | SVG/DOM (CPU) | WebGL/WebAssembly (GPU) |
| Max update rate | ~10 Hz before frame drops | 1000+ Hz sustained |
| Data points per series | ~10K visible | 10M+ with LOD decimation |
| Spectral/scientific chart types | No | Yes (waterfall, heatmap, mountain, 2D spectra) |
| Multi-axis | Limited | Full (N X-axes, M Y-axes) |
| Replay scrubbing | Unusable at scale | Smooth pan/zoom over full dataset |
| Hit-testing / annotations | Limited | Full (cursors, tooltips, horizontal/vertical lines) |
| Cross-platform consistency | Browser only | Browser (JS) + WPF (.NET) |

---

### Open-Source Compliance Strategy

**SciChart is a commercial library. The repository must remain license-clean.**

Three rules:
1. **No binaries or WASM files committed.** SciChart.js WASM (`scichart2d.wasm`, `scichart2d.data`) are installed at build time via `npm install scichart` — never committed to git. Add to `.gitignore`.
2. **No license keys in source code or committed config.** License keys are injected at runtime via environment variables (`SCICHART_LICENSE_KEY`) or CI/CD secrets. Never hardcoded.
3. **Graceful degradation without a license.** The wrapper layer renders a `<div data-testid="chart-unavailable">` placeholder when no license key is present. Contributors without a license can build, run tests, and contribute — charts are disabled, not broken.

**.gitignore additions:**
```
# SciChart build artefacts — never commit
helix-desktop/public/scichart2d.wasm
helix-desktop/public/scichart2d.data
helix-site/public/scichart2d.wasm
helix-site/public/scichart2d.data
```

**CI/CD:** `SCICHART_LICENSE_KEY` injected as a GitHub Actions secret. Build step copies WASM files to `public/` after `npm install`. License key applied once via `SciChartSurface.setRuntimeLicenseKey(process.env.SCICHART_LICENSE_KEY)` at app initialisation.

**Contributor documentation (CONTRIBUTING.md addition):**
> SciChart charts require a commercial license key to render. If you do not have a key, set `SCICHART_ENABLED=false` in your `.env.local` — charts will display a placeholder and all tests will still pass. Contact the project maintainers for development license access.

---

### Wrapper Architecture

A thin abstraction layer prevents SciChart API from leaking into application components. Application code depends only on the wrapper interface; SciChart is an implementation detail.

**Wrapper interface (TypeScript):**
```typescript
// helix-desktop/src/components/charts/ChartProvider.tsx
export interface ScalarChartProps {
  trendName: string;
  data: TrendSample[];           // { timestamp: number, value: number }[]
  unit: string;
  realtimeSource?: Observable<TrendSample>;  // for live updates
  dataSource?: "live" | "replay";
  stale?: boolean;
}

export interface SpectralChartProps {
  uvSeries: SpectralPoint[];     // { wavelength: number, absorbance: number }[]
  mirSeries: SpectralPoint[];
  dataSource?: "live" | "replay";
}

export interface TimelineScrubberProps {
  durationMs: number;
  currentMs: number;
  onSeek: (ms: number) => void;
  annotations?: TimelineAnnotation[];   // step boundaries, fault markers
}
```

**SciChart implementation:**
```typescript
// helix-desktop/src/components/charts/SciScalarChart.tsx
// helix-desktop/src/components/charts/SciSpectralChart.tsx
// helix-desktop/src/components/charts/SciTimelineScrubber.tsx
```

**Fallback (no license):**
```typescript
// helix-desktop/src/components/charts/PlaceholderChart.tsx
export const PlaceholderChart = ({ trendName }: { trendName: string }) => (
  <div data-testid="chart-unavailable" className="chart-placeholder">
    {trendName} — chart unavailable (SciChart license required)
  </div>
);
```

**Runtime selection:**
```typescript
// helix-desktop/src/components/charts/index.ts
const SCICHART_ENABLED = import.meta.env.VITE_SCICHART_ENABLED !== "false"
  && Boolean(import.meta.env.VITE_SCICHART_LICENSE_KEY);

export const ScalarTrendChart = SCICHART_ENABLED ? SciScalarChart : PlaceholderChart;
export const SpectralChart = SCICHART_ENABLED ? SciSpectralChart : PlaceholderChart;
export const TimelineScrubber = SCICHART_ENABLED ? SciTimelineScrubber : PlaceholderScrubber;
```

Existing components (`ScalarTrendChart.tsx`, `SpectralChart.tsx`) import from `charts/index.ts` — no call sites change.

---

### Real-Time Update Architecture

SciChart's high-frequency update path differs from recharts' React re-render model. Updates must bypass React's reconciler to avoid unnecessary re-renders.

**Pattern: direct series append, no state update**
```typescript
// In SciScalarChart.tsx
useEffect(() => {
  const sub = realtimeSource?.subscribe(sample => {
    // Direct append to SciChart data series — no React setState
    dataSeries.current?.append(sample.timestamp, sample.value);
    // SciChart handles its own render loop via requestAnimationFrame
  });
  return () => sub?.unsubscribe();
}, [realtimeSource]);
```

**FIFO buffer for memory management:**
- Rolling window: keep last N data points in the series (configurable, default 10,000)
- Older points evicted automatically: `dataSeries.current?.removeRange(0, dataSeries.count - MAX_POINTS)`
- For replay: load full dataset in chunks using SciChart's `appendRange()` batch API

---

### Chart Types Enabled by SciChart

| Chart type | Use case | Phase |
|---|---|---|
| `FastLineRenderableSeries` | Real-time scalar trends (temperature, pressure, flow) | Replaces ScalarTrendChart |
| `XyScatterRenderableSeries` | Discrete event markers on timeline | Timeline scrubber annotations |
| `FastMountainRenderableSeries` | Spectral overlay (UV/MIR filled area) | Replaces SpectralChart |
| `SpectrogramSurface` (2D) | UV/Vis spectral waterfall over time | New: spectral replay |
| `HeatmapRenderableSeries` | Multi-run comparison heatmap | New: campaign overview |
| `FastColumnRenderableSeries` | Step duration bar chart | New: experiment summary |
| `PieDonutRenderableSeries` | Step outcome distribution | New: summary dashboard |
| `NumericAxis` / `DateTimeAxis` | X-axis: value or timestamp | Replay scrubber |
| `RolloverModifier` | Hover tooltip with crosshair | All charts |
| `ZoomPanModifier` | Pan/zoom on replay | All charts |
| `AnnotationBase` | Step boundary markers, fault flags | Timeline scrubber |

---

### Site Server Integration

SciChart.js is a browser library — the same npm package and wrapper architecture applies to the Site Server React app (`helix-site/`).

**Consistent styling:** A shared `SciChartTheme` object defines colours, line widths, axis styles, and background — applied identically in both Tauri and Site Server.

**Tauri-specific note:** SciChart.js runs inside the Tauri WebView (Chromium). WebGL is available and GPU-accelerated. No special Tauri configuration required.

---

### Testing Strategy

SciChart's WebGL canvas cannot be unit-tested in jsdom (no GPU context). The wrapper architecture handles this cleanly:

- **Unit tests (Vitest/jsdom):** `VITE_SCICHART_ENABLED=false` → tests run against `PlaceholderChart`. Test chart props, data binding, stale state, and legend rendering without SciChart.
- **Integration tests (Playwright/Tauri):** Full SciChart rendering in a real Chromium context. Validate that series append correctly and that high-frequency updates don't drop frames.
- **Smoke tests:** `data-testid="chart-unavailable"` present when licence absent → test passes.

**CI:** unit tests run without `VITE_SCICHART_LICENSE_KEY` → all pass. Integration tests run with the secret injected.

---

### Migration Plan (recharts → SciChart)

**Step 1 — Wrapper layer (no behaviour change):**
- Create `src/components/charts/` directory with interfaces, placeholder, and `index.ts` router
- Existing `ScalarTrendChart.tsx` and `SpectralChart.tsx` become pass-throughs to `charts/index.ts`
- All tests still pass (placeholder mode)

**Step 2 — SciChart scalar implementation:**
- `SciScalarChart.tsx`: replaces recharts `LineChart` with `SciChartSurface` + `FastLineRenderableSeries`
- FIFO buffer, real-time `realtimeSource` subscription, stale state visual
- Update vitest tests to assert `data-testid="chart-unavailable"` in placeholder mode

**Step 3 — SciChart spectral implementation:**
- `SciSpectralChart.tsx`: UV/MIR `FastMountainRenderableSeries` on shared X-axis
- Replace hardcoded UV:/MIR: prefix filtering in App.tsx with typed `SpectralChartProps`

**Step 4 — Timeline scrubber:**
- `SciTimelineScrubber.tsx` with `ZoomPanModifier`, `AnnotationBase` for step boundaries
- Connected to replay session (Record/Replay system)

**Step 5 — Site Server:**
- Same wrapper + SciChart implementation in `helix-site/`
- Shared `SciChartTheme` package or copy

---

## Implementation hints

**Phase dependency:** this migration should occur before or during Phase 7 (Site Server) — before high-frequency charting requirements become blocking. Step 1 (wrapper layer) can land in Phase 6.1 with zero visible change; Steps 2–5 complete in a dedicated charting phase.

**License procurement:** SciChart offers OEM licensing for embedded commercial use, and a free community license for open-source projects. Confirm the correct license tier with SciChart before implementation.

**Performance target:**
- 100 Hz update rate: smooth rendering on a mid-range GPU (target: no dropped frames at 100 Hz, 4 active series)
- 1000 Hz burst: acceptable with frame skipping (render at display refresh rate, not sample rate)
- Replay at 10× speed: full dataset pan/zoom without lag

**Cross-links:**
- Real-time trends: `2026-05-30-real-time-trend-system.md` (SciChart is the rendering layer for all trend engine outputs)
- Record/Replay: `2026-05-30-record-replay-system.md` (timeline scrubber requires SciChart for performant replay)
- AI summarisation: `2026-05-30-ai-experiment-summarisation.md` (summary charts — step duration bars, trend overlays — use SciChart)
- Multi-mode authoring: `2026-05-30-multi-mode-workflow-authoring.md` (trend charts in all three UI modes use the same SciChart wrapper)
