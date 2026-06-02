import { useState, useEffect } from "react";

interface CampaignDto {
  campaignId: string;
  name: string;
  status: string;
}

interface CampaignsPageProps {
  baseUrl: string;
}

export function CampaignsPage({ baseUrl }: CampaignsPageProps) {
  const [campaigns, setCampaigns] = useState<CampaignDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [newCampaignName, setNewCampaignName] = useState("");

  const loadCampaigns = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await fetch(baseUrl + "/api/campaigns");
      if (!res.ok) {
        setError("Failed to load campaigns: " + res.statusText);
        return;
      }
      const data = await res.json() as CampaignDto[];
      setCampaigns(data);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadCampaigns();
  }, [baseUrl]); // eslint-disable-line react-hooks/exhaustive-deps

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newCampaignName.trim()) return;
    setSubmitting(true);
    setError(null);
    try {
      const res = await fetch(baseUrl + "/api/campaigns", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name: newCampaignName.trim() }),
      });
      if (!res.ok) {
        setError("Failed to create campaign: " + res.statusText);
        return;
      }
      setNewCampaignName("");
      await loadCampaigns();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setSubmitting(false);
    }
  };

  const handleStart = async (id: string) => {
    setError(null);
    try {
      const res = await fetch(baseUrl + "/api/campaigns/" + id + "/start", {
        method: "POST",
      });
      if (!res.ok) {
        setError("Failed to start campaign: " + res.statusText);
        return;
      }
      await loadCampaigns();
    } catch (e) {
      setError((e as Error).message);
    }
  };

  const handleDelete = async (id: string) => {
    setError(null);
    try {
      const res = await fetch(baseUrl + "/api/campaigns/" + id, {
        method: "DELETE",
      });
      if (!res.ok) {
        setError("Failed to delete campaign: " + res.statusText);
        return;
      }
      await loadCampaigns();
    } catch (e) {
      setError((e as Error).message);
    }
  };

  return (
    <section aria-label="Campaign Management" data-testid="campaigns-page">
      <h1 style={{ color: "var(--color-text)", fontSize: "var(--text-xl)" }}>
        Campaigns
      </h1>

      <form onSubmit={(e) => { void handleCreate(e); }}
        style={{ display: "flex", gap: "var(--space-2)", marginBottom: "var(--space-4)" }}
      >
        <input
          data-testid="campaign-name-input"
          type="text"
          value={newCampaignName}
          onChange={(e) => setNewCampaignName(e.target.value)}
          placeholder="Campaign name"
          disabled={submitting}
          style={{
            flex: 1,
            background: "var(--color-surface-raised)",
            border: "1px solid var(--color-border)",
            color: "var(--color-text)",
            padding: "var(--space-2) var(--space-3)",
            borderRadius: "var(--radius-sm)",
            fontSize: "var(--text-base)",
          }}
        />
        <button
          data-testid="create-campaign-btn"
          type="submit"
          disabled={submitting || !newCampaignName.trim()}
          style={{
            background: "var(--color-primary)",
            color: "var(--color-bg)",
            border: "none",
            padding: "var(--space-2) var(--space-4)",
            borderRadius: "var(--radius-sm)",
            cursor: submitting ? "not-allowed" : "pointer",
            fontSize: "var(--text-base)",
            fontWeight: 600,
          }}
        >
          {submitting ? "Creating…" : "Create"}
        </button>
      </form>

      {error && <p role="alert" style={{ color: "var(--color-danger)", marginBottom: "var(--space-2)" }}>{error}</p>}

      {loading && (
        <p style={{ color: "var(--color-text-muted)" }}>Loading campaigns…</p>
      )}

      {!loading && campaigns.length === 0 && !error && (
        <p style={{ color: "var(--color-text-muted)" }}>No campaigns found.</p>
      )}

      {campaigns.length > 0 && (
        <table style={{
          width: "100%",
          borderCollapse: "collapse",
          fontSize: "var(--text-base)",
        }}>
          <thead>
            <tr style={{ borderBottom: "1px solid var(--color-border)" }}>
              <th style={{ textAlign: "left", padding: "var(--space-2)", color: "var(--color-text-muted)" }}>Name</th>
              <th style={{ textAlign: "left", padding: "var(--space-2)", color: "var(--color-text-muted)" }}>Status</th>
              <th style={{ padding: "var(--space-2)" }}></th>
            </tr>
          </thead>
          <tbody>
            {campaigns.map((c) => (
              <tr key={c.campaignId} data-testid="campaign-row"
                style={{ borderBottom: "1px solid var(--color-border)" }}>
                <td style={{ padding: "var(--space-2)", color: "var(--color-text)" }}>{c.name}</td>
                <td style={{ padding: "var(--space-2)", color: "var(--color-text-muted)" }}>{c.status}</td>
                <td style={{ padding: "var(--space-2)", display: "flex", gap: "var(--space-2)" }}>
                  <button
                    data-testid={`start-campaign-${c.campaignId}`}
                    onClick={() => { void handleStart(c.campaignId); }}
                    style={{
                      background: "var(--color-success)",
                      color: "var(--color-bg)",
                      border: "none",
                      padding: "var(--space-1) var(--space-2)",
                      borderRadius: "var(--radius-sm)",
                      cursor: "pointer",
                      fontSize: "var(--text-sm)",
                    }}
                  >
                    Start
                  </button>
                  <button
                    data-testid={`delete-campaign-${c.campaignId}`}
                    onClick={() => { void handleDelete(c.campaignId); }}
                    style={{
                      background: "var(--color-danger)",
                      color: "var(--color-bg)",
                      border: "none",
                      padding: "var(--space-1) var(--space-2)",
                      borderRadius: "var(--radius-sm)",
                      cursor: "pointer",
                      fontSize: "var(--text-sm)",
                    }}
                  >
                    Delete
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
