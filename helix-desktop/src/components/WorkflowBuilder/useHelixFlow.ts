import { useCallback, useState } from "react";
import {
  type Node,
  type Edge,
  addEdge,
  type OnConnect,
  type OnNodesChange,
  type OnEdgesChange,
  applyNodeChanges,
  applyEdgeChanges,
} from "reactflow";
import type { WslDocument, WslNode, WslEdge } from "../../types/agent";
import type { HelixNodeData, HelixNodeType } from "./nodes/HelixNodes";

let _nodeId = 1;
function nextId(): string {
  return `node_${_nodeId++}`;
}

export type HelixNode = Node<HelixNodeData>;
export type HelixEdge = Edge;

export interface UseHelixFlowResult {
  nodes: HelixNode[];
  edges: HelixEdge[];
  onNodesChange: OnNodesChange;
  onEdgesChange: OnEdgesChange;
  onConnect: OnConnect;
  addNode: (type: HelixNodeType, position?: { x: number; y: number }) => void;
  exportWsl: (name: string) => WslDocument;
}

export function useHelixFlow(): UseHelixFlowResult {
  const [nodes, setNodes] = useState<HelixNode[]>([]);
  const [edges, setEdges] = useState<HelixEdge[]>([]);

  const onNodesChange: OnNodesChange = useCallback(
    (changes) => setNodes((nds) => applyNodeChanges(changes, nds)),
    []
  );

  const onEdgesChange: OnEdgesChange = useCallback(
    (changes) => setEdges((eds) => applyEdgeChanges(changes, eds)),
    []
  );

  const onConnect: OnConnect = useCallback(
    (connection) =>
      setEdges((eds) =>
        addEdge({ ...connection, type: "helixEdge" }, eds)
      ),
    []
  );

  const addNode = useCallback(
    (type: HelixNodeType, position = { x: 100, y: 100 }) => {
      const id = nextId();
      const data: HelixNodeData = {
        nodeType: type,
        label: type,
        ...(type === "Step"      && { capability: "" }),
        ...(type === "Condition" && { condition: "" }),
        ...(type === "Loop"      && { iterations: 1 }),
        ...(type === "Delay"     && { durationMs: 1000 }),
        ...(type === "WaitFor"   && { condition: "" }),
      };
      setNodes((nds) => [
        ...nds,
        { id, type, position, data },
      ]);
    },
    []
  );

  const exportWsl = useCallback(
    (name: string): WslDocument => {
      const wslNodes: WslNode[] = nodes.map((n) => ({
        nodeId: n.id,
        label: n.data.label,
        nodeType: n.data.nodeType,
        parameters: {
          ...(n.data.capability  !== undefined && { capability: n.data.capability }),
          ...(n.data.condition   !== undefined && { condition: n.data.condition }),
          ...(n.data.iterations  !== undefined && { iterations: n.data.iterations }),
          ...(n.data.durationMs  !== undefined && { durationMs: n.data.durationMs }),
        },
      }));

      const wslEdges: WslEdge[] = edges.map((e) => ({
        edgeId: e.id,
        sourceNodeId: e.source,
        targetNodeId: e.target,
      }));

      return {
        documentId: crypto.randomUUID(),
        name,
        schemaVersion: "1.0",
        nodes: wslNodes,
        edges: wslEdges,
      };
    },
    [nodes, edges]
  );

  return { nodes, edges, onNodesChange, onEdgesChange, onConnect, addNode, exportWsl };
}

