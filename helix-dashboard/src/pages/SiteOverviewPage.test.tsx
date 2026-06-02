import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { SiteOverviewPage } from "./SiteOverviewPage";

describe("SiteOverviewPage", () => {
  it("renders with correct testid", () => {
    render(<SiteOverviewPage />);
    expect(screen.getByTestId("site-overview-page")).toBeInTheDocument();
  });

  it("renders at least 3 fumehood cards", () => {
    render(<SiteOverviewPage />);
    expect(screen.getAllByTestId("fumehood-card").length).toBeGreaterThanOrEqual(3);
  });

  it("shows a running status badge", () => {
    render(<SiteOverviewPage />);
    const badges = screen.getAllByTestId("fumehood-status");
    expect(badges.some((el) => el.textContent === "running")).toBe(true);
  });

  it("shows a faulted status badge", () => {
    render(<SiteOverviewPage />);
    const badges = screen.getAllByTestId("fumehood-status");
    expect(badges.some((el) => el.textContent === "faulted")).toBe(true);
  });

  it("has accessible aria-label on section", () => {
    render(<SiteOverviewPage />);
    expect(screen.getByRole("region", { name: /site overview/i })).toBeInTheDocument();
  });

  it("renders active-count showing number of running fumehoods", () => {
    render(<SiteOverviewPage />);
    const activeCount = screen.getByTestId("active-count");
    expect(activeCount).toBeInTheDocument();
    expect(Number(activeCount.textContent)).toBeGreaterThanOrEqual(1);
  });
});
