// charts/index.ts — SciChart wrapper router (Step 1: placeholder mode)
// When VITE_SCICHART_ENABLED=true and VITE_SCICHART_LICENSE_KEY is set,
// this router will export SciChart implementations (Steps 2–5, future phases).
// In Step 1, all consumers receive the existing recharts implementations unchanged.

export { ScalarTrendChart } from "../TrendCharts/ScalarTrendChart";
export { SpectralChart } from "../TrendCharts/SpectralChart";
export { PlaceholderChart } from "./PlaceholderChart";
export type { ScalarChartProps, SpectralChartProps } from "./types";
