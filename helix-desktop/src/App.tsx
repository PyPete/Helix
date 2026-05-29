import { useState } from "react";
import { DEFAULT_CONNECTION } from "./types/agent";
import type { AgentConnection } from "./types/agent";
import { useAgentHealth } from "./hooks/useAgentHealth";
import { useAgentWebSocket } from "./hooks/useAgentWebSocket";
import ConnectionConfig from "./components/ConnectionConfig";
import DeviceRegistry from "./components/DeviceRegistry";
import { ExecutionPanel } from "./components/ExecutionPanel/ExecutionPanel";
import "./App.css";

export default function App() {
  const [connection, setConnection] = useState<AgentConnection>(DEFAULT_CONNECTION);
  const baseUrl = `${connection.protocol}://${connection.host}:${connection.port}`;
  const health = useAgentHealth(baseUrl);
  const { runspaceEvents } = useAgentWebSocket(baseUrl);

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
        {/* ExecutionPanel: definition wired to WorkflowBuilder.onExport in Plan 08 */}
        <ExecutionPanel
          baseUrl={baseUrl}
          definition={null}
          runspaceEvents={runspaceEvents}
        />
      </main>
    </div>
  );
}
