import { useState } from "react";
import { DEFAULT_CONNECTION } from "./types/agent";
import type { AgentConnection, WslDocument } from "./types/agent";
import { useAgentHealth } from "./hooks/useAgentHealth";
import { useAgentWebSocket } from "./hooks/useAgentWebSocket";
import ConnectionConfig from "./components/ConnectionConfig";
import DeviceRegistry from "./components/DeviceRegistry";
import { WorkflowBuilder } from "./components/WorkflowBuilder/WorkflowBuilder";
import { ExecutionPanel } from "./components/ExecutionPanel/ExecutionPanel";
import { ScalarTrendChart } from "./components/TrendCharts/ScalarTrendChart";
import { SpectralChart } from "./components/TrendCharts/SpectralChart";
import { SafetyPanel } from "./components/SafetyPanel/SafetyPanel";
import "./App.css";

export default function App() {
  const [connection, setConnection] = useState<AgentConnection>(DEFAULT_CONNECTION);
  const baseUrl = `${connection.protocol}://${connection.host}:${connection.port}`;
  const health = useAgentHealth(baseUrl);
  const { runspaceEvents, trendValues, safetyEvents } = useAgentWebSocket(baseUrl);
  const [currentWorkflow, setCurrentWorkflow] = useState<WslDocument | null>(null);

  // Derive temperature chart data from trendValues
  const temperatureData = trendValues
    .filter((t) => t.trendName === "temperature")
    .map((t) => ({ t: new Date(t.timestamp).getTime(), v: t.value }));

  return (
    <div className="app">
      <header>
        <h1>Helix Desktop</h1>
        <span className={health.healthy ? "status-healthy" : "status-unhealthy"}>
          {health.healthy === null ? "Connecting..." : health.healthy ? "Agent Online" : `Agent Offline: ${health.error}`}
        </span>
      </header>
      <main>
        <ConnectionConfig connection={connection} onSave={setConnection} />
        <DeviceRegistry agentBaseUrl={baseUrl} />
        <WorkflowBuilder baseUrl={baseUrl} onExport={setCurrentWorkflow} />
        <ExecutionPanel
          baseUrl={baseUrl}
          definition={currentWorkflow}
          runspaceEvents={runspaceEvents}
        />
        <ScalarTrendChart trendName="Temperature" data={temperatureData} unit="°C" />
        {/* SpectralChart: spectral data from specialised events -- scaffold with empty state */}
        <SpectralChart uvData={[]} mirData={[]} />
        <SafetyPanel safetyEvents={safetyEvents} />
      </main>
    </div>
  );
}
