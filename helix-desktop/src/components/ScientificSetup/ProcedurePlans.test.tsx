import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { ProcedurePlans } from "./ProcedurePlans";

describe("ProcedurePlans", () => {
  it("keeps procedure publishing independent and exposes immutable history", () => {
    render(<ProcedurePlans kind="procedure" />);
    expect(screen.getByRole("button", { name: "Publish named revision" })).toBeInTheDocument();
    expect(screen.getByText("Revision history")).toBeInTheDocument();
  });

  it("shows source evidence for a derived procedure without mutating the publication", () => {
    render(<ProcedurePlans kind="procedure" />);
    expect(screen.getByRole("button", { name: "Derive procedure revision" })).toBeInTheDocument();
    expect(screen.getByLabelText("Derived from procedure revision")).toBeInTheDocument();
  });

  it("requires an explicit adoption confirmation and never publishes a plan automatically", () => {
    render(<ProcedurePlans kind="plan" selectedId="plan-id" />);
    expect(screen.getByRole("button", { name: "Adopt newer procedure" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Publish named revision" })).toBeInTheDocument();
  });
});
