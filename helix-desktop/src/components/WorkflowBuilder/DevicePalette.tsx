import { useState, useEffect } from "react";
import type { CapabilityDescriptor } from "../../types/agent";

interface DevicePaletteProps {
  baseUrl: string;
  onDragStart: (adapterId: string, capability: string, descriptor: CapabilityDescriptor) => void;
}

interface AdapterEntry {
  adapterId: string;
  capabilities: CapabilityDescriptor[];
}

export function DevicePalette({ baseUrl, onDragStart }: DevicePaletteProps) {
  const [adapters, setAdapters] = useState<AdapterEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [fetchError, setFetchError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    const load = async () => {
      try {
        const res = await fetch(`${baseUrl}/devices`);
        if (!res.ok) throw new Error(`Agent returned ${res.status}`);
        const list: { adapterId: string; capabilities: string[] }[] = await res.json();

        const results = await Promise.all(
          list.map(async (entry) => {
            const capRes = await fetch(`${baseUrl}/devices/${entry.adapterId}/capabilities`);
            if (!capRes.ok) throw new Error(`Agent returned ${capRes.status}`);
            const capData: { adapterId: string; capabilities: CapabilityDescriptor[] } =
              await capRes.json();
            return { adapterId: entry.adapterId, capabilities: capData.capabilities };
          })
        );

        if (!cancelled) {
          setAdapters(results);
          setLoading(false);
        }
      } catch {
        if (!cancelled) {
          setFetchError("Failed to load devices");
          setLoading(false);
        }
      }
    };
    load();
    return () => { cancelled = true; };
  }, [baseUrl]);

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
      {!loading && !fetchError && adapters.map((adapter) => (
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
              draggable
              onDragStart={(e) => {
                e.dataTransfer.setData(
                  "application/helix-capability",
                  JSON.stringify({ adapterId: adapter.adapterId, capability: descriptor.verbName, descriptor })
                );
                e.dataTransfer.effectAllowed = "copy";
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
              }}
            >
              {descriptor.displayName || descriptor.verbName}
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
