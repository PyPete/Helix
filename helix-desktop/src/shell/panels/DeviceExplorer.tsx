import { useEffect, useState } from 'react';
import { getAgentDevices } from '../../lib/agentClient';
import type { DeviceAdapter } from '../../types/agent';

export interface DeviceExplorerProps {
  baseUrl: string;
  onOpenDevice: (adapterId: string) => void;
}

function asSimulatorDevices(devices: Awaited<ReturnType<typeof getAgentDevices>>): DeviceAdapter[] {
  return devices
    .filter((device) => device.adapterId === 'simulator')
    .map((device) => ({
      adapterId: device.adapterId,
      healthy: true,
      message: 'registered',
      capabilities: device.capabilities,
    }));
}

export function DeviceExplorer({ baseUrl: _baseUrl, onOpenDevice }: DeviceExplorerProps) {
  const [devices, setDevices] = useState<DeviceAdapter[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    const poll = async () => {
      setLoading(true);
      try {
        const discovered = await getAgentDevices();
        if (!cancelled) {
          setDevices(asSimulatorDevices(discovered));
          setError(null);
        }
      } catch (reason) {
        if (!cancelled) setError(reason instanceof Error ? reason.message : 'Device discovery is unavailable.');
      } finally {
        if (!cancelled) setLoading(false);
      }
    };
    void poll();
    const interval = window.setInterval(() => { void poll(); }, 5000);
    return () => {
      cancelled = true;
      window.clearInterval(interval);
    };
  }, []);

  if (loading && devices.length === 0) return <p>Loading device discovery...</p>;

  return (
    <section aria-label="Device Explorer" data-testid="device-explorer">
      {error && <p role="alert" className="error">{error}</p>}
      {error && devices.length > 0 && <p>Showing the last available device discovery.</p>}
      {!loading && devices.length === 0 && !error && <p>No simulator adapters found</p>}
      <ul>
        {devices.map((device) => (
          <li key={device.adapterId} style={{ overflowWrap: 'anywhere' }}>
            <strong>{device.adapterId}</strong>
            <button onClick={() => onOpenDevice(device.adapterId)} data-testid={`open-device-${device.adapterId}`}>
              Open
            </button>
          </li>
        ))}
      </ul>
    </section>
  );
}
