import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import ConnectionConfig from "./ConnectionConfig";
import type { AgentConnection } from "../types/agent";

// Use vi.hoisted so mockSet/mockSave are available when vi.mock factory runs
const { mockSet, mockSave } = vi.hoisted(() => ({
  mockSet: vi.fn().mockResolvedValue(undefined),
  mockSave: vi.fn().mockResolvedValue(undefined),
}));

// Mock @tauri-apps/plugin-store — LazyStore is the v2 sync-constructor API
vi.mock("@tauri-apps/plugin-store", () => {
  const LazyStoreMock = vi.fn(function (this: Record<string, unknown>, _name: string) {
    this.set = mockSet;
    this.save = mockSave;
  });
  return { LazyStore: LazyStoreMock };
});

const defaultConnection: AgentConnection = {
  host: "localhost",
  port: 5080,
  protocol: "http",
};

describe("ConnectionConfig", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders host input with default value", () => {
    render(
      <ConnectionConfig connection={defaultConnection} onSave={vi.fn()} />
    );
    const hostInput = screen.getByDisplayValue("localhost");
    expect(hostInput).toBeInTheDocument();
  });

  it("renders port input with default value", () => {
    render(
      <ConnectionConfig connection={defaultConnection} onSave={vi.fn()} />
    );
    const portInput = screen.getByDisplayValue("5080");
    expect(portInput).toBeInTheDocument();
  });

  it("shows error when port is 0", async () => {
    render(
      <ConnectionConfig connection={defaultConnection} onSave={vi.fn()} />
    );
    const portInput = screen.getByDisplayValue("5080");
    fireEvent.change(portInput, { target: { value: "0" } });
    fireEvent.click(screen.getByRole("button", { name: /save/i }));

    await waitFor(() => {
      expect(screen.getByRole("alert")).toBeInTheDocument();
    });
    expect(screen.getByRole("alert")).toHaveTextContent("Invalid host or port");
  });

  it("calls onSave with updated connection on valid save", async () => {
    const onSave = vi.fn();
    render(
      <ConnectionConfig connection={defaultConnection} onSave={onSave} />
    );

    const hostInput = screen.getByDisplayValue("localhost");
    fireEvent.change(hostInput, { target: { value: "192.168.1.100" } });
    fireEvent.click(screen.getByRole("button", { name: /save/i }));

    await waitFor(() => {
      expect(onSave).toHaveBeenCalledWith({
        host: "192.168.1.100",
        port: 5080,
        protocol: "http",
      });
    });
    expect(mockSet).toHaveBeenCalledWith("agentConnection", {
      host: "192.168.1.100",
      port: 5080,
      protocol: "http",
    });
    expect(mockSave).toHaveBeenCalled();
  });
});
