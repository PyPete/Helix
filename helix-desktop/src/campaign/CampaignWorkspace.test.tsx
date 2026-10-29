import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, expect, it, vi } from 'vitest';
import CampaignWorkspace from './CampaignWorkspace';

const baseUrl = 'http://localhost:5080';
const storageKey = `helix.campaign-workspace.identity:${baseUrl}`;

function projection(revision: number, lifecycle: 'PendingExternalEvidence' | 'Completed' = 'PendingExternalEvidence') {
  return {
    campaignId: 'campaign-stable',
    activityId: 'activity-stable',
    flowId: 'flow-stable',
    awaitingStepId: 'awaiting-step-stable',
    commandId: 'command-stable',
    revision,
    lifecycle,
    physicalOutcome: 'unknown' as const,
    audit: [{ disposition: revision === 1 ? 'Created' : 'Accepted', occurredAt: `2026-01-01T00:00:0${revision}Z` }],
  };
}

function jsonResponse(value: unknown, status = 200) {
  return new Response(JSON.stringify(value), { status, headers: { 'Content-Type': 'application/json' } });
}

beforeEach(() => {
  window.localStorage.clear();
  vi.restoreAllMocks();
});

it('does_not_create_an_activity_for_an_unknown_identity', async () => {
    window.localStorage.setItem(storageKey, JSON.stringify({ campaignId: 'missing-campaign', activityId: 'missing-activity' }));
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse({ message: 'not found' }, 404));

    render(<CampaignWorkspace baseUrl={baseUrl} />);

    expect(await screen.findByRole('alert')).toHaveTextContent('No new activity was created');
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(fetchMock.mock.calls[0][0]).toBe(`${baseUrl}/campaigns/missing-campaign/activities/missing-activity`);
    expect(fetchMock.mock.calls[0][1]?.method).not.toBe('POST');
    expect(screen.queryByRole('button', { name: 'Start bounded simulated activity' })).not.toBeInTheDocument();
});

it('does_not_regress_pending_projection_from_an_older_revision', async () => {
    window.localStorage.setItem(storageKey, JSON.stringify({ campaignId: 'campaign-stable', activityId: 'activity-stable' }));
    const fetchMock = vi.spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(jsonResponse(projection(2)))
      .mockResolvedValueOnce(jsonResponse(projection(1, 'Completed')));

    render(<CampaignWorkspace baseUrl={baseUrl} />);
    expect(await screen.findByText('Pending external evidence')).toBeInTheDocument();
    expect(await screen.findByText(/Accepted/)).toBeInTheDocument();

    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: 'Refresh Agent projection' }));
    });

    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2));
    expect(screen.getByText('Pending external evidence')).toBeInTheDocument();
    expect(screen.getByText(/Accepted/)).toBeInTheDocument();
});

it('starts once and restores the durable projection after remount', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(jsonResponse(projection(1)))
      .mockResolvedValueOnce(jsonResponse(projection(1)));

    const first = render(<CampaignWorkspace baseUrl={baseUrl} />);
    fireEvent.click(await screen.findByRole('button', { name: 'Start bounded simulated activity' }));
    expect(await screen.findByText('Pending external evidence')).toBeInTheDocument();
    expect(screen.getByText('campaign-stable')).toBeInTheDocument();
    expect(screen.getByText('Created — 2026-01-01T00:00:01Z')).toBeInTheDocument();
    expect(screen.getByText(/does not prove that unobserved physical work completed/)).toBeInTheDocument();
    first.unmount();

    render(<CampaignWorkspace baseUrl={baseUrl} />);
    expect(await screen.findByText('Pending external evidence')).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(fetchMock.mock.calls[0][1]?.method).toBe('POST');
    expect(fetchMock.mock.calls[1][0]).toBe(`${baseUrl}/campaigns/campaign-stable/activities/activity-stable`);
});

it('restores a completed Agent projection without asserting physical completion', async () => {
    window.localStorage.setItem(storageKey, JSON.stringify({ campaignId: 'campaign-stable', activityId: 'activity-stable' }));
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse(projection(2, 'Completed')));

    render(<CampaignWorkspace baseUrl={baseUrl} />);

    expect(await screen.findByText('Completed (Agent projection)')).toBeInTheDocument();
    expect(screen.getByText('unknown')).toBeInTheDocument();
    expect(screen.getByText(/does not prove that unobserved physical work completed/)).toBeInTheDocument();
    expect(fetchMock.mock.calls[0][1]?.method).not.toBe('POST');
});
