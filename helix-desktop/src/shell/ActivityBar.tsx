import './ActivityBar.css';

export interface ActivityBarProps {
  activePanel: string | null;
  onPanelToggle: (panelId: string) => void;
}

interface ActivityBarItem {
  id: string;
  icon: string;
  title: string;
}

const TOP_ITEMS: ActivityBarItem[] = [
  { id: 'explorer', icon: '⬡', title: 'Device Explorer' },
  { id: 'trends', icon: '📈', title: 'Trend Explorer' },
];

const BOTTOM_ITEMS: ActivityBarItem[] = [
  { id: 'settings', icon: '⚙', title: 'Settings' },
];

export function ActivityBar({ activePanel, onPanelToggle }: ActivityBarProps) {
  return (
    <nav className="actbar" aria-label="Activity Bar" data-testid="activity-bar">
      <div className="actbar-top">
        {TOP_ITEMS.map((item) => (
          <button
            key={item.id}
            className={`abtn${activePanel === item.id ? ' active' : ''}`}
            title={item.title}
            data-testid={`actbar-${item.id}`}
            onClick={() => onPanelToggle(item.id)}
            aria-pressed={activePanel === item.id}
          >
            {item.icon}
          </button>
        ))}
      </div>
      <div className="actbar-bottom">
        {BOTTOM_ITEMS.map((item) => (
          <button
            key={item.id}
            className={`abtn${activePanel === item.id ? ' active' : ''}`}
            title={item.title}
            data-testid={`actbar-${item.id}`}
            onClick={() => onPanelToggle(item.id)}
            aria-pressed={activePanel === item.id}
          >
            {item.icon}
          </button>
        ))}
      </div>
    </nav>
  );
}
