import { useState, useEffect } from "react";
import type { FumehoodStatus } from "../types/dashboard";

const STATUS_COLOR: Record<FumehoodStatus["status"], string> = {
  running: "var(--color-busy)",
  idle:    "var(--color-online)",
  faulted: "var(--color-danger)",
  offline: "var(--color-offline)",
};

interface SiteOverviewPageProps {
  baseUrl: string;
}

export function SiteOverviewPage({ baseUrl }: SiteOverviewPageProps) {
  const [fumehoods, setFumehoods] = useState<FumehoodStatus[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const activeCount = fumehoods.filter((fh) => fh.status === "running").length;

  useEffect(() => {
    setLoading(true);
    setError(null);
    fetch(baseUrl + "/api/fumehoods")
      .then((res) => { if (!res.ok) throw new Error(res.statusText); return res.json() as Promise<FumehoodStatus[]>; })
      .then(setFumehoods)
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, [baseUrl]);

  return (
    <section aria-label="Site Overview" data-testid="site-overview-page">
      <h1 style={{ color: "var(--color-text)", fontSize: "var(--text-xl)" }}>
        Site Overview
      </h1>
      <p style={{ color: "var(--color-text-muted)", fontSize: "var(--text-sm)",
        marginBottom: "var(--space-2)" }}>
        Active fumehoods:{" "}
        <span data-testid="active-count" style={{ color: "var(--color-busy)", fontWeight: 600 }}>
          {activeCount}
        </span>
      </p>
      {loading && <p style={{ color: "var(--color-text-muted)" }}>Loading...</p>}
      {error && <p style={{ color: "var(--color-danger)" }}>{error}</p>}
      <div style={{
        display: "grid",
        gridTemplateColumns: "repeat(auto-fill, minmax(220px, 1fr))",
        gap: "var(--space-4)", marginTop: "var(--space-4)",
      }}>
        {fumehoods.map((fh) => (
          <div key={fh.id} data-testid="fumehood-card"
            style={{
              background: "var(--color-surface)",
              border: "1px solid var(--color-border)",
              borderRadius: "var(--radius-md)",
              padding: "var(--space-4)",
            }}
          >
            <div style={{ fontWeight: 600, color: "var(--color-text)" }}>{fh.name}</div>
            <div data-testid="fumehood-status"
              style={{ color: STATUS_COLOR[fh.status], fontSize: "var(--text-sm)" }}>
              {fh.status}
            </div>
            {fh.activeExperimentName && (
              <div style={{ color: "var(--color-text-muted)", fontSize: "var(--text-xs)" }}>
                {fh.activeExperimentName}
              </div>
            )}
          </div>
        ))}
      </div>
    </section>
  );
}
