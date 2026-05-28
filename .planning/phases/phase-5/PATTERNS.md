# Phase 5: Tauri Desktop App — Foundation — Pattern Map

**Mapped:** 2026-05-28
**Files analyzed:** 11 new/modified files across Tauri scaffold, React components, config, and CI
**Analogs found:** 8 / 11 (3 have no codebase analog — new technology layer)

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `helix-desktop/src-tauri/tauri.conf.json` | config | — | `Helix.Orchestration.SiteServer/appsettings.json` | partial (config shape) |
| `helix-desktop/package.json` | config | — | none | no analog |
| `helix-desktop/vite.config.ts` | config | — | none | no analog |
| `helix-desktop/tsconfig.json` | config | — | none | no analog |
| `helix-desktop/src/main.tsx` | component | request-response | none | no analog |
| `helix-desktop/src/App.tsx` | component | request-response | `Helix.Orchestration.SiteServer/Endpoints/DeviceEndpoints.cs` | partial (data fetch + display) |
| `helix-desktop/src/components/ConnectionConfig.tsx` | component | request-response | `Helix.Orchestration.SiteServer/Agent/AgentProxyOptions.cs` + `AgentProxyServiceExtensions.cs` | role-match (config + mode switch) |
| `helix-desktop/src/components/DeviceRegistry.tsx` | component | request-response (poll) | `Helix.Orchestration.SiteServer/Endpoints/DeviceEndpoints.cs` | role-match |
| `helix-desktop/src/hooks/useAgentHealth.ts` | hook | request-response (poll) | `Helix.Orchestration.SiteServer/Agent/GrpcAgentProxy.cs` | partial (error translation pattern) |
| `helix-desktop/src/types/agent.ts` | model | — | `Helix.Orchestration.SiteServer/Agent/AgentProxyOptions.cs` + proto files | role-match (type surface) |
| `.github/workflows/ci.yml` | config | — | `.github/workflows/ci.yml` (self) | exact (extend existing) |

---

## Pattern Assignments

### `.github/workflows/ci.yml` (config — extend existing)

**Analog:** `.github/workflows/ci.yml` (existing file)

The existing CI has three jobs: `build-and-test` (ubuntu-latest, .NET 10), `code-quality` (needs build-and-test), and `publish` (main-branch only). Phase 5 adds a **fourth job** — `tauri-build` — that runs in parallel with `code-quality` (both depend on `build-and-test`).

**Existing job structure to follow** (`.github/workflows/ci.yml` lines 13–55):

```yaml
jobs:
  build-and-test:
    name: Build and Test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/packages.lock.json', '**/*.csproj') }}
          restore-keys: |
            ${{ runner.os }}-nuget-
      - run: dotnet restore Helix.Orchestration.slnx -p:Configuration=Release
      - run: dotnet build Helix.Orchestration.slnx --no-restore -c Release
      - run: dotnet test Helix.Orchestration.slnx --no-build -c Release --logger trx --results-directory test-results
      - uses: actions/upload-artifact@v4
        with:
          name: test-results
          path: test-results
          retention-days: 7

  code-quality:
    needs: build-and-test        # <-- dependency pattern to copy for tauri-build
    runs-on: ubuntu-latest
```

**New `tauri-build` job to add** (insert after `code-quality` block):

```yaml
  tauri-build:
    name: Tauri Linux Build
    runs-on: ubuntu-latest
    needs: build-and-test        # parallel with code-quality; no .NET dependency

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: npm
          cache-dependency-path: helix-desktop/package-lock.json

      - name: Install system dependencies (Tauri Linux)
        run: |
          sudo apt-get update
          sudo apt-get install -y \
            libwebkit2gtk-4.1-dev \
            libappindicator3-dev \
            librsvg2-dev \
            patchelf

      - name: Install Rust stable
        uses: dtolnay/rust-toolchain@stable

      - name: Cache Rust build artifacts
        uses: actions/cache@v4
        with:
          path: |
            ~/.cargo/bin/
            ~/.cargo/registry/index/
            ~/.cargo/registry/cache/
            ~/.cargo/git/db/
            helix-desktop/src-tauri/target/
          key: ${{ runner.os }}-cargo-${{ hashFiles('helix-desktop/src-tauri/Cargo.lock') }}
          restore-keys: |
            ${{ runner.os }}-cargo-

      - name: Install frontend dependencies
        working-directory: helix-desktop
        run: npm ci

      - name: Build Tauri (Linux)
        working-directory: helix-desktop
        run: npm run tauri build -- --bundles appimage,deb

      - name: Upload Tauri bundle
        uses: actions/upload-artifact@v4
        with:
          name: helix-desktop-linux
          path: helix-desktop/src-tauri/target/release/bundle/
          retention-days: 7
```

Key CI conventions to preserve:
- `actions/checkout@v4`, `actions/cache@v4`, `actions/upload-artifact@v4` — match existing action versions exactly
- `retention-days: 7` — matches test-results artifact retention
- Job name format: `"Tauri Linux Build"` (title case, matches `"Build and Test"`, `"Code Quality"`)
- `needs: build-and-test` only — keeps tauri-build parallel with code-quality gate

---

### `helix-desktop/src-tauri/tauri.conf.json` (config)

**Analog:** `Helix.Orchestration.SiteServer/appsettings.json`

The `AgentProxy` section in appsettings.json is the closest structural analog: a named block with `Mode` (enum-like string) and `GrpcAddress` (connection string). Phase 5 mirrors this in Tauri's persisted store.

**`appsettings.json` AgentProxy section** (reference for Tauri config structure):

```json
{
  "AgentProxy": {
    "Mode": "sim",
    "GrpcAddress": "http://localhost:5080"
  },
  "MqttBridge": {
    "BrokerHost": "localhost",
    "BrokerPort": 1883
  }
}
```

**`tauri.conf.json` pattern:**

```json
{
  "productName": "helix-desktop",
  "version": "0.1.0",
  "identifier": "com.helix.desktop",
  "build": {
    "beforeDevCommand": "npm run dev",
    "beforeBuildCommand": "npm run build",
    "devUrl": "http://localhost:5173",
    "frontendDist": "../dist"
  },
  "bundle": {
    "active": true,
    "targets": "all",
    "icon": ["icons/32x32.png", "icons/128x128.png"]
  },
  "app": {
    "windows": [
      {
        "label": "main",
        "title": "Helix Desktop",
        "width": 1280,
        "height": 800,
        "resizable": true
      }
    ],
    "security": { "csp": null }
  }
}
```

**Agent connection defaults** sourced from `Helix.Orchestration.SiteServer/Agent/AgentProxyOptions.cs`:

```csharp
public string Mode { get; set; } = "sim";
public string GrpcAddress { get; set; } = "http://localhost:5080";
// --> Tauri DEFAULT_CONNECTION: host="localhost", port=5080, protocol="http"
```

The REST health endpoint is `GET /health` at `http://localhost:5080/health`. No `/api/` prefix — that convention belongs to SiteServer only.

---

### `helix-desktop/src/types/agent.ts` (model)

**Analogs:**
- `Helix.Orchestration.Contracts/Protos/agent_control.proto` — gRPC surface
- `Helix.Orchestration.Contracts/Protos/adapter.proto` — device adapter surface
- `Helix.Orchestration.SiteServer/Agent/AgentProxyOptions.cs` — connection config shape

**Proto surface** (`agent_control.proto`, package `helix.control`):

```protobuf
rpc SubmitExperiment (ExperimentSubmission) returns (SubmissionAck)
rpc PauseRunspace    (RunspaceCommand)      returns (CommandAck)
rpc ResumeRunspace   (RunspaceCommand)      returns (CommandAck)
rpc AbortRunspace    (RunspaceCommand)      returns (CommandAck)
rpc GetRunspaceStatus(RunspaceQuery)        returns (RunspaceStatusResponse)
// RunspaceStatusResponse: { runspace_id, status, message }
// SubmissionAck:          { accepted, runspace_id, message }
// CommandAck:             { success, message }
```

**Proto surface** (`adapter.proto`, package `helix.adapter`):

```protobuf
rpc GetCapabilities (Empty)             returns (CapabilitiesResponse)   // repeated capabilities
rpc InvokeCapability(CapabilityRequest) returns (CapabilityResponse)
rpc StreamTrends    (Empty)             returns (stream TrendValue)
rpc HealthCheck     (Empty)             returns (HealthStatus)           // { healthy, message }
```

**TypeScript types to produce:**

```typescript
// helix-desktop/src/types/agent.ts

// Connection config — mirrors AgentProxyOptions.cs defaults
export interface AgentConnection {
  host: string;     // default: "localhost"
  port: number;     // default: 5080
  protocol: "http" | "https" | "grpc";
}

// REST /health response (ASP.NET Core HealthChecks format)
export interface AgentHealth {
  status: "Healthy" | "Unhealthy" | "Degraded";
}

// Device adapter — from adapter.proto HealthStatus + GetCapabilities
export interface DeviceAdapter {
  adapterId: string;
  healthy: boolean;
  message: string;
  capabilities: string[];
}

// Runspace status — from agent_control.proto RunspaceStatusResponse
export interface RunspaceStatus {
  runspaceId: string;
  status: string;   // "Running" | "Completed" | "Faulted" | "NotFound"
  message: string;
}
```

---

### `helix-desktop/src/components/ConnectionConfig.tsx` (component, request-response)

**Analog:** `Helix.Orchestration.SiteServer/Agent/AgentProxyServiceExtensions.cs` + `AgentProxyOptions.cs`

The C# analog selects mode at startup from config. The React component mirrors this: a form that reads/writes persisted connection settings and validates before connecting.

**C# mode selection pattern** (`AgentProxyServiceExtensions.cs`):

```csharp
return options.Mode.Equals("grpc", StringComparison.OrdinalIgnoreCase)
    ? ActivatorUtilities.CreateInstance<GrpcAgentProxy>(sp)
    : ActivatorUtilities.CreateInstance<SimAgentProxy>(sp);
```

**C# error handling pattern** (`GrpcAgentProxy.cs`):

```csharp
// Negative ack -> typed exception with message
if (!response.Accepted || string.IsNullOrWhiteSpace(response.RunspaceId))
    throw new InvalidOperationException(response.Message is { Length: > 0 }
        ? response.Message : "Agent rejected experiment submission.");

// RPC transport error -> KeyNotFoundException
catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
    throw new KeyNotFoundException($"Runspace '{runspaceId}' not found.", ex);
```

Mirror in TypeScript: catch fetch errors, set `error: string | null` state — never let unhandled promise rejections propagate.

**Component shape:**

```typescript
// helix-desktop/src/components/ConnectionConfig.tsx
interface ConnectionConfigProps {
  connection: AgentConnection;
  onSave: (c: AgentConnection) => void;
}

const [host, setHost] = useState(connection.host);
const [port, setPort] = useState(connection.port);
const [protocol, setProtocol] = useState(connection.protocol);
const [error, setError] = useState<string | null>(null);

const handleSave = () => {
  if (!host || port < 1 || port > 65535) {
    setError("Invalid host or port");
    return;
  }
  setError(null);
  onSave({ host, port, protocol });
};
```

---

### `helix-desktop/src/components/DeviceRegistry.tsx` (component, request-response poll)

**Analog:** `Helix.Orchestration.SiteServer/Endpoints/DeviceEndpoints.cs`

The C# analog: `GET /api/devices` wraps in try/catch, returns `Results.Ok(devices)` or `Results.Problem(...)`. The React component polls `GET /health` on a timer and maintains adapter status list.

**C# endpoint pattern** (`DeviceEndpoints.cs`):

```csharp
group.MapGet("/", IResult (IAgentProxy agentProxy) =>
{
    try
    {
        var devices = agentProxy.GetRegisteredDevices();
        return Results.Ok(devices);
    }
    catch (Exception ex)
    {
        return Results.Problem(title: "Failed to retrieve registered devices",
            detail: ex.Message, statusCode: 500);
    }
})
```

**React polling pattern to mirror:**

```typescript
// helix-desktop/src/components/DeviceRegistry.tsx
const [devices, setDevices] = useState<DeviceAdapter[]>([]);
const [loading, setLoading] = useState(false);
const [error, setError] = useState<string | null>(null);

useEffect(() => {
  const poll = async () => {
    setLoading(true);
    try {
      const res = await fetch(`${agentBaseUrl}/health`);
      if (!res.ok) throw new Error(`Agent returned ${res.status}`);
      const data = await res.json();
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
```

Error display: render `<p className="error">{error}</p>` inline — mirrors `Results.Problem(...)` which always returns a response.

---

### `helix-desktop/src/hooks/useAgentHealth.ts` (hook, request-response poll)

**Analog:** `Helix.Orchestration.SiteServer/Agent/GrpcAgentProxy.cs` (error translation pattern)

`GrpcAgentProxy` wraps gRPC calls, translates transport errors to typed exceptions, never leaks raw proto errors. The hook mirrors this: wraps `fetch`, translates HTTP/network errors to typed `{ healthy: boolean | null; error: string | null }`.

**C# error translation pattern** (`GrpcAgentProxy.cs`):

```csharp
if (!response.Accepted || string.IsNullOrWhiteSpace(response.RunspaceId))
    throw new InvalidOperationException(response.Message is { Length: > 0 }
        ? response.Message : "Agent rejected ...");

catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
    throw new KeyNotFoundException($"Runspace '{runspaceId}' not found.", ex);
```

**Hook shape:**

```typescript
// helix-desktop/src/hooks/useAgentHealth.ts
export interface AgentHealthState {
  healthy: boolean | null;
  error: string | null;
  lastChecked: Date | null;
}

export function useAgentHealth(baseUrl: string, intervalMs = 5000): AgentHealthState {
  const [state, setState] = useState<AgentHealthState>({
    healthy: null, error: null, lastChecked: null
  });

  useEffect(() => {
    const check = async () => {
      try {
        const res = await fetch(`${baseUrl}/health`,
          { signal: AbortSignal.timeout(3000) });
        setState({
          healthy: res.ok,
          error: res.ok ? null : `HTTP ${res.status}`,
          lastChecked: new Date()
        });
      } catch (e) {
        setState({ healthy: false, error: (e as Error).message, lastChecked: new Date() });
      }
    };
    check();
    const id = setInterval(check, intervalMs);
    return () => clearInterval(id);
  }, [baseUrl, intervalMs]);

  return state;
}
```

---

### `helix-desktop/src/App.tsx` (component, request-response)

**Analog:** `Helix.Orchestration.SiteServer/Program.cs` — top-level composition root.

**C# composition root pattern** (`SiteServer/Program.cs`):

```csharp
builder.Services.AddHelixAgentProxy(builder.Configuration);  // -> React context/state
if (app.Environment.IsDevelopment()) { app.UseSwagger(); }    // -> conditional rendering
app.MapDeviceEndpoints();                                      // -> view sections
app.MapExperimentEndpoints();
```

**App.tsx shape:**

```typescript
// helix-desktop/src/App.tsx

// Default connection — from AgentProxyOptions.cs defaults + appsettings.json
const DEFAULT_CONNECTION: AgentConnection = {
  host: "localhost",
  port: 5080,
  protocol: "http",
};

export default function App() {
  const [connection, setConnection] = useState<AgentConnection>(DEFAULT_CONNECTION);
  const health = useAgentHealth(
    `${connection.protocol}://${connection.host}:${connection.port}`
  );

  return (
    <div className="app">
      <header>
        <span>Helix Desktop</span>
        <AgentStatusBadge healthy={health.healthy} error={health.error} />
      </header>
      <main>
        <ConnectionConfig connection={connection} onSave={setConnection} />
        <DeviceRegistry
          agentBaseUrl={`${connection.protocol}://${connection.host}:${connection.port}`}
        />
      </main>
    </div>
  );
}
```

---

### `helix-desktop/package.json` (config — no codebase analog)

No existing JS/TS project in the repo. Use standard Tauri v2 + React + Vite scaffold.

```json
{
  "name": "helix-desktop",
  "version": "0.1.0",
  "scripts": {
    "dev": "vite",
    "build": "tsc && vite build",
    "tauri": "tauri"
  },
  "dependencies": {
    "react": "^18.3.0",
    "react-dom": "^18.3.0",
    "@tauri-apps/api": "^2.0.0",
    "@tauri-apps/plugin-store": "^2.0.0"
  },
  "devDependencies": {
    "@tauri-apps/cli": "^2.0.0",
    "@types/react": "^18.3.0",
    "@types/react-dom": "^18.3.0",
    "typescript": "^5.5.0",
    "vite": "^5.4.0",
    "@vitejs/plugin-react": "^4.3.0"
  }
}
```

---

### `helix-desktop/vite.config.ts` + `tsconfig.json` (config — no codebase analog)

No existing Vite/TS config. Use standard Tauri v2 scaffold. Set `strict: true` in tsconfig to match C# `<Nullable>enable</Nullable>`.

**`vite.config.ts`:**

```typescript
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig(async () => ({
  plugins: [react()],
  clearScreen: false,
  server: {
    port: 5173,
    strictPort: true,
    watch: { ignored: ["**/src-tauri/**"] },
  },
  envPrefix: ["VITE_", "TAURI_ENV_*"],
  build: {
    target: process.env.TAURI_ENV_PLATFORM === "windows" ? "chrome105" : "safari13",
    minify: !process.env.TAURI_ENV_DEBUG ? "esbuild" : false,
    sourcemap: !!process.env.TAURI_ENV_DEBUG,
  },
}));
```

**`tsconfig.json`:**

```json
{
  "compilerOptions": {
    "target": "ES2020",
    "useDefineForClassFields": true,
    "lib": ["ES2020", "DOM", "DOM.Iterable"],
    "module": "ESNext",
    "skipLibCheck": true,
    "moduleResolution": "bundler",
    "allowImportingTsExtensions": true,
    "isolatedModules": true,
    "noEmit": true,
    "jsx": "react-jsx",
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noFallthroughCasesInSwitch": true
  },
  "include": ["src"]
}
```

---

## Shared Patterns

### Agent REST surface (apply to all components that call Agent)

**Source:** `Helix.Orchestration.Agent/Program.cs` line 99 + `Helix.Orchestration.SiteServer/appsettings.json`

The Agent exposes exactly **one REST endpoint** today:

```
GET /health   ->  ASP.NET Core HealthChecks JSON response
```

Default base URL: `http://localhost:5080` (from `AgentProxy.GrpcAddress` in appsettings.json). No `/api/` prefix.

```typescript
const HEALTH_URL = `${baseUrl}/health`;   // NOT /api/health
```

### Error boundary / graceful degradation (apply to all components)

**Source:** `Helix.Orchestration.SiteServer/Endpoints/DeviceEndpoints.cs` and `ExperimentEndpoints.cs`

Every C# endpoint catches `Exception` and returns `Results.Problem(...)` — never lets exceptions bubble. Every React component and hook must mirror this.

```typescript
try {
  const data = await fetchSomething();
  setData(data);
  setError(null);
} catch (e) {
  setError((e as Error).message);   // typed string — never raw unknown
}
```

### Connection config persistence (apply to ConnectionConfig.tsx)

**Source:** `Helix.Orchestration.SiteServer/appsettings.json` + `AgentProxyOptions.cs`

C# uses `IOptions<AgentProxyOptions>` bound from appsettings to survive restarts. Tauri equivalent: `@tauri-apps/plugin-store`.

```typescript
import { Store } from "@tauri-apps/plugin-store";
const store = new Store("helix-config.json");
await store.set("agentConnection", { host, port, protocol });
await store.save();
```

### Standalone mode (apply to App.tsx and all data hooks)

**Source:** Phase 5 roadmap — "Standalone mode: app connects directly to Agent REST/gRPC; no Site Server required"

No component may depend on a SiteServer URL. All data flows through `AgentConnection.host/port/protocol`. `DEFAULT_CONNECTION` points to the Agent at `localhost:5080`.

### Docker Compose alignment (apply to CI `tauri-build` job)

**Source:** `docker-compose.yml`

The `tauri-build` CI job builds the static bundle only; no `docker compose up` step is needed. Service ports for reference:

```
localhost:5080  — Agent REST + gRPC (.NET process, not in compose)
localhost:1883  — MQTT/TCP (eclipse-mosquitto:2)
localhost:9001  — MQTT over WebSocket (future Phase 6 real-time trends)
localhost:5432  — PostgreSQL 16
localhost:9000  — MinIO S3 API
localhost:9002  — MinIO console
```

---

## gRPC Proto Contract Summary

Phase 5 uses REST (`GET /health`) only. Phase 6 adds gRPC-web. Proto surface documented here for `src/types/agent.ts` type definitions.

**`agent_control.proto`** — `AgentControl` service (package `helix.control`):

| RPC | Request fields | Response fields | Phase 5 use |
|-----|----------------|-----------------|-------------|
| `SubmitExperiment` | `runspace_definition_json` | `accepted, runspace_id, message` | Phase 6 |
| `PauseRunspace` | `runspace_id` | `success, message` | Phase 6 |
| `ResumeRunspace` | `runspace_id` | `success, message` | Phase 6 |
| `AbortRunspace` | `runspace_id` | `success, message` | Phase 6 |
| `GetRunspaceStatus` | `runspace_id` | `runspace_id, status, message` | Phase 6 |

**`adapter.proto`** — `DeviceAdapter` service (package `helix.adapter`):

| RPC | Request | Response fields | Phase 5 use |
|-----|---------|-----------------|-------------|
| `GetCapabilities` | `Empty` | `repeated capabilities` | Phase 5 device registry |
| `InvokeCapability` | `capability_name, parameters_json, lease_id` | `success, result_json, error_message` | Phase 6 |
| `StreamTrends` | `Empty` | stream: `trend_name, value_json, type_tag, timestamp_utc_ticks` | Phase 6 |
| `HealthCheck` | `Empty` | `healthy, message` | Phase 5 device health |

Note: `Helix.Orchestration.Agent/gRPC/Server/` exists but is empty — no gRPC services wired in `Program.cs` beyond `MapHealthChecks("/health")`. `DeviceRegistry` must fall back gracefully to REST when gRPC is unavailable.

---

## No Analog Found

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| `helix-desktop/package.json` | config | — | No existing JS/TS project in repo |
| `helix-desktop/vite.config.ts` | config | — | No existing Vite config in repo |
| `helix-desktop/tsconfig.json` | config | — | No existing TypeScript config in repo |

These three files should be produced by `npm create tauri-app@latest` with the React + TypeScript template, then customised per the patterns above.

---

## Metadata

**Analog search scope:** `.github/workflows/`, `Helix.Orchestration.Agent/`, `Helix.Orchestration.SiteServer/`, `Helix.Orchestration.Contracts/Protos/`, root `docker-compose.yml`, `mosquitto/mosquitto.conf`, `.planning/ROADMAP.md`
**Files scanned:** 18 source files read
**Pattern extraction date:** 2026-05-28
