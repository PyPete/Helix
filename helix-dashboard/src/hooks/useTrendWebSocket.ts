import { useState, useEffect } from "react";
import type { TrendValue } from "../types/dashboard";

export interface TrendWebSocketState {
  trendValues: TrendValue[];
  connected: boolean;
}

export function useTrendWebSocket(baseUrl: string): TrendWebSocketState {
  const [trendValues, setTrendValues] = useState<TrendValue[]>([]);
  const [connected, setConnected] = useState(false);

  useEffect(() => {
    const wsUrl = baseUrl.replace(/^http/, "ws") + "/api/trends/ws";
    const ws = new WebSocket(wsUrl);

    ws.onopen  = () => setConnected(true);
    ws.onclose = () => setConnected(false);
    ws.onerror = () => setConnected(false);
    ws.onmessage = (msg) => {
      try {
        const frame = JSON.parse(msg.data as string);
        if (frame.type === "trend-value") {
          setTrendValues((prev) => [...prev.slice(-200), frame as TrendValue]);
        }
      } catch {
        // ignore malformed frames
      }
    };

    return () => ws.close();
  }, [baseUrl]);

  return { trendValues, connected };
}
