import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { describe, it, expect, vi, afterEach } from "vitest";
import { WorkflowBuilder } from "./WorkflowBuilder";

// Mock rete and all rete-* plugins so no DOM canvas is required in jsdom
// NodeEditor must be a regular function (not arrow) to be new-able
vi.mock("rete", () => {
  function NodeEditorMock(this: Record<string, unknown>) {
    this.getNodes = vi.fn(() => []);
    this.getConnections = vi.fn(() => []);
    this.use = vi.fn();
  }
  const ClassicPreset = {
    Node: class MockNode {
      id = "mock-id";
      label = "mock";
      inputs: Record<string, unknown> = {};
      outputs: Record<string, unknown> = {};
      controls: Record<string, unknown> = {};
      addInput = vi.fn();
      addOutput = vi.fn();
      addControl = vi.fn();
    },
    Input: vi.fn(function () {}),
    Output: vi.fn(function () {}),
    Socket: vi.fn(function () {}),
    InputControl: vi.fn(function () { return { value: "" }; }),
    Schemes: {},
  };
  return { NodeEditor: NodeEditorMock, ClassicPreset };
});

vi.mock("rete-area-plugin", () => {
  function AreaPluginMock(this: Record<string, unknown>) {
    this.use = vi.fn();
    this.destroy = vi.fn();
    this.nodeViews = new Map();
  }
  return {
    AreaPlugin: AreaPluginMock,
    AreaExtensions: {
      zoomAt: vi.fn(),
      simpleNodesOrder: vi.fn(),
      selectableNodes: vi.fn(),
    },
  };
});

vi.mock("rete-connection-plugin", () => {
  function ConnectionPluginMock(this: Record<string, unknown>) {
    this.use = vi.fn();
    this.addPreset = vi.fn();
  }
  return {
    ConnectionPlugin: ConnectionPluginMock,
    Presets: {
      classic: { setup: vi.fn(() => ({})) },
    },
  };
});

vi.mock("rete-react-plugin", () => {
  function ReactPluginMock(this: Record<string, unknown>) {
    this.use = vi.fn();
    this.addPreset = vi.fn();
  }
  return {
    ReactPlugin: ReactPluginMock,
    Presets: {
      classic: { setup: vi.fn(() => ({})) },
    },
  };
});

vi.mock("react-dom/client", () => ({
  createRoot: vi.fn(),
}));

describe("WorkflowBuilder", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("renders workflow builder canvas container", () => {
    vi.stubGlobal("fetch", vi.fn(() => new Promise(() => {})));
    render(
      <WorkflowBuilder baseUrl="http://localhost:5080" onExport={vi.fn()} />
    );
    expect(screen.getByTestId("workflow-builder")).toBeInTheDocument();
    expect(screen.getByTestId("rete-canvas")).toBeInTheDocument();
  });

  it("renders export button", () => {
    vi.stubGlobal("fetch", vi.fn(() => new Promise(() => {})));
    render(
      <WorkflowBuilder baseUrl="http://localhost:5080" onExport={vi.fn()} />
    );
    expect(screen.getByTestId("export-wsl-btn")).toBeInTheDocument();
  });

  it("calls onExport with WslDocument when export button clicked", async () => {
    vi.stubGlobal("fetch", vi.fn(() => new Promise(() => {})));
    const onExport = vi.fn();
    render(
      <WorkflowBuilder baseUrl="http://localhost:5080" onExport={onExport} />
    );
    fireEvent.click(screen.getByTestId("export-wsl-btn"));
    await waitFor(() =>
      expect(onExport).toHaveBeenCalledWith(
        expect.objectContaining({ schemaVersion: "1.0" })
      )
    );
  });

  it("shows error when devices fetch fails", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(() => Promise.reject(new Error("Network error")))
    );
    render(
      <WorkflowBuilder baseUrl="http://localhost:5080" onExport={vi.fn()} />
    );
    await waitFor(() =>
      expect(screen.getByRole("alert")).toBeInTheDocument()
    );
    expect(screen.getByRole("alert")).toHaveTextContent("Network error");
  });

  it("fetches devices on mount", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(() =>
        Promise.resolve({
          ok: true,
          json: () =>
            Promise.resolve([
              {
                adapterId: "VapourtecR4",
                healthy: true,
                message: "OK",
                capabilities: ["SetTemperature"],
              },
            ]),
        } as unknown as Response)
      )
    );
    render(
      <WorkflowBuilder baseUrl="http://localhost:5080" onExport={vi.fn()} />
    );
    await waitFor(() =>
      expect(fetch).toHaveBeenCalledWith("http://localhost:5080/devices")
    );
  });
});
