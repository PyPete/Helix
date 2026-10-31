import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

const { createApparatus } = vi.hoisted(() => ({ createApparatus: vi.fn() }));
vi.mock("../../lib/scientificApparatusClient", () => ({ createApparatus }));

import { ScientificApparatus } from "./ScientificApparatus";

describe("ScientificApparatus", () => {
  it("blocks duplicate submits and shows the durable receipt", async () => {
    let resolveCreate: (value: { operationId: string; apparatusId: string; status: string }) => void;
    createApparatus.mockReturnValueOnce(new Promise((resolve) => { resolveCreate = resolve; }));
    render(<ScientificApparatus />);

    fireEvent.change(screen.getByLabelText("Name"), { target: { value: "Reactor" } });
    fireEvent.click(screen.getByRole("button", { name: "Create apparatus" }));
    fireEvent.click(screen.getByRole("button", { name: "Creating…" }));
    expect(createApparatus).toHaveBeenCalledTimes(1);

    resolveCreate!({ operationId: "operation-1", apparatusId: "apparatus-1", status: "accepted" });
    await waitFor(() => expect(screen.getByRole("status")).toHaveTextContent("apparatus-1"));
  });
});
