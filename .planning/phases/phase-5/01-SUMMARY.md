---
phase: phase-5
plan: "01"
subsystem: helix-desktop
tags: [tauri, react, typescript, rust, scaffold]
dependency_graph:
  requires: []
  provides: [helix-desktop/scaffold, helix-desktop/types]
  affects: [helix-desktop/]
tech_stack:
  added:
    - "Tauri 2.11.2 (Rust desktop framework)"
    - "React 18.3.x + react-dom"
    - "@tauri-apps/api 2.x (Tauri JS IPC bindings)"
    - "@tauri-apps/plugin-store 2.4.3 (persistent key-value store)"
    - "@tauri-apps/cli 2.x (Tauri build toolchain)"
    - "TypeScript 5.5.x (strict mode)"
    - "Vite 6.x (frontend bundler)"
    - "@vitejs/plugin-react 4.3.x"
    - "tonic 0.14.6 (Rust gRPC client)"
    - "tonic-build 0.14.6 (proto codegen build dep)"
    - "tauri-plugin-store 2.4.3 (Rust store plugin)"
    - "tokio 1.x full features (async runtime)"
    - "serde + serde_json 1.x (Rust serialization)"
    - "vitest 4.x + @vitest/coverage-v8"
    - "@testing-library/react 16.x + jest-dom + user-event"
    - "jsdom 29.x (test environment)"
  patterns:
    - "Tauri v2 capabilities system (src-tauri/capabilities/default.json)"
    - "Tauri v2 lib.rs entry point with mobile_entry_point cfg_attr"
    - "TypeScript strict mode mirroring C# Nullable:enable"
    - "Vitest with jsdom environment for React component tests"
key_files:
  created:
    - helix-desktop/package.json
    - helix-desktop/tsconfig.json
    - helix-desktop/tsconfig.node.json
    - helix-desktop/vite.config.ts
    - helix-desktop/vitest.config.ts
    - helix-desktop/index.html
    - helix-desktop/src/main.tsx
    - helix-desktop/src/App.tsx
    - helix-desktop/src/App.css
    - helix-desktop/src/test/setup.ts
    - helix-desktop/src/types/agent.ts
    - helix-desktop/src-tauri/tauri.conf.json
    - helix-desktop/src-tauri/Cargo.toml
    - helix-desktop/src-tauri/Cargo.lock
    - helix-desktop/src-tauri/src/lib.rs
    - helix-desktop/src-tauri/src/main.rs
    - helix-desktop/src-tauri/build.rs
    - helix-desktop/src-tauri/capabilities/default.json
    - helix-desktop/src-tauri/gen/schemas/ (4 generated schema files)
  modified: []
decisions:
  - "Used tonic 0.14.6 (latest 0.14.x, resolves to correct prost transitively)"
  - "Did not add prost explicitly — let tonic resolve its required version"
  - "Converted create-tauri-app v1 scaffold output to Tauri v2 manually (scaffold CLI produced v1 config despite @2 pin)"
  - "Wrote minimal App.tsx without greet command (v1 greet removed, lib.rs is v2 clean)"
  - "tsconfig.node.json uses composite:true without noEmit to satisfy TypeScript project references"
  - "tsconfig.node.json drops allowImportingTsExtensions (incompatible with composite)"
metrics:
  duration: "~30 minutes"
  completed: "2026-05-28"
  tasks_completed: 2
  tasks_total: 2
  files_created: 18
  files_modified: 0
---

# Phase 5 Plan 01: Scaffold + Toolchain + Types Summary

**One-liner:** Tauri v2.11.2 + React 18 + TypeScript 5 strict scaffold with tonic 0.14 gRPC dependency and typed Agent interfaces mirroring C# proto contracts.

---

## Tasks Completed

| Task | Name | Commit | Key Files |
|------|------|--------|-----------|
| T01 | Bootstrap Tauri v2 Scaffold | 9b69b33 | helix-desktop/ (47 files) |
| T02 | Define TypeScript Type Contracts | 9b69b33 | src/types/agent.ts |

---

## Verification Evidence

| Check | Result | Details |
|-------|--------|---------|
| `npm ci` | PASS | 173 packages, 0 vulnerabilities |
| `npm run build` | PASS | tsc + vite build in 475ms, 0 TypeScript errors |
| `cargo check` | PASS | tonic 0.14.6, tauri 2.11.2, tauri-plugin-store 2.4.3 |
| `src/test/setup.ts` exists | PASS | imports @testing-library/jest-dom |
| `src/types/agent.ts` has 4 interfaces | PASS | AgentConnection, AgentHealth, DeviceAdapter, RunspaceStatus |
| prost not in Cargo.toml deps | PASS | Only in comment line; tonic resolves prost 0.13.5 transitively |
| @tauri-apps/plugin-store in package.json | PASS | ^2.0.0 (resolved 2.4.3) |

---

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] `create-tauri-app@2` generated Tauri v1 scaffold**
- **Found during:** T01 — checked generated package.json and Cargo.toml
- **Issue:** `npm create tauri-app@2` invoked `create-tauri-app@2.8.0` which generates Tauri v1 config (package.json with `@tauri-apps/api: ^1.2.0`, Cargo.toml with `tauri: 1.2`, v1 allowlist tauri.conf.json). The `@2` refers to the CLI major version, not the Tauri framework version.
- **Fix:** Manually rewrote all config files to Tauri v2: package.json updated to `@tauri-apps/api@^2.0.0`, Cargo.toml updated to `tauri = "2"` + `tauri-plugin-store = "2"`, tauri.conf.json rewritten with v2 schema (`$schema`, `app.windows`, `bundle`, `build.frontendDist`), lib.rs created with `#[cfg_attr(mobile, tauri::mobile_entry_point)]`, main.rs updated to call `helix_desktop_lib::run()`.
- **Files modified:** helix-desktop/package.json, helix-desktop/src-tauri/Cargo.toml, helix-desktop/src-tauri/tauri.conf.json, helix-desktop/src-tauri/src/lib.rs, helix-desktop/src-tauri/src/main.rs
- **Commit:** 9b69b33

**2. [Rule 1 - Bug] App.tsx imported from Tauri v1 path `@tauri-apps/api/tauri`**
- **Found during:** T01 — generated App.tsx used `import { invoke } from "@tauri-apps/api/tauri"` (v1 path)
- **Issue:** Tauri v2 moved to `@tauri-apps/api/core`; the v1 path would cause a build error with v2 packages. Also referenced a `greet` command removed from lib.rs.
- **Fix:** Replaced App.tsx with a clean minimal component with no tauri invoke calls (scaffold-only placeholder per Phase 5 scope).
- **Files modified:** helix-desktop/src/App.tsx
- **Commit:** 9b69b33

**3. [Rule 1 - Bug] tsconfig.node.json `allowImportingTsExtensions` incompatible with `composite: true`**
- **Found during:** T01 — `npm run build` failed with TS6306/TS6310 requiring `composite: true`, but adding `composite` while keeping `noEmit` and `allowImportingTsExtensions` produced TS5096 error
- **Issue:** `allowImportingTsExtensions` requires `noEmit` or `emitDeclarationOnly`, but `composite: true` requires emit to be enabled. vite.config.ts does not use `.ts` extension imports so the option is not needed.
- **Fix:** Removed `allowImportingTsExtensions` and `noEmit` from tsconfig.node.json; kept `composite: true` and `isolatedModules: true`.
- **Files modified:** helix-desktop/tsconfig.node.json
- **Commit:** 9b69b33

**4. [Rule 1 - Deviation] Used `tonic = "0.12"` initially; corrected to `"0.14"` per plan**
- **Found during:** T01 post-cargo-check review — noticed Cargo resolved tonic v0.12.3 when plan specifies 0.14
- **Fix:** Updated Cargo.toml to `tonic = "0.14"` and `tonic-build = "0.14"`. Cargo re-resolved to 0.14.6; cargo check passed.
- **Files modified:** helix-desktop/src-tauri/Cargo.toml
- **Commit:** 9b69b33

**5. [Rule 3 - Environment] Rust 1.72.0 below Tauri v2 minimum — updated to 1.95.0**
- **Found during:** T01 pre-flight per RESEARCH.md Pitfall 1
- **Action:** Ran `rustup update stable` before scaffolding. Updated from 1.72.0 to 1.95.0 (stable).
- **No files modified** — system-level fix

---

## Known Stubs

None. helix-desktop/ is a scaffold with a minimal placeholder App.tsx. The plan goal is to produce a buildable scaffold — no data flows are expected at this phase.

---

## Threat Flags

No new network endpoints, auth paths, or schema changes introduced. This is a local desktop app scaffold only.

---

## Self-Check: PASSED

- [x] `helix-desktop/` directory created at repo root — commit 9b69b33
- [x] `npm ci` exits 0 — 173 packages, 0 vulnerabilities
- [x] `npm run build` exits 0 — 0 TypeScript errors, strict mode
- [x] `cargo check` exits 0 — tonic 0.14.6, no prost conflict
- [x] `src/test/setup.ts` exists — imports @testing-library/jest-dom
- [x] `src/types/agent.ts` defines AgentConnection, AgentHealth, DeviceAdapter, RunspaceStatus + DEFAULT_CONNECTION
- [x] `prost` NOT explicitly listed in Cargo.toml dependencies (only in comment)
- [x] `@tauri-apps/plugin-store` in package.json dependencies
