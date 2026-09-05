# Testing Patterns

**Analysis Date:** 2026-09-05

## Test Framework

**Runner:**
- Vitest (both packages)
- `helix-desktop`: `vitest` ^4.x — config at `helix-desktop/vitest.config.ts`
- `helix-dashboard`: `vitest --passWithNoTests` — config at `helix-dashboard/vitest.config.ts`

**Assertion Library:**
- Vitest built-in `expect` (Jest-compatible API)
- `@testing-library/jest-dom` for DOM matchers — imported via setup file

**Supporting libraries:**
- `@testing-library/react` ^16 — `render`, `screen`, `fireEvent`, `waitFor`, `act`, `renderHook`, `rerender`
- `@testing-library/user-event` ^14 — available but `fireEvent` used in most existing tests
- `jsdom` — test environment

**Run Commands:**
```bash
# helix-desktop
cd helix-desktop && npm test          # Run all tests (watch mode)
cd helix-desktop && npm test -- --run # Run once (CI)
cd helix-desktop && npm test -- --coverage  # Coverage via @vitest/coverage-v8

# helix-dashboard
cd helix-dashboard && npm test        # Run all tests
```

## Test File Organization

**Location:** Co-located with source files — test file lives next to implementation.

**Naming:** `{ComponentOrModule}.test.tsx` for React components, `{hookOrLib}.test.ts` for pure TypeScript.

**Structure example:**
```
src/
  components/
    ConnectionConfig.tsx
    ConnectionConfig.test.tsx         ← co-located
    ExecutionPanel/
      ExecutionPanel.tsx
      ExecutionPanel.test.tsx         ← co-located in subdirectory
  hooks/
    useAgentHealth.ts
    useAgentHealth.test.ts
  lib/
    workflowLibrary.ts
    workflowLibrary.test.ts
  test/
    setup.ts                          ← global setup only
```

## Test Setup

**Global setup file:** `src/test/setup.ts`
```typescript
import "@testing-library/jest-dom";
```

**Vitest config:**
```typescript
export default defineConfig({
  plugins: [react()],
  test: {
    globals: true,         // describe/it/expect/vi without imports
    environment: "jsdom",
    setupFiles: ["./src/test/setup.ts"],
  },
});
```

## Test Structure

**Suite Organization:**
```typescript
// describe wraps a component or hook or module
describe("ComponentName", () => {
  beforeEach(() => {
    vi.clearAllMocks();       // or vi.restoreAllMocks()
  });

  afterEach(() => {
    vi.restoreAllMocks();     // used when vi.stubGlobal is involved
  });

  it("renders X when Y", () => { ... });
  it("calls Z on valid save", async () => { ... });
});
```

**Patterns:**
- `beforeEach` used to reset mock state or set up stubs — `vi.clearAllMocks()` for spy resets, full stub setup for WebSocket tests
- `afterEach` used primarily with `vi.restoreAllMocks()` to tear down `vi.stubGlobal` calls
- Test descriptions are natural-language sentences describing observable behavior, not implementation details
- Module-level `afterEach` (outside a `describe`) is also used when a file has a single top-level subject

## Mocking

**Framework:** Vitest's `vi` object — `vi.fn()`, `vi.mock()`, `vi.hoisted()`, `vi.stubGlobal()`, `vi.spyOn()`

**Pattern 1 — `vi.hoisted` for module mock factories:**
```typescript
// Use vi.hoisted so mock functions are available when vi.mock factory runs
const { mockSet, mockSave } = vi.hoisted(() => ({
  mockSet: vi.fn().mockResolvedValue(undefined),
  mockSave: vi.fn().mockResolvedValue(undefined),
}));

vi.mock("@tauri-apps/plugin-store", () => {
  const LazyStoreMock = vi.fn(function (this: Record<string, unknown>, _name: string) {
    this.set = mockSet;
    this.save = mockSave;
  });
  return { LazyStore: LazyStoreMock };
});
```

**Pattern 2 — `vi.stubGlobal` for browser globals (fetch, WebSocket):**
```typescript
vi.stubGlobal(
  "fetch",
  vi.fn(() => Promise.resolve({ ok: true, status: 200 } as Response))
);

// WebSocket — capture instance so tests can simulate messages
let lastWsInstance: MockWs | null = null;
vi.stubGlobal("WebSocket", vi.fn(function (this: MockWs) {
  this.onopen = null;
  this.onclose = null;
  this.onerror = null;
  this.onmessage = null;
  this.close = mockClose;
  lastWsInstance = this;
}));
```

**Pattern 3 — `vi.spyOn` for method inspection:**
```typescript
const timeoutSpy = vi.spyOn(AbortSignal, "timeout").mockReturnValue(abortSignal);
```

**What to Mock:**
- All Tauri plugin APIs (`@tauri-apps/plugin-store`, `@tauri-apps/plugin-fs`) — these are native APIs unavailable in jsdom
- `fetch` — all HTTP calls in hooks/components
- `WebSocket` — all WebSocket connections
- `AbortSignal.timeout` — when testing that timeout configuration is applied

**What NOT to Mock:**
- Internal business logic / pure functions — test them directly
- React state and lifecycle — test through rendered output and events
- `@testing-library/react` internals

## Fixtures and Factories

**Test Data:**
```typescript
// Inline typed constants at top of test file
const defaultConnection: AgentConnection = {
  host: "localhost",
  port: 5080,
  protocol: "http",
};

const sampleDefinition: WslDocument = {
  documentId: "doc-1",
  name: "Test Workflow",
  schemaVersion: "1.0",
  nodes: [],
  edges: [],
};

// Helper factory function for events (used in useNodeExecutionState.test.ts)
function makeEvent(overrides: Partial<RunspaceEvent> & { type: string }): RunspaceEvent {
  return {
    runspaceId: "r1",
    timestamp: "2026-01-01T00:00:00.000Z",
    ...overrides,
  };
}
```

**Location:** Inline in each test file — no shared fixture directory or factory module.

## Coverage

**Requirements:** No enforced threshold configured.

**Coverage tool:** `@vitest/coverage-v8` available in both packages.

**View Coverage:**
```bash
cd helix-desktop && npm test -- --coverage
cd helix-dashboard && npm test -- --coverage
```

## Test Types

**Unit Tests:**
- Pure TypeScript modules: `workflowLibrary.test.ts` — tests file I/O logic with Tauri FS mocked
- Custom hooks: `useAgentHealth.test.ts`, `useAgentWebSocket.test.ts`, `useNodeExecutionState.test.ts`, `useTrendWebSocket.test.ts`
- Scope: single module in isolation, all external I/O mocked

**Component Tests (Integration-style):**
- React components rendered in jsdom via `@testing-library/react`
- Tests verify rendered output, user interactions, and async state changes
- Examples: `ConnectionConfig.test.tsx`, `ExecutionPanel.test.tsx`, `SafetyPanel.test.tsx`
- Uses `screen.getByRole`, `screen.getByTestId`, `screen.getByDisplayValue` for queries

**E2E Tests:** Not used.

## Common Patterns

**Async Testing:**
```typescript
// waitFor for async state updates
await waitFor(() => {
  expect(screen.getByTestId("experiment-id")).toHaveTextContent("exp-abc123");
});

// act for manual hook event simulation
act(() => {
  lastWsInstance!.onmessage!({ data: JSON.stringify(frame) });
});
await waitFor(() => expect(result.current.trendValues).toHaveLength(1));
```

**Error Testing:**
```typescript
// Rendered alert element
await waitFor(() => expect(screen.getByRole("alert")).toBeInTheDocument());
expect(screen.getByRole("alert")).toHaveTextContent("Compile errors");

// Rejected promise
await expect(saveWorkflow("!!!", doc)).rejects.toThrow("alphanumeric");
```

**Testing Hook Return Values:**
```typescript
const { result } = renderHook(() => useAgentHealth("http://localhost:5080", 999999));
// Use large intervalMs to prevent polling during test
expect(result.current.healthy).toBeNull();
```

**Chained fetch mocks (sequential HTTP calls):**
```typescript
const mockFetch = vi.fn()
  .mockResolvedValueOnce({ ok: true, status: 200, json: () => Promise.resolve(firstResponse) } as unknown as Response)
  .mockResolvedValueOnce({ ok: true, status: 200, json: () => Promise.resolve(secondResponse) } as unknown as Response);
vi.stubGlobal("fetch", mockFetch);
```

**data-testid selectors:** Components expose `data-testid` attributes for stable test queries (`start-btn`, `abort-btn`, `experiment-id`, `step-list`, `rule-r1`, etc.). Prefer `data-testid` over fragile CSS selectors; use `getByRole` for interactive/semantic elements (buttons, alerts).

---

*Testing analysis: 2026-09-05*
