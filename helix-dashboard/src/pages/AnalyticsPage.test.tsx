import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { AnalyticsPage } from "./AnalyticsPage";

describe("AnalyticsPage", () => {
  it("renders without crash", () => {
    render(
      <MemoryRouter>
        <AnalyticsPage />
      </MemoryRouter>
    );
    expect(screen.getByTestId("analytics-page")).toBeInTheDocument();
  });

  it("renders two SciChartLine placeholders", () => {
    render(
      <MemoryRouter>
        <AnalyticsPage />
      </MemoryRouter>
    );
    const charts = screen.getAllByTestId("scichart-surface");
    expect(charts).toHaveLength(2);
  });

  it("shows placeholder text", () => {
    render(
      <MemoryRouter>
        <AnalyticsPage />
      </MemoryRouter>
    );
    expect(screen.getAllByText(/placeholder/i).length).toBeGreaterThanOrEqual(1);
  });
});
