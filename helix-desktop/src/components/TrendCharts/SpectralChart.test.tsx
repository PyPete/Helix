import { render, screen } from "@testing-library/react";
import { vi, describe, it, afterEach, expect } from "vitest";
import { SpectralChart } from "./SpectralChart";

const { mockAreaChart } = vi.hoisted(() => ({
  mockAreaChart: vi.fn(() => <div data-testid="mock-chart" />),
}));

vi.mock("recharts", () => ({
  AreaChart: mockAreaChart,
  Area: vi.fn(() => null),
  XAxis: vi.fn(() => null),
  YAxis: vi.fn(() => null),
  CartesianGrid: vi.fn(() => null),
  Tooltip: vi.fn(() => null),
  ResponsiveContainer: vi.fn(({ children }: { children: React.ReactNode }) => <>{children}</>),
  Legend: vi.fn(() => null),
}));

afterEach(() => {
  vi.restoreAllMocks();
});

describe("SpectralChart", () => {
  it("renders container with data-testid spectral-chart", () => {
    render(
      <SpectralChart
        uvData={[{ wavelength: 300, absorbance: 0.5 }]}
        mirData={[]}
      />
    );
    expect(screen.getByTestId("spectral-chart")).toBeInTheDocument();
  });

  it("renders with empty uvData and mirData without throwing", () => {
    expect(() =>
      render(<SpectralChart uvData={[]} mirData={[]} />)
    ).not.toThrow();
  });
});
