import { useState, useEffect } from "react";
import { getAgentDeviceCapabilities, getAgentDevices } from "../../lib/agentClient";
import type { CapabilityDescriptor } from "../../types/agent";

interface DevicePaletteProps {
  baseUrl: string;
  onDragStart: (adapterId: string, capability: string, descriptor: CapabilityDescriptor) => void;
}

interface AdapterEntry {
  adapterId: string;
  capabilities: CapabilityDescriptor[];
}

export function DevicePalette({ baseUrl: _baseUrl, onDragStart }: DevicePaletteProps) {
  const [adapters, setAdapters] = useState<AdapterEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [fetchError, setFetchError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    const load = async () => {
      try {
        const list = await getAgentDevices();

        const results = await Promise.all(
          list.filter((entry) => entry.adapterId === "simulator").map(async (entry) => {
            const capData = await getAgentDeviceCapabilities(entry.adapterId);
            return { adapterId: entry.adapterId, capabilities: capData.capabilities };
          })
        );

        if (!cancelled) {
          setAdapters(results);
          setLoading(false);
        }
      } catch (reason) {
        if (!cancelled) {
          setFetchError(reason instanceof Error ? reason.message : "Device discovery is unavailable.");
          setLoading(false);
        }
      }
    };
    load();
    return () => { cancelled = true; };
  }, []);

  return (
    <div
      data-testid="device-palette"
      style={{
        width: 220,
        minWidth: 220,
        background: "#1e1e2e",
        borderRight: "1px solid #3a3a4a",
        overflowY: "auto",
        padding: "8px 0",
        flexShrink: 0,
      }}
    >
      {loading && <p style={{ padding: "8px 10px", color: "#6c7086", fontSize: 12 }}>Loading devices...</p>}
      {fetchError && <p style={{ padding: "8px 10px", color: "#e74c3c", fontSize: 12 }}>{fetchError}</p>}
      {!loading && adapters.length === 0 && !fetchError && <p style={{ padding: "8px 10px", color: "#6c7086", fontSize: 12 }}>No simulator devices available</p>}
      {fetchError && adapters.length > 0 && <p style={{ padding: "8px 10px", color: "#f5a623", fontSize: 12 }}>Showing previously discovered devices</p>}
      {!loading && adapters.map((adapter) => (
        <details
          key={adapter.adapterId}
          data-testid={`palette-group-${adapter.adapterId}`}
          open
          style={{ marginBottom: 4 }}
        >
          <summary
            style={{
              padding: "4px 10px",
              cursor: "pointer",
              color: "#cdd6f4",
              fontSize: 12,
              fontWeight: 600,
            }}
          >
            {adapter.adapterId}
          </summary>
          {adapter.capabilities.map((descriptor) => (
            <div
              key={descriptor.verbName}
              data-testid={`capability-card-${adapter.adapterId}-${descriptor.verbName}`}
              onPointerDown={(e) => {
                e.preventDefault();
                onDragStart(adapter.adapterId, descriptor.verbName, descriptor);
              }}
              style={{
                padding: "5px 12px",
                margin: "2px 6px",
                background: "#2a2a3e",
                border: "1px solid #3a3a4a",
                borderRadius: 4,
                cursor: "grab",
                fontSize: 12,
                color: "#cdd6f4",
                userSelect: "none",
              }}
            >
              <span style={{ overflowWrap: "anywhere" }}>{descriptor.displayName || descriptor.verbName}</span>
              {descriptor.category && (
                <span style={{ marginLeft: 6, fontSize: 10, color: "#6c7086" }}>
                  {descriptor.category}
                </span>
              )}
            </div>
          ))}
        </details>
      ))}
    </div>
  );
}
