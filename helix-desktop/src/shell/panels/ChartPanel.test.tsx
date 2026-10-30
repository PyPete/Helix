import { render, screen, fireEvent } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import { ChartPanel } from "./ChartPanel";

vi.mock("../../components/TrendCharts/ScalarTrendChart", () => ({
  ScalarTrendChart: ({ trendName }: { trendName: string }) => (
    <div data-testid={`chart-${trendName}`}>{trendName}</div>
  ),
}));

describe("ChartPanel", () => {
  it("renders drop zone with data-testid='chart-panel'", () => {
    render(<ChartPanel trendValues={[]} />);
    expect(screen.getByTestId("chart-panel")).toBeInTheDocument();
  });

  it("onDrop with dataTransfer 'text/x-trend-name'='temperature' adds 'temperature' to tracked trend list", () => {
    render(<ChartPanel trendValues={[]} />);

    const panel = screen.getByTestId("chart-panel");

    const getData = vi.fn().mockReturnValue("temperature");
    const dropEvent = new MouseEvent("drop", { bubbles: true });
    Object.defineProperty(dropEvent, "dataTransfer", {
      value: { getData },
      writable: false,
    });

    fireEvent(panel, dropEvent);

    expect(getData).toHaveBeenCalledWith("text/x-trend-name");
    expect(screen.getByTestId("chart-temperature")).toBeInTheDocument();
  });

  it("when trendNames includes 'temperature', renders a ScalarTrendChart with trendName='temperature'", () => {
    render(<ChartPanel initialTrendName="temperature" trendValues={[]} />);

    expect(screen.getByTestId("chart-temperature")).toBeInTheDocument();
    expect(screen.getByTestId("chart-temperature")).toHaveTextContent("temperature");
  });

  it("isDragOver visual state toggles on dragover/dragleave", () => {
    render(<ChartPanel trendValues={[]} />);

    const panel = screen.getByTestId("chart-panel");

    expect(panel).not.toHaveClass("drag-over");

    fireEvent.dragOver(panel);
    expect(panel).toHaveClass("drag-over");

    fireEvent.dragLeave(panel);
    expect(panel).not.toHaveClass("drag-over");
  });
});
