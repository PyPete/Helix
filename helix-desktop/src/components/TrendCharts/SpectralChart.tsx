import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend,
} from "recharts";

interface SpectralChartProps {
  uvData: Array<{ wavelength: number; absorbance: number }>;
  mirData: Array<{ wavelength: number; absorbance: number }>;
}

export function SpectralChart({ uvData, mirData }: SpectralChartProps) {
  // Merge UV and MIR datasets into a single array indexed by wavelength
  const wavelengthSet = new Set([
    ...uvData.map((d) => d.wavelength),
    ...mirData.map((d) => d.wavelength),
  ]);
  const uvMap = new Map(uvData.map((d) => [d.wavelength, d.absorbance]));
  const mirMap = new Map(mirData.map((d) => [d.wavelength, d.absorbance]));

  const mergedData = Array.from(wavelengthSet)
    .sort((a, b) => a - b)
    .map((wavelength) => ({
      wavelength,
      uv: uvMap.get(wavelength),
      mir: mirMap.get(wavelength),
    }));

  return (
    <section aria-label="Spectral Chart" data-testid="spectral-chart">
      <h3>Spectral Chart</h3>
      <ResponsiveContainer width="100%" height={200}>
        <AreaChart data={mergedData}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="wavelength" label={{ value: "nm", position: "insideRight" }} />
          <YAxis label={{ value: "Abs", angle: -90, position: "insideLeft" }} />
          <Tooltip />
          <Legend />
          <Area
            type="monotone"
            dataKey="uv"
            name="UV"
            fill="#3b82f6"
            stroke="#1d4ed8"
            isAnimationActive={false}
          />
          <Area
            type="monotone"
            dataKey="mir"
            name="MIR"
            fill="#10b981"
            stroke="#065f46"
            isAnimationActive={false}
          />
        </AreaChart>
      </ResponsiveContainer>
    </section>
  );
}
