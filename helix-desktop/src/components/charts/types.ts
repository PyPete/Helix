// Chart wrapper interface types — implementation-agnostic.
// Consumers import from charts/index.ts, never from SciChart directly.

export interface TrendSample {
  timestamp: number; // Unix ms
  value: number;
}

export interface SpectralPoint {
  wavelength: number;
  absorbance: number;
}

// ScalarChartProps matches the existing ScalarTrendChart props shape
export interface ScalarChartProps {
  trendName: string;
  data: Array<{ t: number; v: number }>;
  unit?: string;
}

// SpectralChartProps matches the existing SpectralChart props shape
export interface SpectralChartProps {
  uvData: SpectralPoint[];
  mirData: SpectralPoint[];
}

export interface PlaceholderChartProps {
  trendName?: string;
  "data-testid"?: string;
}
