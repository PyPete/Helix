import 'rc-dock/dist/rc-dock.css';
import '../styles/helix-dock.css';

import React, { useRef, useImperativeHandle, useMemo, useCallback } from 'react';
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
        // Right sidebar: Trend Explorer
        mode: 'vertical',
        size: 200,
        children: [
          {
            id: 'panel-right',
            tabs: [{ id: 'trend-explorer' }],
            activeId: 'trend-explorer',
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
      }),
      [
        props.baseUrl,
        props.currentWorkflow,
        props.runspaceEvents,
        props.safetyEvents,
        props.onWorkflowExport,
        trendNames,
        openDeviceTab,
      ]
    );

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

    return (
      <DockLayout
        ref={dockRef}
        // LayoutBase is compatible with LayoutData for defaultLayout (rc-dock materialises it)
        defaultLayout={DEFAULT_LAYOUT as LayoutData}
        groups={PANEL_GROUPS}
        saveTab={saveTab}
        loadTab={loadTab}
        style={{ flex: '1 1 0', overflow: 'hidden', minHeight: 0 }}
        data-testid="dock-layout"
      />
    );
  }
);

WorkspaceLayout.displayName = 'WorkspaceLayout';


