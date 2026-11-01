import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import { describe, it, expect, vi, afterEach } from "vitest";
import { DevicePalette } from "./DevicePalette";
import type { CapabilityDescriptor } from "../../types/agent";

const { getAgentDevices, getAgentDeviceCapabilities } = vi.hoisted(() => ({
  getAgentDevices: vi.fn(),
  getAgentDeviceCapabilities: vi.fn(),
}));

vi.mock("../../lib/agentClient", () => ({ getAgentDevices, getAgentDeviceCapabilities }));

const setFlowDescriptor: CapabilityDescriptor = {
  verbName: "SetFlow",
  displayName: "Set Flow",
  description: "Sets the flow rate",
  category: "Control",
  parameters: [
    {
      name: "flowRate",
      displayName: "Flow Rate",
      type: "Numeric",
      unit: "mL/min",
      min: 0,
      max: 500,
      step: 0.1,
    },
  ],
  isDestructive: false,
  requiresActiveRunspace: false,
};

function arrangeDiscovery() {
  getAgentDevices.mockResolvedValue([{ adapterId: "simulator", capabilities: ["SetFlow"] }]);
  getAgentDeviceCapabilities.mockResolvedValue({ adapterId: "simulator", capabilities: [setFlowDescriptor] });
}

afterEach(() => {
  vi.restoreAllMocks();
  vi.clearAllMocks();
  getAgentDevices.mockReset();
  getAgentDeviceCapabilities.mockReset();
});

describe("DevicePalette", () => {
  it("renders palette-group-Alicat details element after fetch resolves", async () => {
    arrangeDiscovery();
    render(<DevicePalette baseUrl="http://localhost:5080" onDragStart={vi.fn()} />);

    await waitFor(() =>
      expect(screen.getByTestId("palette-group-simulator")).toBeInTheDocument()
    );
  });

  it("capability-card element is draggable", async () => {
    arrangeDiscovery();
    render(<DevicePalette baseUrl="http://localhost:5080" onDragStart={vi.fn()} />);

    await waitFor(() =>
      expect(screen.getByTestId("capability-card-simulator-SetFlow")).toBeInTheDocument()
    );

    const card = screen.getByTestId("capability-card-simulator-SetFlow");
    expect(card).toHaveStyle({ cursor: "grab" });
  });

  it("pointer down on a capability card starts an adapter-scoped drag", async () => {
    arrangeDiscovery();
    const onDragStart = vi.fn();
    render(<DevicePalette baseUrl="http://localhost:5080" onDragStart={onDragStart} />);

    await waitFor(() =>
      expect(screen.getByTestId("capability-card-simulator-SetFlow")).toBeInTheDocument()
    );

    const card = screen.getByTestId("capability-card-simulator-SetFlow");
    fireEvent.pointerDown(card);

    expect(onDragStart).toHaveBeenCalledWith("simulator", "SetFlow", setFlowDescriptor);
  });

  it("shows Failed to load devices message when fetch errors", async () => {
    getAgentDevices.mockRejectedValue(new Error("Network error"));
    render(<DevicePalette baseUrl="http://localhost:5080" onDragStart={vi.fn()} />);

    await waitFor(() =>
      expect(screen.getByText("Network error")).toBeInTheDocument()
    );
  });
});
