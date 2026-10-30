import { SciChartLine } from "../components/SciChartLine";
import type { TrendValue } from "../types/dashboard";

function makeSpectrum(n = 50): TrendValue[] {
  return Array.from({ length: n }, (_, i) => ({
    type: "trend-value" as const,
    trendName: "UV Spectrum",
    value: Math.sin(i / 5) * 0.8 + 1 + Math.random() * 0.1,
    unit: "AU",
    timestamp: new Date(Date.now() - (n - i) * 1000).toISOString(),
  }));
}

function makeDAD(n = 50): TrendValue[] {
  return Array.from({ length: n }, (_, i) => ({
    type: "trend-value" as const,
    trendName: "DAD 254nm",
    value: Math.exp(-((i - 25) ** 2) / 50) * 1200,
    unit: "mAU",
    timestamp: new Date(Date.now() - (n - i) * 1000).toISOString(),
  }));
}

const SPECTRUM_DATA = makeSpectrum();
const DAD_DATA = makeDAD();

export function AnalyticsPage() {
  return (
    <section aria-label="Analytical Data Viewer" data-testid="analytics-page">
      <h1 style={{ color: "var(--color-text)", fontSize: "var(--text-xl)" }}>
        Analytical Data
      </h1>
      <p style={{ color: "var(--color-text-muted)", fontSize: "var(--text-sm)",
        marginBottom: "var(--space-4)" }}>
        Placeholder charts — live analytical data requires a later phase.
      </p>
      <SciChartLine trendName="UV Spectrum (placeholder)" trendValues={SPECTRUM_DATA} />
      <SciChartLine trendName="DAD Chromatogram 254nm (placeholder)" trendValues={DAD_DATA} />
    </section>
  );
}
