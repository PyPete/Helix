# Phase 5: Tauri Desktop App — Foundation — Research

**Researched:** 2026-05-28
**Domain:** Tauri v2, React + TypeScript + Vite, gRPC-Web, Connect-ES, CI/CD
**Confidence:** HIGH (core scaffold), MEDIUM (gRPC connectivity approach), HIGH (CI)

---

## Summary

Phase 5 scaffolds the `helix-desktop/` Tauri v2 desktop application targeting Windows 11 and
Linux (Debian/Ubuntu). The app connects directly to the Helix Agent REST/gRPC in standalone mode
and presents a device registry view with health status. There is no existing frontend code in the
repo — this is a fully greenfield project within the monorepo.

The most consequential technical decision is **how the webview communicates with the Agent gRPC**.
Browsers (including Tauri's embedded WebView2/WebKitGTK) cannot initiate native HTTP/2 gRPC calls
directly. Three viable strategies exist, each with different tradeoffs for this phase:

1. **Tauri Rust commands proxy gRPC** — the TypeScript frontend invokes `invoke()` calls; the Rust
   backend uses the `tonic` crate to make real gRPC calls and serialises results back as JSON.
   This is the most robust approach for Phase 5 because it avoids modifying the Agent and works with
   both insecure and TLS gRPC.

2. **gRPC-Web (agent-side middleware)** — add `Grpc.AspNetCore.Web` NuGet to the Agent, enable CORS,
   and use `@connectrpc/connect-web` with gRPC-Web transport in the frontend. This is clean and
   gives the frontend full type-safe stubs, but requires a small Agent-side change.

3. **REST-only polling** — the Agent exposes `GET /health`; for device status the Tauri frontend
   would need either a new REST endpoint on the Agent or to accept health-only information. This is
   the fallback path if gRPC integration proves complex.

**Primary recommendation:** Use the **Rust tonic proxy** approach for Phase 5 (minimal footprint,
no Agent changes, works immediately with existing proto contracts). Design the TypeScript layer to
be interface-driven so the gRPC-Web approach can be swapped in during Phase 6 if needed.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Connection config (host/port/protocol) | Tauri Rust (store plugin) | React UI form | Config is persisted in OS-appropriate appdata via store plugin; UI is just the editor |
| gRPC calls to Agent | Tauri Rust backend (tonic) | — | WebView cannot issue native gRPC; Rust crosses the protocol boundary |
| Health polling (REST `/health`) | React frontend (fetch) | Tauri Rust | Simple GET; browser fetch works fine for REST; no proxy needed for HTTP/1.1 |
| Device registry display | React frontend | — | Pure rendering; data comes from Rust commands |
| Build and packaging | Tauri CLI (src-tauri) | GitHub Actions tauri-action | Rust build system; CI runner provides Linux build environment |
| TypeScript type safety for Agent API | Generated code (connect-es or manual types) | — | Types generated from proto at build time |

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| C1 | Tauri app scaffold targeting Windows 11 + Linux (Debian/Ubuntu) | `npm create tauri-app@latest` with React+TypeScript+Vite template; Windows uses WebView2 (already in Win10+), Linux needs apt packages |
| C7 | Standalone mode: app connects directly to Agent REST/gRPC without Site Server | Tauri Rust commands via `tonic`; React polling `GET /health` via fetch; no Site Server dependency |
| C8 | Device registry view: list of adapters, connection status, health check results | `DeviceAdapter.HealthCheck` RPC + `DeviceAdapter.GetCapabilities` RPC invoked via Rust command; results rendered in React component |

</phase_requirements>

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `@tauri-apps/cli` | 2.11.2 [VERIFIED: npm registry] | Tauri build toolchain (dev dep) | Official Tauri CLI; `npm create tauri-app` scaffolds with it |
| `@tauri-apps/api` | 2.11.0 [VERIFIED: npm registry] | TypeScript IPC bindings (`invoke`, events) | Official Tauri JS API; required for all frontend-Rust communication |
| `react` | 18.x [ASSUMED] | UI framework | Chosen in phase description; `create-tauri-app` includes React template |
| `react-dom` | 18.x [ASSUMED] | React DOM renderer | Paired with React |
| `typescript` | 5.x [ASSUMED] | Type safety | Standard for React+Vite projects |
| `vite` | 6.x [ASSUMED] | Frontend dev server and bundler | Tauri's default for React+TS; fast HMR; embedded in `create-tauri-app` template |
| `tauri` (Rust crate) | 2.11.2 [VERIFIED: cargo registry] | Tauri Rust core | Mandatory Rust dependency |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `@tauri-apps/plugin-store` | 2.4.3 [VERIFIED: npm registry] | Persistent key-value store | Persisting agent connection settings (host/port/protocol) |
| `tauri-plugin-store` (Rust) | 2.4.3 [VERIFIED: cargo registry] | Store plugin Rust side | Paired with JS plugin; registered in `lib.rs` |
| `tonic` (Rust crate) | 0.14.6 [VERIFIED: cargo registry] | gRPC client in Rust backend | Rust-side gRPC calls to Agent from Tauri commands |
| `prost` (Rust crate) | [ASSUMED] | Protobuf message types (used by tonic) | Code-generated from .proto files |
| `tokio` (Rust crate) | [ASSUMED] | Async runtime | Required by tonic; Tauri already uses tokio |
| `serde` + `serde_json` (Rust) | [ASSUMED] | Serialise gRPC responses to JSON for IPC | Standard Rust serialisation; Tauri requires Serialize on command return types |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Rust tonic proxy | grpc-web via `@connectrpc/connect-web` frontend | grpc-web requires Agent-side `Grpc.AspNetCore.Web` middleware + CORS config; preferred for Phase 6 once Agent changes are acceptable |
| Rust tonic proxy | REST bridge (`@tauri-apps/plugin-http` + fetch) | Only works for REST endpoints; Agent currently has only `GET /health` and gRPC — no REST device endpoints exist |
| `@tauri-apps/plugin-store` | `localStorage` | `localStorage` is per-origin and cleared by OS; store plugin persists in OS appdata directory (platform-standard paths) |
| Vite | Webpack/CRA | Tauri recommends Vite; much faster; `create-tauri-app` uses it by default |

### Installation (helix-desktop/ root)

```bash
# Scaffold (run from repo root, creates helix-desktop/)
npm create tauri-app@latest helix-desktop -- --template react-ts

# Inside helix-desktop/ — add store plugin
cd helix-desktop
npm run tauri add store

# TypeScript tooling for connect-es (optional — only needed if switching to grpc-web later)
# npm install @connectrpc/connect @connectrpc/connect-web @bufbuild/protobuf
# npm install -D @bufbuild/protoc-gen-es @connectrpc/protoc-gen-connect-es
```

---

## Package Legitimacy Audit

> slopcheck flagged all packages as [SLOP] because it checked the PyPI registry for npm packages.
> This is the known cross-ecosystem false-positive described in the package legitimacy protocol.
> All packages were independently verified on the npm registry with `npm view` and on cargo.io
> with `cargo search`. No postinstall scripts were found on any package. All source repositories
> are confirmed canonical.

| Package | Registry | Age | Downloads | Source Repo | slopcheck | Disposition |
|---------|----------|-----|-----------|-------------|-----------|-------------|
| `@tauri-apps/cli` | npm | ~4 yrs (Apr 2021) | Very high | github.com/tauri-apps/tauri | [SLOP]* | Approved — verified npm + GitHub |
| `@tauri-apps/api` | npm | ~4 yrs (Apr 2021) | Very high | github.com/tauri-apps/tauri | [SLOP]* | Approved — verified npm + GitHub |
| `@tauri-apps/plugin-store` | npm | ~2 yrs (May 2023) | High | github.com/tauri-apps/plugins-workspace | [SLOP]* | Approved — verified npm + GitHub |
| `@tauri-apps/plugin-http` | npm | ~2 yrs (May 2023) | High | github.com/tauri-apps/plugins-workspace | [SLOP]* | Approved — verified npm + GitHub |
| `@connectrpc/connect` | npm | ~2 yrs (Aug 2023) | High | github.com/connectrpc/connect-es | [SLOP]* | Approved — verified npm + GitHub |
| `@connectrpc/connect-web` | npm | ~2 yrs (Aug 2023) | High | github.com/connectrpc/connect-es | [SLOP]* | Approved — verified npm + GitHub |
| `@bufbuild/protobuf` | npm | ~3 yrs (Mar 2022) | Very high | github.com/bufbuild/protobuf-es | [SLOP]* | Approved — verified npm + GitHub |
| `@bufbuild/protoc-gen-es` | npm | ~3 yrs (Mar 2022) | High | github.com/bufbuild/protobuf-es | [SLOP]* | Approved — verified npm + GitHub |
| `@connectrpc/protoc-gen-connect-es` | npm | ~2 yrs (Aug 2023) | High | github.com/connectrpc/connect-es | [SLOP]* | Approved — verified npm + GitHub |

**\* slopcheck ran against PyPI (Python) registry for these npm packages — cross-ecosystem false positive. The [SLOP] verdict is invalid. All packages verified on npm with `npm view` and confirmed against official GitHub repositories.**

**Packages removed due to slopcheck [SLOP] verdict:** None — all [SLOP] verdicts were false positives from PyPI/npm ecosystem mismatch.
**Packages flagged as suspicious [SUS]:** None.

---

## Architecture Patterns

### System Architecture Diagram

```
 helix-desktop/
 ┌─────────────────────────────────────────────────────────────┐
 │  React + TypeScript (Vite)   [WebView2 / WebKitGTK]         │
 │                                                              │
 │  ConnectionConfigScreen ──────────────────────────────────┐ │
 │  DeviceRegistryView ─────→ invoke('get_device_health', …) │ │
 │  HealthStatusBar ────────→ fetch('http://host/health')    │ │
 └─────────────────────────────────────────────────────────────┘
                         │ Tauri IPC (invoke / events)
 ┌─────────────────────────────────────────────────────────────┐
 │  Tauri Rust Backend  (src-tauri/)                            │
 │                                                              │
 │  #[tauri::command] get_device_health(host, port)             │
 │  #[tauri::command] get_device_capabilities(host, port)       │
 │  #[tauri::command] save_connection_config(config)            │
 │  #[tauri::command] load_connection_config()                  │
 │                                                              │
 │  tonic gRPC client ──────────────────────────────────────┐  │
 │  tauri_plugin_store ─→ OS appdata store.json             │  │
 └─────────────────────────────────────────────────────────────┘
                         │ gRPC (HTTP/2, tonic)
 ┌───────────────────────▼─────────────────────────────────────┐
 │  Helix Agent  (Helix.Orchestration.Agent)                    │
 │                                                              │
 │  DeviceAdapter service  (adapter.proto)                      │
 │    ├── HealthCheck() → HealthStatus                          │
 │    ├── GetCapabilities() → CapabilitiesResponse              │
 │    └── StreamTrends() → stream TrendValue                    │
 │  AgentControl service  (agent_control.proto)                 │
 │    ├── GetRunspaceStatus()                                    │
 │    └── SubmitExperiment() …                                  │
 │  REST: GET /health                                           │
 └─────────────────────────────────────────────────────────────┘
```

### Recommended Project Structure

```
helix-desktop/
├── src/                         # React TypeScript frontend
│   ├── components/
│   │   ├── ConnectionConfig.tsx # Host/port/protocol config screen
│   │   └── DeviceRegistry.tsx   # Adapter list + health status
│   ├── hooks/
│   │   ├── useAgentConnection.ts # Connection state management
│   │   └── useDeviceHealth.ts    # Polling hook
│   ├── services/
│   │   └── agentService.ts       # invoke() wrapper functions
│   ├── types/
│   │   └── agent.ts              # TypeScript types for Agent responses
│   ├── App.tsx
│   └── main.tsx
├── src-tauri/
│   ├── src/
│   │   ├── lib.rs               # App setup, plugin registration, command registration
│   │   ├── commands/
│   │   │   ├── mod.rs
│   │   │   ├── connection.rs    # save/load_connection_config commands
│   │   │   └── device.rs        # get_device_health, get_device_capabilities commands
│   │   └── grpc/
│   │       ├── mod.rs
│   │       └── agent_client.rs  # tonic client wrapper (DeviceAdapter + AgentControl)
│   ├── proto/                   # Symlink or copy of adapter.proto + agent_control.proto
│   │   ├── adapter.proto
│   │   └── agent_control.proto
│   ├── build.rs                 # tonic_build::compile_protos() calls
│   ├── Cargo.toml
│   └── tauri.conf.json
├── package.json
└── vite.config.ts
```

### Pattern 1: Tauri Command for gRPC Proxy

**What:** A `#[tauri::command]` async function in Rust that opens a tonic channel to the Agent
at the configured address, calls the RPC, and returns a serializable result to the frontend.

**When to use:** Any RPC call from the frontend that the webview cannot make natively.

```rust
// Source: Tauri v2 docs (https://v2.tauri.app/develop/calling-rust/)
// src-tauri/src/commands/device.rs

use serde::Serialize;
use tauri::State;

#[derive(Serialize)]
pub struct DeviceHealthResult {
    pub healthy: bool,
    pub message: String,
}

#[tauri::command]
pub async fn get_device_health(
    host: String,
    port: u16,
) -> Result<DeviceHealthResult, String> {
    let addr = format!("http://{}:{}", host, port);
    let mut client = helix_adapter_client::DeviceAdapterClient::connect(addr)
        .await
        .map_err(|e| e.to_string())?;
    let response = client
        .health_check(tonic::Request::new(adapter::Empty {}))
        .await
        .map_err(|e| e.to_string())?;
    let status = response.into_inner();
    Ok(DeviceHealthResult {
        healthy: status.healthy,
        message: status.message,
    })
}
```

```typescript
// Source: Tauri v2 docs (https://v2.tauri.app/develop/calling-rust/)
// src/services/agentService.ts
import { invoke } from '@tauri-apps/api/core';

export interface DeviceHealth { healthy: boolean; message: string; }

export async function getDeviceHealth(host: string, port: number): Promise<DeviceHealth> {
  return invoke<DeviceHealth>('get_device_health', { host, port });
}
```

### Pattern 2: Store Plugin for Connection Config

**What:** Persist and reload connection configuration (host, port, protocol) using the Tauri
store plugin, which writes to a JSON file in the OS appdata directory.

**When to use:** Any user-configurable setting that must survive app restarts.

```typescript
// Source: Tauri v2 store plugin docs (https://v2.tauri.app/plugin/store/)
import { load } from '@tauri-apps/plugin-store';

interface ConnectionConfig {
  host: string;
  port: number;
  protocol: 'grpc' | 'rest';
}

const DEFAULT_CONFIG: ConnectionConfig = { host: 'localhost', port: 5000, protocol: 'grpc' };

export async function loadConfig(): Promise<ConnectionConfig> {
  const store = await load('helix-config.json', { autoSave: true });
  return (await store.get<ConnectionConfig>('connection')) ?? DEFAULT_CONFIG;
}

export async function saveConfig(config: ConnectionConfig): Promise<void> {
  const store = await load('helix-config.json', { autoSave: true });
  await store.set('connection', config);
}
```

### Pattern 3: tonic build.rs for Proto Compilation

**What:** Use `tonic-build` in `build.rs` to compile `.proto` files into Rust client stubs
at build time. This avoids maintaining hand-written Rust types.

```toml
# Source: tonic docs (https://docs.rs/tonic-build/latest/tonic_build/)
# src-tauri/Cargo.toml (additions)
[dependencies]
tonic = "0.14"
prost = "0.13"
tokio = { version = "1", features = ["full"] }
serde = { version = "1", features = ["derive"] }
serde_json = "1"

[build-dependencies]
tonic-build = "0.14"
```

```rust
// src-tauri/build.rs
fn main() {
    tonic_build::compile_protos("proto/adapter.proto")
        .unwrap_or_else(|e| panic!("Failed to compile protos: {}", e));
    tonic_build::compile_protos("proto/agent_control.proto")
        .unwrap_or_else(|e| panic!("Failed to compile protos: {}", e));
    tauri_build::build()
}
```

### Pattern 4: REST Health Polling from React

**What:** The Agent exposes `GET /health`. React can poll this directly using the browser
`fetch` API (no Tauri plugin needed for same-origin-equivalent local HTTP calls).

**Caveat:** Tauri v2's HTTP plugin is needed only if the URL is external and requires
the Rust-side security scope enforcement. For local `http://localhost:PORT/health` calls,
standard `fetch()` works in the webview.

```typescript
// Standard browser fetch — works in Tauri webview for localhost
export async function checkRestHealth(host: string, port: number): Promise<boolean> {
  try {
    const response = await fetch(`http://${host}:${port}/health`, { signal: AbortSignal.timeout(3000) });
    return response.ok;
  } catch {
    return false;
  }
}
```

### Anti-Patterns to Avoid

- **Using `@grpc/grpc-js` in the Vite/browser bundle:** This is a Node.js library and will not
  work in a browser-environment webview. Tauri's webview is a browser — Node.js modules are not
  available in the frontend unless explicitly shimmed. Use Rust commands instead.

- **Hardcoding `http://localhost` as the Agent URL:** The whole purpose of Phase 5 is
  standalone connectivity where the agent may run at a different host or port. Always read from
  the store plugin config.

- **Storing connection config in `localStorage`:** `localStorage` is volatile in Tauri and can
  be cleared; the store plugin writes to OS appdata which survives app updates.

- **Using the Tauri HTTP plugin with wildcard `*` URLs:** There is no such wildcard permission.
  For dynamic-host REST calls (user enters any host:port), use a Tauri Rust command with
  `reqwest` instead, which has no URL restriction at the Rust level.

- **Expecting native HTTP/2 gRPC from the webview:** Tauri's embedded WebView2 (Windows) and
  WebKitGTK (Linux) are browsers — they cannot issue native HTTP/2 gRPC frames. The gRPC-Web
  protocol (via `Grpc.AspNetCore.Web`) is the browser-compatible subset, but it requires
  Agent-side changes. Use Rust tonic commands in Phase 5 to avoid that dependency.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Persistent settings | Custom file I/O in Rust or JS localStorage | `@tauri-apps/plugin-store` | Handles OS-appropriate paths, atomic writes, crash safety |
| gRPC client from Rust | Hand-written HTTP/2 framing | `tonic` crate | Generates type-safe clients from proto at build time; handles multiplexing, streaming, error codes |
| Protobuf serialization in Rust | Manual byte packing | `prost` (via tonic-build) | Generated at build time from exact same `.proto` files the Agent uses; zero desync risk |
| App window management | Custom JS window APIs | Tauri window API (`@tauri-apps/api/window`) | Native window controls including resize, close, minimize |
| CI build for Tauri | Custom Dockerfile or manual build scripts | `tauri-apps/tauri-action` GitHub Action | Handles multi-platform builds, Rust setup, system deps, artifact upload |

**Key insight:** gRPC from a Tauri frontend has a fundamental browser limitation — the Rust
backend is the only path to native gRPC. Routing everything through `invoke()` commands keeps
the architecture clean and the Agent unmodified.

---

## Common Pitfalls

### Pitfall 1: Rust Version Too Old for Tauri v2

**What goes wrong:** `cargo build` in `src-tauri/` fails with "minimum supported Rust version"
error. Tauri 2.x requires a recent stable Rust (approximately 1.77+). The machine currently has
`rustc 1.72.0`.

**Why it happens:** `rustup` defaulted to the stable toolchain from 2023; Tauri v2 requires a
newer minimum.

**How to avoid:** Update Rust before scaffolding:
```bash
rustup update stable
```

**Warning signs:** Error mentions `rustc` version or `edition2021` feature not available.

### Pitfall 2: Missing Linux System Dependencies on CI

**What goes wrong:** `ubuntu-latest` GitHub Actions runner missing `libwebkit2gtk-4.1-dev`
and related packages; Tauri build fails during `cargo build` with linker errors.

**Why it happens:** ubuntu-latest does not have GTK/WebKit headers; they are not part of the
default runner image.

**How to avoid:** Add this step before the Tauri build in the workflow:
```yaml
- name: Install Linux dependencies
  if: runner.os == 'Linux'
  run: |
    sudo apt-get update
    sudo apt-get install -y libwebkit2gtk-4.1-dev libappindicator3-dev librsvg2-dev patchelf
```

[VERIFIED: tauri-apps/tauri-action README — exact package list]

**Warning signs:** `error: linking with cc failed` or `pkg-config` cannot find `webkit2gtk`.

### Pitfall 3: tonic Version Mismatch with Tauri's tokio

**What goes wrong:** `tonic` crate pulls in a different version of `tokio` than Tauri's
internal runtime; Cargo resolver error or runtime panic about "no current runtime".

**Why it happens:** `tonic 0.14` targets tokio 1.x; Tauri 2.x also uses tokio 1.x — they
should be compatible, but the `features` on tokio need to be aligned.

**How to avoid:** In `Cargo.toml`, specify `tokio = { version = "1", features = ["full"] }`.
Tauri manages its own tokio; tonic commands run on the same runtime automatically.

**Warning signs:** Panic at runtime: "Cannot start a runtime from within a runtime."

### Pitfall 4: gRPC TLS/insecure mismatch

**What goes wrong:** The Agent runs without TLS (insecure gRPC) in development; `tonic`
defaults to TLS-required channels.

**Why it happens:** `Channel::from_static` or `connect()` with an `http://` address must
explicitly be set to insecure, or tonic rejects the connection.

**How to avoid:** Use `tonic::transport::Channel::from_static("http://…")` (not `https://`),
or use `Endpoint::new(addr)?.connect_lazy()` for insecure. For production add TLS config.

### Pitfall 5: Tauri v2 Capability Permissions Missing

**What goes wrong:** App runs but `invoke()` calls silently fail or throw permission errors;
store plugin reads/writes do nothing.

**Why it happens:** Tauri v2 replaced the v1 allowlist with a capabilities system; newly
added plugins need capability entries in `src-tauri/capabilities/default.json`.

**How to avoid:** After `npm run tauri add store`, verify `src-tauri/capabilities/default.json`
includes `"store:default"`. After adding HTTP plugin, include relevant `http:*` permissions.
The `tauri add` command handles this automatically for official plugins.

### Pitfall 6: `create-tauri-app` Places Frontend at Repo Root by Default

**What goes wrong:** Running `npm create tauri-app@latest` in the repo root creates
`package.json` at the top level, mixing Tauri config with the .NET solution.

**Why it happens:** The scaffold targets the current directory by default.

**How to avoid:** Pass the target directory explicitly:
```bash
npm create tauri-app@latest helix-desktop -- --template react-ts
```
This creates everything under `helix-desktop/` isolated from the .NET projects.

---

## Runtime State Inventory

This is a greenfield phase — no rename/refactor involved. No runtime state inventory required.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Node.js | Vite / npm / Tauri CLI | YES | v24.12.0 | — |
| npm | Package management | YES | 11.6.2 | — |
| Rust (stable) | Tauri build | YES (outdated) | 1.72.0 — needs update | `rustup update stable` |
| Cargo | Rust build | YES | 1.72.0 | — |
| WebView2 Runtime | Windows Tauri app | YES* | Pre-installed Win10/11 | Download Evergreen Bootstrapper |
| WebKitGTK (Linux) | Linux Tauri app | NO (CI only) | — | Install via apt in CI workflow |
| `libwebkit2gtk-4.1-dev` (Linux) | Tauri build on Linux | NO | — | `sudo apt-get install libwebkit2gtk-4.1-dev libappindicator3-dev librsvg2-dev patchelf` |
| `pkg-config` (Linux CI) | tonic/Tauri build | Pre-installed on ubuntu-latest | — | — |
| Microsoft C++ Build Tools | Windows Tauri build | YES* | VS Build Tools on dev machine | Install VS Build Tools |

**Missing dependencies with no fallback:**
- Rust version: current `1.72.0` is below Tauri v2's minimum. `rustup update stable` is required before first build. [ASSUMED minimum ~1.77; verify with `rustup update` then check tauri build error]

**Missing dependencies with fallback:**
- WebKitGTK: not present on dev machine (Windows); Linux only needed in CI — install via apt step.

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Tauri v1 allowlist (`tauri.allowlist`) | Tauri v2 capabilities (`src-tauri/capabilities/*.json`) | Tauri 2.0 stable (Oct 2024) | Permissions are now per-window and more granular; v1 config will not work |
| `@tauri-apps/api/tauri` module | `@tauri-apps/api/core` | Tauri 2.0 | Import path changed; old import will throw |
| Tauri v1 `tauri.window` config | `app.windows` in `tauri.conf.json` | Tauri 2.0 | Config key renamed |
| `Window` type (Tauri JS API) | `WebviewWindow` | Tauri 2.0 | Type/class renamed |
| Built-in filesystem/HTTP in core | Separate plugin packages (`@tauri-apps/plugin-http`, etc.) | Tauri 2.0 | Must install plugins explicitly; `tauri add <plugin>` handles this |
| gRPC from frontend via `@grpc/grpc-js` | Rust tonic command proxy OR gRPC-Web + connect-es | n/a | Node.js gRPC never worked in browsers; the Rust proxy is the correct pattern |

**Deprecated/outdated:**
- `@tauri-apps/api/tauri`: replaced by `@tauri-apps/api/core` — any tutorial using the old path is Tauri v1.
- `grpc-web` npm package (from `grpc/grpc-web`): the older Google-maintained client; `@connectrpc/connect-web` with gRPC-Web transport is the modern replacement with better TypeScript support.
- `Opc.UaFx.Advanced` commercial OPC package: already removed in Phase 1.

---

## gRPC-Web Path (for Phase 6 reference)

If the plan later requires a full TypeScript gRPC client without Rust proxying (e.g., for streaming trends from the frontend), the gRPC-Web approach requires:

**Agent side (one-time addition):**
```csharp
// Helix.Orchestration.Agent/Program.cs
builder.Services.AddGrpc();
// ...
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
app.UseCors("AllowAll");
// Must configure Kestrel for Http1AndHttp2 in appsettings.json
```
[CITED: https://learn.microsoft.com/en-us/aspnet/core/grpc/grpcweb]

**Frontend code generation:**
```yaml
# helix-desktop/buf.gen.yaml
version: v2
plugins:
  - local: protoc-gen-es
    out: src/gen
    opt: target=ts
  - local: protoc-gen-connect-es
    out: src/gen
    opt: target=ts
inputs:
  - directory: ../Helix.Orchestration.Contracts/Protos
```

```bash
npx buf generate
```

**Limitation:** `StreamTrends` is a server-streaming RPC. gRPC-Web supports server streaming
only (not client or bidirectional). This is sufficient for Phase 5/6 trend polling. [CITED: MS docs]

---

## CI Build Pattern

The existing `.github/workflows/ci.yml` targets `ubuntu-latest` and does .NET build/test only.
Phase 5 adds a new job for Tauri.

```yaml
# .github/workflows/ci.yml — new job to add
tauri-build:
  name: Tauri Desktop Build
  runs-on: ubuntu-latest

  steps:
    - uses: actions/checkout@v4

    - name: Install Linux system dependencies
      run: |
        sudo apt-get update
        sudo apt-get install -y \
          libwebkit2gtk-4.1-dev \
          libappindicator3-dev \
          librsvg2-dev \
          patchelf

    - uses: actions/setup-node@v4
      with:
        node-version: 20
        cache: 'npm'
        cache-dependency-path: helix-desktop/package-lock.json

    - uses: dtolnay/rust-toolchain@stable

    - uses: Swatinem/rust-cache@v2
      with:
        workspaces: helix-desktop/src-tauri -> target

    - name: Install frontend dependencies
      run: npm ci
      working-directory: helix-desktop

    - name: Build Tauri app (Linux)
      uses: tauri-apps/tauri-action@v0
      with:
        projectPath: helix-desktop
        args: --target x86_64-unknown-linux-gnu
```

[CITED: tauri-apps/tauri-action GitHub — https://github.com/tauri-apps/tauri-action]

**Note on Windows CI:** GitHub Actions Windows runner is `windows-latest`. For Phase 5, Linux
build is the CI target. Windows build is verified locally during dev. A Windows matrix entry
can be added in Phase 8.

---

## Validation Architecture

> `workflow.nyquist_validation` is `true` in `.planning/config.json` — section included.

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Vitest 4.x (co-installed with Vite) |
| Config file | `helix-desktop/vite.config.ts` (vitest config inline) |
| Quick run command | `npm run test` in `helix-desktop/` |
| Full suite command | `npm run test -- --coverage` |

### Phase Requirements to Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| C1 | Tauri app builds without error | Build / smoke | `npm run tauri build` (CI) | Wave 0 |
| C7 | Standalone mode — connects to Agent without Site Server | Unit (mock invoke) | `npm test -- src/services/agentService.test.ts` | Wave 0 |
| C8 | Device registry displays adapter list and health status | Component (Testing Library) | `npm test -- src/components/DeviceRegistry.test.tsx` | Wave 0 |

**Note on testing Rust commands:** Tauri commands are Rust code tested with standard Rust unit
tests (`cargo test` in `src-tauri/`). gRPC calls are tested with a mock tonic server.

### Wave 0 Gaps

- [ ] `helix-desktop/src/services/agentService.test.ts` — covers C7 (mock `invoke`)
- [ ] `helix-desktop/src/components/DeviceRegistry.test.tsx` — covers C8 (renders adapter list)
- [ ] `helix-desktop/vite.config.ts` — add vitest configuration block
- [ ] `helix-desktop/src-tauri/src/commands/device_test.rs` — Rust unit tests for gRPC command
- [ ] Install: `npm install -D vitest @testing-library/react @testing-library/user-event jsdom`

---

## Security Domain

> `security_enforcement` not explicitly set in config.json; treated as enabled.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | No — Phase 5 is standalone, no user auth | — |
| V3 Session Management | No — desktop app, no web sessions | — |
| V4 Access Control | Partial — Tauri capability system restricts IPC | Tauri capabilities (`src-tauri/capabilities/`) |
| V5 Input Validation | Yes — host/port inputs from user | Validate host is valid hostname/IP; port is 1–65535 |
| V6 Cryptography | No — no secrets stored; gRPC is insecure in dev | TLS configuration deferred to Phase 10 |

### Known Threat Patterns for Tauri Stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Webview XSS via CSP bypass | Spoofing / Tampering | Tauri default CSP; do not disable `dangerousDisableAssetCspModification` |
| IPC command injection (malformed host string) | Tampering | Validate host/port in Rust before passing to tonic; reject non-IP/hostname inputs |
| Capability scope creep | Elevation of Privilege | Only grant `store:default` and required HTTP permissions; do not use `core:default` for all |
| Insecure gRPC (no TLS) in production | Information Disclosure | Acceptable in Phase 5 (local dev); flag as open question for Phase 10 hardening |

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | React 18.x and Vite 6.x are the versions installed by `create-tauri-app@latest` React template | Standard Stack | Scaffold will still work; version number in docs may be wrong |
| A2 | Tauri v2 minimum Rust version is approximately 1.77 | Environment Availability | If minimum is higher or lower, `rustup update stable` (to current stable) will always satisfy it |
| A3 | `tonic 0.14.6` is compatible with the version of tokio used by Tauri 2.11.2 | Standard Stack | Cargo resolver will surface incompatibility; fixable by version bump |
| A4 | The Agent runs without TLS in the lab environment (insecure gRPC) | Pitfall 4 | If TLS is required, tonic channel setup is more complex (certs needed) |
| A5 | `prost` version aligns automatically with `tonic 0.14` dependency | Standard Stack | Cargo resolver handles this; including prost explicitly may cause version conflict |

---

## Open Questions

1. **Should the Agent be modified to add `Grpc.AspNetCore.Web` middleware now or in Phase 6?**
   - What we know: gRPC-Web allows TypeScript to call gRPC directly; requires small Agent change;
     Rust tonic proxy works without Agent changes.
   - What's unclear: Phase 6 will need streaming trends; gRPC-Web server-streaming from TypeScript
     is possible; but Rust command proxy also supports streaming with Tauri events.
   - Recommendation: Defer Agent gRPC-Web change to Phase 6. Phase 5 uses Rust tonic proxy. Plan
     for migration by keeping the TypeScript API surface abstract.

2. **gRPC insecure vs TLS: is the Agent's Kestrel configured for HTTP/2 without TLS?**
   - What we know: ASP.NET Core Kestrel in development defaults to HTTP/2 with TLS (HTTP/1.1
     for HTTP). Without TLS, HTTP/2 requires explicit Kestrel config (`AllowInsecureHttp2`).
   - What's unclear: Current Agent `appsettings.json` content; whether it enables non-TLS HTTP/2.
   - Recommendation: Check Agent `appsettings.json` during Wave 0; add `"Protocols": "Http2"` with
     `"DisableTls": true` if not already configured.

3. **Proto file duplication: copy or symlink `.proto` files into `helix-desktop/src-tauri/proto/`?**
   - What we know: tonic-build needs to locate the `.proto` files at build time.
   - What's unclear: CI runner symlink support; whether a relative `build.rs` path traversal is
     allowed by `tonic_build`.
   - Recommendation: Copy the `.proto` files at scaffold time; add a comment noting canonical
     source is `Helix.Orchestration.Contracts/Protos/`. A future task can automate sync.

---

## Sources

### Primary (HIGH confidence)

- [CITED: https://v2.tauri.app/start/create-project/] — scaffold commands, template options, v2 setup
- [CITED: https://v2.tauri.app/plugin/store/] — store plugin API, LazyStore, capability config
- [CITED: https://v2.tauri.app/start/prerequisites/] — Linux apt packages, Windows requirements
- [CITED: https://v2.tauri.app/develop/calling-rust/] — invoke pattern, async commands, state access
- [CITED: https://v2.tauri.app/start/migrate/from-tauri-1/] — v1→v2 breaking changes summary
- [CITED: https://learn.microsoft.com/en-us/aspnet/core/grpc/browser] — gRPC browser limitations
- [CITED: https://learn.microsoft.com/en-us/aspnet/core/grpc/grpcweb] — Grpc.AspNetCore.Web, CORS, streaming limits
- [CITED: https://github.com/tauri-apps/tauri-action] — CI action, Linux deps, workflow pattern
- [CITED: https://connectrpc.com/docs/web/getting-started] — connect-web overview, TypeScript client
- [CITED: https://connectrpc.com/docs/web/generating-code] — buf.gen.yaml, code generation commands
- [VERIFIED: npm registry] — all npm package versions via `npm view`
- [VERIFIED: cargo registry] — tonic 0.14.6, tauri-plugin-store 2.4.3 via `cargo search`

### Secondary (MEDIUM confidence)

- [CITED: https://v2.tauri.app/plugin/http-client/] — HTTP plugin permissions (no wildcard URL)
- [CITED: https://v2.tauri.app/security/capabilities/] — capability JSON structure

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all packages verified on registries with official repos
- Architecture (Rust tonic proxy): HIGH — confirmed by Tauri IPC docs + tonic cargo existence
- gRPC-Web path: HIGH — confirmed by official MS ASP.NET Core docs
- CI setup: HIGH — confirmed by tauri-action README (exact apt package list)
- Pitfalls: HIGH — verified against official docs and registry checks
- Rust version requirement: MEDIUM — exact minimum is [ASSUMED]; `rustup update` always satisfies

**Research date:** 2026-05-28
**Valid until:** 2026-08-28 (90 days; Tauri and connect-es release frequently but API is stable)
