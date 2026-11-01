import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { ScientificRecordUi } from "./ScientificRecordUi";

describe("ScientificRecordUi", () => {
  it("renders uncertain operation evidence with a keyboard-accessible full-ID control", () => {
    render(<ScientificRecordUi entityLabel="Procedure" entityId="procedure-123" state="Uncertain" />);
    expect(screen.getByRole("button", { name: "View full ID" })).toBeInTheDocument();
  });
});
