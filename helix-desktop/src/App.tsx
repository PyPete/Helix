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
  const [activeTab, setActiveTab] = useState<"builder" | "execution" | "monitor">("builder");

  const temperatureData = trendValues
    .filter((t) => t.trendName === "temperature")
    .map((t) => ({ t: new Date(t.timestamp).getTime(), v: t.value }));

  const uvMap = new Map<number, number>();
  trendValues
    .filter((t) => t.trendName.startsWith("UV:"))
    .forEach((t) => {
      const wavelength = parseInt(t.trendName.slice(3), 10);
      if (!isNaN(wavelength)) uvMap.set(wavelength, t.value);
    });
  const uvData = Array.from(uvMap.entries())
    .sort(([a], [b]) => a - b)
    .map(([wavelength, absorbance]) => ({ wavelength, absorbance }));

  const mirMap = new Map<number, number>();
  trendValues
    .filter((t) => t.trendName.startsWith("MIR:"))
    .forEach((t) => {
      const wavelength = parseInt(t.trendName.slice(4), 10);
      if (!isNaN(wavelength)) mirMap.set(wavelength, t.value);
    });
  const mirData = Array.from(mirMap.entries())
    .sort(([a], [b]) => a - b)
    .map(([wavelength, absorbance]) => ({ wavelength, absorbance }));

  return (
    <div className="app">
      <header>
        <h1>Helix Desktop</h1>
        <span className={health.healthy ? "status-healthy" : "status-unhealthy"}>
          {health.healthy === null ? "Connecting..." : health.healthy ? "Agent Online" : `Agent Offline: ${health.error}`}
        </span>
      </header>
      <main>
        <div className="app-always-visible">
          <ConnectionConfig connection={connection} onSave={setConnection} />
          <DeviceRegistry agentBaseUrl={baseUrl} />
        </div>
        <nav className="app-tabs">
          <button
            data-testid="tab-builder"
            className={activeTab === "builder" ? "tab-btn tab-btn--active" : "tab-btn"}
            onClick={() => setActiveTab("builder")}
          >
            Builder
          </button>
          <button
            data-testid="tab-execution"
            className={activeTab === "execution" ? "tab-btn tab-btn--active" : "tab-btn"}
            onClick={() => setActiveTab("execution")}
          >
            Execution
          </button>
          <button
            data-testid="tab-monitor"
            className={activeTab === "monitor" ? "tab-btn tab-btn--active" : "tab-btn"}
            onClick={() => setActiveTab("monitor")}
          >
            Monitor
          </button>
        </nav>
        <div
          className={activeTab === "builder" ? "tab-panel tab-panel--active" : "tab-panel"}
          aria-hidden={activeTab !== "builder"}
        >
          <WorkflowBuilder baseUrl={baseUrl} onExport={setCurrentWorkflow} />
        </div>
        <div
          className={activeTab === "execution" ? "tab-panel tab-panel--active" : "tab-panel"}
          aria-hidden={activeTab !== "execution"}
        >
          <ExecutionPanel
            baseUrl={baseUrl}
            definition={currentWorkflow}
            runspaceEvents={runspaceEvents}
          />
          <ScalarTrendChart trendName="Temperature" data={temperatureData} unit="°C" />
          <SpectralChart uvData={uvData} mirData={mirData} />
        </div>
        <div
          className={activeTab === "monitor" ? "tab-panel tab-panel--active" : "tab-panel"}
          aria-hidden={activeTab !== "monitor"}
        >
          <SafetyPanel safetyEvents={safetyEvents} />
        </div>
      </main>
    </div>
  );
}