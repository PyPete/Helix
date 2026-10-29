import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import App from './App';

const configuredAgentUrl = 'http://127.0.0.1:5099';

function projection() {
  return {
    campaignId: 'campaign-selected-agent',
    activityId: 'activity-selected-agent',
    flowId: 'flow-selected-agent',
    awaitingStepId: 'awaiting-step-selected-agent',
    commandId: 'command-selected-agent',
    revision: 1,
    lifecycle: 'PendingExternalEvidence' as const,
    physicalOutcome: 'unknown' as const,
    audit: [{ disposition: 'Created', occurredAt: '2026-01-01T00:00:01Z' }],
  };
}

function jsonResponse(value: unknown) {
  return new Response(JSON.stringify(value), {
    status: 200,
    headers: { 'Content-Type': 'application/json' },
  });
}

vi.mock('./shell/HelixShell', () => ({
  default: ({ onConnectionChange }: { onConnectionChange?: (connection: { protocol: string; host: string; port: number }) => void }) => (
    <div data-testid="helix-shell">
      <div data-testid="ribbon-tab-Home" />
      <div data-testid="activity-bar" />
      <div data-testid="status-bar" />
      <button type="button" onClick={() => onConnectionChange?.({ protocol: 'http', host: '127.0.0.1', port: 5099 })}>
        Use configured Agent
      </button>
      <button type="button" onClick={() => onConnectionChange?.({ protocol: 'http', host: 'localhost', port: 5080 })}>
        Use default Agent
      </button>
    </div>
  ),
}));

// --- Mock rc-dock (class component --- use forwardRef-compatible function mock) ---
vi.mock('rc-dock', () => {
  const { forwardRef } = require('react') as typeof import('react');

  const instanceMethods = {
    saveLayout: vi.fn().mockReturnValue({ dockbox: { mode: 'horizontal', children: [] } }),
    loadLayout: vi.fn(),
    dockMove: vi.fn(),
    updateTab: vi.fn(),
    getLayout: vi.fn().mockReturnValue({ dockbox: { mode: 'horizontal', children: [] } }),
  };

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const DockLayoutMock = forwardRef<typeof instanceMethods, Record<string, unknown>>(
    (_props: Record<string, unknown>, ref: React.ForwardedRef<typeof instanceMethods>) => {
      if (typeof ref === 'function') {
        ref(instanceMethods);
      } else if (ref && typeof ref === 'object') {
        (ref as React.MutableRefObject<typeof instanceMethods>).current = instanceMethods;
      }
      return null;
    }
  );
  DockLayoutMock.displayName = 'DockLayoutMock';
  return { default: DockLayoutMock };
});

// --- Mock @tauri-apps/plugin-store ---
vi.mock('@tauri-apps/plugin-store', () => {
  const LazyStore = vi.fn(function (this: Record<string, unknown>) {
    this.get = vi.fn().mockResolvedValue(null);
    this.set = vi.fn().mockResolvedValue(undefined);
    this.save = vi.fn().mockResolvedValue(undefined);
  });
  return { LazyStore };
});

// --- Mock useAgentWebSocket ---
vi.mock('./hooks/useAgentWebSocket', () => ({
  useAgentWebSocket: vi.fn(() => ({
    runspaceEvents: [],
    trendValues: [],
    safetyEvents: [],
    deviceStatusEvents: [],
    experimentEvents: [],
    activeExperimentId: null,
    connected: false,
  })),
}));

// --- Mock useAgentHealth ---
vi.mock('./hooks/useAgentHealth', () => ({
  useAgentHealth: vi.fn(() => ({ healthy: null, error: null })),
}));

// --- Mock ConnectionConfig to avoid LazyStore module-level side effects ---
vi.mock('./components/ConnectionConfig', () => ({
  default: () => <div data-testid="connection-config-stub" />,
}));

describe('App', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders HelixShell container', () => {
    render(<App />);
    expect(screen.getByTestId('helix-shell')).toBeInTheDocument();
  });

  it('renders ribbon tabs', () => {
    render(<App />);
    expect(screen.getAllByTestId(/ribbon-tab-/).length).toBeGreaterThan(0);
  });

  it('renders activity bar', () => {
    render(<App />);
    expect(screen.getByTestId('activity-bar')).toBeInTheDocument();
  });

  it('renders status bar', () => {
    render(<App />);
    expect(screen.getByTestId('status-bar')).toBeInTheDocument();
  });

  it('adds the campaign workspace without replacing the Helix shell', () => {
    render(<App />);
    expect(screen.getByTestId('helix-shell')).toBeInTheDocument();
    expect(screen.getByRole('complementary', { name: 'Campaign workspace' })).toBeInTheDocument();
  });

  it('routes selected-Agent creation and restoration without using the default Agent', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(jsonResponse(projection()))
      .mockResolvedValueOnce(jsonResponse(projection()));

    render(<App />);

    fireEvent.click(screen.getByRole('button', { name: 'Use configured Agent' }));
    fireEvent.click(screen.getByRole('button', { name: 'Start bounded simulated activity' }));
    expect(await screen.findByText('campaign-selected-agent')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Use default Agent' }));
    expect(screen.getByRole('button', { name: 'Start bounded simulated activity' })).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Use configured Agent' }));
    expect(await screen.findByText('campaign-selected-agent')).toBeInTheDocument();

    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(fetchMock.mock.calls[0][0]).toBe(`${configuredAgentUrl}/campaigns/bounded-activity`);
    expect(fetchMock.mock.calls[0][1]?.method).toBe('POST');
    expect(fetchMock.mock.calls[1][0]).toBe(
      `${configuredAgentUrl}/campaigns/campaign-selected-agent/activities/activity-selected-agent`,
    );
    expect(fetchMock.mock.calls.every(([url]) => !String(url).startsWith('http://localhost:5080'))).toBe(true);
  });
});
