import { useState, useEffect } from "react";
import type { DeviceAdapter } from "../types/agent";
import DeviceManualControl from "./DeviceManualControl/DeviceManualControl";

interface DeviceRegistryProps {
  agentBaseUrl: string;
}

function mapHealthToAdapters(data: { status?: string }): DeviceAdapter[] {
  return [
    {
      adapterId: "agent",
      healthy: data.status === "Healthy",
      message: data.status ?? "Unknown",
      capabilities: [],
    },
  ];
}

export default function DeviceRegistry({ agentBaseUrl }: DeviceRegistryProps) {
  const [devices, setDevices] = useState<DeviceAdapter[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedDevice, setSelectedDevice] = useState<string | null>(null);

  useEffect(() => {
    const poll = async () => {
      setLoading(true);
      try {
        const res = await fetch(`${agentBaseUrl}/health`);
        if (!res.ok) throw new Error(`Agent returned ${res.status}`);
        const text = await res.text();
        const data: { status?: string } = { status: text.trim() };
        setDevices(mapHealthToAdapters(data));
        setError(null);
      } catch (e) {
        setError((e as Error).message);
      } finally {
        setLoading(false);
      }
    };
    poll();
    const interval = setInterval(poll, 5000);
    return () => clearInterval(interval);
  }, [agentBaseUrl]);

  if (loading && devices.length === 0) return <p>Loading...</p>;

  return (
    <section aria-label="Device Registry">
      <h2>Device Registry</h2>
      {error && <p role="alert" className="error">{error}</p>}
      {devices.length === 0 && !error && <p>No adapters found</p>}
      <ul>
        {devices.map(d => (
          <li key={d.adapterId}>
            <strong>{d.adapterId}</strong> — {d.healthy ? "Healthy" : "Unhealthy"}: {d.message}
            <button onClick={() => setSelectedDevice(d.adapterId)}>Manual Control</button>
          </li>
        ))}
      </ul>
      {selectedDevice && <DeviceManualControl deviceId={selectedDevice} baseUrl={agentBaseUrl} />}
    </section>
  );
}
