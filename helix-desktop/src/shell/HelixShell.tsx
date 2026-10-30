import './shell.css';
import { useState, useRef, useEffect } from 'react';
import { DEFAULT_CONNECTION } from '../types/agent';
import type { AgentConnection, WslDocument } from '../types/agent';
import { type AgentSessionStatus, controlAgentExperiment } from '../lib/agentClient';
import { useAgentHealth } from '../hooks/useAgentHealth';
import { useAgentWebSocket } from '../hooks/useAgentWebSocket';
import { useWorkspaceLayouts } from '../hooks/useWorkspaceLayouts';
import ConnectionConfig from '../components/ConnectionConfig';
import { AgentSession } from '../components/AgentSession/AgentSession';
import { Ribbon } from './Ribbon';
import type { RibbonTab } from './Ribbon';
import { ActivityBar } from './ActivityBar';
import { StatusBar } from './StatusBar';
import { WorkspaceLayout } from './WorkspaceLayout';
import type { WorkspaceLayoutHandle } from './WorkspaceLayout';

/** The desktop shell composes untrusted views; all lifecycle mutations remain native-Agent requests. */
export default function HelixShell() {
  const [connection, setConnection] = useState<AgentConnection>(DEFAULT_CONNECTION);
  const baseUrl = `${connection.protocol}://${connection.host}:${connection.port}`;
  const health = useAgentHealth(baseUrl);
  const { runspaceEvents, trendValues, safetyEvents, experimentEvents, activeExperimentId, connected } = useAgentWebSocket(baseUrl);
  const [currentWorkflow, setCurrentWorkflow] = useState<WslDocument | null>(null);
  const [activeRibbonTab, setActiveRibbonTab] = useState<RibbonTab>('Home');
  const [activePanel, setActivePanel] = useState<string | null>(null);
  const [savedLayoutNames, setSavedLayoutNames] = useState<string[]>([]);
  const [session, setSession] = useState<AgentSessionStatus | null>(null);
  const [selectedExecutionId, setSelectedExecutionId] = useState<string | null>(null);
  const [durableLifecycle, setDurableLifecycle] = useState<string | null>(null);
  const [shellError, setShellError] = useState<string | null>(null);
  const [pausePending, setPausePending] = useState(false);
  const [abortConfirmationOpen, setAbortConfirmationOpen] = useState(false);
  const { saveLayout, loadLayouts, listLayoutNames } = useWorkspaceLayouts();
  const workspaceRef = useRef<WorkspaceLayoutHandle>(null);

  useEffect(() => {
    void listLayoutNames().then(setSavedLayoutNames).catch(() => {});
  // listLayoutNames is a stable function reference from the hook
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Keep the last Agent execution selected after its live stream becomes terminal so its timeline stays reachable.
  useEffect(() => {
    if (activeExperimentId) setSelectedExecutionId(activeExperimentId);
  }, [activeExperimentId]);

  useEffect(() => {
    const event = [...runspaceEvents].reverse().find((candidate) => candidate.experimentId === selectedExecutionId);
    if (!event) return;
    const lifecycleByEvent: Record<string, string> = {
      'runspace-started': 'Running',
      'runspace-paused': 'Paused',
      'runspace-completed': 'Completed',
      'runspace-aborted': 'Aborted',
      'runspace-faulted': 'Held',
    };
    const lifecycle = lifecycleByEvent[event.type];
    if (lifecycle) setDurableLifecycle(lifecycle);
  }, [runspaceEvents, selectedExecutionId]);

  const isRunning = durableLifecycle === 'Running';
  const isPaused = durableLifecycle === 'Paused' || durableLifecycle === 'Held';
  const canControl = session?.authenticated === true && selectedExecutionId !== null && (isRunning || isPaused);
  const timelineExperimentId = activeExperimentId ?? selectedExecutionId;

  function showExecutionPanel() {
    workspaceRef.current?.activateTab('execution-panel');
  }

  // A run cannot originate from this shell button: it must include the editable, approved policy in ExecutionPanel.
  function handleRun() {
    showExecutionPanel();
  }

  async function handlePause() {
    if (!selectedExecutionId || !canControl) return;
    setPausePending(true);
    setShellError(null);
    try {
      const outcome = await controlAgentExperiment(selectedExecutionId, 'pause');
      if (outcome.uncertain) {
        setDurableLifecycle('Held');
        setShellError('Outcome not confirmed. This command is held pending reconciliation; do not retry it.');
      } else if (outcome.result) setDurableLifecycle(outcome.result.state);
    } finally {
      setPausePending(false);
    }
  }

  function handleAbort() {
    if (canControl) setAbortConfirmationOpen(true);
  }

  async function confirmAbort() {
    if (!selectedExecutionId || !canControl) return;
    setShellError(null);
    try {
      const outcome = await controlAgentExperiment(selectedExecutionId, 'abort');
      if (outcome.uncertain) {
        setDurableLifecycle('Held');
        setShellError('Outcome not confirmed. This command is held pending reconciliation; do not retry it.');
      } else if (outcome.result) setDurableLifecycle(outcome.result.state);
    } finally {
      setAbortConfirmationOpen(false);
    }
  }

  const handleSaveLayout = async (name: string) => {
    if (!name.trim()) return;
    const layout = workspaceRef.current?.saveLayout();
    if (layout) {
      await saveLayout(name, layout);
      setSavedLayoutNames(await listLayoutNames());
    }
  };

  const handleLoadLayout = async (name: string) => {
    if (!name) return;
    const layout = (await loadLayouts())[name];
    if (layout) workspaceRef.current?.loadLayout(layout);
  };

  const handleOpenChartPanel = () => workspaceRef.current?.openChartPanel('new-chart-' + Date.now());
  const handleOpenManualControl = () => workspaceRef.current?.openDeviceTab('simulator');
  const activityBarTabMap: Record<string, string> = { explorer: 'device-explorer', trends: 'trend-explorer' };
  const handlePanelToggle = (panelId: string) => {
    setActivePanel((previous) => (previous === panelId ? null : panelId));
    const dockTabId = activityBarTabMap[panelId];
    if (dockTabId) workspaceRef.current?.activateTab(dockTabId);
    if (panelId === 'settings') setActiveRibbonTab('Settings');
  };

  return (
    <div className="helix-shell" data-testid="helix-shell">
      <div className="titlebar">Helix Desktop</div>
      <AgentSession onStatusChange={setSession} />
      {shellError && <p role="alert" className="shell-error">{shellError}</p>}
      <Ribbon
        activeTab={activeRibbonTab}
        onTabChange={setActiveRibbonTab}
        onRunWorkflow={handleRun}
        onPauseWorkflow={() => void handlePause()}
        onAbortWorkflow={handleAbort}
        onOpenChartPanel={handleOpenManualControl}
        onSaveLayout={(name) => void handleSaveLayout(name)}
        onLoadLayout={(name) => void handleLoadLayout(name)}
        savedLayoutNames={savedLayoutNames}
        isRunning={isRunning || pausePending}
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
          experimentEvents={experimentEvents}
          activeExperimentId={timelineExperimentId}
          currentWorkflow={currentWorkflow}
          onWorkflowExport={setCurrentWorkflow}
        />
      </div>
      {abortConfirmationOpen && <div role="dialog" aria-modal="true" aria-labelledby="shell-abort-title"><h2 id="shell-abort-title">Abort simulation</h2><p>Abort this run? No new commands will start. An in-flight action may already have completed; Helix will record its observed outcome and will not replay it.</p><button onClick={() => void confirmAbort()}>Abort simulation</button><button onClick={() => setAbortConfirmationOpen(false)}>Keep running</button></div>}
      {activeRibbonTab === 'Settings' && <div className="settings-overlay" data-testid="settings-overlay"><ConnectionConfig connection={connection} onSave={setConnection} /></div>}
      <StatusBar connected={connected} healthy={health.healthy} agentUrl={baseUrl} />
    </div>
  );
}
