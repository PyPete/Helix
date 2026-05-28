---
phase: phase-5
plan: 02
type: execute
wave: 2
depends_on:
  - phase-5/01-PLAN.md
files_modified:
  - helix-desktop/src/hooks/useAgentHealth.ts
  - helix-desktop/src/hooks/useAgentHealth.test.ts
  - helix-desktop/src/components/ConnectionConfig.tsx
  - helix-desktop/src/components/ConnectionConfig.test.tsx
  - helix-desktop/src/components/DeviceRegistry.tsx
  - helix-desktop/src/components/DeviceRegistry.test.tsx
  - helix-desktop/src/App.tsx
  - helix-desktop/src-tauri/src/commands/mod.rs
  - helix-desktop/src-tauri/src/commands/connection.rs
  - helix-desktop/src-tauri/src/lib.rs
autonomous: true
requirements:
  - C7
  - C8

must_haves:
  truths:
    - "useAgentHealth polls GET {baseUrl}/health every 5s with AbortSignal.timeout(3000)"
    - "ConnectionConfig renders host/port/protocol inputs and persists via @tauri-apps/plugin-store"
    - "DeviceRegistry polls Agent /health every 5s and renders adapter list or error state"
    - "App.tsx wires DEFAULT_CONNECTION -> useAgentHealth -> ConnectionConfig -> DeviceRegistry with no SiteServer reference"
    - "npm run test passes all unit tests (vitest)"
    - "No component imports from a SiteServer URL or references SiteServer"
  artifacts:
    - "helix-desktop/src/hooks/useAgentHealth.ts"
    - "helix-desktop/src/components/DeviceRegistry.tsx"
    - "helix-desktop/src/App.tsx"

key_links:
  - "useAgentHealth -> GET {baseUrl}/health via fetch (not via Tauri invoke)"
  - "DeviceRegistry -> useAgentHealth (consumes AgentHealthState)"
  - "ConnectionConfig -> @tauri-apps/plugin-store Store('helix-config.json')"
  - "App.tsx -> DEFAULT_CONNECTION from src/types/agent.ts (localhost:5080)"
  - "src-tauri/commands/connection.rs -> load_config/save_config Tauri commands"
---

# Phase 5 — Plan 02: React Components + Rust Config Commands

**Goal:** Implement the `useAgentHealth` hook, `ConnectionConfig` form, `DeviceRegistry` view, and `App.tsx` composition root. Wire connection config persistence through a Rust Tauri command. All data flows through REST `GET /health` — no gRPC calls in Phase 5 (Agent has no gRPC services wired).

**Phase 5 architectural decision:** REST-only. `GET /health` is the only Agent endpoint available. The tonic crate is scaffolded in Plan 01 for future use (Phase 6 gRPC) but no Rust gRPC commands are invoked in this phase. The Rust backend handles config persistence only.

---

## Threat Model

| Threat | Likelihood | Mitigation |
|--------|-----------|------------|
| Tauri CORS blocks `fetch` to localhost Agent | Medium | Tauri webview does not enforce same-origin for localhost by default; document as known if blocked |
| `@tauri-apps/plugin-store` Tauri v2 API differs from v1 | Low | Use `Store` class from `@tauri-apps/plugin-store` (v2 API confirmed in RESEARCH.md) |
| vitest + jsdom `fetch` not available | Medium | Mock `fetch` with `vi.stubGlobal("fetch", ...)` in tests |
| `useAgentHealth` re-renders loop if `baseUrl` is not memoised | Low | `baseUrl` is derived from state, already stable |
| Test file imports fail on CI (Linux, no display) | Low | vitest + jsdom runs headless |

---

## Deviation Protocol

If `@tauri-apps/plugin-store` Store constructor API has changed, fall back to: read/write JSON config file via the Tauri filesystem plugin. Document the deviation in SUMMARY.md.

If a React component test cannot be wired with jsdom (Tauri-specific APIs absent in test environment), mock the Tauri `invoke` with `vi.mock('@tauri-apps/api/core')`.

---

## Tasks

### T01 — `useAgentHealth` Hook + Tests

**Type:** auto  
**Files:**
- `helix-desktop/src/hooks/useAgentHealth.ts` (CREATE)
- `helix-desktop/src/hooks/useAgentHealth.test.ts` (CREATE)

**Actions:**

CREATE `helix-desktop/src/hooks/useAgentHealth.ts`:
```typescript
import { useState, useEffect } from "react";

export interface AgentHealthState {
  healthy: boolean | null;
  error: string | null;
  lastChecked: Date | null;
}

export function useAgentHealth(baseUrl: string, intervalMs = 5000): AgentHealthState {
  const [state, setState] = useState<AgentHealthState>({
    healthy: null,
    error: null,
    lastChecked: null,
  });

  useEffect(() => {
    const check = async () => {
      try {
        const res = await fetch(`${baseUrl}/health`, {
          signal: AbortSignal.timeout(3000),
        });
        setState({
          healthy: res.ok,
          error: res.ok ? null : `HTTP ${res.status}`,
          lastChecked: new Date(),
        });
      } catch (e) {
        setState({
          healthy: false,
          error: (e as Error).message,
          lastChecked: new Date(),
        });
      }
    };
    check();
    const id = setInterval(check, intervalMs);
    return () => clearInterval(id);
  }, [baseUrl, intervalMs]);

  return state;
}
```

CREATE `helix-desktop/src/hooks/useAgentHealth.test.ts` with tests:
- `healthy is null before first check`
- `healthy becomes true when fetch returns 200`
- `healthy becomes false when fetch returns 500`
- `error is set when fetch throws (network error)`
- `AbortSignal.timeout(3000) is used in fetch call`

**Verify:** `npm run test -- --run hooks/`

**Acceptance criteria:**
- All hook tests pass
- No `SiteServer` string in the file

---

### T02 — `ConnectionConfig` Component + Persistence

**Type:** auto  
**Files:**
- `helix-desktop/src/components/ConnectionConfig.tsx` (CREATE)
- `helix-desktop/src/components/ConnectionConfig.test.tsx` (CREATE)
- `helix-desktop/src-tauri/src/commands/connection.rs` (CREATE)
- `helix-desktop/src-tauri/src/commands/mod.rs` (CREATE)
- `helix-desktop/src-tauri/src/lib.rs` (MODIFY — register commands)

**Actions:**

CREATE `helix-desktop/src/components/ConnectionConfig.tsx`:
```typescript
import { useState } from "react";
import { Store } from "@tauri-apps/plugin-store";
import type { AgentConnection } from "../types/agent";

interface ConnectionConfigProps {
  connection: AgentConnection;
  onSave: (c: AgentConnection) => void;
}

const store = new Store("helix-config.json");

export default function ConnectionConfig({ connection, onSave }: ConnectionConfigProps) {
  const [host, setHost] = useState(connection.host);
  const [port, setPort] = useState(connection.port);
  const [protocol, setProtocol] = useState<"http" | "https">(connection.protocol);
  const [error, setError] = useState<string | null>(null);

  const handleSave = async () => {
    if (!host || port < 1 || port > 65535) {
      setError("Invalid host or port");
      return;
    }
    setError(null);
    const updated: AgentConnection = { host, port, protocol };
    await store.set("agentConnection", updated);
    await store.save();
    onSave(updated);
  };

  return (
    <section aria-label="Agent Connection">
      <h2>Agent Connection</h2>
      {error && <p role="alert" className="error">{error}</p>}
      <label>
        Host
        <input value={host} onChange={e => setHost(e.target.value)} />
      </label>
      <label>
        Port
        <input type="number" value={port} onChange={e => setPort(Number(e.target.value))} />
      </label>
      <label>
        Protocol
        <select value={protocol} onChange={e => setProtocol(e.target.value as "http" | "https")}>
          <option value="http">http</option>
          <option value="https">https</option>
        </select>
      </label>
      <button onClick={handleSave}>Save</button>
    </section>
  );
}
```

CREATE `helix-desktop/src-tauri/src/commands/connection.rs`:
```rust
use serde::{Deserialize, Serialize};
use tauri::AppHandle;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AgentConnectionConfig {
    pub host: String,
    pub port: u16,
    pub protocol: String,
}

#[tauri::command]
pub async fn load_config(_app: AppHandle) -> Result<AgentConnectionConfig, String> {
    Ok(AgentConnectionConfig {
        host: "localhost".to_string(),
        port: 5080,
        protocol: "http".to_string(),
    })
}

#[tauri::command]
pub async fn save_config(
    _app: AppHandle,
    _config: AgentConnectionConfig,
) -> Result<(), String> {
    Ok(())
}
```

CREATE `helix-desktop/src-tauri/src/commands/mod.rs`:
```rust
pub mod connection;
```

MODIFY `helix-desktop/src-tauri/src/lib.rs` to register commands:
```rust
mod commands;
use commands::connection::{load_config, save_config};

pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_store::Builder::new().build())
        .invoke_handler(tauri::generate_handler![load_config, save_config])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
```

CREATE `helix-desktop/src/components/ConnectionConfig.test.tsx`:
- Mock `@tauri-apps/plugin-store` Store class
- Test: renders host input with default value
- Test: renders port input with default value
- Test: shows error when port is 0
- Test: calls onSave with updated connection on valid save

**Verify:** `npm run test -- --run components/ConnectionConfig`

**Acceptance criteria:**
- All ConnectionConfig tests pass
- `load_config` and `save_config` commands are registered in `lib.rs`
- `cargo check` still exits 0

---

### T03 — `DeviceRegistry` Component + Tests

**Type:** auto  
**Files:**
- `helix-desktop/src/components/DeviceRegistry.tsx` (CREATE)
- `helix-desktop/src/components/DeviceRegistry.test.tsx` (CREATE)

**Actions:**

CREATE `helix-desktop/src/components/DeviceRegistry.tsx`:
```typescript
import { useState, useEffect } from "react";
import type { DeviceAdapter } from "../types/agent";

interface DeviceRegistryProps {
  agentBaseUrl: string;
}

function mapHealthToAdapters(data: { status?: string }): DeviceAdapter[] {
  return [
    {
      adapterId: "agent",
      healthy: data.status === "Healthy",
      message: data.status ?? "Unknown",
      capabilities: [],
    },
  ];
}

export default function DeviceRegistry({ agentBaseUrl }: DeviceRegistryProps) {
  const [devices, setDevices] = useState<DeviceAdapter[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const poll = async () => {
      setLoading(true);
      try {
        const res = await fetch(`${agentBaseUrl}/health`);
        if (!res.ok) throw new Error(`Agent returned ${res.status}`);
        const data = await res.json() as { status?: string };
        setDevices(mapHealthToAdapters(data));
        setError(null);
      } catch (e) {
        setError((e as Error).message);
      } finally {
        setLoading(false);
      }
    };
    poll();
    const interval = setInterval(poll, 5000);
    return () => clearInterval(interval);
  }, [agentBaseUrl]);

  if (loading && devices.length === 0) return <p>Loading...</p>;

  return (
    <section aria-label="Device Registry">
      <h2>Device Registry</h2>
      {error && <p role="alert" className="error">{error}</p>}
      {devices.length === 0 && !error && <p>No adapters found</p>}
      <ul>
        {devices.map(d => (
          <li key={d.adapterId}>
            <strong>{d.adapterId}</strong> — {d.healthy ? "Healthy" : "Unhealthy"}: {d.message}
          </li>
        ))}
      </ul>
    </section>
  );
}
```

CREATE `helix-desktop/src/components/DeviceRegistry.test.tsx`:
- Mock `fetch` with `vi.stubGlobal`
- Test: shows "Loading..." on initial render before fetch resolves
- Test: renders adapter list when fetch returns `{ status: "Healthy" }`
- Test: shows error message when fetch returns non-200
- Test: shows error message when fetch throws (network error)
- Test: shows "No adapters found" when fetch returns `{ status: "Degraded" }` (healthy=false)

**Verify:** `npm run test -- --run components/DeviceRegistry`

**Acceptance criteria:**
- All DeviceRegistry tests pass
- Component does not reference any SiteServer URL

---

### T04 — `App.tsx` Composition Root

**Type:** auto  
**Files:**
- `helix-desktop/src/App.tsx` (OVERWRITE scaffold-generated)

**Actions:**

OVERWRITE `helix-desktop/src/App.tsx`:
```typescript
import { useState } from "react";
import { DEFAULT_CONNECTION } from "./types/agent";
import type { AgentConnection } from "./types/agent";
import { useAgentHealth } from "./hooks/useAgentHealth";
import ConnectionConfig from "./components/ConnectionConfig";
import DeviceRegistry from "./components/DeviceRegistry";
import "./App.css";

export default function App() {
  const [connection, setConnection] = useState<AgentConnection>(DEFAULT_CONNECTION);
  const baseUrl = `${connection.protocol}://${connection.host}:${connection.port}`;
  const health = useAgentHealth(baseUrl);

  return (
    <div className="app">
      <header>
        <h1>Helix Desktop</h1>
        <span className={health.healthy ? "status-healthy" : "status-unhealthy"}>
          {health.healthy === null ? "Connecting..." : health.healthy ? "Agent Online" : `Agent Offline: ${health.error}`}
        </span>
      </header>
      <main>
        <ConnectionConfig connection={connection} onSave={setConnection} />
        <DeviceRegistry agentBaseUrl={baseUrl} />
      </main>
    </div>
  );
}
```

**Verify:**
```bash
cd helix-desktop
npm run build
grep -r "SiteServer" src/ && exit 1 || echo "no SiteServer references — OK"
npm run test -- --run
```

**Acceptance criteria:**
- `npm run build` exits 0
- `grep -r "SiteServer" src/` returns no matches
- All tests pass
- `cargo check` still exits 0

---

## Self-Check

- [ ] All vitest tests pass (no failures, no skips)
- [ ] `npm run build` exits 0 with strict TypeScript
- [ ] `cargo check` exits 0
- [ ] No component references a SiteServer URL
- [ ] `useAgentHealth` uses `AbortSignal.timeout(3000)`
- [ ] `ConnectionConfig` uses `@tauri-apps/plugin-store`
- [ ] `DEFAULT_CONNECTION` default is `localhost:5080`
- [ ] `DeviceRegistry` handles error state gracefully (no unhandled rejections)
