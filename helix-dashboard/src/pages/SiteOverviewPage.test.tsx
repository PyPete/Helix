import { describe, it, expect, beforeEach, afterEach, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { SiteOverviewPage } from "./SiteOverviewPage";
import type { FumehoodStatus } from "../types/dashboard";

const mockFumehoods: FumehoodStatus[] = [
  {
    id: "fh-001",
    name: "Fumehood fh-001",
    status: "running",
    activeExperimentName: undefined,
    lastSeenAt: new Date().toISOString(),
  },
  {
    id: "fh-002",
    name: "Fumehood fh-002",
    status: "faulted",
    activeExperimentName: undefined,
    lastSeenAt: new Date().toISOString(),
  },
];

describe("SiteOverviewPage", () => {
  beforeEach(() => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve(mockFumehoods),
      } as Response),
    );
  });

  afterEach(() => vi.restoreAllMocks());

  it("renders with correct testid", () => {
    render(<SiteOverviewPage baseUrl="http://localhost:5090" />);
    expect(screen.getByTestId("site-overview-page")).toBeInTheDocument();
  });

  it("has accessible aria-label on section", () => {
    render(<SiteOverviewPage baseUrl="http://localhost:5090" />);
    expect(screen.getByRole("region", { name: /site overview/i })).toBeInTheDocument();
  });

  it("renders fumehood cards from API", async () => {
    render(<SiteOverviewPage baseUrl="http://localhost:5090" />);
    await waitFor(() =>
      expect(screen.getAllByTestId("fumehood-card").length).toBeGreaterThanOrEqual(1),
    );
  });

  it("shows a running status badge", async () => {
    render(<SiteOverviewPage baseUrl="http://localhost:5090" />);
    await waitFor(() => {
      const badges = screen.getAllByTestId("fumehood-status");
      expect(badges.some((el) => el.textContent === "running")).toBe(true);
    });
  });

  it("shows a faulted status badge", async () => {
    render(<SiteOverviewPage baseUrl="http://localhost:5090" />);
    await waitFor(() => {
      const badges = screen.getAllByTestId("fumehood-status");
      expect(badges.some((el) => el.textContent === "faulted")).toBe(true);
    });
  });

  it("renders active-count showing number of running fumehoods", async () => {
    render(<SiteOverviewPage baseUrl="http://localhost:5090" />);
    await waitFor(() => {
      const activeCount = screen.getByTestId("active-count");
      expect(Number(activeCount.textContent)).toBeGreaterThanOrEqual(1);
    });
  });
});
