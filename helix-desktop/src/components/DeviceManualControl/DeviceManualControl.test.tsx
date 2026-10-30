import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import DeviceManualControl from "./DeviceManualControl";
const { proposeAgentCommand, approveAgentPolicy, dispatchAgentCommand, readAgentEvidence } = vi.hoisted(() => ({ proposeAgentCommand: vi.fn(), approveAgentPolicy: vi.fn(), dispatchAgentCommand: vi.fn(), readAgentEvidence: vi.fn() }));
vi.mock("../../lib/agentClient", () => ({ bootstrapAgentSession: vi.fn().mockResolvedValue({ authenticated: true }), proposeAgentCommand, approveAgentPolicy, dispatchAgentCommand, readAgentEvidence }));
describe("DeviceManualControl", () => {
  beforeEach(() => { vi.clearAllMocks(); proposeAgentCommand.mockResolvedValue({ state: "Allowed" }); dispatchAgentCommand.mockResolvedValue({ state: "Dispatched" }); readAgentEvidence.mockResolvedValue({ intent: { commandId: "id" }, evidence: { state: "Completed" } }); });
  it("offers only approved simulator capabilities and submits a typed proposal", async () => { render(<DeviceManualControl deviceId="simulator" baseUrl="ignored" />); await waitFor(() => expect(screen.getByRole("button", { name: "Set Flow" })).toBeEnabled()); expect(screen.getByRole("option", { name: "SetMode" })).toBeInTheDocument(); fireEvent.click(screen.getByRole("button", { name: "Set Flow" })); await waitFor(() => expect(proposeAgentCommand).toHaveBeenCalled()); expect(proposeAgentCommand.mock.calls[0][0]).toEqual(expect.objectContaining({ deviceId: "simulator", capability: "SetFlow", policyRevision: "simulation-policy-v1" })); });
  it("does not dispatch a held proposal", async () => { proposeAgentCommand.mockResolvedValue({ state: "Held" }); render(<DeviceManualControl deviceId="simulator" baseUrl="ignored" />); await waitFor(() => expect(screen.getByRole("button", { name: "Set Flow" })).toBeEnabled()); fireEvent.click(screen.getByRole("button", { name: "Set Flow" })); await waitFor(() => expect(screen.getByRole("alert")).toHaveTextContent("Held for scientist review")); expect(dispatchAgentCommand).not.toHaveBeenCalled(); });

  it("shows native string rejection details while redacting credentials", async () => {
    proposeAgentCommand.mockRejectedValueOnce("Native command rejected: token=secret-token password: \"secret-password\"");
    render(<DeviceManualControl deviceId="simulator" baseUrl="ignored" />);
    await waitFor(() => expect(screen.getByRole("button", { name: "Set Flow" })).toBeEnabled());
    fireEvent.click(screen.getByRole("button", { name: "Set Flow" }));

    await waitFor(() => expect(screen.getByText("Native command rejected: token=[REDACTED] password: [REDACTED]")).toBeInTheDocument());
    expect(screen.queryByText(/secret-token|secret-password/)).not.toBeInTheDocument();
    expect(approveAgentPolicy).not.toHaveBeenCalled();
    expect(dispatchAgentCommand).not.toHaveBeenCalled();
  });

  it.each([{}, "", "   "]) ("uses the generic fallback for unknown or empty rejection values (%s)", async (reason) => {
    proposeAgentCommand.mockRejectedValueOnce(reason);
    render(<DeviceManualControl deviceId="simulator" baseUrl="ignored" />);
    await waitFor(() => expect(screen.getByRole("button", { name: "Set Flow" })).toBeEnabled());
    fireEvent.click(screen.getByRole("button", { name: "Set Flow" }));

    await waitFor(() => expect(screen.getByText("Agent command failed.")).toBeInTheDocument());
  });
});
