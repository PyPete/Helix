import { useState } from "react";
import HelixShell from "./shell/HelixShell";
import CampaignWorkspace from "./campaign/CampaignWorkspace";
import { DEFAULT_CONNECTION } from "./types/agent";
import type { AgentConnection } from "./types/agent";

export default function App() {
  const [connection, setConnection] = useState<AgentConnection>(DEFAULT_CONNECTION);
  const baseUrl = `${connection.protocol}://${connection.host}:${connection.port}`;

  return (
    <div style={{ display: 'flex', height: '100vh', minWidth: 0 }}>
      <div style={{ flex: '1 1 auto', minWidth: 0 }}>
        <HelixShell connection={connection} onConnectionChange={setConnection} />
      </div>
      <CampaignWorkspace key={baseUrl} baseUrl={baseUrl} />
    </div>
  );
}
