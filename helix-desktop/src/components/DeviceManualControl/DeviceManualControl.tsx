import { useState, useEffect } from "react";
import type { CapabilityDescriptor } from "../../types/agent";

interface DeviceManualControlProps {
  deviceId: string;
  baseUrl: string;
}

export default function DeviceManualControl({ deviceId, baseUrl }: DeviceManualControlProps) {
  const [capabilities, setCapabilities] = useState<CapabilityDescriptor[]>([]);
  const [loading, setLoading] = useState(true);
  const [values, setValues] = useState<Record<string, string>>({});
  const [results, setResults] = useState<Record<string, string>>({});

  useEffect(() => {
    setLoading(true);
    fetch(`${baseUrl}/devices/${deviceId}/capabilities`)
      .then((res) => res.json())
      .then((data: { adapterId: string; capabilities: CapabilityDescriptor[] }) => {
        setCapabilities(data.capabilities ?? []);
      })
      .catch(() => {
        setCapabilities([]);
      })
      .finally(() => {
        setLoading(false);
      });
  }, [baseUrl, deviceId]);

  async function handleExecute(descriptor: CapabilityDescriptor) {
    if (descriptor.isDestructive) {
      const confirmed = window.confirm(`Confirm: execute ${descriptor.displayName}?`);
      if (!confirmed) return;
    }

    const parameters: Record<string, string | number> = {};
    for (const param of descriptor.parameters) {
      const key = `${descriptor.verbName}.${param.name}`;
      parameters[param.name] = values[key] ?? "";
    }

    try {
      const res = await fetch(`${baseUrl}/devices/${deviceId}/execute`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ capability: descriptor.verbName, parameters }),
      });
      if (!res.ok) throw new Error(`Request failed: ${res.status}`);
      setResults((prev) => ({ ...prev, [descriptor.verbName]: "Success" }));
    } catch (e) {
      setResults((prev) => ({ ...prev, [descriptor.verbName]: (e as Error).message }));
    }
  }

  if (loading) {
    return (
      <section data-testid="device-manual-control">
        <p>Loading capabilities...</p>
      </section>
    );
  }

  return (
    <section data-testid="device-manual-control">
      <h3>{deviceId} — Manual Control</h3>
      {capabilities.map((descriptor) => (
        <div key={descriptor.verbName} data-testid={`capability-${descriptor.verbName}`}>
          <h4>{descriptor.displayName}</h4>
          <p>{descriptor.description}</p>
          {descriptor.parameters.map((param) =>
            param.type === "Numeric" ? (
              <div key={param.name}>
                <label htmlFor={`${descriptor.verbName}.${param.name}`}>{param.displayName}</label>
                <input
                  id={`${descriptor.verbName}.${param.name}`}
                  type="number"
                  min={param.min}
                  max={param.max}
                  step={param.step ?? 1}
                  value={values[`${descriptor.verbName}.${param.name}`] ?? ""}
                  onChange={(e) =>
                    setValues((v) => ({
                      ...v,
                      [`${descriptor.verbName}.${param.name}`]: e.target.value,
                    }))
                  }
                />
                <span>{param.unit}</span>
              </div>
            ) : null
          )}
          <button onClick={() => handleExecute(descriptor)}>Execute</button>
          {results[descriptor.verbName] && (
            <span data-testid={`result-${descriptor.verbName}`}>{results[descriptor.verbName]}</span>
          )}
        </div>
      ))}
    </section>
  );
}
