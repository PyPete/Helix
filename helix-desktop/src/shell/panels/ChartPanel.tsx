import { useState } from 'react';
import type { TrendValue } from '../../types/agent';
import { ScalarTrendChart } from '../../components/TrendCharts/ScalarTrendChart';
import './panels.css';

export interface ChartPanelProps {
  initialTrendName?: string;
  trendValues: TrendValue[];
}

export function ChartPanel({ initialTrendName, trendValues }: ChartPanelProps) {
  const [trackedTrends, setTrackedTrends] = useState<string[]>(
    initialTrendName ? [initialTrendName] : []
  );
  const [isDragOver, setIsDragOver] = useState(false);

  function handleDragOver(e: React.DragEvent<HTMLDivElement>) {
    e.preventDefault();
    try {
      e.dataTransfer.dropEffect = 'copy';
    } catch {
      // jsdom does not support setting dropEffect — safe to ignore in tests
    }
    setIsDragOver(true);
  }

  function handleDragLeave() {
    setIsDragOver(false);
  }

  function handleDrop(e: React.DragEvent<HTMLDivElement>) {
    e.preventDefault();
    setIsDragOver(false);
    const name = e.dataTransfer.getData('text/x-trend-name');
    if (name && !trackedTrends.includes(name)) {
      setTrackedTrends(prev => [...prev, name]);
    }
  }

  // Derive chart data for the first tracked trend
  const firstTrend = trackedTrends[0];
  const chartData = firstTrend
    ? trendValues
        .filter(t => t.trendName === firstTrend)
        .map(t => ({ t: new Date(t.timestamp).getTime(), v: t.value }))
    : [];

  const unit = firstTrend
    ? (trendValues.filter(t => t.trendName === firstTrend).pop()?.unit ?? '')
    : '';

  return (
    <div
      data-testid="chart-panel"
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
      className={isDragOver ? 'chart-panel drag-over' : 'chart-panel'}
    >
      {trackedTrends.length === 0 ? (
        <p className="panel-empty">Drop a trend here to chart it</p>
      ) : (
        <ScalarTrendChart
          trendName={firstTrend}
          data={chartData}
          unit={unit}
        />
      )}
    </div>
  );
}
