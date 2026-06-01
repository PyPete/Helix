// Connection config — mirrors AgentProxyOptions.cs defaults (localhost:5080)
export interface AgentConnection {
  host: string;
  port: number;
  protocol: "http" | "https";
}

export const DEFAULT_CONNECTION: AgentConnection = {
  host: "localhost",
  port: 5080,
  protocol: "http",
};

// REST /health response (ASP.NET Core HealthChecks format)
export interface AgentHealth {
  status: "Healthy" | "Unhealthy" | "Degraded";
}

// Device adapter health — derived from REST /health response
// Phase 5: synthesised from health endpoint; Phase 6 will use DeviceAdapter gRPC RPC
export interface DeviceAdapter {
  adapterId: string;
  healthy: boolean;
  message: string;
  capabilities: string[];
}

// Runspace status — from agent_control.proto RunspaceStatusResponse (Phase 6)
export interface RunspaceStatus {
  runspaceId: string;
  status: "Running" | "Completed" | "Faulted" | "NotFound";
  message: string;
}


// WSL types -- Workflow Specification Language authoring contracts (Phase 6)
export interface WslNode {
  nodeId: string;
  label: string;
  nodeType: string;
  parameters: Record<string, unknown>;
}
export interface WslEdge {
  edgeId: string;
  sourceNodeId: string;
  targetNodeId: string;
}
export interface WslDocument {
  documentId: string;
  name: string;
  schemaVersion: string;
  nodes: WslNode[];
  edges: WslEdge[];
}
// WebSocket frame types (Phase 6)
export interface RunspaceEvent {
  type: string;
  runspaceId: string;
  timestamp: string;
  estimatedDurationMs?: number;
  [key: string]: unknown;
}
export interface TrendValue {
  type: "trend-value";
  trendName: string;
  value: number;
  unit: string;
  timestamp: string;
}
export interface SafetyEvent {
  type: "safety-event";
  ruleId: string;
  description: string;
  severity: string;
  triggered: boolean;
  timestamp: string;
  currentValue?: number;
}

// Phase 6.1 additions — CapabilityDescriptor schema + DeviceStatusEvent
export type ParameterType = "Numeric" | "Boolean" | "Enum" | "String" | "Duration";
export type CapabilityCategory = "Control" | "Measurement" | "Status" | "Utility";

export interface ParameterSchema {
  name: string;
  displayName: string;
  type: ParameterType;
  unit: string;
  min?: number;
  max?: number;
  step?: number;
  options?: string[];
  defaultValue?: unknown;
}

export interface CapabilityDescriptor {
  verbName: string;
  displayName: string;
  description: string;
  category: CapabilityCategory;
  parameters: ParameterSchema[];
  isDestructive: boolean;
  requiresActiveRunspace: boolean;
}

export interface DeviceStatusEvent {
  type: "device-status";
  deviceId: string;
  status: "online" | "offline";
  capabilities: string[];
}
