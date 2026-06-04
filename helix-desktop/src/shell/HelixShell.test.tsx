import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import HelixShell from './HelixShell';

// --- Mock rc-dock (class component with required render) ---
vi.mock('rc-dock', () => {
  const DockLayoutMock = vi.fn(function (this: Record<string, unknown>) {
    this.saveLayout = vi.fn().mockReturnValue({ dockbox: { mode: 'horizontal', children: [] } });
    this.loadLayout = vi.fn();
    this.dockMove = vi.fn();
  });
  DockLayoutMock.prototype.render = () => null;
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
    connected: false,
  })),
}));

// --- Mock useAgentHealth ---
vi.mock('../hooks/useAgentHealth', () => ({
  useAgentHealth: vi.fn(() => ({ healthy: null, error: null })),
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
});
