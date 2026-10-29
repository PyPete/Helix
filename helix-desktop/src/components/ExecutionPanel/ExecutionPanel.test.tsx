import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import { ExecutionPanel } from "./ExecutionPanel";

const { submitSimulationWorkflow, controlAgentExperiment } = vi.hoisted(() => ({ submitSimulationWorkflow: vi.fn(), controlAgentExperiment: vi.fn() }));
vi.mock("../../lib/agentClient", () => ({
  bootstrapAgentSession: vi.fn().mockResolvedValue({ authenticated: true }),
  submitSimulationWorkflow,
  controlAgentExperiment,
}));

const definition = { documentId: "workflow-1", name: "Simulation", schemaVersion: "1.0", nodes: [{ nodeId: "step-1", label: "Set flow", nodeType: "DeviceAction", parameters: {} }], edges: [] };
const response = { executionId: "11111111-1111-4111-8111-111111111111", runtimeAdmission: { state: "Dispatched", checks: ["Capability", "Safety", "Lease", "Resource", "Budget", "Termination", "Device health"].map((check) => ({ check, status: "Passed", code: "OK" })) } };

describe("ExecutionPanel", () => {
  beforeEach(() => { vi.clearAllMocks(); submitSimulationWorkflow.mockResolvedValue(response); controlAgentExperiment.mockResolvedValue({ uncertain: false }); });
  it("submits the nonempty original WSL and approved policy only after explicit approval", async () => {
    render(<ExecutionPanel baseUrl="ignored" definition={definition} runspaceEvents={[]} />);
    await waitFor(() => expect(screen.getByTestId("start-btn")).toBeDisabled());
    fireEvent.click(screen.getByLabelText(/I approve this frozen/i));
    await waitFor(() => expect(screen.getByTestId("start-btn")).toBeEnabled());
    fireEvent.click(screen.getByTestId("start-btn"));
    await waitFor(() => expect(submitSimulationWorkflow).toHaveBeenCalledTimes(1));
    expect(submitSimulationWorkflow.mock.calls[0][0]).toEqual(expect.objectContaining({ originalWsl: definition, definitionReference: "workflow-1", policy: expect.objectContaining({ approved: true, revision: "simulation-policy-v1" }) }));
  });
  it("shows missing admission evidence as Unknown and blocks a retry", async () => {
    submitSimulationWorkflow.mockResolvedValueOnce({ executionId: "11111111-1111-4111-8111-111111111111", runtimeAdmission: { state: "Held", checks: [] } });
    render(<ExecutionPanel baseUrl="ignored" definition={definition} runspaceEvents={[]} />);
    expect(screen.getAllByText("Unknown")).toHaveLength(7);
    fireEvent.click(screen.getByLabelText(/I approve this frozen/i));
    await waitFor(() => expect(screen.getByTestId("start-btn")).toBeEnabled());
    fireEvent.click(screen.getByTestId("start-btn"));
    await waitFor(() => expect(submitSimulationWorkflow).toHaveBeenCalledTimes(1));
    expect(screen.getByTestId("start-btn")).toBeDisabled();
  });
  it("keeps Abort pending until the explicit confirmation is accepted", async () => {
    render(<ExecutionPanel baseUrl="ignored" definition={definition} runspaceEvents={[{ type: "runspace-started", runspaceId: "run", timestamp: "now", experimentId: "11111111-1111-4111-8111-111111111111" }]} />);
    await waitFor(() => expect(screen.getByRole("button", { name: "Abort" })).toBeEnabled());
    fireEvent.click(screen.getByRole("button", { name: "Abort" }));
    expect(screen.getByRole("dialog")).toHaveTextContent("An in-flight action may already have completed");
    expect(controlAgentExperiment).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: "Abort simulation" }));
    await waitFor(() => expect(controlAgentExperiment).toHaveBeenCalledWith("11111111-1111-4111-8111-111111111111", "abort"));
  });
});
