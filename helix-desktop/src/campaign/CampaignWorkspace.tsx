import { useCallback, useEffect, useState } from 'react';
import {
  createBoundedActivity,
  getCampaignProjection,
} from './agentCampaignClient';
import type { CampaignIdentity, CampaignProjection } from './agentCampaignClient';

const identityStorageKey = (baseUrl: string) => `helix.campaign-workspace.identity:${baseUrl}`;

function readIdentity(key: string): CampaignIdentity | null {
  const value = window.localStorage.getItem(key);
  if (value === null) return null;

  try {
    const parsed = JSON.parse(value) as Partial<CampaignIdentity>;
    if (typeof parsed.campaignId !== 'string' || !parsed.campaignId ||
        typeof parsed.activityId !== 'string' || !parsed.activityId) {
      throw new Error('Stored identity is incomplete.');
    }
    return { campaignId: parsed.campaignId, activityId: parsed.activityId };
  } catch {
    throw new Error('The saved campaign/activity identity is invalid. No new activity was created.');
  }
}

interface CampaignWorkspaceProps {
  baseUrl: string;
}

export default function CampaignWorkspace({ baseUrl }: CampaignWorkspaceProps) {
  const storageKey = identityStorageKey(baseUrl);
  const [projection, setProjection] = useState<CampaignProjection | null>(null);
  const [hasStoredIdentity, setHasStoredIdentity] = useState(false);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const applyProjection = useCallback((next: CampaignProjection) => {
    setProjection(current => {
      if (current && (
        current.campaignId !== next.campaignId ||
        current.activityId !== next.activityId ||
        next.revision <= current.revision
      )) return current;
      return next;
    });
  }, []);

  const restore = useCallback(async (identity: CampaignIdentity) => {
    setBusy(true);
    setError(null);
    try {
      const next = await getCampaignProjection(baseUrl, identity);
      if (next.campaignId !== identity.campaignId || next.activityId !== identity.activityId) {
        throw new Error('Agent returned a different campaign/activity identity.');
      }
      applyProjection(next);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Unable to restore the Agent campaign projection.');
    } finally {
      setBusy(false);
      setLoading(false);
    }
  }, [applyProjection, baseUrl]);

  useEffect(() => {
    let active = true;
    let identity: CampaignIdentity | null;
    try {
      identity = readIdentity(storageKey);
    } catch (cause) {
      setHasStoredIdentity(true);
      setError(cause instanceof Error ? cause.message : 'Unable to read the saved campaign identity.');
      setLoading(false);
      return () => { active = false; };
    }

    if (!identity) {
      setLoading(false);
      return () => { active = false; };
    }

    setHasStoredIdentity(true);
    void getCampaignProjection(baseUrl, identity).then(next => {
      if (!active) return;
      if (next.campaignId !== identity.campaignId || next.activityId !== identity.activityId) {
        throw new Error('Agent returned a different campaign/activity identity.');
      }
      applyProjection(next);
    }).catch(cause => {
      if (active) setError(cause instanceof Error ? cause.message : 'Unable to restore the Agent campaign projection.');
    }).finally(() => {
      if (active) setLoading(false);
    });

    return () => { active = false; };
  }, [applyProjection, baseUrl, storageKey]);

  const startActivity = async () => {
    if (busy || hasStoredIdentity || projection) return;
    setBusy(true);
    setError(null);
    try {
      const created = await createBoundedActivity(baseUrl);
      applyProjection(created);
      const identity = { campaignId: created.campaignId, activityId: created.activityId };
      window.localStorage.setItem(storageKey, JSON.stringify(identity));
      setHasStoredIdentity(true);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Unable to start the bounded simulated activity.');
    } finally {
      setBusy(false);
    }
  };

  const refresh = async () => {
    if (busy) return;
    try {
      const identity = projection ?? readIdentity(storageKey);
      if (!identity) return;
      await restore({ campaignId: identity.campaignId, activityId: identity.activityId });
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Unable to read the saved campaign identity.');
    }
  };

  return (
    <aside aria-label="Campaign workspace" style={{
      boxSizing: 'border-box',
      flex: '0 0 360px',
      height: '100vh',
      overflowY: 'auto',
      padding: '20px',
      borderLeft: '1px solid var(--color-border)',
      background: 'var(--color-bg)',
      color: 'var(--color-text)',
      fontFamily: 'var(--font-sans)',
    }}>
      <h1 style={{ marginTop: 0 }}>Campaign workspace</h1>
      <p>A bounded simulated activity, projected from the Agent.</p>

      {loading ? <p role="status">Restoring Agent projection…</p> : null}
      {error ? <p role="alert">{error}</p> : null}

      {!projection && !hasStoredIdentity && !loading ? (
        <button type="button" onClick={startActivity} disabled={busy}>
          {busy ? 'Starting…' : 'Start bounded simulated activity'}
        </button>
      ) : null}

      {!projection && hasStoredIdentity && !loading ? (
        <button type="button" onClick={refresh} disabled={busy}>
          {busy ? 'Restoring…' : 'Retry Agent recovery'}
        </button>
      ) : null}

      {projection ? (
        <section aria-label="Current campaign projection">
          <dl>
            <dt>Campaign ID</dt><dd>{projection.campaignId}</dd>
            <dt>Activity ID</dt><dd>{projection.activityId}</dd>
            <dt>Flow ID</dt><dd>{projection.flowId}</dd>
            <dt>Command ID</dt><dd>{projection.commandId}</dd>
            <dt>Revision</dt><dd>{projection.revision}</dd>
            <dt>Lifecycle</dt>
            <dd>{projection.lifecycle === 'PendingExternalEvidence'
              ? 'Pending external evidence'
              : 'Completed (Agent projection)'}</dd>
            <dt>Physical outcome</dt><dd>{projection.physicalOutcome}</dd>
          </dl>
          <p role="note">
            This bounded simulated activity is an Agent projection; it does not prove that unobserved physical work completed.
          </p>
          <button type="button" onClick={refresh} disabled={busy}>
            {busy ? 'Refreshing…' : 'Refresh Agent projection'}
          </button>
          <h2>Agent audit history</h2>
          {projection.audit.length ? (
            <ol>
              {projection.audit.map((entry, index) => (
                <li key={`${entry.occurredAt}-${entry.disposition}-${index}`}>
                  {entry.disposition} — {entry.occurredAt}
                </li>
              ))}
            </ol>
          ) : <p>No Agent audit entries.</p>}
        </section>
      ) : null}
    </aside>
  );
}
