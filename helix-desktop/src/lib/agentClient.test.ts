import { beforeEach, describe, expect, it, vi } from "vitest";
import { act, render, renderHook, screen, waitFor } from "@testing-library/react";
import { Fragment, createElement } from "react";

const { invokeMock } = vi.hoisted(() => ({ invokeMock: vi.fn() }));

vi.mock("@tauri-apps/api/core", () => ({
  invoke: invokeMock,
}));

import {
  controlAgentExperiment,
  getAgentDeviceCapabilities,
  getAgentDevices,
  getAgentSessionStatus,
  pollCommittedEvidence,
  proposeAgentCommand,
  startAgentExperiment,
} from "./agentClient";
import { useExperiment } from "../hooks/useExperiment";
import DeviceRegistry from "../components/DeviceRegistry";
import { DeviceExplorer } from "../shell/panels/DeviceExplorer";
import { DevicePalette } from "../components/WorkflowBuilder/DevicePalette";
import { WorkflowBuilder } from "../components/WorkflowBuilder/WorkflowBuilder";

const commandId = "11111111-1111-4111-8111-111111111111";
const executionId = "22222222-2222-4222-8222-222222222222";
const correlationId = "33333333-3333-4333-8333-333333333333";

describe("agentClient", () => {
  beforeEach(() => {
    invokeMock.mockReset();
    vi.stubGlobal("ResizeObserver", class {
      observe() { /* React Flow test layout stub */ }
      unobserve() { /* React Flow test layout stub */ }
      disconnect() { /* React Flow test layout stub */ }
    });
  });

  it("maps canonical proposals through the native allowlist without actor or destination arguments", async () => {
    invokeMock.mockResolvedValueOnce(JSON.stringify({
      commandId,
      state: "Allowed",
      sequence: 1,
      checks: [],
    }));

    await proposeAgentCommand({
      commandId,
      executionId,
      correlationId,
      deviceId: "simulator",
      capability: "SetFlow",
      canonicalParameters: JSON.stringify({ flowRate: 1 }),
      policyRevision: "simulation-policy-v1",
    });

    expect(invokeMock).toHaveBeenCalledWith("agent_propose", {
      proposal: expect.stringContaining('"capability":"SetFlow"'),
    });
    expect(invokeMock.mock.calls[0][1].proposal).not.toContain("actor");
    expect(invokeMock.mock.calls[0][1].proposal).not.toContain("http");
  });

  it("rejects noncanonical capability identifiers before native invocation", async () => {
    await expect(proposeAgentCommand({
      commandId,
      executionId,
      correlationId,
      deviceId: "simulator",
      capability: "SetPumpMode" as "SetFlow",
      canonicalParameters: "{}",
      policyRevision: "simulation-policy-v1",
    })).rejects.toThrow("canonical Agent capability");
    expect(invokeMock).not.toHaveBeenCalled();
  });

  it("returns only redacted session identity and visible restoration failures", async () => {
    invokeMock.mockResolvedValueOnce(JSON.stringify({
      authenticated: false,
      verifiedScientist: null,
      expiresAt: null,
      restored: false,
      restorationError: "The local Agent session expired.",
    }));

    await expect(getAgentSessionStatus()).resolves.toEqual(expect.objectContaining({
      authenticated: false,
      restorationError: "The local Agent session expired.",
    }));
    expect(invokeMock).toHaveBeenCalledWith("agent_session_status", {});
  });

  it("retains a stable operation id and holds an experiment mutation with a lost acknowledgement", async () => {
    const operationId = "44444444-4444-4444-8444-444444444444";
    invokeMock.mockRejectedValueOnce(new Error("pipe closed"));

    await expect(startAgentExperiment("A run", operationId)).resolves.toMatchObject({
      operationId,
      state: "Held",
      uncertain: true,
    });
    expect(invokeMock).toHaveBeenCalledTimes(1);
    expect(invokeMock).toHaveBeenCalledWith("agent_experiment_start", expect.objectContaining({
      request: expect.stringContaining(operationId),
    }));
  });

  it("does not retry a control mutation after its acknowledgement is lost", async () => {
    invokeMock.mockRejectedValueOnce(new Error("connection reset"));

    const outcome = await controlAgentExperiment(commandId, "abort", "55555555-5555-4555-8555-555555555555");

    expect(outcome).toMatchObject({ state: "Held", uncertain: true });
    expect(invokeMock).toHaveBeenCalledTimes(1);
  });

  it("polls committed cursor pages through native invocation and stops at the committed cursor", async () => {
    invokeMock.mockResolvedValueOnce(JSON.stringify({ cursor: "1", nextCursor: null, records: [] }));
    const pages = [];
    for await (const page of pollCommittedEvidence({ cursor: "1", pageSize: 2 })) pages.push(page);

    expect(pages).toHaveLength(1);
    expect(invokeMock).toHaveBeenCalledWith("agent_evidence_cursor", { cursor: "1", pageSize: 2 });
  });

  it("reads device and capability discovery through the native broker", async () => {
    invokeMock
      .mockResolvedValueOnce(JSON.stringify([{ adapterId: "simulator", capabilities: ["SetFlow"] }]))
      .mockResolvedValueOnce(JSON.stringify({ adapterId: "simulator", capabilities: [] }));

    await expect(getAgentDevices()).resolves.toEqual([{ adapterId: "simulator", capabilities: ["SetFlow"] }]);
    await expect(getAgentDeviceCapabilities("simulator")).resolves.toMatchObject({ adapterId: "simulator" });
    expect(invokeMock).toHaveBeenNthCalledWith(1, "agent_devices", {});
    expect(invokeMock).toHaveBeenNthCalledWith(2, "agent_device_capabilities", { deviceId: "simulator" });
  });

  it("renders registry, explorer, and palette from native simulator discovery", async () => {
    invokeMock.mockImplementation((command: string) => {
      if (command === "agent_devices") {
        return Promise.resolve(JSON.stringify([{ adapterId: "simulator", capabilities: ["SetFlow"] }]));
      }
      if (command === "agent_device_capabilities") {
        return Promise.resolve(JSON.stringify({
          adapterId: "simulator",
          capabilities: [{ verbName: "SetFlow", displayName: "Set flow", description: "", category: "Control", parameters: [], isDestructive: false, requiresActiveRunspace: false }],
        }));
      }
      return Promise.reject(new Error(`Unexpected command ${command}`));
    });

    render(createElement(Fragment, null,
      createElement(DeviceRegistry, { agentBaseUrl: "https://untrusted.invalid" }),
      createElement(DeviceExplorer, { baseUrl: "https://untrusted.invalid", onOpenDevice: () => undefined }),
      createElement(DevicePalette, { baseUrl: "https://untrusted.invalid", onDragStart: () => undefined }),
    ));

    await waitFor(() => expect(screen.getAllByText("simulator")).not.toHaveLength(0));
    expect(await screen.findByTestId("capability-card-simulator-SetFlow")).toBeTruthy();
    expect(invokeMock.mock.calls.some(([command]) => command === "agent_devices")).toBe(true);
    expect(invokeMock.mock.calls.some(([command]) => command === "agent_device_capabilities")).toBe(true);
  });

  it("renders native discovery failures without treating them as permission", async () => {
    invokeMock.mockRejectedValue(new Error("No desktop Agent session is available."));
    render(createElement(DeviceExplorer, { baseUrl: "https://untrusted.invalid", onOpenDevice: () => undefined }));

    expect(await screen.findByRole("alert")).toHaveTextContent("No desktop Agent session is available.");
  });

  it("renders the workflow builder's simulator capability picker from native discovery", async () => {
    invokeMock.mockImplementation((command: string) => {
      if (command === "agent_devices") {
        return Promise.resolve(JSON.stringify([{ adapterId: "simulator", capabilities: ["SetFlow"] }]));
      }
      if (command === "agent_device_capabilities") {
        return Promise.resolve(JSON.stringify({ adapterId: "simulator", capabilities: [] }));
      }
      if (command === "agent_evidence_cursor") {
        return Promise.resolve(JSON.stringify({ cursor: null, nextCursor: null, records: [] }));
      }
      return Promise.reject(new Error(`Unexpected command ${command}`));
    });
    render(createElement(WorkflowBuilder, { baseUrl: "https://untrusted.invalid", onExport: () => undefined }));

    await waitFor(() => expect(screen.getByTestId("capability-picker")).toBeTruthy());
    expect(screen.getByText("simulator: SetFlow")).toBeTruthy();
  });

  it("uses native authoritative experiment status after a start without using a renderer URL", async () => {
    const experimentId = "66666666-6666-4666-8666-666666666666";
    invokeMock
      .mockResolvedValueOnce(JSON.stringify({
        experimentId,
        name: "Native run",
        lifecycle: "Running",
        activePhase: "Preparing",
        activeRunspaceId: null,
      }))
      .mockResolvedValueOnce(JSON.stringify({
        experimentId,
        name: "Native run",
        lifecycle: "Completed",
        activePhase: null,
        activeRunspaceId: null,
      }));
    const { result } = renderHook(() => useExperiment("https://untrusted.invalid"));

    let startedId: string | null = null;
    await act(async () => { startedId = await result.current.startExperiment("Native run"); });

    expect(startedId).toBe(experimentId);
    await waitFor(() => expect(result.current.isRunning).toBe(false));
    expect(invokeMock.mock.calls.map(([command]) => command)).toEqual([
      "agent_experiment_start",
      "agent_experiment_status",
    ]);
  });

  it("keeps selection and reports the stable id when a hook mutation acknowledgement is lost", async () => {
    const experimentId = "77777777-7777-4777-8777-777777777777";
    invokeMock
      .mockResolvedValueOnce(JSON.stringify({
        experimentId,
        name: "Native run",
        lifecycle: "Running",
        activePhase: "Preparing",
        activeRunspaceId: null,
      }))
      .mockResolvedValueOnce(JSON.stringify({
        experimentId,
        name: "Native run",
        lifecycle: "Running",
        activePhase: "Preparing",
        activeRunspaceId: null,
      }))
      .mockRejectedValueOnce(new Error("acknowledgement lost"));
    const { result } = renderHook(() => useExperiment());

    await act(async () => { await result.current.startExperiment("Native run"); });
    await act(async () => { await result.current.abortExperiment(); });

    expect(result.current.experimentId).toBe(experimentId);
    expect(result.current.error).toContain("will not be retried automatically");
    expect(invokeMock.mock.calls.filter(([command]) => command === "agent_experiment_control")).toHaveLength(1);
  });
});
