import { useState, useEffect } from "react";

interface ExperimentSummaryDto {
  runspaceId: string;
  campaignId?: string;
  name: string;
  status: string;
  createdAt: string;
  startedAt?: string;
  endedAt?: string;
  faultMessage?: string;
}

interface HistoryPageProps {
  baseUrl: string;
}

export function HistoryPage({ baseUrl }: HistoryPageProps) {
  const [experiments, setExperiments] = useState<ExperimentSummaryDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadExperiments = async () => {
      setLoading(true);
      setError(null);
      try {
        const res = await fetch(baseUrl + "/api/experiments");
        if (!res.ok) {
          setError("Failed to load experiments: " + res.statusText);
          return;
        }
        const data = await res.json() as ExperimentSummaryDto[];
        setExperiments(data);
      } catch (e) {
        setError((e as Error).message);
      } finally {
        setLoading(false);
      }
    };
    void loadExperiments();
  }, [baseUrl]);

  return (
    <section aria-label="Experiment History" data-testid="history-page">
      <h1 style={{ color: "var(--color-text)", fontSize: "var(--text-xl)" }}>
        Experiment History
      </h1>

      {error && <p role="alert" style={{ color: "var(--color-danger)", marginBottom: "var(--space-2)" }}>{error}</p>}

      {loading && (
        <p style={{ color: "var(--color-text-muted)" }}>Loading experiments…</p>
      )}

      {!loading && !error && experiments.length === 0 && (
        <p style={{ color: "var(--color-text-muted)" }}>No experiments found.</p>
      )}

      {experiments.length > 0 && (
        <table style={{
          width: "100%",
          borderCollapse: "collapse",
          fontSize: "var(--text-base)",
          marginTop: "var(--space-4)",
        }}>
          <thead>
            <tr style={{ borderBottom: "1px solid var(--color-border)" }}>
              <th style={{ textAlign: "left", padding: "var(--space-2)", color: "var(--color-text-muted)" }}>Name</th>
              <th style={{ textAlign: "left", padding: "var(--space-2)", color: "var(--color-text-muted)" }}>Status</th>
              <th style={{ textAlign: "left", padding: "var(--space-2)", color: "var(--color-text-muted)" }}>Created</th>
              <th style={{ textAlign: "left", padding: "var(--space-2)", color: "var(--color-text-muted)" }}>Ended</th>
            </tr>
          </thead>
          <tbody>
            {experiments.map((exp) => (
              <tr key={exp.runspaceId} data-testid="experiment-row"
                style={{ borderBottom: "1px solid var(--color-border)" }}>
                <td style={{ padding: "var(--space-2)", color: "var(--color-text)" }}>{exp.name}</td>
                <td style={{ padding: "var(--space-2)", color: "var(--color-text-muted)" }}>{exp.status}</td>
                <td style={{ padding: "var(--space-2)", color: "var(--color-text-muted)", fontSize: "var(--text-sm)" }}>
                  {new Date(exp.createdAt).toLocaleString()}
                </td>
                <td style={{ padding: "var(--space-2)", color: "var(--color-text-muted)", fontSize: "var(--text-sm)" }}>
                  {exp.endedAt ? new Date(exp.endedAt).toLocaleString() : "—"}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {!loading && !error && experiments.length > 0 && (
        <p style={{ color: "var(--color-text-dim)", fontSize: "var(--text-sm)", marginTop: "var(--space-2)" }}>
          {experiments.length} experiment{experiments.length !== 1 ? "s" : ""} found.
        </p>
      )}
    </section>
  );
}
