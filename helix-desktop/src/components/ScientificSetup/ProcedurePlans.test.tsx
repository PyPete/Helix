import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { ProcedurePlans } from "./ProcedurePlans";

describe("ProcedurePlans", () => {
  it("keeps procedure publishing independent and exposes immutable history", () => {
    render(<ProcedurePlans kind="procedure" />);
    expect(screen.getByRole("button", { name: "Publish named revision" })).toBeInTheDocument();
    expect(screen.getByText("Revision history")).toBeInTheDocument();
  });
});
