import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";

const sample = { identity: { sampleId: "sample-immutable-id", origin: "Collection" as const, parentSampleIds: [] }, label: "Collected sample", location: "Hood A", serverSequence: 1, lastEventId: "event-1" };

vi.mock("../../lib/scientificClient", () => ({
  listSamples: vi.fn().mockResolvedValue([]), sampleEvents: vi.fn().mockResolvedValue([]),
  confirmSample: vi.fn(), appendSampleEvent: vi.fn(), readSampleOperation: vi.fn(), sampleLineage: vi.fn().mockResolvedValue({ entries: [], isTruncated: false }),
}));

import { Samples } from "./Samples";
import { listSamples, sampleEvents } from "../../lib/scientificClient";

describe("Samples", () => {
  it("does not infer sample collection from a planned reference", async () => {
    render(<Samples />);
    expect(await screen.findByText("No confirmed samples yet. A planned step is not a sample. Confirm actual collection to create an immutable sample record.")).toBeInTheDocument();
    expect(screen.getByText("Planned reference only — collection not confirmed.")).toBeInTheDocument();
  });

  it("requires an explicit confirmation dialog before collection", async () => {
    render(<Samples />);
    await userEvent.click(screen.getByRole("button", { name: "Confirm sample collection" }));
    expect(screen.getByRole("dialog")).toHaveTextContent("This records a new immutable sample fact");
    expect(screen.getByLabelText("Occurrence time")).toBeRequired();
    expect(screen.getByLabelText("Location")).toBeRequired();
  });

  it("requires complete parent-to-activity-to-child evidence before recording a transformation", async () => {
    render(<Samples />);
    await userEvent.click(screen.getByRole("button", { name: "Record transformation" }));
    expect(screen.getByRole("dialog")).toHaveTextContent("Created by transformation");
    expect(screen.getByLabelText("Operation description")).toBeRequired();
    expect(screen.getByLabelText("Parent sample IDs")).toBeRequired();
    expect(screen.getByLabelText("New child labels")).toBeRequired();
  });

  it("keeps a selected sample's exact immutable identity available through accessible evidence controls", async () => {
    vi.mocked(listSamples).mockResolvedValueOnce([sample]);
    vi.mocked(sampleEvents).mockResolvedValueOnce([]);
    render(<Samples selectedId="sample-immutable-id" />);

    expect(await screen.findByLabelText("Sample evidence")).toHaveTextContent("sample-immutable-id");
    expect(screen.getByRole("button", { name: "View full ID" })).toBeInTheDocument();
  });
});
