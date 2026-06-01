import { render, screen } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import { StepNodeComponent } from "./HelixNodes";

// HelixNodes imports reactflow — mock it at the module level
vi.mock("@xyflow/react", () => ({
  memo: (c: unknown) => c,
  Handle: ({ id }: { id?: string }) => <div data-handle-id={id} />,
  Position: { Left: "left", Right: "right", Top: "top", Bottom: "bottom" },
  useEdges: vi.fn(() => []),
}));

describe("HelixNodes NodeTimingFooter", () => {
  const baseProps = {
    id: "node_1",
    selected: false,
    isConnectable: true,
    positionAbsoluteX: 0,
    positionAbsoluteY: 0,
    zIndex: 0,
    type: "Step",
    dragging: false,
    selectable: true,
    draggable: true,
    deletable: true,
    width: 160,
    height: 60,
    sourcePosition: undefined,
    targetPosition: undefined,
    dragHandle: undefined,
    parentId: undefined,
  };

  it("renders no timing footer when executionState is idle", () => {
    render(
      <StepNodeComponent
        {...baseProps}
        data={{ nodeType: "Step", label: "A", executionState: "idle" }}
      />
    );
    expect(screen.queryByTestId("timing-footer-running")).not.toBeInTheDocument();
    expect(screen.queryByTestId("timing-footer-succeeded")).not.toBeInTheDocument();
    expect(screen.queryByTestId("timing-footer-failed")).not.toBeInTheDocument();
  });

  it("renders no timing footer when executionState is undefined", () => {
    render(
      <StepNodeComponent
        {...baseProps}
        data={{ nodeType: "Step", label: "A" }}
      />
    );
    expect(screen.queryByTestId("timing-footer-running")).not.toBeInTheDocument();
  });

  it("renders timing-footer-running when executionState is running", () => {
    render(
      <StepNodeComponent
        {...baseProps}
        data={{
          nodeType: "Step",
          label: "A",
          executionState: "running",
          startedAt: new Date("2026-01-01T00:00:00Z").toISOString(),
        }}
      />
    );
    expect(screen.getByTestId("timing-footer-running")).toBeInTheDocument();
  });

  it("renders timing-footer-succeeded when executionState is succeeded", () => {
    render(
      <StepNodeComponent
        {...baseProps}
        data={{
          nodeType: "Step",
          label: "A",
          executionState: "succeeded",
          startedAt: new Date("2026-01-01T00:00:00Z").toISOString(),
          finishedAt: new Date("2026-01-01T00:01:30Z").toISOString(),
        }}
      />
    );
    expect(screen.getByTestId("timing-footer-succeeded")).toBeInTheDocument();
    expect(screen.getByTestId("timing-footer-succeeded")).toHaveTextContent("Finished in 1m 30s");
  });

  it("renders timing-footer-failed when executionState is failed", () => {
    render(
      <StepNodeComponent
        {...baseProps}
        data={{
          nodeType: "Step",
          label: "A",
          executionState: "failed",
          faultMessage: "timeout",
        }}
      />
    );
    expect(screen.getByTestId("timing-footer-failed")).toBeInTheDocument();
    expect(screen.getByTestId("timing-footer-failed")).toHaveTextContent("Failed at");
  });
});
