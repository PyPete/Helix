interface PlaceholderProps {
  trendName?: string;
}

export function PlaceholderChart({ trendName }: PlaceholderProps) {
  return (
    <div data-testid="chart-unavailable" className="chart-placeholder">
      {trendName ? `${trendName} — ` : ""}chart unavailable (SciChart license required)
    </div>
  );
}
