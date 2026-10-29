import HelixShell from "./shell/HelixShell";
import CampaignWorkspace from "./campaign/CampaignWorkspace";
import { DEFAULT_CONNECTION } from "./types/agent";

export default function App() {
  const baseUrl = `${DEFAULT_CONNECTION.protocol}://${DEFAULT_CONNECTION.host}:${DEFAULT_CONNECTION.port}`;

  return (
    <div style={{ display: 'flex', height: '100vh', minWidth: 0 }}>
      <div style={{ flex: '1 1 auto', minWidth: 0 }}>
        <HelixShell />
      </div>
      <CampaignWorkspace baseUrl={baseUrl} />
    </div>
  );
}
