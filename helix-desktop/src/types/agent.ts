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
