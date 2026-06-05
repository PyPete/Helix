import '../styles/helix-dock.css';

import React, { useRef, useImperativeHandle, useMemo, useCallback, useEffect } from 'react';
import DockLayout from 'rc-dock';
import type { LayoutBase, LayoutData, TabData, TabBase, TabGroup } from 'rc-dock';
import type { RunspaceEvent, TrendValue, SafetyEvent, WslDocument } from '../types/agent';
import { useTrendRegistry } from '../hooks/useTrendRegistry';

// Panel components
import { DeviceExplorer } from './panels/DeviceExplorer';
import { TrendExplorer } from './panels/TrendExplorer';
import { ChartPanel } from './panels/ChartPanel';
import { LogOutput } from './panels/LogOutput';

// Main-area components
import { WorkflowBuilder } from '../components/WorkflowBuilder/WorkflowBuilder';
import { ExecutionPanel } from '../components/ExecutionPanel/ExecutionPanel';
import { SafetyPanel } from '../components/SafetyPanel/SafetyPanel';
import DeviceManualControl from '../components/DeviceManualControl/DeviceManualControl';

// ---------------------------------------------------------------------------
// Props + Handle
// ---------------------------------------------------------------------------

export interface WorkspaceLayoutProps {
  baseUrl: string;
  onSaveLayout?: (name: string) => Promise<void>;
  onLoadLayout?: (name: string) => Promise<void>;
  // passed to panels:
  runspaceEvents: RunspaceEvent[];
  trendValues: TrendValue[];
  safetyEvents: SafetyEvent[];
  currentWorkflow: WslDocument | null;
  onWorkflowExport: (doc: WslDocument) => void;
}

export interface WorkspaceLayoutHandle {
  saveLayout(): LayoutBase;
  loadLayout(lb: LayoutBase): void;
  openChartPanel(trendName: string): void;
  openDeviceTab(adapterId: string): void;
  activateTab(tabId: string): void;
}

// ---------------------------------------------------------------------------
// DEFAULT_LAYOUT â€” 3-column structure (per D-02 + RESEARCH.md Pattern 2)
// ---------------------------------------------------------------------------

// LayoutBase is sufficient for defaultLayout â€” rc-dock materializes BoxBase â†’ BoxData internally.
// We pass it as LayoutData (which the prop requires) using a type assertion on assignment.
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const DEFAULT_LAYOUT: LayoutBase = {
  dockbox: {
    mode: 'horizontal',
    children: [
      {
        // Left sidebar: Device Explorer
        mode: 'vertical',
        size: 220,
        children: [
          {
            id: 'panel-left',
            tabs: [{ id: 'device-explorer' }],
            activeId: 'device-explorer',
          },
        ],
      },
      {
        // Center: main tabs + bottom output
        mode: 'vertical',
        size: 600,
        children: [
          {
            id: 'panel-main',
            tabs: [{ id: 'workflow-editor' }, { id: 'execution-panel' }],
            activeId: 'workflow-editor',
          },
          {
            id: 'panel-bottom',
            size: 140,
            tabs: [{ id: 'output' }, { id: 'safety-log' }],
            activeId: 'output',
          },
        ],
      },
      {
        // Right sidebar: Trend Explorer + default Chart Panel
        mode: 'vertical',
        size: 200,
        children: [
          {
            id: 'panel-right-top',
            tabs: [{ id: 'trend-explorer' }],
            activeId: 'trend-explorer',
          },
          {
            id: 'panel-right-bottom',
            tabs: [{ id: 'chart-0' }],
            activeId: 'chart-0',
          },
        ],
      },
    ],
  },
};

// ---------------------------------------------------------------------------
// PANEL_GROUPS
// ---------------------------------------------------------------------------

const PANEL_GROUPS: Record<string, TabGroup> = {
  sidebar: {
    floatable: false,
    maximizable: false,
    tabLocked: false,
  },
  main: {
    floatable: true,
    maximizable: true,
    newWindow: false,
  },
};

// ---------------------------------------------------------------------------
// WorkspaceLayout component
// ---------------------------------------------------------------------------

export const WorkspaceLayout = React.forwardRef<WorkspaceLayoutHandle, WorkspaceLayoutProps>(
  (props, ref) => {
    const dockRef = useRef<DockLayout>(null);

    // Accumulate trend names from live trendValues stream (D-07)
    const trendNames = useTrendRegistry(props.trendValues);

    // openDeviceTab: creates a DeviceManualControl tab and docks it in panel-main
    const openDeviceTab = useCallback(
      (adapterId: string) => {
        const id = `device-control-${adapterId}`;
        const newTab: TabData = {
          id,
          title: `${adapterId}`,
          content: <DeviceManualControl deviceId={adapterId} baseUrl={props.baseUrl} />,
          group: 'main',
          closable: true,
        };
        dockRef.current?.dockMove(newTab, 'panel-main', 'middle');
      },
      [props.baseUrl]
    );

    // tabRegistry: memoized factory map for all registered tab ids.
    // Rebuilds when any prop that affects panel content changes.
    const tabRegistry = useMemo(
      () => ({
        'device-explorer': (): TabData => ({
          id: 'device-explorer',
          title: 'Devices',
          content: (
            <DeviceExplorer baseUrl={props.baseUrl} onOpenDevice={openDeviceTab} />
          ),
          group: 'sidebar',
          closable: false,
        }),
        'trend-explorer': (): TabData => ({
          id: 'trend-explorer',
          title: 'Trends',
          content: <TrendExplorer trendNames={trendNames} />,
          group: 'sidebar',
          closable: false,
        }),
        'workflow-editor': (): TabData => ({
          id: 'workflow-editor',
          title: 'Workflow Builder',
          content: (
            <WorkflowBuilder
              baseUrl={props.baseUrl}
              onExport={props.onWorkflowExport}
            />
          ),
          group: 'main',
          closable: true,
        }),
        'execution-panel': (): TabData => ({
          id: 'execution-panel',
          title: 'Execution',
          content: (
            <ExecutionPanel
              baseUrl={props.baseUrl}
              definition={props.currentWorkflow}
              runspaceEvents={props.runspaceEvents}
            />
          ),
          group: 'main',
          closable: true,
        }),
        'output': (): TabData => ({
          id: 'output',
          title: 'Output',
          content: (
            <LogOutput
              runspaceEvents={props.runspaceEvents}
              safetyEvents={props.safetyEvents}
            />
          ),
          group: 'main',
          closable: false,
        }),
        'safety-log': (): TabData => ({
          id: 'safety-log',
          title: 'Safety Log',
          content: <SafetyPanel safetyEvents={props.safetyEvents} />,
          group: 'main',
          closable: false,
        }),
        'chart-0': (): TabData => ({
          id: 'chart-0',
          title: 'Chart',
          content: (
            <ChartPanel
              initialTrendName={undefined}
              trendValues={props.trendValues}
            />
          ),
          group: 'main',
          closable: false,
        }),
      }),
      [
        props.baseUrl,
        props.currentWorkflow,
        props.runspaceEvents,
        props.safetyEvents,
        props.onWorkflowExport,
        props.trendValues,
        trendNames,
        openDeviceTab,
      ]
    );

    // Push updated streaming content into already-docked tabs.
    // rc-dock stores TabData.content in its own state at mount time and doesn't
    // re-call loadTab when parent props change — updateTab is the only way to refresh.
    useEffect(() => {
      if (!dockRef.current) return;
      dockRef.current.updateTab('output', tabRegistry['output'](), false);
      dockRef.current.updateTab('safety-log', tabRegistry['safety-log'](), false);
      dockRef.current.updateTab('execution-panel', tabRegistry['execution-panel'](), false);
      dockRef.current.updateTab('chart-0', tabRegistry['chart-0'](), false);
    }, [tabRegistry]);

    useImperativeHandle(ref, () => ({
      saveLayout(): LayoutBase {
        if (!dockRef.current) throw new Error('DockLayout ref not attached');
        return dockRef.current.saveLayout();
      },
      loadLayout(lb: LayoutBase): void {
        dockRef.current?.loadLayout(lb);
      },
      openChartPanel(trendName: string): void {
        const id = trendName ? `chart-${trendName}` : `chart-${Date.now()}`;
        const newTab: TabData = {
          id,
          title: `${trendName ?? 'Chart'}`,
          content: (
            <ChartPanel
              initialTrendName={trendName || undefined}
              trendValues={props.trendValues}
            />
          ),
          group: 'main',
          closable: true,
        };
        dockRef.current?.dockMove(newTab, 'panel-main', 'middle');
      },
      openDeviceTab,
      activateTab(tabId: string): void {
        dockRef.current?.updateTab(tabId, null, true);
      },
    }));

    // saveTab: persist only the id (content is not JSON serializable)
    function saveTab(tab: TabData): TabBase {
      return { id: tab.id };
    }

    // loadTab: use memoized tabRegistry; handle dynamic chart-* and device-control-* ids
    function loadTab(tab: TabBase): TabData {
      const tabId = tab.id ?? 'unknown';

      const factory = tabRegistry[tabId as keyof typeof tabRegistry];
      if (factory) {
        return factory();
      }

      // Dynamic chart panels: id = "chart-{trendName}"
      if (tabId.startsWith('chart-')) {
        const trendName = tabId.slice(6);
        return {
          id: tabId,
          title: `${trendName}`,
          content: (
            <ChartPanel
              initialTrendName={trendName}
              trendValues={props.trendValues}
            />
          ),
          group: 'main',
          closable: true,
        };
      }

      // Dynamic device control panels: id = "device-control-{adapterId}"
      if (tabId.startsWith('device-control-')) {
        const adapterId = tabId.slice('device-control-'.length);
        return {
          id: tabId,
          title: `${adapterId}`,
          content: (
            <DeviceManualControl deviceId={adapterId} baseUrl={props.baseUrl} />
          ),
          group: 'main',
          closable: true,
        };
      }

      // Safe placeholder for unrecognised tab ids â€” no eval, no dangerouslySetInnerHTML
      return {
        id: tabId,
        title: tabId,
        content: <div data-testid="unknown-panel">Unknown panel: {tabId}</div>,
      };
    }

    useEffect(() => {
      const id = 'helix-dock-styles';
      const existing = document.getElementById(id);
      if (existing) existing.remove();
      const el = document.createElement('style');
      el.id = id;
      el.textContent = [
        /* panel body */
        '.dock-panel { background: #252540 !important; color: #cdd6f4 !important; border: 1px solid #5a5aaa !important; border-radius: 3px !important; }',
        /* tab bar — slightly darker than panel body */
        '.dock-bar, .dock-top .dock-bar { background: #1a1a35 !important; border-bottom: 1px solid #5a5aaa !important; padding-left: 4px !important; }',
        '.dock-tab { background: transparent !important; color: #7e8ba8 !important; }',
        '.dock-tab > div { color: inherit !important; padding: 4px 12px !important; }',
        '.dock-tab:hover { color: #cdd6f4 !important; background: #2e2e50 !important; }',
        '.dock-tab-active > div, .dock-tab-active:hover > div { color: #cdd6f4 !important; }',
        '.dock-tab-active { background: #252540 !important; border-top: 2px solid #9d7fff !important; }',
        '.dock-ink-bar { display: none !important; }',
        '.dock-tabpane, .dock-content, .dock-content-holder { background: #252540 !important; color: #cdd6f4 !important; }',
        '.dock-divider { background: #3a3a70 !important; flex: 0 0 4px !important; }',
        '.dock-divider:hover { background: #9d7fff !important; }',
        '.dock-tab-close-btn { color: #7e8ba8 !important; }',
        '.dock-panel-max-btn { color: #7e8ba8 !important; }',
      ].join('\n');
      document.head.appendChild(el);

    }, []);

    return (
      <div style={{ flex: 1, minWidth: 0, position: 'relative', overflow: 'hidden' }}>
        <DockLayout
          ref={dockRef}
          defaultLayout={DEFAULT_LAYOUT as LayoutData}
          groups={PANEL_GROUPS}
          saveTab={saveTab}
          loadTab={loadTab}
          style={{ position: 'absolute', inset: 0 }}
          data-testid="dock-layout"
        />
      </div>
    );
  }
);

WorkspaceLayout.displayName = 'WorkspaceLayout';






