import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import ConnectionConfig from "./ConnectionConfig";
import type { AgentConnection } from "../types/agent";

const { bootstrapSessionMock, sessionStatusMock } = vi.hoisted(() => ({
  bootstrapSessionMock: vi.fn(),
  sessionStatusMock: vi.fn(),
}));

vi.mock("../lib/agentClient", () => ({
  bootstrapAgentSession: bootstrapSessionMock,
  getAgentSessionStatus: sessionStatusMock,
}));

const localConnection: AgentConnection = { host: "127.0.0.1", port: 5080, protocol: "http" };

const verifiedSession = {
  authenticated: true,
  verifiedScientist: "scientist-with-a-long-verified-windows-identity@example.invalid",
  expiresAt: "2026-10-29T10:15:00Z",
  restored: true,
  restorationError: null,
};

describe("ConnectionConfig", () => {
  beforeEach(() => {
    bootstrapSessionMock.mockReset();
    sessionStatusMock.mockReset();
    sessionStatusMock.mockResolvedValue(verifiedSession);
    bootstrapSessionMock.mockResolvedValue(verifiedSession);
  });

  it("shows the immutable local native broker target without editable destination fields", () => {
    render(<ConnectionConfig connection={localConnection} onSave={vi.fn()} />);

    expect(screen.getByText("Native broker target: 127.0.0.1:5080")).toBeInTheDocument();
    expect(screen.queryByRole("textbox")).not.toBeInTheDocument();
    expect(screen.queryByRole("spinbutton")).not.toBeInTheDocument();
  });

  it("makes a legacy remote setting visible but ignores it", () => {
    render(
      <ConnectionConfig
        connection={{ host: "remote.example", port: 443, protocol: "https" }}
        onSave={vi.fn()}
      />,
    );

    expect(screen.getByRole("alert")).toHaveTextContent("legacy remote connection setting was ignored");
  });

  it("shows the verified scientist, expiry, and restoration errors without a credential", async () => {
    render(<ConnectionConfig connection={localConnection} onSave={vi.fn()} />);

    expect(await screen.findByLabelText("Verified local session")).toHaveTextContent(verifiedSession.verifiedScientist);
    expect(screen.getByText(`Session expiry: ${verifiedSession.expiresAt}`)).toBeInTheDocument();
    expect(screen.queryByText(/Bearer|token/i)).not.toBeInTheDocument();
  });

  it("shows a session inspection failure as actionable local status", async () => {
    sessionStatusMock.mockRejectedValueOnce(new Error("Unable to establish the local desktop session."));
    render(<ConnectionConfig connection={localConnection} onSave={vi.fn()} />);

    expect(await screen.findByRole("alert")).toHaveTextContent("Unable to establish the local desktop session.");
  });

  it("only restores the fixed local destination", async () => {
    const onSave = vi.fn();
    render(
      <ConnectionConfig
        connection={{ host: "remote.example", port: 443, protocol: "https" }}
        onSave={onSave}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Restore local Agent session" }));
    await waitFor(() => expect(onSave).toHaveBeenCalledWith(localConnection));
  });
});
