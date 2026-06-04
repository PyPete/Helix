import './panels.css';

export interface TrendExplorerProps {
  trendNames: string[];
}

export function TrendExplorer({ trendNames }: TrendExplorerProps) {
  function handleDragStart(name: string) {
    return (e: React.DragEvent<HTMLDivElement>) => {
      e.dataTransfer.setData('text/x-trend-name', name);
      e.dataTransfer.effectAllowed = 'copy';
    };
  }

  return (
    <section aria-label="Trend Explorer" data-testid="trend-explorer">
      {trendNames.length === 0 ? (
        <p className="panel-empty">No trends yet</p>
      ) : (
        <ul className="trend-list">
          {trendNames.map(name => (
            <li key={name}>
              <div
                draggable
                onDragStart={handleDragStart(name)}
                className="trend-item"
                data-testid={`trend-item-${name}`}
              >
                {name}
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
