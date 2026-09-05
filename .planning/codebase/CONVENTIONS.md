# Coding Conventions

**Analysis Date:** 2026-09-05

## Naming Patterns

**Files:**
- React components: PascalCase matching the exported component name — `ConnectionConfig.tsx`, `ExecutionPanel.tsx`, `SafetyPanel.tsx`
- React hooks: camelCase with `use` prefix — `useAgentHealth.ts`, `useAgentWebSocket.ts`, `useNodeExecutionState.ts`
- Test files: co-located with source, `.test.tsx` or `.test.ts` suffix — `ConnectionConfig.test.tsx`, `useAgentHealth.test.ts`
- Type/domain files: camelCase for modules — `agent.ts`, `workflowLibrary.ts`
- Shell sub-components: PascalCase in `src/shell/` — `HelixShell.tsx`, `Ribbon.tsx`, `ActivityBar.tsx`
- Barrel files: `index.ts` used in `src/components/charts/index.ts`

**Functions and Components:**
- Default exports for top-level page/root components — `export default function App()`, `export default function ConnectionConfig()`
- Named exports for sub-components and hooks — `export function ExecutionPanel()`, `export function useAgentHealth()`
- Handler functions prefixed with `handle` — `handleSave`, `handleStart`, `handleAbort`, `handleCompile`
- Async event handlers declared as `async` arrow functions or `async` function declarations

**Variables:**
- camelCase for all local variables and state — `experimentId`, `submitting`, `baseUrl`
- UPPER_SNAKE_CASE for module-level constant lookup tables — `STATUS_LABELS`, `STATUS_COLORS`
- Module-level singletons using `const` — `const store = new LazyStore("helix-config.json")`

**Types and Interfaces:**
- `interface` for component props with `Props` suffix — `ConnectionConfigProps`, `ExecutionPanelProps`
- `interface` for domain objects — `AgentConnection`, `WslDocument`, `RunspaceEvent`
- `type` for union types — `ExperimentEvent`, `ParameterType`, `CapabilityCategory`
- Types co-located in `src/types/agent.ts` as the single domain type file per package
- Interface properties use camelCase throughout

## Code Style

**Formatting:**
- No Prettier config detected — formatting is handled by the developer/editor; TypeScript strict mode enforced by `tsc`
- Strings: double quotes in `.tsx`/`.ts` files (observed consistently)
- Semicolons: present on all statements

**Linting:**
- Tool: ESLint with `eslint.config.js` flat config (`helix-dashboard/eslint.config.js`)
- Extends: `@eslint/js` recommended, `typescript-eslint` recommended, `eslint-plugin-react-hooks` recommended, `eslint-plugin-react-refresh`
- Scope: `**/*.{ts,tsx}` files, `dist/` ignored
- `eslint-disable` comments used sparingly and only with explanation:
  ```typescript
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);
  ```

## Import Organization

**Order (observed pattern):**
1. React/framework imports — `import { useState, useEffect } from "react"`
2. Third-party packages — `import { LazyStore } from "@tauri-apps/plugin-store"`
3. Internal type imports — `import type { AgentConnection } from "../types/agent"`
4. Internal module imports — `import { useAgentHealth } from '../hooks/useAgentHealth'`
5. CSS/style imports — `import './shell.css'` (placed first in shell files)

**Path style:** Relative paths only (`../types/agent`, `./ConnectionConfig`). No path aliases configured.

**Type-only imports:** `import type { ... }` used consistently for type-only imports — never mixing value and type in the same import when the value is unused.

## Error Handling

**Async fetch pattern:**
```typescript
try {
  const res = await fetch(baseUrl + "/endpoint", { method: "POST", ... });
  if (!res.ok) {
    setError("Failed: " + res.statusText);
    return;
  }
  const data = await res.json();
} catch (e) {
  setError((e as Error).message);
} finally {
  setSubmitting(false);
}
```

**Error state:** Errors surface to UI state via `useState<string | null>(null)`, rendered as `<p role="alert" className="error">{error}</p>`.

**Error casting:** Unknown errors cast with `(e as Error).message` — no utility wrapper used.

**HTTP status checks:** `res.ok` for general checks; specific status codes for semantic cases (e.g., `res.status === 422` for compile validation errors).

**WebSocket resilience:** Malformed JSON frames caught silently — the hook parses inside a try/catch and drops bad frames without throwing.

## Logging

**Framework:** `console` only — no structured logging library.

**Pattern:** No systematic logging observed in production code. Tests do not assert on console output.

## Comments

**When to comment:**
- Phase markers used in `src/types/agent.ts` to track which domain types arrived in each development phase: `// Phase 6.1 additions`
- Cross-file contract notes: `// mirrors AgentProxyOptions.cs defaults`
- Intentional no-op comments in tests: `/* intentionally never resolves */`
- JSDoc-style `/** ... */` block comments on exported shell components: `/** HelixShell – composition root... */`

**JSDoc/TSDoc:**
- Used on major shell component default exports (`HelixShell`)
- Not used consistently on hooks or smaller components — no enforced TSDoc policy

## Function Design

**Size:** Functions kept short; complex orchestration split into named sub-handlers (`handleCompile`, `handleStart` call each other).

**Parameters:** Props are destructured at the function signature: `function ExecutionPanel({ baseUrl, definition, runspaceEvents }: ExecutionPanelProps)`.

**Return values:** Hooks return plain objects with named fields, not arrays — `return state` for `useAgentHealth`, `return { connected, runspaceEvents, trendValues, ... }` for `useAgentWebSocket`.

**Async:** Handlers are `async` functions assigned to event props. Hooks use `useEffect` with inner `async` function (`const check = async () => { ... }`).

## Module Design

**Exports:** Mix of default (top-level components) and named (hooks, sub-components, utilities).

**Barrel files:** Used in `src/components/charts/index.ts`. Not used globally — most modules export directly.

**Constants files:** Module-level constant objects (`STATUS_LABELS`, `STATUS_COLORS`) co-located in the component file that uses them.

**Domain types:** All agent/domain types consolidated in `src/types/agent.ts` — the single source of truth per package. Dashboard has its own equivalent.

---

*Convention analysis: 2026-09-05*
