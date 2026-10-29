import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import { AgentSession } from "./AgentSession";
const { getAgentSessionStatus, bootstrapAgentSession, logoutAgentSession } = vi.hoisted(() => ({ getAgentSessionStatus: vi.fn(), bootstrapAgentSession: vi.fn(), logoutAgentSession: vi.fn() }));
vi.mock("../../lib/agentClient", () => ({ getAgentSessionStatus, bootstrapAgentSession, logoutAgentSession }));
describe("AgentSession", () => {
  beforeEach(() => { vi.clearAllMocks(); });
  it("displays only the verified scientist from native session status", async () => { getAgentSessionStatus.mockResolvedValue({ authenticated: true, verifiedScientist: "Dr. Long Scientist Identity", expiresAt: null, restored: true, restorationError: null }); render(<AgentSession />); expect(await screen.findByText("Session: ", { exact: false })).toHaveTextContent("Dr. Long Scientist Identity"); expect(screen.getByText("Local Agent — Connected")).toBeInTheDocument(); });
  it("shows safe restoration error without a credential", async () => { getAgentSessionStatus.mockRejectedValue(new Error("offline")); bootstrapAgentSession.mockRejectedValue(new Error("offline")); render(<AgentSession />); expect(await screen.findByRole("alert")).toHaveTextContent("could not verify your desktop session"); });
  it("logs out through the native client", async () => { getAgentSessionStatus.mockResolvedValue({ authenticated: true, verifiedScientist: "Scientist", expiresAt: null, restored: false, restorationError: null }); logoutAgentSession.mockResolvedValue(undefined); render(<AgentSession />); fireEvent.click(await screen.findByRole("button", { name: "Sign out" })); await waitFor(() => expect(logoutAgentSession).toHaveBeenCalled()); });
});
