import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { describe, it, expect, vi, afterEach } from "vitest";
import { WorkflowBuilder } from "./WorkflowBuilder";
import type { NodeProps } from "reactflow";

// ReactFlow renders SVG/canvas internals that don't exist in jsdom.
// Mock the entire reactflow module so the component tree renders without DOM errors.
vi.mock("reactflow", () => {
  const ReactFlow = vi.fn(({
    children,
    nodes,
    nodeTypes,
    onPaneContextMenu,
    onPaneClick,
    onInit,
  }: {
    children?: React.ReactNode;
    nodes?: Array<{ id: string; type?: string; data: unknown; position: { x: number; y: number } }>;
    nodeTypes?: Record<string, React.ComponentType<NodeProps>>;
    onPaneContextMenu?: (e: React.MouseEvent) => void;
    onPaneClick?: (e: React.MouseEvent) => void;
    onInit?: (instance: unknown) => void;
  }) => {
    onInit?.({
      screenToFlowPosition: ({ x, y }: { x: number; y: number }) => ({ x, y }),
    });
    return (
      <div
        data-testid="react-flow-canvas"
        onContextMenu={onPaneContextMenu}
        onClick={onPaneClick}
      >
        {/* Render node components so data-testid attributes are accessible */}
        {nodes?.map((node) => {
          const NodeComp = nodeTypes?.[node.type ?? ""];
          if (!NodeComp) return null;
          return <NodeComp key={node.id} id={node.id} data={node.data as never} selected={false} isConnectable={true} xPos={node.position.x} yPos={node.position.y} zIndex={0} type={node.type ?? ""} dragging={false} />;
        })}
        {children}
      </div>
    );
  });
  return {
    default: ReactFlow,
    Background: () => null,
    BackgroundVariant: { Dots: "dots" },
    Controls: () => null,
    MiniMap: () => null,
    Handle: ({ id }: { id?: string }) => <div data-handle-id={id} />,
    Position: { Left: "left", Right: "right", Top: "top", Bottom: "bottom" },
    useReactFlow: vi.fn(() => ({
      screenToFlowPosition: (p: { x: number; y: number }) => p,
    })),
    useEdges: vi.fn(() => []),
    addEdge: vi.fn((conn: unknown, eds: unknown[]) => [...eds, conn]),
    applyNodeChanges: vi.fn((_: unknown, nds: unknown[]) => nds),
    applyEdgeChanges: vi.fn((_: unknown, eds: unknown[]) => eds),
  };
});

// Mock DevicePalette to prevent fetch side-effects in WorkflowBuilder tests
vi.mock("./DevicePalette", () => ({
  DevicePalette: () => <div data-testid="device-palette" />,
}));

// Mock workflowLibrary
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

  it("renders workflow builder container", () => {
    vi.stubGlobal("fetch", vi.fn(() => new Promise(() => {})));
    render(<WorkflowBuilder baseUrl="http://localhost:5080" onExport={vi.fn()} />);
    expect(screen.getByTestId("workflow-builder")).toBeInTheDocument();
    expect(screen.getByTestId("rete-canvas")).toBeInTheDocument();
  });

  it("renders export button", () => {
    vi.stubGlobal("fetch", vi.fn(() => new Promise(() => {})));
    render(<WorkflowBuilder baseUrl="http://localhost:5080" onExport={vi.fn()} />);
    expect(screen.getByTestId("export-wsl-btn")).toBeInTheDocument();
  });

  it("calls onExport with WslDocument when export button clicked", async () => {
    vi.stubGlobal("fetch", vi.fn(() => new Promise(() => {})));
    const onExport = vi.fn();
    render(<WorkflowBuilder baseUrl="http://localhost:5080" onExport={onExport} />);
    fireEvent.click(screen.getByTestId("export-wsl-btn"));
    await waitFor(() =>
      expect(onExport).toHaveBeenCalledWith(
        expect.objectContaining({ schemaVersion: "1.0" })
      )
    );
  });

  it("shows error when devices fetch fails", async () => {
    vi.stubGlobal("fetch", vi.fn(() => Promise.reject(new Error("Network error"))));
    render(<WorkflowBuilder baseUrl="http://localhost:5080" onExport={vi.fn()} />);
    await waitFor(() => expect(screen.getByRole("alert")).toBeInTheDocument());
    expect(screen.getByRole("alert")).toHaveTextContent("Network error");
  });

  it("fetches devices on mount", async () => {
    vi.stubGlobal("fetch", vi.fn(() =>
      Promise.resolve({
        ok: true,
        json: () => Promise.resolve([{
          adapterId: "VapourtecR4",
          healthy: true,
          message: "OK",
          capabilities: ["SetTemperature"],
        }]),
      } as unknown as Response)
    ));
    render(<WorkflowBuilder baseUrl="http://localhost:5080" onExport={vi.fn()} />);
    await waitFor(() =>
      expect(fetch).toHaveBeenCalledWith("http://localhost:5080/devices")
    );
  });

  it("renders workflow name input", () => {
    vi.stubGlobal("fetch", vi.fn(() => new Promise(() => {})));
    render(<WorkflowBuilder baseUrl="http://localhost:5080" onExport={vi.fn()} />);
    expect(screen.getByTestId("workflow-name-input")).toBeInTheDocument();
  });

  it("calls saveWorkflow on Save button click", async () => {
    vi.stubGlobal("fetch", vi.fn(() => new Promise(() => {})));
    render(<WorkflowBuilder baseUrl="http://localhost:5080" onExport={vi.fn()} />);
    fireEvent.click(screen.getByTestId("save-workflow-btn"));
    await waitFor(() => expect(mockSave).toHaveBeenCalled());
  });

  it("calls listWorkflows on Load button click", async () => {
    vi.stubGlobal("fetch", vi.fn(() => new Promise(() => {})));
    render(<WorkflowBuilder baseUrl="http://localhost:5080" onExport={vi.fn()} />);
    fireEvent.click(screen.getByTestId("list-workflows-btn"));
    await waitFor(() => expect(mockList).toHaveBeenCalled());
  });

  it("capability-picker renders when devices are fetched", async () => {
    vi.stubGlobal("fetch", vi.fn(() =>
      Promise.resolve({
        ok: true,
        json: () => Promise.resolve([{
          adapterId: "VapourtecR4",
          healthy: true,
          message: "OK",
          capabilities: ["SetTemperature"],
        }]),
      } as unknown as Response)
    ));
    render(<WorkflowBuilder baseUrl="http://localhost:5080" onExport={vi.fn()} />);
    await waitFor(() =>
      expect(screen.getByTestId("capability-picker")).toBeInTheDocument()
    );
    expect(() =>
      fireEvent.change(screen.getByTestId("capability-picker"), {
        target: { value: "SetTemperature" },
      })
    ).not.toThrow();
  });

  it("right-click on canvas shows node context menu", () => {
    vi.stubGlobal("fetch", vi.fn(() => new Promise(() => {})));
    render(<WorkflowBuilder baseUrl="http://localhost:5080" onExport={vi.fn()} />);
    const canvas = screen.getByTestId("react-flow-canvas");
    fireEvent.contextMenu(canvas, { clientX: 200, clientY: 200 });
    expect(screen.getByTestId("node-context-menu")).toBeInTheDocument();
    expect(screen.getByTestId("add-node-step")).toBeInTheDocument();
    expect(screen.getByTestId("add-node-condition")).toBeInTheDocument();
  });

  it("clicking a node type in context menu adds a node", () => {
    vi.stubGlobal("fetch", vi.fn(() => new Promise(() => {})));
    render(<WorkflowBuilder baseUrl="http://localhost:5080" onExport={vi.fn()} />);
    const canvas = screen.getByTestId("react-flow-canvas");
    fireEvent.contextMenu(canvas, { clientX: 200, clientY: 200 });
    fireEvent.click(screen.getByTestId("add-node-step"));
    // Context menu should close after adding
    expect(screen.queryByTestId("node-context-menu")).not.toBeInTheDocument();
  });

  it("context menu closes on canvas click", () => {
    vi.stubGlobal("fetch", vi.fn(() => new Promise(() => {})));
    render(<WorkflowBuilder baseUrl="http://localhost:5080" onExport={vi.fn()} />);
    const canvas = screen.getByTestId("react-flow-canvas");
    fireEvent.contextMenu(canvas, { clientX: 200, clientY: 200 });
    expect(screen.getByTestId("node-context-menu")).toBeInTheDocument();
    fireEvent.click(canvas);
    expect(screen.queryByTestId("node-context-menu")).not.toBeInTheDocument();
  });

  it("drop on canvas creates a DeviceAction node", async () => {
    vi.stubGlobal("fetch", vi.fn(() => new Promise(() => {})));
    render(<WorkflowBuilder baseUrl="http://localhost:5080" onExport={vi.fn()} />);

    const descriptor = {
      verbName: "SetFlow",
      displayName: "Set Flow",
      description: "Sets the flow rate",
      category: "Control",
      parameters: [],
      isDestructive: false,
      requiresActiveRunspace: false,
    };
    const payload = JSON.stringify({
      adapterId: "Alicat",
      capability: "SetFlow",
      descriptor,
    });

    const canvas = screen.getByTestId("rete-canvas");
    fireEvent.drop(canvas, {
      dataTransfer: {
        getData: (key: string) => (key === "application/helix-capability" ? payload : ""),
      },
      clientX: 300,
      clientY: 300,
    });

    await waitFor(() =>
      expect(screen.getByTestId(/node-deviceaction-/)).toBeInTheDocument()
    );
  });

  it("exportWsl produces nodeType Step with capability parameter for DeviceAction node", async () => {
    vi.stubGlobal("fetch", vi.fn(() => new Promise(() => {})));
    const onExport = vi.fn();
    render(<WorkflowBuilder baseUrl="http://localhost:5080" onExport={onExport} />);

    const descriptor = {
      verbName: "SetFlow",
      displayName: "Set Flow",
      description: "Sets the flow rate",
      category: "Control",
      parameters: [],
      isDestructive: false,
      requiresActiveRunspace: false,
    };
    const payload = JSON.stringify({
      adapterId: "Alicat",
      capability: "SetFlow",
      descriptor,
    });

    const canvas = screen.getByTestId("rete-canvas");
    fireEvent.drop(canvas, {
      dataTransfer: {
        getData: (key: string) => (key === "application/helix-capability" ? payload : ""),
      },
      clientX: 300,
      clientY: 300,
    });

    await waitFor(() =>
      expect(screen.getByTestId(/node-deviceaction-/)).toBeInTheDocument()
    );

    fireEvent.click(screen.getByTestId("export-wsl-btn"));

    await waitFor(() => {
      expect(onExport).toHaveBeenCalledWith(
        expect.objectContaining({
          schemaVersion: "1.0",
          nodes: expect.arrayContaining([
            expect.objectContaining({
              nodeType: "Step",
              parameters: expect.objectContaining({ capability: "Alicat.SetFlow" }),
            }),
          ]),
        })
      );
    });
  });
});
