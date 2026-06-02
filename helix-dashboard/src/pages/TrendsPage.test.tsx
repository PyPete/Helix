import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { TrendsPage } from "./TrendsPage";
import type { TrendValue } from "../types/dashboard";

const MOCK_TRENDS: TrendValue[] = [
  { type: "trend-value", trendName: "Temperature",
    value: 25.3, unit: "C", timestamp: "2026-06-02T00:00:00Z" },
  { type: "trend-value", trendName: "Pressure",
    value: 1.02, unit: "bar", timestamp: "2026-06-02T00:00:01Z" },
  { type: "trend-value", trendName: "Temperature",
    value: 25.5, unit: "C", timestamp: "2026-06-02T00:00:02Z" },
];

describe("TrendsPage", () => {
  it("renders with correct testid", () => {
    render(<TrendsPage trendValues={[]} />);
    expect(screen.getByTestId("trends-page")).toBeInTheDocument();
  });

  it("shows empty state when no trend values", () => {
    render(<TrendsPage trendValues={[]} />);
    expect(screen.getByTestId("trends-empty")).toBeInTheDocument();
  });

  it("renders one chart per unique trendName", () => {
    render(<TrendsPage trendValues={MOCK_TRENDS} />);
    expect(screen.getAllByTestId("scichart-line")).toHaveLength(2);
  });

  it("renders Temperature heading", () => {
    render(<TrendsPage trendValues={MOCK_TRENDS} />);
    expect(screen.getByText("Temperature")).toBeInTheDocument();
  });

  it("does not render empty state when trends present", () => {
    render(<TrendsPage trendValues={MOCK_TRENDS} />);
    expect(screen.queryByTestId("trends-empty")).not.toBeInTheDocument();
  });
});
