import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import { describe, it, expect, vi, afterEach } from "vitest";
import { DeviceExplorer } from "./DeviceExplorer";

const { getAgentDevices } = vi.hoisted(() => ({
  getAgentDevices: vi.fn(),
}));

vi.mock("../../lib/agentClient", () => ({ getAgentDevices }));

afterEach(() => {
  vi.restoreAllMocks();
  getAgentDevices.mockReset();
});

describe("DeviceExplorer", () => {
  it("renders loading state when fetch is pending and device list is empty", () => {
    getAgentDevices.mockReturnValue(new Promise(() => { /* intentionally never resolves */ }));

    render(
      <DeviceExplorer baseUrl="http://localhost:5080" onOpenDevice={vi.fn()} />
    );

    expect(screen.getByText("Loading device discovery...")).toBeInTheDocument();
  });

  it("renders device list with adapterId labels after successful fetch", async () => {
    getAgentDevices.mockResolvedValue([{ adapterId: "simulator", capabilities: ["Control"] }]);

    render(
      <DeviceExplorer baseUrl="http://localhost:5080" onOpenDevice={vi.fn()} />
    );

    await waitFor(() => {
      expect(screen.getByText("simulator")).toBeInTheDocument();
    });
    expect(screen.queryByText("device-2")).not.toBeInTheDocument();
  });

  it("clicking a device button calls onOpenDevice(adapterId)", async () => {
    getAgentDevices.mockResolvedValue([{ adapterId: "simulator", capabilities: ["Control"] }]);

    const onOpenDevice = vi.fn();
    render(
      <DeviceExplorer baseUrl="http://localhost:5080" onOpenDevice={onOpenDevice} />
    );

    await waitFor(() => {
      expect(screen.getByTestId("open-device-simulator")).toBeInTheDocument();
    });

    fireEvent.click(screen.getByTestId("open-device-simulator"));
    expect(onOpenDevice).toHaveBeenCalledWith("simulator");
  });

  it("renders error message (role=alert) when fetch fails", async () => {
    getAgentDevices.mockRejectedValue(new Error("Agent returned 503"));

    render(
      <DeviceExplorer baseUrl="http://localhost:5080" onOpenDevice={vi.fn()} />
    );

    await waitFor(() => {
      expect(screen.getByRole("alert")).toBeInTheDocument();
    });
    expect(screen.getByRole("alert")).toHaveTextContent("Agent returned 503");
  });

  it("does NOT render DeviceManualControl inline — only fires onOpenDevice on click", async () => {
    getAgentDevices.mockResolvedValue([{ adapterId: "simulator", capabilities: ["Control"] }]);

    const onOpenDevice = vi.fn();
    render(
      <DeviceExplorer baseUrl="http://localhost:5080" onOpenDevice={onOpenDevice} />
    );

    await waitFor(() => {
      expect(screen.getByTestId("open-device-simulator")).toBeInTheDocument();
    });

    // No device-manual-control element should be rendered
    expect(screen.queryByTestId("device-manual-control")).not.toBeInTheDocument();

    // Click fires callback
    fireEvent.click(screen.getByTestId("open-device-simulator"));
    expect(onOpenDevice).toHaveBeenCalledWith("simulator");
    expect(onOpenDevice).toHaveBeenCalledTimes(1);
  });
});
