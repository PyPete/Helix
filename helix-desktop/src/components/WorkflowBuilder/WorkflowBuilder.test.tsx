import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { WorkflowBuilder } from "./WorkflowBuilder";
import type { NodeProps } from "@xyflow/react";

// ReactFlow renders SVG/canvas internals that don't exist in jsdom.
// Mock the entire reactflow module so the component tree renders without DOM errors.
vi.mock("@xyflow/react", () => {
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
          return <NodeComp key={node.id} id={node.id} data={node.data as never} selected={false} isConnectable={true} positionAbsoluteX={node.position.x} positionAbsoluteY={node.position.y} selectable={true} draggable={true} deletable={true} zIndex={0} type={node.type ?? ""} dragging={false} />;
        })}
        {children}
      </div>
    );
  });
  return {
    default: ReactFlow,
    ReactFlow,
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

const deviceActionDescriptor = {
  verbName: "SetFlow",
  displayName: "Set Flow",
  description: "Sets the flow rate",
  category: "Control",
  parameters: [],
  isDestructive: false,
  requiresActiveRunspace: false,
};

// The production palette starts pointer drags; expose that contract directly here.
vi.mock("./DevicePalette", () => ({
  DevicePalette: ({ onDragStart }: { onDragStart: (adapterId: string, capability: string, descriptor: typeof deviceActionDescriptor) => void }) => (
    <button data-testid="palette-set-flow" onPointerDown={() => onDragStart("simulator", "SetFlow", deviceActionDescriptor)}>
      Set Flow
    </button>
  ),
}));

// Mock workflowLibrary
const { mockSave, mockList } = vi.hoisted(() => ({
  mockSave: vi.fn().mockResolvedValue(undefined),
  mockList: vi.fn().mockResolvedValue(["test-workflow"]),
}));

const { getAgentDevices } = vi.hoisted(() => ({
  getAgentDevices: vi.fn(),
}));

let committedRunspaceEvents: Array<Record<string, unknown>> = [];

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

vi.mock("../../lib/agentClient", () => ({ getAgentDevices }));
vi.mock("../../hooks/useAgentWebSocket", () => ({
  useAgentWebSocket: () => ({ runspaceEvents: committedRunspaceEvents }),
}));

describe("WorkflowBuilder", () => {
  beforeEach(() => {
    getAgentDevices.mockResolvedValue([]);
    committedRunspaceEvents = [];
  });

  afterEach(() => {
    vi.restoreAllMocks();
    vi.clearAllMocks();
    getAgentDevices.mockReset();
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
    getAgentDevices.mockRejectedValue(new Error("Network error"));
    render(<WorkflowBuilder baseUrl="http://localhost:5080" onExport={vi.fn()} />);
    await waitFor(() => expect(screen.getByRole("alert")).toBeInTheDocument());
    expect(screen.getByRole("alert")).toHaveTextContent("Network error");
  });

  it("queries Agent-authoritative devices on mount", async () => {
    render(<WorkflowBuilder baseUrl="http://localhost:5080" onExport={vi.fn()} />);
    await waitFor(() =>
      expect(getAgentDevices).toHaveBeenCalledOnce()
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
    getAgentDevices.mockResolvedValue([{ adapterId: "simulator", capabilities: ["SetTemperature"] }]);
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
    render(<WorkflowBuilder baseUrl="http://localhost:5080" onExport={vi.fn()} />);
    const canvas = screen.getByTestId("rete-canvas");
    vi.spyOn(canvas, "getBoundingClientRect").mockReturnValue(new DOMRect(0, 0, 500, 500));
    fireEvent.pointerDown(screen.getByTestId("palette-set-flow"));
    await waitFor(() => expect(screen.getByTestId("palette-set-flow")).toBeInTheDocument());
    fireEvent.pointerUp(window, { clientX: 300, clientY: 300 });

    await waitFor(() =>
      expect(screen.getByTestId(/node-deviceaction-/)).toBeInTheDocument()
    );
  });

  it("committed evidence event transitions node to running and shows timing-footer-running", async () => {
    const { rerender } = render(<WorkflowBuilder baseUrl="http://localhost:5080" onExport={vi.fn()} />);

    // Add a Step node � it will get the next available id from the module-level counter
    const canvas = screen.getByTestId("react-flow-canvas");
    fireEvent.contextMenu(canvas, { clientX: 200, clientY: 200 });
    fireEvent.click(screen.getByTestId("add-node-step"));

    // Find the node id from the rendered data-testid
    const nodeEl = await screen.findByTestId(/node-step-/);
    const nodeId = nodeEl.getAttribute("data-testid")!.replace("node-step-", "");

    committedRunspaceEvents = [{
      type: "step-started",
      runspaceId: "r1",
      timestamp: new Date().toISOString(),
      stepId: nodeId,
      stepLabel: "A",
    }];
    rerender(<WorkflowBuilder baseUrl="http://localhost:5080" onExport={vi.fn()} />);

    // The node should transition to running and show the timing footer
    await waitFor(() =>
      expect(screen.getByTestId("timing-footer-running")).toBeInTheDocument()
    );
  });

  it("exportWsl produces nodeType Step with capability parameter for DeviceAction node", async () => {
    const onExport = vi.fn();
    render(<WorkflowBuilder baseUrl="http://localhost:5080" onExport={onExport} />);
    const canvas = screen.getByTestId("rete-canvas");
    vi.spyOn(canvas, "getBoundingClientRect").mockReturnValue(new DOMRect(0, 0, 500, 500));
    fireEvent.pointerDown(screen.getByTestId("palette-set-flow"));
    await waitFor(() => expect(screen.getByTestId("palette-set-flow")).toBeInTheDocument());
    fireEvent.pointerUp(window, { clientX: 300, clientY: 300 });

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
               parameters: expect.objectContaining({ capability: "simulator.SetFlow" }),
            }),
          ]),
        })
      );
    });
  });
});
