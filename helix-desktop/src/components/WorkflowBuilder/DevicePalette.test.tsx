import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import { describe, it, expect, vi, afterEach } from "vitest";
import { DevicePalette } from "./DevicePalette";
import type { CapabilityDescriptor } from "../../types/agent";

const setFlowDescriptor: CapabilityDescriptor = {
  verbName: "SetFlow",
  displayName: "Set Flow",
  description: "Sets the flow rate",
  category: "Control",
  parameters: [
    {
      name: "flowRate",
      displayName: "Flow Rate",
      type: "Numeric",
      unit: "mL/min",
      min: 0,
      max: 500,
      step: 0.1,
    },
  ],
  isDestructive: false,
  requiresActiveRunspace: false,
};

function makeFetchMock() {
  return vi.fn((url: string) => {
    if (url.endsWith("/devices")) {
      return Promise.resolve({
        ok: true,
        json: () => Promise.resolve([{ adapterId: "Alicat", capabilities: ["SetFlow"] }]),
      } as unknown as Response);
    }
    if (url.includes("/devices/Alicat/capabilities")) {
      return Promise.resolve({
        ok: true,
        json: () =>
          Promise.resolve({
            adapterId: "Alicat",
            capabilities: [setFlowDescriptor],
          }),
      } as unknown as Response);
    }
    return Promise.reject(new Error(`Unexpected fetch: ${url}`));
  });
}

afterEach(() => {
  vi.restoreAllMocks();
  vi.clearAllMocks();
});

describe("DevicePalette", () => {
  it("renders palette-group-Alicat details element after fetch resolves", async () => {
    vi.stubGlobal("fetch", makeFetchMock());
    render(<DevicePalette baseUrl="http://localhost:5080" onDragStart={vi.fn()} />);

    await waitFor(() =>
      expect(screen.getByTestId("palette-group-Alicat")).toBeInTheDocument()
    );
  });

  it("capability-card element is draggable", async () => {
    vi.stubGlobal("fetch", makeFetchMock());
    render(<DevicePalette baseUrl="http://localhost:5080" onDragStart={vi.fn()} />);

    await waitFor(() =>
      expect(screen.getByTestId("capability-card-Alicat-SetFlow")).toBeInTheDocument()
    );

    const card = screen.getByTestId("capability-card-Alicat-SetFlow");
    expect(card).toHaveAttribute("draggable", "true");
  });

  it("firing dragstart on a card calls dataTransfer.setData with application/helix-capability key", async () => {
    vi.stubGlobal("fetch", makeFetchMock());
    render(<DevicePalette baseUrl="http://localhost:5080" onDragStart={vi.fn()} />);

    await waitFor(() =>
      expect(screen.getByTestId("capability-card-Alicat-SetFlow")).toBeInTheDocument()
    );

    const card = screen.getByTestId("capability-card-Alicat-SetFlow");
    const setDataSpy = vi.fn();
    fireEvent.dragStart(card, {
      dataTransfer: { setData: setDataSpy, effectAllowed: "" },
    });

    expect(setDataSpy).toHaveBeenCalledWith(
      "application/helix-capability",
      expect.stringContaining("SetFlow")
    );
  });

  it("shows Failed to load devices message when fetch errors", async () => {
    vi.stubGlobal("fetch", vi.fn(() => Promise.reject(new Error("Network error"))));
    render(<DevicePalette baseUrl="http://localhost:5080" onDragStart={vi.fn()} />);

    await waitFor(() =>
      expect(screen.getByText("Failed to load devices")).toBeInTheDocument()
    );
  });
});
