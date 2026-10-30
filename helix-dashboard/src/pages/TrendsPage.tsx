import type { TrendValue } from "../types/dashboard";
import { SciChartLine } from "../components/SciChartLine";

interface TrendsPageProps { trendValues: TrendValue[]; }

export function TrendsPage({ trendValues }: TrendsPageProps) {
  const byName = trendValues.reduce<Record<string, TrendValue[]>>((acc, v) => {
    (acc[v.trendName] ??= []).push(v);
    return acc;
  }, {});
  const names = Object.keys(byName).sort();

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
