import { describe, it, expect, beforeEach, afterEach, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { CampaignsPage } from "./CampaignsPage";

const BASE = "http://localhost:5000";

describe("CampaignsPage", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({
      ok: true,
      json: async () => [{ campaignId: "c-001", name: "Campaign Alpha", status: "Idle" }],
    }));
  });
  afterEach(() => vi.restoreAllMocks());

  it("renders with correct testid", () => {
    render(<CampaignsPage baseUrl={BASE} />);
    expect(screen.getByTestId("campaigns-page")).toBeInTheDocument();
  });

  it("loads and displays campaigns", async () => {
    render(<CampaignsPage baseUrl={BASE} />);
    await waitFor(() => expect(screen.getByText("Campaign Alpha")).toBeInTheDocument());
  });

  it("renders name input and create button", () => {
    render(<CampaignsPage baseUrl={BASE} />);
    expect(screen.getByTestId("campaign-name-input")).toBeInTheDocument();
    expect(screen.getByTestId("create-campaign-btn")).toBeInTheDocument();
  });

  it("calls POST /api/campaigns on create", async () => {
    const user = userEvent.setup();
    render(<CampaignsPage baseUrl={BASE} />);
    await user.type(screen.getByTestId("campaign-name-input"), "New Run");
    await user.click(screen.getByTestId("create-campaign-btn"));
    await waitFor(() =>
      expect(vi.mocked(fetch)).toHaveBeenCalledWith(
        expect.stringContaining("/api/campaigns"),
        expect.objectContaining({ method: "POST" })
      )
    );
  });

  it("shows error alert on fetch failure", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({
      ok: false, statusText: "Server Error",
    }));
    render(<CampaignsPage baseUrl={BASE} />);
    await waitFor(() => expect(screen.getByRole("alert")).toBeInTheDocument());
  });
});
