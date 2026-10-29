export interface CampaignAuditEntry {
  disposition: string;
  occurredAt: string;
}

export interface CampaignProjection {
  campaignId: string;
  activityId: string;
  flowId: string;
  awaitingStepId: string;
  commandId: string;
  revision: number;
  lifecycle: 'PendingExternalEvidence' | 'Completed';
  physicalOutcome: 'unknown' | 'uncertain';
  audit: CampaignAuditEntry[];
}

export interface CampaignIdentity {
  campaignId: string;
  activityId: string;
}

async function readProjection(response: Response): Promise<CampaignProjection> {
  if (!response.ok) {
    const message = response.status === 404
      ? 'The saved campaign/activity identity was not found. No new activity was created.'
      : `Agent campaign request failed (HTTP ${response.status}).`;
    throw new Error(message);
  }

  return await response.json() as CampaignProjection;
}

export async function createBoundedActivity(baseUrl: string): Promise<CampaignProjection> {
  const response = await fetch(`${baseUrl}/campaigns/bounded-activity`, { method: 'POST' });
  return readProjection(response);
}

export async function getCampaignProjection(
  baseUrl: string,
  identity: CampaignIdentity,
): Promise<CampaignProjection> {
  const campaignId = encodeURIComponent(identity.campaignId);
  const activityId = encodeURIComponent(identity.activityId);
  const response = await fetch(
    `${baseUrl}/campaigns/${campaignId}/activities/${activityId}`,
  );
  return readProjection(response);
}
