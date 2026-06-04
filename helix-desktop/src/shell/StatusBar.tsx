import './StatusBar.css';

export interface StatusBarProps {
  connected: boolean;
  healthy: boolean | null;
  agentUrl: string;
}

export function StatusBar({ connected, healthy, agentUrl }: StatusBarProps) {
  const dotClass =
    healthy === true ? 'status-dot healthy' :
    healthy === false ? 'status-dot unhealthy' :
    'status-dot connecting';

  const statusText =
    healthy === null ? 'Connecting...' :
    healthy ? 'Agent Online' :
    'Agent Offline';

  return (
    <footer className="statusbar" aria-label="Status Bar" data-testid="status-bar">
      <span>
        <span className={dotClass} aria-hidden="true" />
        {statusText}
        {!connected && healthy !== null && ' (disconnected)'}
      </span>
      <span className="statusbar-url">{agentUrl}</span>
    </footer>
  );
}
