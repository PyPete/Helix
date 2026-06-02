import { Routes, Route } from "react-router";
import { Layout } from "./components/Layout";
import { useTrendWebSocket } from "./hooks/useTrendWebSocket";
import { SiteOverviewPage } from "./pages/SiteOverviewPage";
import { CampaignsPage } from "./pages/CampaignsPage";
import { HistoryPage } from "./pages/HistoryPage";
import { TrendsPage } from "./pages/TrendsPage";
import { AnalyticsPage } from "./pages/AnalyticsPage";

export function App() {
  const baseUrl = import.meta.env.VITE_SITE_SERVER_URL ?? "http://localhost:5000";
  const { connected, trendValues } = useTrendWebSocket(baseUrl);

  return (
    <Routes>
      <Route element={<Layout connected={connected} />}>
        <Route index element={<SiteOverviewPage />} />
        <Route path="campaigns" element={<CampaignsPage baseUrl={baseUrl} />} />
        <Route path="history" element={<HistoryPage baseUrl={baseUrl} />} />
        <Route path="trends" element={<TrendsPage trendValues={trendValues} />} />
        <Route path="analytics" element={<AnalyticsPage />} />
      </Route>
    </Routes>
  );
}
