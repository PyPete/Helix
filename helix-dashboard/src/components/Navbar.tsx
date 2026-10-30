import { NavLink } from "react-router";

interface NavbarProps { connected?: boolean; }

export function Navbar({ connected = false }: NavbarProps) {
  return (
    <nav role="navigation" aria-label="Site navigation"
      style={{
        background: "var(--color-ribbon-bg)",
        borderBottom: "1px solid var(--color-border)",
        padding: "0 var(--space-4)",
        display: "flex", alignItems: "center",
        gap: "var(--space-4)", height: "40px",
      }}
    >
      <span style={{
        color: "var(--color-primary)", fontWeight: 600,
        fontSize: "var(--text-lg)", marginRight: "var(--space-6)",
      }}>Helix</span>
      <NavLink to="/">Overview</NavLink>
      <NavLink to="/campaigns">Campaigns</NavLink>
      <NavLink to="/history">History</NavLink>
      <NavLink to="/trends">Trends</NavLink>
      <NavLink to="/analytics">Analytics</NavLink>
      <span data-testid="ws-status" style={{
        marginLeft: "auto", fontSize: "var(--text-xs)",
        color: connected ? "var(--color-online)" : "var(--color-offline)",
      }}>
        {connected ? "Live" : "Offline"}
      </span>
    </nav>
  );
}
