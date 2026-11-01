import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { FrozenAssembly } from "./FrozenAssembly";
import { assemblyDefaults, listScientificPolicies } from "../../lib/scientificClient";

vi.mock("../../lib/scientificClient", () => ({
  assemblyDefaults: vi.fn().mockResolvedValue({ apparatusRevisionId: "app-1", planRevisionId: "plan-1", procedureRevisionId: "procedure-pinned-1", policyRevisionId: "policy-1", previewToken: "preview-1" }),
  listScientificPolicies: vi.fn().mockResolvedValue([{ revisionId: "policy-1", policyId: "policy", schema: "policy/v1", contentHash: "hash-1", recordedAtUtc: "2026-01-01", approval: { approvalId: "approval-1", revisionId: "policy-1", contentHash: "hash-1", schema: "policy/v1", status: "Approved" } }]),
  assembleScientific: vi.fn(),
  readScientificAssembly: vi.fn(),
}));

describe("FrozenAssembly", () => {
  beforeEach(() => {
    vi.mocked(assemblyDefaults).mockResolvedValue({ apparatusRevisionId: "app-1", planRevisionId: "plan-1", procedureRevisionId: "procedure-pinned-1", policyRevisionId: "policy-1", previewToken: "preview-1" });
    vi.mocked(listScientificPolicies).mockResolvedValue([{ revisionId: "policy-1", policyId: "policy", schema: "policy/v1", contentHash: "hash-1", recordedAtUtc: "2026-01-01", approval: { approvalId: "approval-1", revisionId: "policy-1", contentHash: "hash-1", schema: "policy/v1", status: "Approved" } }]);
  });
  it("requires an explicit exact-reference confirmation before freezing", async () => {
    render(<FrozenAssembly />);

    expect(await screen.findByText("Plan-pinned procedure revision")).toBeInTheDocument();
    expect(screen.getByText("procedure-pinned-1")).toBeInTheDocument();
    expect(screen.getByText(/This action creates no campaign and sends no physical command/)).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Assemble frozen references" }));
    expect(screen.getByRole("dialog", { name: "Confirm frozen reference assembly" })).toBeInTheDocument();
  });

  it("blocks production authority that is unavailable without presenting an approval substitute", async () => {
    vi.mocked(listScientificPolicies).mockResolvedValue([{ revisionId: "policy-1", policyId: "policy", schema: "policy/v1", contentHash: "hash-1", recordedAtUtc: "2026-01-01", approval: { approvalId: "approval-1", revisionId: "policy-1", contentHash: "hash-1", schema: "policy/v1", status: "Unavailable" } }]);
    render(<FrozenAssembly />);

    expect(await screen.findByRole("alert")).toHaveTextContent("valid authoritative policy revision is required");
    expect(screen.queryByRole("button", { name: "Assemble frozen references" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /approve|start/i })).not.toBeInTheDocument();
  });
});
