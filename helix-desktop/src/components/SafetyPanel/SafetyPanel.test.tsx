import { render, screen } from "@testing-library/react";
import { vi, describe, it, afterEach, expect } from "vitest";
import { SafetyPanel } from "./SafetyPanel";
import type { SafetyEvent } from "../../types/agent";

afterEach(() => {
  vi.restoreAllMocks();
});

describe("SafetyPanel", () => {
  it("renders empty state when no safety events", () => {
    render(<SafetyPanel safetyEvents={[]} />);
    expect(screen.getByTestId("no-rules")).toBeInTheDocument();
  });

  it("renders a safe rule as 'Safe'", () => {
    const events: SafetyEvent[] = [
      {
        type: "safety-event",
        ruleId: "r1",
        description: "Temp OK",
        severity: "Critical",
        triggered: false,
        timestamp: "",
      },
    ];
    render(<SafetyPanel safetyEvents={events} />);
    expect(screen.getByTestId("rule-r1")).toHaveTextContent("Safe");
  });

  it("shows alert when a rule is triggered", () => {
    const events: SafetyEvent[] = [
      {
        type: "safety-event",
        ruleId: "r1",
        description: "Temp High",
        severity: "Critical",
        triggered: true,
        timestamp: "",
      },
    ];
    render(<SafetyPanel safetyEvents={events} />);
    expect(screen.getByRole("alert")).toBeInTheDocument();
  });

  it("shows latest state per ruleId (deduplicates)", () => {
    const events: SafetyEvent[] = [
      {
        type: "safety-event",
        ruleId: "r1",
        description: "Temp OK",
        severity: "Critical",
        triggered: false,
        timestamp: "2026-05-29T00:00:00Z",
      },
      {
        type: "safety-event",
        ruleId: "r1",
        description: "Temp High",
        severity: "Critical",
        triggered: true,
        timestamp: "2026-05-29T00:01:00Z",
      },
    ];
    render(<SafetyPanel safetyEvents={events} />);
    expect(screen.getByTestId("rule-r1")).toHaveTextContent("TRIGGERED");
  });

  it("renders multiple rules from multiple events", () => {
    const events: SafetyEvent[] = [
      {
        type: "safety-event",
        ruleId: "r1",
        description: "Temp OK",
        severity: "Critical",
        triggered: false,
        timestamp: "",
      },
      {
        type: "safety-event",
        ruleId: "r2",
        description: "Pressure OK",
        severity: "Warning",
        triggered: false,
        timestamp: "",
      },
    ];
    render(<SafetyPanel safetyEvents={events} />);
    expect(screen.getByTestId("rule-list")).toContainElement(
      screen.getByTestId("rule-r1")
    );
    expect(screen.getByTestId("rule-list")).toContainElement(
      screen.getByTestId("rule-r2")
    );
  });

  it("displays currentValue when present in safety event", () => {
    const events: SafetyEvent[] = [
      {
        type: "safety-event",
        ruleId: "r1",
        description: "Temp High",
        severity: "Critical",
        triggered: true,
        timestamp: "",
        currentValue: 95.5,
      },
    ];
    render(<SafetyPanel safetyEvents={events} />);
    expect(screen.getByTestId("rule-value-r1")).toBeInTheDocument();
    expect(screen.getByTestId("rule-value-r1")).toHaveTextContent("95.50");
  });
});
