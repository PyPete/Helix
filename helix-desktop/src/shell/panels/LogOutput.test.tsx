import { render, screen } from "@testing-library/react";
import { describe, it, expect } from "vitest";
import { LogOutput } from "./LogOutput";
import type { RunspaceEvent, SafetyEvent } from "../../types/agent";

describe("LogOutput", () => {
  it("renders data-testid='log-output'", () => {
    render(<LogOutput runspaceEvents={[]} safetyEvents={[]} />);
    expect(screen.getByTestId("log-output")).toBeInTheDocument();
  });

  it("renders each runspaceEvent.type as a list item", () => {
    const events: RunspaceEvent[] = [
      { type: "runspace-started", runspaceId: "r1", timestamp: "2026-06-04T00:00:00Z" },
      { type: "step-completed", runspaceId: "r1", timestamp: "2026-06-04T00:01:00Z" },
    ];

    render(<LogOutput runspaceEvents={events} safetyEvents={[]} />);

    expect(screen.getByTestId("log-runspace-runspace-started")).toBeInTheDocument();
    expect(screen.getByTestId("log-runspace-step-completed")).toBeInTheDocument();
  });

  it("renders safety events (triggered=true) with severity label", () => {
    const safetyEvents: SafetyEvent[] = [
      {
        type: "safety-event",
        ruleId: "overheat",
        description: "Temperature exceeded limit",
        severity: "critical",
        triggered: true,
        timestamp: "2026-06-04T00:00:00Z",
      },
      {
        type: "safety-event",
        ruleId: "pressure-ok",
        description: "Pressure normal",
        severity: "info",
        triggered: false,
        timestamp: "2026-06-04T00:00:00Z",
      },
    ];

    render(<LogOutput runspaceEvents={[]} safetyEvents={safetyEvents} />);

    // Only triggered=true events are rendered
    expect(screen.getByTestId("log-safety-overheat")).toBeInTheDocument();
    expect(screen.getByTestId("log-safety-overheat")).toHaveTextContent("critical");
    expect(screen.queryByTestId("log-safety-pressure-ok")).not.toBeInTheDocument();
  });
});
