import { render, screen } from "@testing-library/react";
import { vi, describe, it, afterEach, expect } from "vitest";
import { ScalarTrendChart } from "./ScalarTrendChart";

const { mockLineChart } = vi.hoisted(() => ({
  mockLineChart: vi.fn(() => <div data-testid="mock-chart" />),
}));

vi.mock("recharts", () => ({
  LineChart: mockLineChart,
  Line: vi.fn(() => null),
  XAxis: vi.fn(() => null),
  YAxis: vi.fn(() => null),
  CartesianGrid: vi.fn(() => null),
  Tooltip: vi.fn(() => null),
  ResponsiveContainer: vi.fn(({ children }: { children: React.ReactNode }) => <>{children}</>),
}));

afterEach(() => {
  vi.restoreAllMocks();
});

describe("ScalarTrendChart", () => {
  it("renders container with data-testid scalar-trend-chart", () => {
    render(
      <ScalarTrendChart
        trendName="Temperature"
        data={[{ t: Date.now(), v: 22.5 }]}
      />
    );
    expect(screen.getByTestId("scalar-trend-chart")).toBeInTheDocument();
  });

  it("renders with empty data prop without throwing", () => {
    expect(() =>
      render(<ScalarTrendChart trendName="Temperature" data={[]} />)
    ).not.toThrow();
  });
});
