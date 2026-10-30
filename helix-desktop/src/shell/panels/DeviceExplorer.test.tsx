import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import { describe, it, expect, vi, afterEach } from "vitest";
import { DeviceExplorer } from "./DeviceExplorer";

afterEach(() => {
  vi.restoreAllMocks();
});

describe("DeviceExplorer", () => {
  it("renders loading state when fetch is pending and device list is empty", () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(() => new Promise(() => { /* never resolves */ }))
    );

    render(
      <DeviceExplorer baseUrl="http://localhost:5080" onOpenDevice={vi.fn()} />
    );

    expect(screen.getByText("Loading...")).toBeInTheDocument();
  });

  it("renders device list with adapterId labels after successful fetch", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => [
          { adapterId: "device-1", capabilities: ["Control"] },
          { adapterId: "device-2", capabilities: ["Measurement"] },
        ],
      })
    );

    render(
      <DeviceExplorer baseUrl="http://localhost:5080" onOpenDevice={vi.fn()} />
    );

    await waitFor(() => {
      expect(screen.getByText("device-1")).toBeInTheDocument();
    });
    expect(screen.getByText("device-2")).toBeInTheDocument();
  });

  it("clicking a device button calls onOpenDevice(adapterId)", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => [
          { adapterId: "device-1", capabilities: ["Control"] },
        ],
      })
    );

    const onOpenDevice = vi.fn();
    render(
      <DeviceExplorer baseUrl="http://localhost:5080" onOpenDevice={onOpenDevice} />
    );

    await waitFor(() => {
      expect(screen.getByTestId("open-device-device-1")).toBeInTheDocument();
    });

    fireEvent.click(screen.getByTestId("open-device-device-1"));
    expect(onOpenDevice).toHaveBeenCalledWith("device-1");
  });

  it("renders error message (role=alert) when fetch fails", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: false,
        status: 503,
      })
    );

    render(
      <DeviceExplorer baseUrl="http://localhost:5080" onOpenDevice={vi.fn()} />
    );

    await waitFor(() => {
      expect(screen.getByRole("alert")).toBeInTheDocument();
    });
    expect(screen.getByRole("alert")).toHaveTextContent("Agent returned 503");
  });

  it("does NOT render DeviceManualControl inline — only fires onOpenDevice on click", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => [
          { adapterId: "device-1", capabilities: ["Control"] },
        ],
      })
    );

    const onOpenDevice = vi.fn();
    render(
      <DeviceExplorer baseUrl="http://localhost:5080" onOpenDevice={onOpenDevice} />
    );

    await waitFor(() => {
      expect(screen.getByTestId("open-device-device-1")).toBeInTheDocument();
    });

    // No device-manual-control element should be rendered
    expect(screen.queryByTestId("device-manual-control")).not.toBeInTheDocument();

    // Click fires callback
    fireEvent.click(screen.getByTestId("open-device-device-1"));
    expect(onOpenDevice).toHaveBeenCalledWith("device-1");
    expect(onOpenDevice).toHaveBeenCalledTimes(1);
  });
});
