import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

const { readApparatusDraft, saveApparatusDraft } = vi.hoisted(() => ({ readApparatusDraft: vi.fn(), saveApparatusDraft: vi.fn() }));
vi.mock("../../lib/scientificApparatusClient", () => ({ readApparatusDraft, saveApparatusDraft }));
import { ScientificSetup } from "./ScientificSetup";

describe("ScientificSetup", () => {
  it("retains local input and offers reload/compare after an Agent conflict", async () => {
    saveApparatusDraft.mockRejectedValueOnce(new Error("409 conflict"));
    readApparatusDraft.mockResolvedValue({ apparatus: { apparatusId: "a", name: "Agent draft", operationId: "op" }, token: 2 });
    render(<ScientificSetup apparatusId="a" initialDraft={{ apparatus: { apparatusId: "a", name: "Local draft", operationId: "op" }, token: 1 }} />);
    fireEvent.change(screen.getByLabelText("Name"), { target: { value: "Unsaved local" } });
    fireEvent.click(screen.getByRole("button", { name: "Save draft" }));
    await waitFor(() => expect(screen.getByRole("alert")).toHaveTextContent("Draft changed elsewhere"));
    expect(screen.getByLabelText("Name")).toHaveValue("Unsaved local");
    fireEvent.click(screen.getByRole("button", { name: "Compare" }));
    await waitFor(() => expect(screen.getByLabelText("Draft comparison")).toHaveTextContent("Agent draft"));
    fireEvent.click(screen.getByRole("button", { name: "Reload" }));
    await waitFor(() => expect(screen.getByLabelText("Name")).toHaveValue("Agent draft"));
  });

  it("makes the immutable apparatus identity available in a keyboard-operable full-ID dialog", async () => {
    render(<ScientificSetup apparatusId="apparatus-immutable-id" initialDraft={{ apparatus: { apparatusId: "apparatus-immutable-id", name: "Local draft", operationId: "op" }, token: 1 }} />);

    fireEvent.click(screen.getByRole("button", { name: "View full ID" }));

    expect(screen.getByRole("dialog", { name: "Apparatus draft full ID" })).toHaveTextContent("apparatus-immutable-id");
    expect(screen.getByRole("button", { name: "Copy ID" })).toBeInTheDocument();
  });
});
