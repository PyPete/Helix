import type { AgentConnection } from "../types/agent";

interface ConnectionConfigProps {
  connection: AgentConnection;
  onSave: (c: AgentConnection) => void;
}

export default function ConnectionConfig({ connection, onSave }: ConnectionConfigProps) {
  const fixedConnection: AgentConnection = { host: "127.0.0.1", port: 5080, protocol: "http" };

  return (
    <section aria-label="Agent Connection">
      <h2>Agent Connection</h2>
      <p>Native Agent broker: 127.0.0.1:5080</p>
      {connection.host !== fixedConnection.host && <p role="alert">Remote Agent destinations are not permitted.</p>}
      <button onClick={() => onSave(fixedConnection)}>Use local Agent</button>
    </section>
  );
}
