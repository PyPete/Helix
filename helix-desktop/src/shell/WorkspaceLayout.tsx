import 'rc-dock/dist/rc-dock.css';
import '../styles/helix-dock.css';

import React, { useRef, useImperativeHandle } from 'react';
import DockLayout from 'rc-dock';
import type { LayoutBase, LayoutData, TabData, TabBase, TabGroup } from 'rc-dock';
import type { RunspaceEvent, TrendValue, SafetyEvent, WslDocument } from '../types/agent';

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
}

// ---------------------------------------------------------------------------
// DEFAULT_LAYOUT — 3-column structure (per D-02 + RESEARCH.md Pattern 2)
// ---------------------------------------------------------------------------

// LayoutBase is sufficient for defaultLayout — rc-dock materializes BoxBase → BoxData internally.
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
// TAB_REGISTRY — Wave 1 stubs (Plan 03 replaces with real panel imports)
// Factory signature: (props: WorkspaceLayoutProps) => TabData
// ---------------------------------------------------------------------------

const TAB_REGISTRY: Record<string, (props: WorkspaceLayoutProps) => TabData> = {
  'device-explorer': (_props) => ({
    id: 'device-explorer',
    title: 'Devices',
    content: <div data-testid="device-explorer-stub" />,
    group: 'sidebar',
    closable: false,
  }),
  'trend-explorer': (_props) => ({
    id: 'trend-explorer',
    title: 'Trends',
    content: <div data-testid="trend-explorer-stub" />,
    group: 'sidebar',
    closable: false,
  }),
  'workflow-editor': (_props) => ({
    id: 'workflow-editor',
    title: 'Workflow Builder',
    content: <div data-testid="workflow-editor-stub" />,
    group: 'main',
    closable: true,
  }),
  'execution-panel': (_props) => ({
    id: 'execution-panel',
    title: 'Execution',
    content: <div data-testid="execution-panel-stub" />,
    group: 'main',
    closable: true,
  }),
  'output': (_props) => ({
    id: 'output',
    title: 'Output',
    content: <div data-testid="output-stub" />,
    group: 'main',
    closable: false,
  }),
  'safety-log': (_props) => ({
    id: 'safety-log',
    title: 'Safety Log',
    content: <div data-testid="safety-log-stub" />,
    group: 'main',
    closable: false,
  }),
};

// ---------------------------------------------------------------------------
// WorkspaceLayout component
// ---------------------------------------------------------------------------

export const WorkspaceLayout = React.forwardRef<WorkspaceLayoutHandle, WorkspaceLayoutProps>(
  (props, ref) => {
    const dockRef = useRef<DockLayout>(null);

    useImperativeHandle(ref, () => ({
      saveLayout(): LayoutBase {
        if (!dockRef.current) throw new Error('DockLayout ref not attached');
        return dockRef.current.saveLayout();
      },
      loadLayout(lb: LayoutBase): void {
        dockRef.current?.loadLayout(lb);
      },
      openChartPanel(trendName: string): void {
        const id = `chart-${trendName}`;
        const newTab: TabData = {
          id,
          title: `${trendName}`,
          content: <div data-testid={`chart-panel-${trendName}-stub`} />,
          group: 'main',
          closable: true,
        };
        dockRef.current?.dockMove(newTab, 'panel-main', 'middle');
      },
    }));

    // saveTab: persist only the id (content is not JSON serializable)
    function saveTab(tab: TabData): TabBase {
      return { id: tab.id };
    }

    // loadTab: look up TAB_REGISTRY; return fallback for unknown ids (T-11-05 mitigation)
    function loadTab(tab: TabBase): TabData {
      const tabId = tab.id ?? 'unknown';
      const factory = TAB_REGISTRY[tabId];
      if (factory) {
        return factory(props);
      }
      // Safe placeholder for unrecognised tab ids — no eval, no dangerouslySetInnerHTML
      return {
        id: tabId,
        title: tabId,
        content: <div>Unknown panel: {tabId}</div>,
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
