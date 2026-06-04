import { useState, useEffect } from 'react';
import type { DeviceAdapter } from '../../types/agent';

export interface DeviceExplorerProps {
  baseUrl: string;
  onOpenDevice: (adapterId: string) => void;
}

export function DeviceExplorer({ baseUrl, onOpenDevice }: DeviceExplorerProps) {
  const [devices, setDevices] = useState<DeviceAdapter[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const poll = async () => {
      setLoading(true);
      try {
        const res = await fetch(`${baseUrl}/devices`);
        if (!res.ok) throw new Error(`Agent returned ${res.status}`);
        const data: { adapterId: string; capabilities: string[] }[] = await res.json();
        setDevices(
          data.map(d => ({
            adapterId: d.adapterId,
            healthy: true,
            message: 'registered',
            capabilities: d.capabilities,
          }))
        );
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
  }, [baseUrl]);

  if (loading && devices.length === 0) return <p>Loading...</p>;

  return (
    <section aria-label="Device Explorer" data-testid="device-explorer">
      {error && <p role="alert" className="error">{error}</p>}
      {devices.length === 0 && !error && <p>No adapters found</p>}
      <ul>
        {devices.map(d => (
          <li key={d.adapterId}>
            <strong>{d.adapterId}</strong>
            <button
              onClick={() => onOpenDevice(d.adapterId)}
              data-testid={`open-device-${d.adapterId}`}
            >
              Open
            </button>
          </li>
        ))}
      </ul>
    </section>
  );
}
