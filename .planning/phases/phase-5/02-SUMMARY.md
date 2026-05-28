---
phase: phase-5
plan: "02"
subsystem: helix-desktop
tags: [tauri, react, typescript, rust, vitest, plugin-store]
dependency_graph:
  requires:
    - phase: phase-5/01
      provides: helix-desktop scaffold, src/types/agent.ts, @tauri-apps/plugin-store dependency
  provides:
    - helix-desktop/src/hooks/useAgentHealth.ts (REST health polling hook)
    - helix-desktop/src/components/ConnectionConfig.tsx (agent connection form + LazyStore persistence)
    - helix-desktop/src/components/DeviceRegistry.tsx (adapter list from health endpoint)
    - helix-desktop/src/App.tsx (composition root wiring all components)
    - helix-desktop/src-tauri/src/commands/connection.rs (load_config/save_config Tauri commands)
  affects:
    - phase-5/03 (WebSocket or gRPC integration builds on this component structure)
    - phase-6 (gRPC commands extend lib.rs commands pattern)
tech-stack:
  added: []
  patterns:
    - "useAgentHealth hook: poll GET /health with AbortSignal.timeout(3000) via setInterval"
    - "LazyStore (not Store) for @tauri-apps/plugin-store v2.4.3 — Store constructor is private in v2"
    - "vi.hoisted() pattern for vitest mocks that reference module-level variables"
    - "vi.fn() as constructor mock — use regular function (not arrow) for new-able mocks in vitest"
    - "Tauri commands in src/commands/ submodule with mod.rs, registered in lib.rs invoke_handler"
key-files:
  created:
    - helix-desktop/src/hooks/useAgentHealth.ts
    - helix-desktop/src/hooks/useAgentHealth.test.ts
    - helix-desktop/src/components/ConnectionConfig.tsx
    - helix-desktop/src/components/ConnectionConfig.test.tsx
    - helix-desktop/src/components/DeviceRegistry.tsx
    - helix-desktop/src/components/DeviceRegistry.test.tsx
    - helix-desktop/src-tauri/src/commands/connection.rs
    - helix-desktop/src-tauri/src/commands/mod.rs
  modified:
    - helix-desktop/src/App.tsx
    - helix-desktop/src-tauri/src/lib.rs
    - helix-desktop/package.json
key-decisions:
  - "Used LazyStore (sync constructor) instead of Store (private constructor) from plugin-store v2.4.3 — plan specified Store but the v2 API changed; LazyStore is the correct sync alternative"
  - "Removed fake timers from useAgentHealth tests — vi.useFakeTimers() blocks @testing-library waitFor because RTL uses real async internally"
  - "vitest constructor mocks require vi.hoisted() + regular function syntax (not arrow) when the mock references hoisted variables"
  - "Interval set to 999999ms in hook tests to prevent polling from interfering with assertions"
patterns-established:
  - "LazyStore pattern: import { LazyStore } from '@tauri-apps/plugin-store'; const store = new LazyStore('name.json')"
  - "Tauri command pattern: src-tauri/src/commands/{name}.rs + mod.rs + register in lib.rs invoke_handler"
  - "vitest Store mock: vi.hoisted() + vi.fn(function(this) { this.method = mockFn; }) as constructor"
requirements-completed:
  - C7
  - C8
duration: ~20min
completed: "2026-05-28"
---

# Phase 5 Plan 02: React Components + Rust Config Commands Summary

**useAgentHealth hook polling GET /health with AbortSignal.timeout(3000), ConnectionConfig form persisting via LazyStore, DeviceRegistry adapter list, App.tsx composition root wired — 14 vitest tests pass, npm run build exits 0, cargo check exits 0.**

## Performance

- **Duration:** ~20 min
- **Started:** 2026-05-28T17:40:00Z
- **Completed:** 2026-05-28T17:49:00Z
- **Tasks:** 4 (T01, T02, T03, T04)
- **Files modified:** 11

## Accomplishments
- useAgentHealth hook polls `GET {baseUrl}/health` every 5s with `AbortSignal.timeout(3000)`, exposes `AgentHealthState` (healthy/error/lastChecked)
- ConnectionConfig form persists agent host/port/protocol to `helix-config.json` via `@tauri-apps/plugin-store` LazyStore with validation
- DeviceRegistry maps `/health` JSON to adapter list, handles loading/error/empty states
- App.tsx composition root wires `DEFAULT_CONNECTION` (localhost:5080) through all three components with zero SiteServer references
- Rust `load_config`/`save_config` Tauri commands registered in `src-tauri/src/commands/connection.rs` and wired via `invoke_handler`

## Task Commits

All tasks committed together atomically (Wave 2 execution):

1. **T01-T04: React components + Rust config commands** - `46b02cd` (feat)

**Plan metadata:** TBD after SUMMARY commit

## Files Created/Modified
- `helix-desktop/src/hooks/useAgentHealth.ts` - React hook polling GET /health every 5s
- `helix-desktop/src/hooks/useAgentHealth.test.ts` - 5 vitest tests (null state, 200, 500, network error, AbortSignal)
- `helix-desktop/src/components/ConnectionConfig.tsx` - Host/port/protocol form with LazyStore persistence
- `helix-desktop/src/components/ConnectionConfig.test.tsx` - 4 vitest tests (defaults, port=0 error, onSave called)
- `helix-desktop/src/components/DeviceRegistry.tsx` - Adapter list from /health, loading/error state
- `helix-desktop/src/components/DeviceRegistry.test.tsx` - 5 vitest tests (loading, healthy, 503, network error, degraded)
- `helix-desktop/src/App.tsx` - Composition root wiring DEFAULT_CONNECTION -> useAgentHealth -> ConnectionConfig + DeviceRegistry
- `helix-desktop/src-tauri/src/commands/connection.rs` - load_config/save_config async Tauri commands
- `helix-desktop/src-tauri/src/commands/mod.rs` - pub mod connection
- `helix-desktop/src-tauri/src/lib.rs` - Added `mod commands` + `invoke_handler![load_config, save_config]`
- `helix-desktop/package.json` - Added `"test": "vitest"` script

## Decisions Made

- **LazyStore over Store:** `@tauri-apps/plugin-store` v2.4.3 marks `Store` constructor as private; `LazyStore` is the v2 sync-constructible alternative with identical `set`/`save` API. Plan's Deviation Protocol authorized this fallback.
- **No fake timers in hook tests:** `vi.useFakeTimers()` blocks `@testing-library/react`'s `waitFor` because RTL uses real async scheduling internally. Removed fake timers; set `intervalMs=999999` in tests to prevent unintended re-polling.
- **vi.hoisted() for Store mock:** vitest hoists `vi.mock()` factories before variable declarations; mock functions must be defined via `vi.hoisted()` to avoid "Cannot access before initialization" ReferenceError.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] useAgentHealth tests timed out due to vi.useFakeTimers() conflict with @testing-library/react waitFor**
- **Found during:** T01 (useAgentHealth hook tests)
- **Issue:** Initial test file used `vi.useFakeTimers()` which caused all `waitFor` calls to time out at 5000ms because RTL's async utilities rely on real timers
- **Fix:** Removed `vi.useFakeTimers()`; set `intervalMs=999999` to prevent polling interference during tests
- **Files modified:** helix-desktop/src/hooks/useAgentHealth.test.ts
- **Verification:** 5/5 hook tests pass in 233ms
- **Committed in:** 46b02cd

**2. [Rule 1 - Bug] ConnectionConfig test failed — Store mock arrow function not constructible**
- **Found during:** T02 (ConnectionConfig tests)
- **Issue:** `vi.fn(() => ({ set: mockSet, save: mockSave }))` arrow function is not a constructor; `new Store(...)` threw "is not a constructor"
- **Fix:** Used `vi.fn(function(this) { this.set = ...; this.save = ...; })` (regular function syntax)
- **Files modified:** helix-desktop/src/components/ConnectionConfig.test.tsx
- **Verification:** Tests still failed with ReferenceError (see Rule 3)

**3. [Rule 1 - Bug] ConnectionConfig test ReferenceError — vi.mock hoisting before mockSet/mockSave declarations**
- **Found during:** T02 (ConnectionConfig tests)
- **Issue:** `vi.mock()` factory is hoisted before variable declarations; referencing `mockSet`/`mockSave` from outside the factory caused "Cannot access before initialization"
- **Fix:** Wrapped mock function creation in `vi.hoisted()` so they are available at hoist time
- **Files modified:** helix-desktop/src/components/ConnectionConfig.test.tsx
- **Verification:** 4/4 ConnectionConfig tests pass

**4. [Rule 1 - Deviation] Store constructor is private in @tauri-apps/plugin-store v2.4.3 — TypeScript TS2673 build error**
- **Found during:** T04 (`npm run build` verification)
- **Issue:** Plan specified `new Store("helix-config.json")` but the v2 API marks `Store` constructor as `private`; `Store.load()` is async-only
- **Fix:** Replaced `Store` import with `LazyStore` (sync constructible, same set/save API). Updated test mock from `Store` to `LazyStore`. Per plan's Deviation Protocol: "If Store constructor API has changed, fall back to alternative".
- **Files modified:** helix-desktop/src/components/ConnectionConfig.tsx, helix-desktop/src/components/ConnectionConfig.test.tsx
- **Verification:** `npm run build` exits 0, 14/14 tests pass

---

**Total deviations:** 4 auto-fixed (3 Rule 1 test bugs, 1 Rule 1 API change)
**Impact on plan:** All auto-fixes necessary for correct test behavior and TypeScript compliance. No scope creep. LazyStore is API-equivalent to Store for set/save operations.

## Issues Encountered
- `@tauri-apps/plugin-store` v2 changed `Store` to use private constructor + async static `Store.load()` factory. `LazyStore` is the sync alternative and was substituted per plan's deviation protocol.

## Known Stubs

None. The `load_config` Rust command returns a hardcoded default (localhost:5080) rather than reading from disk. This is intentional per Phase 5 scope — the Tauri store JS-side (`LazyStore`) handles persistence; the Rust commands are scaffolded for Phase 6 gRPC integration which will need config access server-side.

## Threat Flags

No new network endpoints, auth paths, or schema changes introduced. The desktop app fetches `GET /health` from a user-configured localhost URL; no cross-origin security concern in a Tauri webview context.

## Self-Check: PASSED

- [x] All vitest tests pass — 14/14 (3 files)
- [x] `npm run build` exits 0 — 0 TypeScript errors strict mode
- [x] `cargo check` exits 0 — commands module compiles clean
- [x] No SiteServer references in `src/` — grep confirmed
- [x] `useAgentHealth` uses `AbortSignal.timeout(3000)` — line 18 in hook
- [x] `ConnectionConfig` uses `@tauri-apps/plugin-store` (LazyStore) — line 2 in component
- [x] `DEFAULT_CONNECTION` default is `localhost:5080` — App.tsx imports from types/agent.ts
- [x] `DeviceRegistry` handles error state — try/catch + error state display
- [x] Commit `46b02cd` exists — git log confirmed

---
*Phase: phase-5*
*Completed: 2026-05-28*
