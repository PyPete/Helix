import { useRef, useEffect, useCallback } from "react";
import { SciChartReact } from "scichart-react";
import {
  SciChartSurface, DateTimeNumericAxis, NumericAxis, FastLineRenderableSeries,
  XyDataSeries, SciChartJsNavyTheme,
} from "scichart";
import type { TrendValue } from "../types/dashboard";

interface SciChartLineProps {
  trendName?: string;
  trendValues: TrendValue[];
}

export function SciChartLine({ trendName = "Trend", trendValues }: SciChartLineProps) {
  const seriesRef = useRef<XyDataSeries | null>(null);
  const lastAppendedMs = useRef<number>(-Infinity);

  useEffect(() => {
    const s = seriesRef.current;
    if (!s || !trendValues.length) return;
    try {
      for (const v of trendValues) {
        const t = new Date(v.timestamp).getTime();
        if (!Number.isFinite(t) || t <= lastAppendedMs.current) continue;
        s.append(t, v.value);
        lastAppendedMs.current = t;
      }
    } catch { /* series disposed during StrictMode unmount — next initChart will reset */ }
  }, [trendValues]);

  const initChart = useCallback(async (rootElement: string | HTMLDivElement) => {
    const { sciChartSurface, wasmContext } = await SciChartSurface.create(
      rootElement as HTMLDivElement, { theme: new SciChartJsNavyTheme() }
    );
    sciChartSurface.xAxes.add(new DateTimeNumericAxis(wasmContext));
    sciChartSurface.yAxes.add(new NumericAxis(wasmContext));
    const dataSeries = new XyDataSeries(wasmContext, { fifoCapacity: 500 });
    seriesRef.current = dataSeries;
    lastAppendedMs.current = -Infinity; // reset so all buffered values are re-appended
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
