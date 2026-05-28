import { useState } from "react";
import { LazyStore } from "@tauri-apps/plugin-store";
import type { AgentConnection } from "../types/agent";

interface ConnectionConfigProps {
  connection: AgentConnection;
  onSave: (c: AgentConnection) => void;
}

const store = new LazyStore("helix-config.json");

export default function ConnectionConfig({ connection, onSave }: ConnectionConfigProps) {
  const [host, setHost] = useState(connection.host);
  const [port, setPort] = useState(connection.port);
  const [protocol, setProtocol] = useState<"http" | "https">(connection.protocol);
  const [error, setError] = useState<string | null>(null);

  const handleSave = async () => {
    if (!host || port < 1 || port > 65535) {
      setError("Invalid host or port");
      return;
    }
    setError(null);
    const updated: AgentConnection = { host, port, protocol };
    await store.set("agentConnection", updated);
    await store.save();
    onSave(updated);
  };

  return (
    <section aria-label="Agent Connection">
      <h2>Agent Connection</h2>
      {error && <p role="alert" className="error">{error}</p>}
      <label>
        Host
        <input value={host} onChange={e => setHost(e.target.value)} />
      </label>
      <label>
        Port
        <input type="number" value={port} onChange={e => setPort(Number(e.target.value))} />
      </label>
      <label>
        Protocol
        <select value={protocol} onChange={e => setProtocol(e.target.value as "http" | "https")}>
          <option value="http">http</option>
          <option value="https">https</option>
        </select>
      </label>
      <button onClick={handleSave}>Save</button>
    </section>
  );
}
