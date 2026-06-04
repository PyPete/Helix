import { render, screen, fireEvent } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import { TrendExplorer } from "./TrendExplorer";

describe("TrendExplorer", () => {
  it("renders 'No trends yet' message when trendNames is empty", () => {
    render(<TrendExplorer trendNames={[]} />);
    expect(screen.getByText("No trends yet")).toBeInTheDocument();
  });

  it("renders each trend name as a draggable item (draggable=true attribute present)", () => {
    render(<TrendExplorer trendNames={["temperature", "pressure"]} />);

    const tempItem = screen.getByTestId("trend-item-temperature");
    const pressItem = screen.getByTestId("trend-item-pressure");

    expect(tempItem).toBeInTheDocument();
    expect(pressItem).toBeInTheDocument();
    expect(tempItem).toHaveAttribute("draggable", "true");
    expect(pressItem).toHaveAttribute("draggable", "true");
  });

  it("onDragStart sets dataTransfer data with key 'text/x-trend-name' equal to the trend name", () => {
    render(<TrendExplorer trendNames={["temperature"]} />);

    const item = screen.getByTestId("trend-item-temperature");

    const setData = vi.fn();
    const dragEvent = new MouseEvent("dragstart", { bubbles: true });
    Object.defineProperty(dragEvent, "dataTransfer", {
      value: {
        setData,
        effectAllowed: "",
      },
      writable: false,
    });

    fireEvent(item, dragEvent);

    expect(setData).toHaveBeenCalledWith("text/x-trend-name", "temperature");
  });
});
