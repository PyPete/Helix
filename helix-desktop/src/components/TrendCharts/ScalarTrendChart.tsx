import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from "recharts";

interface ScalarTrendChartProps {
  trendName: string;
  data: Array<{ t: number; v: number }>;
  unit?: string;
}

export function ScalarTrendChart({ trendName, data, unit }: ScalarTrendChartProps) {
  return (
    <section aria-label={`Trend: ${trendName}`} data-testid="scalar-trend-chart">
      <h3>{trendName}</h3>
      <ResponsiveContainer width="100%" height={200}>
        <LineChart data={data}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="t" tickFormatter={(v) => new Date(v).toLocaleTimeString()} />
          <YAxis unit={unit} />
          <Tooltip />
          <Line
            type="monotone"
            dataKey="v"
            name={trendName}
            dot={false}
            isAnimationActive={false}
            stroke="#2563eb"
          />
        </LineChart>
      </ResponsiveContainer>
    </section>
  );
}
