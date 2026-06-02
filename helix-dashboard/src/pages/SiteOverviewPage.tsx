import type { FumehoodStatus } from "../types/dashboard";

const PLACEHOLDER_FUMEHOODS: FumehoodStatus[] = [
  { id: "fh-001", name: "Fumehood A", status: "running",
    activeExperimentName: "Vapourtec R4 Test", lastSeenAt: new Date().toISOString() },
  { id: "fh-002", name: "Fumehood B", status: "idle",
    lastSeenAt: new Date().toISOString() },
  { id: "fh-003", name: "Fumehood C", status: "faulted",
    lastSeenAt: new Date(Date.now() - 300_000).toISOString() },
];

const STATUS_COLOR: Record<FumehoodStatus["status"], string> = {
  running: "var(--color-busy)",
  idle:    "var(--color-online)",
  faulted: "var(--color-danger)",
  offline: "var(--color-offline)",
};

export function SiteOverviewPage() {
  const activeCount = PLACEHOLDER_FUMEHOODS.filter((fh) => fh.status === "running").length;

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
      <div style={{
        display: "grid",
        gridTemplateColumns: "repeat(auto-fill, minmax(220px, 1fr))",
        gap: "var(--space-4)", marginTop: "var(--space-4)",
      }}>
        {PLACEHOLDER_FUMEHOODS.map((fh) => (
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
