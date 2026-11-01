import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";

vi.mock("../../lib/scientificClient", () => ({
  listSamples: vi.fn().mockResolvedValue([]), sampleEvents: vi.fn().mockResolvedValue([]),
  confirmSample: vi.fn(), appendSampleEvent: vi.fn(), readSampleOperation: vi.fn(),
}));

import { Samples } from "./Samples";

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
});
