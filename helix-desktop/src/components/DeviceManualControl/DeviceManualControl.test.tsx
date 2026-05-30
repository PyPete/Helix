import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import { describe, it, expect, vi, afterEach } from "vitest";
import DeviceManualControl from "./DeviceManualControl";
import type { CapabilityDescriptor } from "../../types/agent";

const { mockFetch } = vi.hoisted(() => ({ mockFetch: vi.fn() }));
vi.stubGlobal("fetch", mockFetch);

afterEach(() => {
  vi.clearAllMocks();
  vi.restoreAllMocks();
});

const numericCapability: CapabilityDescriptor = {
  verbName: "SetTemperature",
  displayName: "Set Temperature",
  description: "Set the target temperature",
  category: "Control",
  parameters: [
    {
      name: "targetTemp",
      displayName: "Target Temperature",
      type: "Numeric",
      unit: "°C",
      min: 0,
      max: 100,
      step: 0.5,
    },
  ],
  isDestructive: false,
  requiresActiveRunspace: false,
};

const destructiveCapability: CapabilityDescriptor = {
  verbName: "EmergencyStop",
  displayName: "Emergency Stop",
  description: "Stop all device operations immediately",
  category: "Control",
  parameters: [],
  isDestructive: true,
  requiresActiveRunspace: false,
};

describe("DeviceManualControl", () => {
  it("renders Loading capabilities... during fetch", () => {
    // fetch never resolves — loading state stays visible
    mockFetch.mockReturnValue(new Promise(() => { /* intentionally never resolves */ }));

    render(<DeviceManualControl deviceId="device-1" baseUrl="http://localhost:5080" />);
    expect(screen.getByText("Loading capabilities...")).toBeInTheDocument();
  });

  it("renders numeric input widget for a ParameterType Numeric capability", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () =>
        Promise.resolve({
          adapterId: "device-1",
          capabilities: [numericCapability],
        }),
    } as unknown as Response);

    render(<DeviceManualControl deviceId="device-1" baseUrl="http://localhost:5080" />);

    await waitFor(() => {
      expect(screen.getByText("Set Temperature")).toBeInTheDocument();
    });

    const input = screen.getByRole("spinbutton");
    expect(input).toBeInTheDocument();
    expect(input).toHaveAttribute("type", "number");
    expect(input).toHaveAttribute("min", "0");
    expect(input).toHaveAttribute("max", "100");
    expect(screen.getByText("°C")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Execute" })).toBeInTheDocument();
  });

  it("clicking Execute calls POST /devices/{id}/execute with correct body", async () => {
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        json: () =>
          Promise.resolve({
            adapterId: "device-1",
            capabilities: [numericCapability],
          }),
      } as unknown as Response)
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({ success: true }),
      } as unknown as Response);

    render(<DeviceManualControl deviceId="device-1" baseUrl="http://localhost:5080" />);

    await waitFor(() => {
      expect(screen.getByRole("spinbutton")).toBeInTheDocument();
    });

    const input = screen.getByRole("spinbutton");
    fireEvent.change(input, { target: { value: "37" } });

    const executeBtn = screen.getByRole("button", { name: "Execute" });
    fireEvent.click(executeBtn);

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledWith(
        "http://localhost:5080/devices/device-1/execute",
        expect.objectContaining({
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            capability: "SetTemperature",
            parameters: { targetTemp: "37" },
          }),
        })
      );
    });
  });

  it("confirm dialog appears for isDestructive=true capability before POST is called", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () =>
        Promise.resolve({
          adapterId: "device-1",
          capabilities: [destructiveCapability],
        }),
    } as unknown as Response);

    const confirmSpy = vi.spyOn(window, "confirm").mockReturnValue(false);

    render(<DeviceManualControl deviceId="device-1" baseUrl="http://localhost:5080" />);

    await waitFor(() => {
      expect(screen.getByText("Emergency Stop")).toBeInTheDocument();
    });

    const executeBtn = screen.getByRole("button", { name: "Execute" });
    fireEvent.click(executeBtn);

    expect(confirmSpy).toHaveBeenCalledWith("Confirm: execute Emergency Stop?");
    // POST should NOT be called because confirm returned false
    expect(mockFetch).toHaveBeenCalledTimes(1); // only the GET capabilities call
  });
});
