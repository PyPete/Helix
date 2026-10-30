import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { SafetyPanel } from "./SafetyPanel";
import type { SafetyEvent } from "../../types/agent";

const triggered: SafetyEvent = { type: "safety-event", ruleId: "rule-1", description: "Temperature above limit", severity: "Critical", triggered: true, timestamp: "2026-01-01" };

describe("SafetyPanel", () => {
  it("does not infer safety from zero rules", () => {
    render(<SafetyPanel safetyEvents={[]} />);
    expect(screen.getByTestId("no-rules")).toHaveTextContent("No interlock rules active");
    expect(screen.getByTestId("no-rules")).toHaveTextContent("unknown");
    expect(screen.getByRole("alert")).toHaveTextContent("Safety state unknown");
  });

  it("makes loading and read errors explicit", () => {
    const { rerender } = render(<SafetyPanel safetyEvents={[]} snapshotState="loading" />);
    expect(screen.getByRole("alert")).toHaveTextContent("Safety snapshot loading");
    rerender(<SafetyPanel safetyEvents={[]} snapshotState="error" snapshotError="desktop broker unavailable" />);
    expect(screen.getByRole("alert")).toHaveTextContent("Safety snapshot unavailable: desktop broker unavailable");
  });

  it("renders a triggered rule with persistent text and an accessible alert", () => {
    render(<SafetyPanel safetyEvents={[triggered]} snapshotState="known" />);
    expect(screen.getByTestId("rule-rule-1")).toHaveTextContent("TRIGGERED");
    expect(screen.getByTestId("safety-alert")).toHaveTextContent("SAFETY TRIGGERED");
  });

  it("deduplicates rules, preserves many entries, and labels missing values", () => {
    const older = { ...triggered, triggered: false, description: "Old observation" };
    const rules = [older, triggered, ...Array.from({ length: 12 }, (_, index): SafetyEvent => ({ type: "safety-event", ruleId: `r-${index}`, description: `Long rule description ${"x".repeat(40)}`, severity: "Warning", triggered: false, timestamp: "" }))];
    render(<SafetyPanel safetyEvents={rules} snapshotState="known" />);
    expect(screen.getAllByRole("listitem")).toHaveLength(13);
    expect(screen.getByTestId("rule-rule-1")).toHaveTextContent("Temperature above limit");
    expect(screen.getByTestId("rule-r-0")).toHaveTextContent("Current value: Not recorded");
  });
});
