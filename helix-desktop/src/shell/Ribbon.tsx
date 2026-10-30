import './Ribbon.css';
import { useState } from 'react';

export type RibbonTab = 'Home' | 'Devices' | 'Trends' | 'Workflows' | 'Charts' | 'Developer' | 'Settings';

export interface RibbonProps {
  activeTab: RibbonTab;
  onTabChange: (tab: RibbonTab) => void;
  onRunWorkflow: () => void;
  onPauseWorkflow: () => void;
  onAbortWorkflow: () => void;
  onOpenChartPanel: () => void;
  onSaveLayout: (name: string) => void;
  onLoadLayout: (name: string) => void;
  savedLayoutNames: string[];
  isRunning: boolean;
  isPaused: boolean;
}

const RIBBON_TABS: RibbonTab[] = [
  'Home',
  'Devices',
  'Trends',
  'Workflows',
  'Charts',
  'Developer',
  'Settings',
];

export function Ribbon({
  activeTab,
  onTabChange,
  onRunWorkflow,
  onPauseWorkflow,
  onAbortWorkflow,
  onOpenChartPanel,
  onSaveLayout,
  onLoadLayout,
  savedLayoutNames,
  isRunning,
  isPaused,
}: RibbonProps) {
  const [layoutName, setLayoutName] = useState('');
  const [loadSelectValue, setLoadSelectValue] = useState('');

  function renderBand() {
    switch (activeTab) {
      case 'Home':
        return (
          <div className="ribbon-band" data-testid="ribbon-band-home">
            <div className="ribbon-group">
              <div className="ribbon-group-buttons">
                <button
                  className="ribbon-btn"
                  data-testid="ribbon-run-btn"
                  disabled={isRunning}
                  onClick={onRunWorkflow}
                >
                  Run
                </button>
                <button
                  className="ribbon-btn"
                  data-testid="ribbon-pause-btn"
                  disabled={!isRunning || isPaused}
                  onClick={onPauseWorkflow}
                >
                  Pause
                </button>
                <button
                  className="ribbon-btn"
                  data-testid="ribbon-abort-btn"
                  disabled={!isRunning}
                  onClick={onAbortWorkflow}
                >
                  Abort
                </button>
              </div>
              <span className="ribbon-group-label">Execution</span>
            </div>
            <div className="ribbon-group">
              <div className="ribbon-group-buttons">
                <input
                  className="ribbon-input"
                  placeholder="Layout name"
                  data-testid="layout-name-input"
                  value={layoutName}
                  onChange={(e) => setLayoutName(e.target.value)}
                />
                <button
                  className="ribbon-btn"
                  data-testid="ribbon-save-layout-btn"
                  onClick={() => onSaveLayout(layoutName)}
                >
                  Save
                </button>
                <select
                  className="ribbon-select"
                  data-testid="layout-load-select"
                  value={loadSelectValue}
                  onChange={(e) => {
                    setLoadSelectValue(e.target.value);
                    if (e.target.value) onLoadLayout(e.target.value);
                  }}
                >
                  <option value="">-- load layout --</option>
                  {savedLayoutNames.map((name) => (
                    <option key={name} value={name}>
                      {name}
                    </option>
                  ))}
                </select>
              </div>
              <span className="ribbon-group-label">Workspace</span>
            </div>
          </div>
        );

      case 'Devices':
        return (
          <div className="ribbon-band" data-testid="ribbon-band-devices">
            <div className="ribbon-group">
              <div className="ribbon-group-buttons">
                <button
                  className="ribbon-btn"
                  data-testid="ribbon-open-devices-btn"
                  onClick={onOpenChartPanel}
                >
                  Open Manual Control
                </button>
              </div>
              <span className="ribbon-group-label">Devices</span>
            </div>
          </div>
        );

      default: {
        const lowerTab = activeTab.toLowerCase();
        return (
          <div
            className="ribbon-band ribbon-band--empty"
            data-testid={`ribbon-band-${lowerTab}`}
          >
            <span className="ribbon-band-coming-soon">â€” coming soon â€”</span>
          </div>
        );
      }
    }
  }

  return (
    <>
      <div className="ribbon-tabs" role="tablist" aria-label="Ribbon">
        {RIBBON_TABS.map((tab) => (
          <button
            key={tab}
            role="tab"
            aria-selected={tab === activeTab}
            className={`ribbon-tab${tab === activeTab ? ' active' : ''}`}
            data-testid={`ribbon-tab-${tab.toLowerCase()}`}
            onClick={() => onTabChange(tab)}
          >
            {tab}
          </button>
        ))}
      </div>
      {renderBand()}
    </>
  );
}

