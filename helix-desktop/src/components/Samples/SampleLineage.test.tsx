import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

vi.mock("../../lib/scientificClient", () => ({ sampleLineage: vi.fn().mockResolvedValue({ entries: [], isTruncated: false, continuation: undefined }) }));

import { SampleLineage } from "./SampleLineage";
import { sampleLineage } from "../../lib/scientificClient";

describe("SampleLineage", () => {
  it("renders bounded empty lineage evidence with an accessible direction switch", async () => {
    render(<SampleLineage sampleId="sample-1" onSelect={vi.fn()} />);
    expect(await screen.findByText("No ancestors lineage evidence.")).toBeInTheDocument();
    expect(screen.getByLabelText("Direction")).toHaveValue("Ancestors");
  });

  it("keeps pagination explicit when lineage evidence has more approved bounded pages", async () => {
    vi.mocked(sampleLineage).mockResolvedValueOnce({ entries: [], isTruncated: true, continuation: "next-page" });
    render(<SampleLineage sampleId="sample-1" onSelect={vi.fn()} />);

    expect(await screen.findByRole("button", { name: "Load more" })).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Load more" }));
    expect(sampleLineage).toHaveBeenLastCalledWith("sample-1", "Ancestors", "next-page");
  });
});
