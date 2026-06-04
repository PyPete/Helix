import './shell.css';
import { useState, useRef } from 'react';
import { DEFAULT_CONNECTION } from '../types/agent';
import type { AgentConnection, WslDocument } from '../types/agent';
import { useAgentHealth } from '../hooks/useAgentHealth';
import { useAgentWebSocket } from '../hooks/useAgentWebSocket';
import { useWorkspaceLayouts } from '../hooks/useWorkspaceLayouts';
import ConnectionConfig from '../components/ConnectionConfig';
import { Ribbon } from './Ribbon';
import type { RibbonTab } from './Ribbon';
import { ActivityBar } from './ActivityBar';
import { StatusBar } from './StatusBar';
import { WorkspaceLayout } from './WorkspaceLayout';
import type { WorkspaceLayoutHandle } from './WorkspaceLayout';

/**
 * HelixShell — composition root for the Helix IDE shell (Wave 1).
 * Replaces App.tsx as the top-level component when mounted.
 * App.tsx is not modified in Wave 1 — HelixShell is standalone.
 */
export default function HelixShell() {
  // --- Connection & health ---
  const [connection, setConnection] = useState<AgentConnection>(DEFAULT_CONNECTION);
  const baseUrl = `${connection.protocol}://${connection.host}:${connection.port}`;
  const health = useAgentHealth(baseUrl);

  // --- WebSocket data stream ---
  const { runspaceEvents, trendValues, safetyEvents, connected } = useAgentWebSocket(baseUrl);

  // --- Derived state ---
  const currentStatus = runspaceEvents[runspaceEvents.length - 1]?.type ?? null;
  const isRunning =
    currentStatus === 'runspace-started' || currentStatus === 'step-started';
  const isPaused = currentStatus === 'runspace-paused';

  // --- Workflow state ---
  const [currentWorkflow, setCurrentWorkflow] = useState<WslDocument | null>(null);

  // --- Shell UI state ---
  const [activeRibbonTab, setActiveRibbonTab] = useState<RibbonTab>('Home');
  const [activePanel, setActivePanel] = useState<string | null>(null);
  const [savedLayoutNames, setSavedLayoutNames] = useState<string[]>([]);

  // --- Workspace layout persistence ---
  const { saveLayout, loadLayouts, listLayoutNames } = useWorkspaceLayouts();

  // --- DockLayout handle ---
  const workspaceRef = useRef<WorkspaceLayoutHandle>(null);

  // --- Execution handlers (follows ExecutionPanel.tsx fetch pattern) ---
  const handleRun = async () => {
    await fetch(`${baseUrl}/api/runspaces/start`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(currentWorkflow ?? {}),
    });
  };

  const handlePause = async () => {
    await fetch(`${baseUrl}/api/runspaces/pause`, { method: 'POST' });
  };

  const handleAbort = async () => {
    await fetch(`${baseUrl}/api/runspaces/abort`, { method: 'POST' });
  };

  // --- Layout handlers ---
  const handleSaveLayout = async (name: string) => {
    if (!name.trim()) return;
    const layout = workspaceRef.current?.saveLayout();
    if (layout) {
      await saveLayout(name, layout);
      const names = await listLayoutNames();
      setSavedLayoutNames(names);
    }
  };

  const handleLoadLayout = async (name: string) => {
    if (!name) return;
    const layouts = await loadLayouts();
    const lb = layouts[name];
    if (lb) {
      workspaceRef.current?.loadLayout(lb);
    }
  };

  const handleOpenChartPanel = () => {
    workspaceRef.current?.openChartPanel('new-chart-' + Date.now());
  };

  // Map activity bar button ids → dock tab ids
  const ACTIVITY_BAR_TAB_MAP: Record<string, string> = {
    explorer: 'device-explorer',
    trends:   'trend-explorer',
  };

  const handlePanelToggle = (panelId: string) => {
    setActivePanel((prev) => (prev === panelId ? null : panelId));
    // Navigate the dock to the corresponding tab (if mapped)
    const dockTabId = ACTIVITY_BAR_TAB_MAP[panelId];
    if (dockTabId) {
      workspaceRef.current?.activateTab(dockTabId);
    }
    // Settings button switches the ribbon to the Settings tab instead
    if (panelId === 'settings') {
      setActiveRibbonTab('Settings');
    }
  };

  return (
    <div className="helix-shell" data-testid="helix-shell">
      <div className="titlebar">Helix Desktop</div>

      <Ribbon
        activeTab={activeRibbonTab}
        onTabChange={setActiveRibbonTab}
        onRunWorkflow={handleRun}
        onPauseWorkflow={handlePause}
        onAbortWorkflow={handleAbort}
        onOpenChartPanel={handleOpenChartPanel}
        onSaveLayout={handleSaveLayout}
        onLoadLayout={handleLoadLayout}
        savedLayoutNames={savedLayoutNames}
        isRunning={isRunning}
        isPaused={isPaused}
      />

      <div className="workspace">
        <ActivityBar activePanel={activePanel} onPanelToggle={handlePanelToggle} />
        <WorkspaceLayout
          ref={workspaceRef}
          baseUrl={baseUrl}
          runspaceEvents={runspaceEvents}
          trendValues={trendValues}
          safetyEvents={safetyEvents}
          currentWorkflow={currentWorkflow}
          onWorkflowExport={setCurrentWorkflow}
        />
      </div>

      {activeRibbonTab === 'Settings' && (
        <div className="settings-overlay" data-testid="settings-overlay">
          <ConnectionConfig connection={connection} onSave={setConnection} />
        </div>
      )}

      <StatusBar
        connected={connected}
        healthy={health.healthy}
        agentUrl={baseUrl}
      />
    </div>
  );
}

