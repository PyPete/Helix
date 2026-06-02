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
