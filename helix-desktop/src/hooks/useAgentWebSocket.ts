import { useState, useEffect } from "react";
import type { RunspaceEvent, TrendValue, SafetyEvent, DeviceStatusEvent } from "../types/agent";

export interface AgentWebSocketState {
  runspaceEvents: RunspaceEvent[];
  trendValues: TrendValue[];
  safetyEvents: SafetyEvent[];
  deviceStatusEvents: DeviceStatusEvent[];
  connected: boolean;
}

export function useAgentWebSocket(baseUrl: string): AgentWebSocketState {
  const [runspaceEvents, setRunspaceEvents] = useState<RunspaceEvent[]>([]);
  const [trendValues, setTrendValues] = useState<TrendValue[]>([]);
  const [safetyEvents, setSafetyEvents] = useState<SafetyEvent[]>([]);
  const [deviceStatusEvents, setDeviceStatusEvents] = useState<DeviceStatusEvent[]>([]);
  const [connected, setConnected] = useState(false);

  useEffect(() => {
    const wsUrl = baseUrl.replace(/^http/, "ws") + "/ws";
    const ws = new WebSocket(wsUrl);

    ws.onopen = () => setConnected(true);
    ws.onclose = () => setConnected(false);
    ws.onerror = () => setConnected(false);
    ws.onmessage = (msg) => {
      try {
        const frame = JSON.parse(msg.data as string);
        if (frame.type === "trend-value") {
          setTrendValues((prev) => [...prev.slice(-200), frame as TrendValue]);
        } else if (frame.type === "safety-event") {
          setSafetyEvents((prev) => [...prev.slice(-50), frame as SafetyEvent]);
        } else if (frame.type === "device-status") {
          setDeviceStatusEvents((prev) => [...prev.slice(-50), frame as DeviceStatusEvent]);
        } else {
          setRunspaceEvents((prev) => [
            ...prev.slice(-100),
            frame as RunspaceEvent,
          ]);
        }
      } catch {
        // ignore malformed frames
      }
    };

    return () => ws.close();
  }, [baseUrl]);

  return { runspaceEvents, trendValues, safetyEvents, deviceStatusEvents, connected };
}