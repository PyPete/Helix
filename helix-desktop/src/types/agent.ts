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
  edgeKind?: "exec" | "data";
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
  experimentId?: string;
  experimentPhase?: string;
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
  returns?: ParameterSchema[];
}

export interface DeviceStatusEvent {
  type: "device-status";
  deviceId: string;
  status: "online" | "offline";
  capabilities: string[];
}

// Phase 12: Experiment event types
export interface ExperimentStartedEvent {
  type: "experiment-started";
  experimentId: string;
  name: string;
  experimentPhase: string;
  timestamp: string;
}

export interface ExperimentPhaseChangedEvent {
  type: "experiment-phase-changed";
  experimentId: string;
  experimentPhase: string;
  timestamp: string;
}

export interface ExperimentCompletedEvent {
  type: "experiment-completed";
  experimentId: string;
  timestamp: string;
}

export interface ExperimentFailedEvent {
  type: "experiment-failed";
  experimentId: string;
  reason?: string;
  timestamp: string;
}

export type ExperimentEvent =
  | ExperimentStartedEvent
  | ExperimentPhaseChangedEvent
  | ExperimentCompletedEvent
  | ExperimentFailedEvent;

export interface CommandEvidenceEvent {
  evidenceId: string;
  commandId: string;
  executionId: string;
  sequence: number;
  state: "Allowed" | "Rejected" | "Held";
  verifiedActor: string;
  capability: string;
  deviceId: string;
  timestamp: string;
  checks: Array<{ check: string; status: "Passed" | "Failed" | "Unknown"; code: string }>;
}

export type CommandEvidenceOutcome = "Allowed" | "Rejected" | "Held" | "Dispatched" | "Completed" | "Unavailable";

/** A committed immutable command-history fact; missing legacy provenance remains explicit. */
export interface CommandEvidenceHistoryRecord {
  evidenceId: string;
  commandId: string;
  executionId: string;
  correlationId: string | null;
  sequence: number;
  outcome: CommandEvidenceOutcome;
  verifiedActor: string | null;
  deviceId: string | null;
  capability: string | null;
  timestamp: string;
  parameters: string | null;
  checks: Array<{ check: string; status: "Passed" | "Failed" | "Unknown"; code: string; reason?: string }>;
  source?: "committed" | "recovered-spool" | "legacy";
  claimedActor?: string | null;
  agentGeneration?: string | null;
  spoolOrdinal?: number | null;
}

export interface CommandEvidenceHistoryState {
  records: CommandEvidenceHistoryRecord[];
  loading: boolean;
  unavailable: boolean;
  error: string | null;
  refresh: () => Promise<void>;
}

export interface ExperimentRecord {
  experimentId: string;
  name: string;
  lifecycle: string;
  activePhase: string | null;
  activeRunspaceId: string | null;
}
