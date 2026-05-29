import { ClassicPreset, NodeEditor } from "rete";
import type { BaseSchemes } from "rete";
import type { WslDocument, WslNode, WslEdge } from "../../../types/agent";

export type HelixNodeType =
  | "Step"
  | "Condition"
  | "Branch"
  | "Loop"
  | "Parallel"
  | "Delay"
  | "WaitFor";

// ---- Node classes ----

export class StepNode extends ClassicPreset.Node {
  readonly nodeType: HelixNodeType = "Step";
  constructor(label = "Step") {
    super(label);
    this.addInput("exec-in", new ClassicPreset.Input(new ClassicPreset.Socket("exec"), "In"));
    this.addOutput("exec-out", new ClassicPreset.Output(new ClassicPreset.Socket("exec"), "Out"));
    this.addControl("capability", new ClassicPreset.InputControl("text", { initial: "" }));
  }
  getParameters(): Record<string, unknown> {
    return {
      capability: (this.controls["capability"] as ClassicPreset.InputControl<"text">).value ?? "",
    };
  }
}

export class ConditionNode extends ClassicPreset.Node {
  readonly nodeType: HelixNodeType = "Condition";
  constructor(label = "Condition") {
    super(label);
    this.addInput("exec-in", new ClassicPreset.Input(new ClassicPreset.Socket("exec"), "In"));
    this.addOutput("true-out", new ClassicPreset.Output(new ClassicPreset.Socket("exec"), "True"));
    this.addOutput("false-out", new ClassicPreset.Output(new ClassicPreset.Socket("exec"), "False"));
    this.addControl("condition", new ClassicPreset.InputControl("text", { initial: "" }));
  }
  getParameters(): Record<string, unknown> {
    return {
      condition: (this.controls["condition"] as ClassicPreset.InputControl<"text">).value ?? "",
    };
  }
}

export class BranchNode extends ClassicPreset.Node {
  readonly nodeType: HelixNodeType = "Branch";
  constructor(label = "Branch") {
    super(label);
    this.addInput("exec-in", new ClassicPreset.Input(new ClassicPreset.Socket("exec"), "In"));
    this.addOutput("branch-a", new ClassicPreset.Output(new ClassicPreset.Socket("exec"), "Branch A"));
    this.addOutput("branch-b", new ClassicPreset.Output(new ClassicPreset.Socket("exec"), "Branch B"));
  }
  getParameters(): Record<string, unknown> {
    return {};
  }
}

export class LoopNode extends ClassicPreset.Node {
  readonly nodeType: HelixNodeType = "Loop";
  constructor(label = "Loop") {
    super(label);
    this.addInput("exec-in", new ClassicPreset.Input(new ClassicPreset.Socket("exec"), "In"));
    this.addOutput("body", new ClassicPreset.Output(new ClassicPreset.Socket("exec"), "Body"));
    this.addOutput("exec-out", new ClassicPreset.Output(new ClassicPreset.Socket("exec"), "Out"));
    this.addControl("iterations", new ClassicPreset.InputControl("number", { initial: 1 }));
  }
  getParameters(): Record<string, unknown> {
    return {
      iterations: (this.controls["iterations"] as ClassicPreset.InputControl<"number">).value ?? 1,
    };
  }
}

export class ParallelNode extends ClassicPreset.Node {
  readonly nodeType: HelixNodeType = "Parallel";
  constructor(label = "Parallel") {
    super(label);
    this.addInput("exec-in", new ClassicPreset.Input(new ClassicPreset.Socket("exec"), "In"));
    this.addOutput("branch-a", new ClassicPreset.Output(new ClassicPreset.Socket("exec"), "Branch A"));
    this.addOutput("branch-b", new ClassicPreset.Output(new ClassicPreset.Socket("exec"), "Branch B"));
    this.addOutput("exec-out", new ClassicPreset.Output(new ClassicPreset.Socket("exec"), "Out"));
  }
  getParameters(): Record<string, unknown> {
    return {};
  }
}

export class DelayNode extends ClassicPreset.Node {
  readonly nodeType: HelixNodeType = "Delay";
  constructor(label = "Delay") {
    super(label);
    this.addInput("exec-in", new ClassicPreset.Input(new ClassicPreset.Socket("exec"), "In"));
    this.addOutput("exec-out", new ClassicPreset.Output(new ClassicPreset.Socket("exec"), "Out"));
    this.addControl("durationMs", new ClassicPreset.InputControl("number", { initial: 1000 }));
  }
  getParameters(): Record<string, unknown> {
    return {
      durationMs: (this.controls["durationMs"] as ClassicPreset.InputControl<"number">).value ?? 1000,
    };
  }
}

export class WaitForNode extends ClassicPreset.Node {
  readonly nodeType: HelixNodeType = "WaitFor";
  constructor(label = "WaitFor") {
    super(label);
    this.addInput("exec-in", new ClassicPreset.Input(new ClassicPreset.Socket("exec"), "In"));
    this.addOutput("exec-out", new ClassicPreset.Output(new ClassicPreset.Socket("exec"), "Out"));
    this.addControl("condition", new ClassicPreset.InputControl("text", { initial: "" }));
  }
  getParameters(): Record<string, unknown> {
    return {
      condition: (this.controls["condition"] as ClassicPreset.InputControl<"text">).value ?? "",
    };
  }
}

export type HelixNode =
  | StepNode
  | ConditionNode
  | BranchNode
  | LoopNode
  | ParallelNode
  | DelayNode
  | WaitForNode;

// ---- WSL Export ----

export function exportToWsl(
  editor: NodeEditor<BaseSchemes>,
  name: string
): WslDocument {
  const nodes: WslNode[] = editor.getNodes().map((n) => ({
    nodeId: n.id,
    label: (n as unknown as ClassicPreset.Node).label ?? "node",
    nodeType: (n as unknown as HelixNode).nodeType ?? "Step",
    parameters: (n as unknown as { getParameters?: () => Record<string, unknown> }).getParameters
      ? (n as unknown as { getParameters: () => Record<string, unknown> }).getParameters()
      : {},
  }));

  const edges: WslEdge[] = editor.getConnections().map((c) => ({
    edgeId: c.id,
    sourceNodeId: c.source,
    targetNodeId: c.target,
  }));

  return {
    documentId: crypto.randomUUID(),
    name,
    schemaVersion: "1.0",
    nodes,
    edges,
  };
}
