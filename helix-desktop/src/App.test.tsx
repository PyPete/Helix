import React from 'react';
import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import App from './App';

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
});
