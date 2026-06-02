import { Routes, Route } from "react-router";
import { Layout } from "./components/Layout";
import { useTrendWebSocket } from "./hooks/useTrendWebSocket";

// Wave 2 placeholders — replaced one-by-one as tasks 07-2-* complete
const SiteOverviewPage = () => <div data-testid="site-overview-page">Overview (placeholder)</div>;
const CampaignsPage = () => <div data-testid="campaigns-page">Campaigns (placeholder)</div>;
const HistoryPage = () => <div data-testid="history-page">History (placeholder)</div>;
const TrendsPage = () => <div data-testid="trends-page">Trends (placeholder)</div>;
const AnalyticsPage = () => <div data-testid="analytics-page">Analytics (placeholder)</div>;

export function App() {
  const baseUrl = import.meta.env.VITE_SITE_SERVER_URL ?? "http://localhost:5000";
  const { connected } = useTrendWebSocket(baseUrl);

  return (
    <Routes>
      <Route element={<Layout connected={connected} />}>
        <Route index element={<SiteOverviewPage />} />
        <Route path="campaigns" element={<CampaignsPage />} />
        <Route path="history" element={<HistoryPage />} />
        <Route path="trends" element={<TrendsPage />} />
        <Route path="analytics" element={<AnalyticsPage />} />
      </Route>
    </Routes>
  );
}
