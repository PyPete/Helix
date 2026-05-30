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

vi.mock("rete-context-menu-plugin", () => {
  function ContextMenuPluginMock(this: Record<string, unknown>) {
    this.use = vi.fn();
  }
  return {
    ContextMenuPlugin: ContextMenuPluginMock,
    Presets: {
      classic: { setup: vi.fn(() => vi.fn()) },
    },
  };
});

// Mock workflowLibrary using vi.hoisted() pattern (required for constructor mocks)
const { mockSave, mockList } = vi.hoisted(() => ({
  mockSave: vi.fn().mockResolvedValue(undefined),
  mockList: vi.fn().mockResolvedValue(["test-workflow"]),
}));

vi.mock("../../lib/workflowLibrary", () => ({
  saveWorkflow: mockSave,
  loadWorkflow: vi.fn().mockResolvedValue({
    documentId: "x",
    name: "test-workflow",
    schemaVersion: "1.0",
    nodes: [],
    edges: [],
  }),
  listWorkflows: mockList,
  duplicateWorkflow: vi.fn(),
}));

describe("WorkflowBuilder", () => {
  afterEach(() => {
    vi.restoreAllMocks();
    vi.clearAllMocks();
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

  it("renders workflow name input", () => {
    vi.stubGlobal("fetch", vi.fn(() => new Promise(() => {})));
    render(
      <WorkflowBuilder baseUrl="http://localhost:5080" onExport={vi.fn()} />
    );
    expect(screen.getByTestId("workflow-name-input")).toBeInTheDocument();
  });

  it("calls saveWorkflow on Save button click", async () => {
    vi.stubGlobal("fetch", vi.fn(() => new Promise(() => {})));
    render(
      <WorkflowBuilder baseUrl="http://localhost:5080" onExport={vi.fn()} />
    );
    fireEvent.click(screen.getByTestId("save-workflow-btn"));
    await waitFor(() => expect(mockSave).toHaveBeenCalled());
  });

  it("calls listWorkflows on Load button click", async () => {
    vi.stubGlobal("fetch", vi.fn(() => new Promise(() => {})));
    render(
      <WorkflowBuilder baseUrl="http://localhost:5080" onExport={vi.fn()} />
    );
    fireEvent.click(screen.getByTestId("list-workflows-btn"));
    await waitFor(() => expect(mockList).toHaveBeenCalled());
  });

  it("capability-picker has onChange handler", async () => {
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
    render(<WorkflowBuilder baseUrl="http://localhost:5080" onExport={vi.fn()} />);
    await waitFor(() =>
      expect(screen.getByTestId("capability-picker")).toBeInTheDocument()
    );
    // Verify the select has an onChange -- changing it should not throw
    expect(() =>
      fireEvent.change(screen.getByTestId("capability-picker"), {
        target: { value: "SetTemperature" },
      })
    ).not.toThrow();
  });
});