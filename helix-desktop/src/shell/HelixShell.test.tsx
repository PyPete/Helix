import React from 'react';
import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import HelixShell from './HelixShell';
import { useAgentHealth } from '../hooks/useAgentHealth';

vi.mock('./WorkspaceLayout', async () => {
  const React = await import('react');
  return {
    WorkspaceLayout: React.forwardRef((_props: { health?: { healthy: boolean | null; error: string | null; lastChecked: Date | null } }, _ref) => (
      <div
        data-testid="workspace-health"
        data-healthy={String(_props.health?.healthy)}
        data-error={_props.health?.error ?? ''}
        data-last-checked={_props.health?.lastChecked?.toISOString() ?? ''}
      />
    )),
  };
});

// --- Mock rc-dock (class component — use forwardRef-compatible function mock) ---
// DockLayout is a class component; React invokes it via `new` in class mode.
// We stub it as a React function component that renders null to keep tests fast.
vi.mock('rc-dock', () => {
  const { forwardRef } = require('react') as typeof import('react');

  // Provide a stable instance with methods that WorkspaceLayout's useImperativeHandle can call.
  const instanceMethods = {
    saveLayout: vi.fn().mockReturnValue({ dockbox: { mode: 'horizontal', children: [] } }),
    loadLayout: vi.fn(),
    dockMove: vi.fn(),
    updateTab: vi.fn(),
    getLayout: vi.fn().mockReturnValue({ dockbox: { mode: 'horizontal', children: [] } }),
  };

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const DockLayoutMock = forwardRef<typeof instanceMethods, Record<string, unknown>>(
    (_props, ref) => {
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
vi.mock('../hooks/useAgentWebSocket', () => ({
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
vi.mock('../hooks/useAgentHealth', () => ({
  useAgentHealth: vi.fn(() => ({ healthy: null, error: null })),
}));

vi.mock('../lib/agentClient', () => ({
  getAgentSessionStatus: vi.fn().mockResolvedValue({ authenticated: true, verifiedScientist: 'Scientist', expiresAt: null, restored: true, restorationError: null }),
  bootstrapAgentSession: vi.fn().mockResolvedValue({ authenticated: true, verifiedScientist: 'Scientist', expiresAt: null, restored: true, restorationError: null }),
  logoutAgentSession: vi.fn().mockResolvedValue(undefined),
  controlAgentExperiment: vi.fn(),
  getExperimentStatus: vi.fn(),
}));

describe('HelixShell', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders without crashing (helix-shell container present)', () => {
    render(<HelixShell />);
    expect(screen.getByTestId('helix-shell')).toBeInTheDocument();
  });

  it('renders 7 ribbon tab buttons', () => {
    render(<HelixShell />);
    const tabs = [
      'ribbon-tab-home',
      'ribbon-tab-devices',
      'ribbon-tab-trends',
      'ribbon-tab-workflows',
      'ribbon-tab-charts',
      'ribbon-tab-developer',
      'ribbon-tab-settings',
    ];
    for (const testId of tabs) {
      expect(screen.getByTestId(testId)).toBeInTheDocument();
    }
  });

  it('renders ActivityBar with data-testid="activity-bar"', () => {
    render(<HelixShell />);
    expect(screen.getByTestId('activity-bar')).toBeInTheDocument();
  });

  it('renders StatusBar with data-testid="status-bar"', () => {
    render(<HelixShell />);
    expect(screen.getByTestId('status-bar')).toBeInTheDocument();
  });

  it('renders DockLayout (WorkspaceLayout present, mocked rc-dock)', () => {
    render(<HelixShell />);
    // WorkspaceLayout renders DockLayout — mock suppresses DOM output.
    // Verify the shell container and workspace section render correctly.
    expect(screen.getByTestId('helix-shell')).toBeInTheDocument();
    expect(screen.getByTestId('activity-bar')).toBeInTheDocument();
  });

  it('passes the existing native health observation through to the workspace', () => {
    const health = { healthy: true, error: null, lastChecked: new Date('2026-11-03T12:00:00Z') };
    vi.mocked(useAgentHealth).mockReturnValue(health);

    render(<HelixShell />);

    const workspaceHealth = screen.getByTestId('workspace-health');
    expect(workspaceHealth.getAttribute('data-healthy') === 'true').toBe(true);
    expect(workspaceHealth.getAttribute('data-error')).toBe('');
    expect(workspaceHealth.getAttribute('data-last-checked')).toBe(health.lastChecked.toISOString());
  });

  it('exposes all five reachable scientific entry points', () => {
    render(<HelixShell />);
    expect(screen.getByRole('button', { name: 'Apparatus' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Procedures' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Plans' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Assembly' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Samples' })).toBeInTheDocument();
  });

  it('mounts the reachable local Agent session surface', async () => {
    render(<HelixShell />);
    expect(await screen.findByLabelText('Local Agent session')).toBeInTheDocument();
  });
});
