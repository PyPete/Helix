import { render, screen, waitFor } from "@testing-library/react";
import { describe, it, expect, vi, afterEach } from "vitest";
import DeviceRegistry from "./DeviceRegistry";

const { getAgentDevices } = vi.hoisted(() => ({
  getAgentDevices: vi.fn(),
}));

vi.mock("../lib/agentClient", () => ({ getAgentDevices }));

afterEach(() => {
  vi.restoreAllMocks();
  getAgentDevices.mockReset();
});

describe("DeviceRegistry", () => {
  it("shows Loading... on initial render before fetch resolves", () => {
    // fetch never resolves — loading state stays visible
    getAgentDevices.mockReturnValue(new Promise(() => { /* intentionally never resolves */ }));

    render(<DeviceRegistry agentBaseUrl="http://localhost:5080" />);
    expect(screen.getByText("Loading device discovery...")).toBeInTheDocument();
  });

  it("renders adapter list when fetch returns devices array", async () => {
    getAgentDevices.mockResolvedValue([{ adapterId: "simulator", capabilities: ["simulator.SetFlow"] }]);

    render(<DeviceRegistry agentBaseUrl="http://localhost:5080" />);

    await waitFor(() => {
      expect(screen.getByText(/simulator/i)).toBeInTheDocument();
    });
    expect(screen.getByText("Manual Control")).toBeInTheDocument();
  });

  it("shows error message when fetch returns non-200", async () => {
    getAgentDevices.mockRejectedValue(new Error("Agent returned 503"));

    render(<DeviceRegistry agentBaseUrl="http://localhost:5080" />);

    await waitFor(() => {
      expect(screen.getByRole("alert")).toBeInTheDocument();
    });
    expect(screen.getByRole("alert")).toHaveTextContent("Agent returned 503");
  });

  it("shows error message when fetch throws (network error)", async () => {
    getAgentDevices.mockRejectedValue(new Error("Failed to connect"));

    render(<DeviceRegistry agentBaseUrl="http://localhost:5080" />);

    await waitFor(() => {
      expect(screen.getByRole("alert")).toBeInTheDocument();
    });
    expect(screen.getByRole("alert")).toHaveTextContent("Failed to connect");
  });

  it("shows No adapters found when fetch returns empty array", async () => {
    getAgentDevices.mockResolvedValue([]);

    render(<DeviceRegistry agentBaseUrl="http://localhost:5080" />);

    await waitFor(() => {
      expect(screen.getByText(/No simulator adapters found/i)).toBeInTheDocument();
    });
  });
});
