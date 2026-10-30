import { Outlet } from "react-router";
import { Navbar } from "./Navbar";

interface LayoutProps { connected?: boolean; }

export function Layout({ connected }: LayoutProps) {
  return (
    <div data-testid="app-layout" style={{
      minHeight: "100vh",
      background: "var(--color-bg)",
      color: "var(--color-text)",
      fontFamily: "var(--font-sans)",
    }}>
      <Navbar connected={connected} />
      <main style={{ padding: "var(--space-4)" }}><Outlet /></main>
    </div>
  );
}
