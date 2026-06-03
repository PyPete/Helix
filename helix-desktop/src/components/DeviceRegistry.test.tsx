import { render, screen, waitFor } from "@testing-library/react";
import { describe, it, expect, vi, afterEach } from "vitest";
import DeviceRegistry from "./DeviceRegistry";

afterEach(() => {
  vi.restoreAllMocks();
});

describe("DeviceRegistry", () => {
  it("shows Loading... on initial render before fetch resolves", () => {
    // fetch never resolves — loading state stays visible
    vi.stubGlobal(
      "fetch",
      vi.fn(() => new Promise(() => { /* intentionally never resolves */ }))
    );

    render(<DeviceRegistry agentBaseUrl="http://localhost:5080" />);
    expect(screen.getByText("Loading...")).toBeInTheDocument();
  });

  it("renders adapter list when fetch returns devices array", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(() =>
        Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve([{ adapterId: "simulator", capabilities: ["simulator.SetFlow"] }]),
        } as unknown as Response)
      )
    );

    render(<DeviceRegistry agentBaseUrl="http://localhost:5080" />);

    await waitFor(() => {
      expect(screen.getByText(/simulator/i)).toBeInTheDocument();
    });
    expect(screen.getByText("Manual Control")).toBeInTheDocument();
  });

  it("shows error message when fetch returns non-200", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(() =>
        Promise.resolve({
          ok: false,
          status: 503,
        } as unknown as Response)
      )
    );

    render(<DeviceRegistry agentBaseUrl="http://localhost:5080" />);

    await waitFor(() => {
      expect(screen.getByRole("alert")).toBeInTheDocument();
    });
    expect(screen.getByRole("alert")).toHaveTextContent("Agent returned 503");
  });

  it("shows error message when fetch throws (network error)", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(() => Promise.reject(new Error("Failed to connect")))
    );

    render(<DeviceRegistry agentBaseUrl="http://localhost:5080" />);

    await waitFor(() => {
      expect(screen.getByRole("alert")).toBeInTheDocument();
    });
    expect(screen.getByRole("alert")).toHaveTextContent("Failed to connect");
  });

  it("shows No adapters found when fetch returns empty array", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(() =>
        Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve([]),
        } as unknown as Response)
      )
    );

    render(<DeviceRegistry agentBaseUrl="http://localhost:5080" />);

    await waitFor(() => {
      expect(screen.getByText(/No adapters found/i)).toBeInTheDocument();
    });
  });
});
