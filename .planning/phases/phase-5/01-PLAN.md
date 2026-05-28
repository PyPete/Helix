---
phase: phase-5
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - helix-desktop/package.json
  - helix-desktop/tsconfig.json
  - helix-desktop/tsconfig.node.json
  - helix-desktop/vite.config.ts
  - helix-desktop/vitest.config.ts
  - helix-desktop/index.html
  - helix-desktop/src/main.tsx
  - helix-desktop/src/App.css
  - helix-desktop/src/types/agent.ts
  - helix-desktop/src-tauri/tauri.conf.json
  - helix-desktop/src-tauri/Cargo.toml
  - helix-desktop/src-tauri/src/lib.rs
  - helix-desktop/src-tauri/capabilities/default.json
  - helix-desktop/src/test/setup.ts
autonomous: true
requirements:
  - C1

must_haves:
  truths:
    - "helix-desktop/ directory exists with package.json, tsconfig.json, vite.config.ts, vitest.config.ts"
    - "npm ci succeeds inside helix-desktop/"
    - "npm run build succeeds (TypeScript strict mode, no type errors)"
    - "Rust project compiles: cargo check in helix-desktop/src-tauri"
    - "src/types/agent.ts defines AgentConnection, AgentHealth, DeviceAdapter, RunspaceStatus interfaces"
  artifacts:
    - "helix-desktop/package.json"
    - "helix-desktop/src/types/agent.ts"
    - "helix-desktop/src-tauri/Cargo.toml"

key_links:
  - "src/types/agent.ts -> AgentConnection.port default 5080 (from AgentProxyOptions.cs)"
  - "src-tauri/Cargo.toml -> tonic = '0.14' (scaffold only; not called in Phase 5)"
  - "tsconfig.json -> strict: true (mirrors C# Nullable:enable)"
---

# Phase 5 — Plan 01: Scaffold + Toolchain + Types

**Goal:** Bootstrap the `helix-desktop/` Tauri v2 project with React + TypeScript + Vite, install all npm + Rust dependencies, configure strict TypeScript, and define the TypeScript type contracts for Agent communication.

---

## Threat Model

| Threat | Likelihood | Mitigation |
|--------|-----------|------------|
| `npm create tauri-app` CLI version mismatch generates wrong scaffold | Medium | Use `npm create tauri-app@2` pinned to v2 major |
| tonic version conflict with prost | High | Do NOT pin explicit `prost` — let tonic pull its transitive prost version |
| Rust toolchain not installed on dev machine | Medium | Executor must run `rustup show` first and install stable if absent |
| TypeScript strict mode breaks generated scaffold types | Low | Fix inline at scaffold time before adding application code |
| `@tauri-apps/plugin-store` v2 API differs from v1 | Low | Import path is `@tauri-apps/plugin-store` (no `/2` suffix); verified from RESEARCH.md |

---

## Deviation Protocol

If `npm create tauri-app@2` fails (CLI not available globally), fall back to:
```
npx create-tauri-app@2 helix-desktop --template react-ts --manager npm
```

If the generated scaffold produces a Cargo.toml with `tonic` not present (older template), add it manually in T01.

Do not add `prost` explicitly — this caused a version conflict (RESEARCH.md assumption A5).

---

## Tasks

### T01 — Bootstrap Tauri v2 Scaffold

**Type:** auto  
**Files:**
- `helix-desktop/package.json` (scaffold output, then patched)
- `helix-desktop/tsconfig.json` (scaffold output, then patched)
- `helix-desktop/tsconfig.node.json` (scaffold output)
- `helix-desktop/vite.config.ts` (scaffold output, then patched)
- `helix-desktop/vitest.config.ts` (CREATE)
- `helix-desktop/index.html` (scaffold output)
- `helix-desktop/src/main.tsx` (scaffold output)
- `helix-desktop/src/App.css` (CREATE minimal)
- `helix-desktop/src-tauri/tauri.conf.json` (scaffold output, then patched)
- `helix-desktop/src-tauri/Cargo.toml` (scaffold output, then patched)
- `helix-desktop/src-tauri/src/lib.rs` (scaffold output, then patched)
- `helix-desktop/src-tauri/capabilities/default.json` (CREATE)
- `helix-desktop/src/test/setup.ts` (CREATE)

**Actions:**

1. From repo root, run scaffold:
   ```
   npm create tauri-app@2 helix-desktop -- --template react-ts --manager npm
   ```
   If this fails, use: `npx create-tauri-app@2 helix-desktop --template react-ts --manager npm`

2. Inside `helix-desktop/`, install additional dependencies:
   ```
   npm install @tauri-apps/plugin-store
   npm install --save-dev vitest @vitest/coverage-v8 @testing-library/react @testing-library/jest-dom @testing-library/user-event jsdom
   ```

3. Patch `tsconfig.json` — ensure these compilerOptions are set:
   ```json
   "strict": true,
   "noUnusedLocals": true,
   "noUnusedParameters": true,
   "noFallthroughCasesInSwitch": true
   ```

4. Patch `tauri.conf.json` — set:
   ```json
   "productName": "helix-desktop",
   "identifier": "com.helix.desktop",
   "app.windows[0].title": "Helix Desktop",
   "app.windows[0].width": 1280,
   "app.windows[0].height": 800
   ```

5. CREATE `helix-desktop/vitest.config.ts`:
   ```typescript
   import { defineConfig } from "vitest/config";
   import react from "@vitejs/plugin-react";

   export default defineConfig({
     plugins: [react()],
     test: {
       globals: true,
       environment: "jsdom",
       setupFiles: ["./src/test/setup.ts"],
     },
   });
   ```

6. CREATE `helix-desktop/src/test/setup.ts`:
   ```typescript
   import "@testing-library/jest-dom";
   ```

7. Patch `helix-desktop/src-tauri/Cargo.toml` — add to `[dependencies]`:
   ```toml
   serde = { version = "1", features = ["derive"] }
   serde_json = "1"
   tokio = { version = "1", features = ["full"] }
   tonic = "0.14"
   # Do NOT add prost explicitly — let tonic pull its required version transitively
   ```

   In `[build-dependencies]` add:
   ```toml
   tonic-build = "0.14"
   ```

8. CREATE `helix-desktop/src-tauri/capabilities/default.json`:
   ```json
   {
     "$schema": "../node_modules/@tauri-apps/cli/schema/desktop-schema.json",
     "identifier": "default",
     "description": "Capability for main window",
     "windows": ["main"],
     "permissions": [
       "core:default",
       "store:default"
     ]
   }
   ```

9. Patch `helix-desktop/src-tauri/src/lib.rs` — ensure it is valid Rust with `#[cfg_attr(mobile, tauri::mobile_entry_point)]`:
   ```rust
   pub fn run() {
       tauri::Builder::default()
           .plugin(tauri_plugin_store::Builder::new().build())
           .invoke_handler(tauri::generate_handler![])
           .run(tauri::generate_context!())
           .expect("error while running tauri application");
   }
   ```

10. Patch `vite.config.ts` to add Tauri env-aware build settings (from PATTERNS.md):
    ```typescript
    build: {
      target: process.env.TAURI_ENV_PLATFORM === "windows" ? "chrome105" : "safari13",
      minify: !process.env.TAURI_ENV_DEBUG ? "esbuild" : false,
      sourcemap: !!process.env.TAURI_ENV_DEBUG,
    },
    envPrefix: ["VITE_", "TAURI_ENV_*"],
    ```

**Verify:**
```bash
cd helix-desktop
npm ci
npm run build        # tsc + vite build — must exit 0
cd src-tauri
cargo check          # Rust compile check — must exit 0
```

**Acceptance criteria:**
- `npm ci` exits 0
- `npm run build` exits 0 with 0 TypeScript errors
- `cargo check` exits 0 (tonic present, no prost conflict)
- `src/test/setup.ts` exists and imports `@testing-library/jest-dom`

---

### T02 — Define TypeScript Type Contracts

**Type:** auto  
**Files:**
- `helix-desktop/src/types/agent.ts` (CREATE)

**Actions:**

CREATE `helix-desktop/src/types/agent.ts`:
```typescript
// Connection config — mirrors AgentProxyOptions.cs defaults (localhost:5080)
export interface AgentConnection {
  host: string;
  port: number;
  protocol: "http" | "https";
}

export const DEFAULT_CONNECTION: AgentConnection = {
  host: "localhost",
  port: 5080,
  protocol: "http",
};

// REST /health response (ASP.NET Core HealthChecks format)
export interface AgentHealth {
  status: "Healthy" | "Unhealthy" | "Degraded";
}

// Device adapter health — derived from REST /health response
// Phase 5: synthesised from health endpoint; Phase 6 will use DeviceAdapter gRPC RPC
export interface DeviceAdapter {
  adapterId: string;
  healthy: boolean;
  message: string;
  capabilities: string[];
}

// Runspace status — from agent_control.proto RunspaceStatusResponse (Phase 6)
export interface RunspaceStatus {
  runspaceId: string;
  status: "Running" | "Completed" | "Faulted" | "NotFound";
  message: string;
}
```

**Verify:**
```bash
cd helix-desktop && npm run build
```

**Acceptance criteria:**
- `src/types/agent.ts` exports all 4 interfaces + `DEFAULT_CONNECTION`
- `npm run build` still exits 0

---

## Self-Check

- [ ] `helix-desktop/` directory created at repo root
- [ ] `npm ci` inside `helix-desktop/` exits 0
- [ ] `npm run build` exits 0 with strict TypeScript
- [ ] `cargo check` in `helix-desktop/src-tauri/` exits 0
- [ ] `src/test/setup.ts` exists (required by vitest setupFiles)
- [ ] `src/types/agent.ts` defines all 4 interfaces
- [ ] `prost` is NOT explicitly listed in Cargo.toml dependencies
- [ ] `@tauri-apps/plugin-store` is in package.json dependencies
