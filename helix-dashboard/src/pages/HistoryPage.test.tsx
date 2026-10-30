import { describe, it, expect, beforeEach, afterEach, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { HistoryPage } from "./HistoryPage";

const BASE = "http://localhost:5000";

const MOCK = [
  { runspaceId: "r-001", name: "Vapourtec Run 1",
    status: "Completed", createdAt: "2026-06-01T10:00:00Z" },
  { runspaceId: "r-002", name: "Knauer HPLC Calibration",
    status: "Running",   createdAt: "2026-06-02T08:00:00Z" },
];

describe("HistoryPage", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({
      ok: true, json: async () => MOCK,
    }));
  });
  afterEach(() => vi.restoreAllMocks());

  it("renders with correct testid", () => {
    render(<HistoryPage baseUrl={BASE} />);
    expect(screen.getByTestId("history-page")).toBeInTheDocument();
  });

  it("displays 2 experiment rows", async () => {
    render(<HistoryPage baseUrl={BASE} />);
    await waitFor(() =>
      expect(screen.getAllByTestId("experiment-row")).toHaveLength(2)
    );
  });

  it("shows experiment name", async () => {
    render(<HistoryPage baseUrl={BASE} />);
    await waitFor(() =>
      expect(screen.getByText("Vapourtec Run 1")).toBeInTheDocument()
    );
  });

  it("shows empty state when no experiments", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({
      ok: true, json: async () => [],
    }));
    render(<HistoryPage baseUrl={BASE} />);
    await waitFor(() =>
      expect(screen.getByText(/no experiments/i)).toBeInTheDocument()
    );
  });

  it("shows error alert on fetch failure", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({
      ok: false, statusText: "Internal Server Error",
    }));
    render(<HistoryPage baseUrl={BASE} />);
    await waitFor(() => expect(screen.getByRole("alert")).toBeInTheDocument());
  });
});
