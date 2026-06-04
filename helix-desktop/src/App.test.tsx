import React from 'react';
import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import App from './App';

// --- Mock rc-dock (class component) ---
vi.mock('rc-dock', () => {
  const { forwardRef } = require('react');

  const instanceMethods = {
    saveLayout: vi.fn().mockReturnValue({ dockbox: { mode: 'horizontal', children: [] } }),
    loadLayout: vi.fn(),
    dockMove: vi.fn(),
    getLayout: vi.fn().mockReturnValue({ dockbox: { mode: 'horizontal', children: [] } }),
  };

  const DockLayoutMock = forwardRef((_props, ref) => {
    if (typeof ref === 'function') {
      ref(instanceMethods);
    } else if (ref && typeof ref === 'object') {
      ref.current = instanceMethods;
    }
    return null;
  });
  DockLayoutMock.displayName = 'DockLayoutMock';
  return { default: DockLayoutMock };
});

// --- Mock @tauri-apps/plugin-store ---
vi.mock('@tauri-apps/plugin-store', () => {
  const LazyStore = vi.fn(function () {
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
    connected: false,
  })),
}));

// --- Mock useAgentHealth ---
vi.mock('./hooks/useAgentHealth', () => ({
  useAgentHealth: vi.fn(() => ({ healthy: null, error: null })),
}));

// --- Mock ConnectionConfig to avoid LazyStore module-level side effects ---
vi.mock('./components/ConnectionConfig', () => ({
  default: () => React.createElement('div', { 'data-testid': 'connection-config-stub' }),
}));

describe('App', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders HelixShell container', () => {
    render(React.createElement(App));
    expect(screen.getByTestId('helix-shell')).toBeInTheDocument();
  });

  it('renders ribbon tabs', () => {
    render(React.createElement(App));
    expect(screen.getAllByTestId(/ribbon-tab-/).length).toBeGreaterThan(0);
  });

  it('renders activity bar', () => {
    render(React.createElement(App));
    expect(screen.getByTestId('activity-bar')).toBeInTheDocument();
  });

  it('renders status bar', () => {
    render(React.createElement(App));
    expect(screen.getByTestId('status-bar')).toBeInTheDocument();
  });
});
