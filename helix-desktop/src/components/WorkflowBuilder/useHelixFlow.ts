import { useCallback, useState, type Dispatch, type SetStateAction } from "react";
import {
  type Node,
  type Edge,
  addEdge,
  type OnConnect,
  type OnNodesChange,
  type OnEdgesChange,
  applyNodeChanges,
  applyEdgeChanges,
} from "@xyflow/react";
import type { CapabilityDescriptor, WslDocument, WslNode, WslEdge } from "../../types/agent";
import type { DeviceActionNodeData, HelixNodeData, HelixNodeType } from "./nodes/HelixNodes";

export const START_NODE_ID = "start_node";

let _nodeId = 1;
function nextId(): string {
  return `node_${_nodeId++}`;
}

export type HelixNode = Node<HelixNodeData>;
export type HelixEdge = Edge;

export interface UseHelixFlowResult {
  nodes: HelixNode[];
  edges: HelixEdge[];
  onNodesChange: OnNodesChange<HelixNode>;
  onEdgesChange: OnEdgesChange<HelixEdge>;
  onConnect: OnConnect;
  addNode: (type: HelixNodeType, position?: { x: number; y: number }) => void;
  addDeviceActionNode: (
    adapterId: string,
    capability: string,
    descriptor: CapabilityDescriptor,
    position?: { x: number; y: number }
  ) => void;
  exportWsl: (name: string) => WslDocument;
  setNodes: Dispatch<SetStateAction<HelixNode[]>>;
  setEdges: Dispatch<SetStateAction<HelixEdge[]>>;
}

export function useHelixFlow(): UseHelixFlowResult {
  const [nodes, setNodes] = useState<HelixNode[]>([
    {
      id: START_NODE_ID,
      type: "Start",
      position: { x: 40, y: 150 },
      data: { nodeType: "Start", label: "Start" },
    },
  ]);
  const [edges, setEdges] = useState<HelixEdge[]>([]);

  const onNodesChange: OnNodesChange<HelixNode> = useCallback(
    (changes) =>
      setNodes((nds) =>
        applyNodeChanges(
          // Prevent the Start node from being deleted
          changes.filter((c) => !(c.type === "remove" && c.id === START_NODE_ID)),
          nds
        )
      ),
    []
  );

  const onEdgesChange: OnEdgesChange<HelixEdge> = useCallback(
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
        ...(type === "Delay"     && { durationMin: 1 }),
        ...(type === "WaitFor"   && { condition: "" }),
        onNodeDataChange: (updates: Record<string, unknown>) =>
          setNodes((nds) => nds.map((n) => n.id === id ? { ...n, data: { ...n.data, ...updates } } : n)),
      };
      setNodes((nds) => [
        ...nds,
        { id, type, position, data },
      ]);
    },
    [setNodes]
  );

  const addDeviceActionNode = useCallback(
    (
      adapterId: string,
      capability: string,
      descriptor: CapabilityDescriptor,
      position = { x: 200, y: 200 }
    ) => {
      const id = nextId();
      const data: DeviceActionNodeData = {
        nodeType: "DeviceAction",
        adapterId,
        capability,
        parameters: descriptor.parameters,
        returns: descriptor.returns,
        fieldValues: {},
        label: `${adapterId}.${capability}`,
      };
      setNodes((nds) => [...nds, { id, type: "DeviceAction", position, data } as unknown as HelixNode]);
    },
    []
  );

  const exportWsl = useCallback(
    (name: string): WslDocument => {
      const wslNodes: WslNode[] = nodes.filter((n) => n.id !== START_NODE_ID).map((n) => {
        if (n.data.nodeType === "DeviceAction") {
          const d = n.data as DeviceActionNodeData;
          return {
            nodeId: n.id,
            label: d.label,
            nodeType: "Step",
            parameters: {
              capability: `${d.adapterId}.${d.capability}`,
              ...d.fieldValues,
            },
          };
        }
        return {
          nodeId: n.id,
          label: n.data.label,
          nodeType: n.data.nodeType,
          parameters: {
            ...(n.data.capability  !== undefined && { capability: n.data.capability }),
            ...(n.data.condition   !== undefined && { condition: n.data.condition }),
            ...(n.data.iterations  !== undefined && { iterations: n.data.iterations }),
            ...(n.data.durationMin !== undefined && { durationMin: n.data.durationMin }),
          },
        };
      });

      const wslEdges: WslEdge[] = edges.filter((e) => e.source !== START_NODE_ID).map((e) => ({
        edgeId: e.id,
        sourceNodeId: e.source,
        targetNodeId: e.target,
        edgeKind: e.sourceHandle === "exec-out" ? "exec" : "data",
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

  return { nodes, edges, onNodesChange, onEdgesChange, onConnect, addNode, addDeviceActionNode, exportWsl, setNodes, setEdges };
}

