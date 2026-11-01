import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

vi.mock("../../lib/scientificClient", () => ({ sampleLineage: vi.fn().mockResolvedValue({ entries: [], isTruncated: false }) }));

import { SampleLineage } from "./SampleLineage";

describe("SampleLineage", () => {
  it("renders bounded empty lineage evidence with an accessible direction switch", async () => {
    render(<SampleLineage sampleId="sample-1" onSelect={vi.fn()} />);
    expect(await screen.findByText("No ancestors lineage evidence.")).toBeInTheDocument();
    expect(screen.getByLabelText("Direction")).toHaveValue("Ancestors");
  });
});
