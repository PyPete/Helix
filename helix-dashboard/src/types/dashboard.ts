// Copied verbatim from helix-desktop/src/types/agent.ts — per D-06
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

/** Placeholder fumehood status — hardcoded in Phase 7, no API endpoint yet (D-05). */
export interface FumehoodStatus {
  id: string;
  name: string;
  status: "idle" | "running" | "faulted" | "offline";
  activeExperimentName?: string;
  lastSeenAt: string; // ISO 8601
}

export interface SiteServerConnection {
  host: string;
  port: number;
  protocol: "http" | "https";
}

export const DEFAULT_SITE_CONNECTION: SiteServerConnection = {
  host: "localhost",
  port: 5000,
  protocol: "http",
};
