# Phase 6: Tauri Desktop App - Workflow Builder + Execution - Pattern Map

**Mapped:** 2026-05-29
**Files analyzed:** 19
**Analogs found:** 17 / 19

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Helix.Orchestration.Agent/Program.cs` | config | request-response | `Helix.Orchestration.Agent/Program.cs` (self — extend) | self |
| `Helix.Orchestration.Agent/Api/WorkflowEndpoints.cs` | controller | request-response | `Helix.Orchestration.SiteServer/Endpoints/ExperimentEndpoints.cs` | exact |
| `Helix.Orchestration.Agent/Api/RunspaceEndpoints.cs` | controller | request-response | `Helix.Orchestration.SiteServer/Endpoints/CampaignEndpoints.cs` | exact |
| `Helix.Orchestration.Agent/Api/DeviceEndpoints.cs` | controller | request-response | `Helix.Orchestration.SiteServer/Endpoints/DeviceEndpoints.cs` | exact |
| `Helix.Orchestration.Agent/Events/IRunspaceEventEmitter.cs` | service | event-driven | `Helix.Orchestration.Core/Safety/ISafetyEngine.cs` | role-match |
| `Helix.Orchestration.Agent/Events/RunspaceEventEmitter.cs` | service | event-driven | `Helix.Orchestration.Core/Capabilities/CapabilityRegistry.cs` | role-match |
| `Helix.Orchestration.Agent/Events/RunspaceEvent.cs` | model | event-driven | `Helix.Orchestration.Core/WSL/CompilationResult.cs` | role-match |
| `Helix.Orchestration.Agent/AgentService.cs` | service | event-driven | `Helix.Orchestration.Agent/AgentService.cs` (self — promote) | self |
| `Helix.Orchestration.Core/WSL/WslCompiler.cs` | service | transform | `Helix.Orchestration.Core/WSL/WslCompiler.cs` (self — complete stubs) | self |
| `helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx` | component | event-driven | `helix-desktop/src/components/DeviceRegistry.tsx` | role-match |
| `helix-desktop/src/components/WorkflowBuilder/useReteEditor.ts` | hook | event-driven | `helix-desktop/src/hooks/useAgentHealth.ts` | role-match |
| `helix-desktop/src/components/ExecutionPanel/ExecutionPanel.tsx` | component | request-response | `helix-desktop/src/components/ConnectionConfig.tsx` | role-match |
| `helix-desktop/src/components/TrendCharts/ScalarTrendChart.tsx` | component | streaming | `helix-desktop/src/components/DeviceRegistry.tsx` | role-match |
| `helix-desktop/src/components/TrendCharts/SpectralChart.tsx` | component | streaming | `helix-desktop/src/components/DeviceRegistry.tsx` | role-match |
| `helix-desktop/src/components/SafetyPanel/SafetyPanel.tsx` | component | streaming | `helix-desktop/src/components/DeviceRegistry.tsx` | role-match |
| `helix-desktop/src/hooks/useAgentWebSocket.ts` | hook | streaming | `helix-desktop/src/hooks/useAgentHealth.ts` | exact |
| `helix-desktop/src/lib/workflowLibrary.ts` | utility | file-I/O | `helix-desktop/src/components/ConnectionConfig.tsx` | role-match |
| `helix-desktop/src-tauri/Cargo.toml` | config | — | `helix-desktop/src-tauri/Cargo.toml` (self — add dep) | self |
| `helix-desktop/src-tauri/src/lib.rs` | config | — | `helix-desktop/src-tauri/src/lib.rs` (self — add plugin) | self |
| `helix-desktop/src-tauri/capabilities/default.json` | config | — | `helix-desktop/src-tauri/capabilities/default.json` (self — add fs perms) | self |

---

## Pattern Assignments

### `Helix.Orchestration.Agent/Program.cs` (config — extend existing)

**Analog:** `Helix.Orchestration.Agent/Program.cs` (lines 1–125, extend in-place)

**Existing DI registration pattern** (lines 48–92):
```csharp
builder.Services.AddSingleton<InMemoryDeviceLeasingService>();
builder.Services.AddSingleton<IDeviceLeasingService>(sp =>
    sp.GetRequiredService<InMemoryDeviceLeasingService>());
builder.Services.AddMigratedDeviceAdapters();
// ...
builder.Services.AddSingleton<ISafetyEngine, SafetyEngine>();
builder.Services.AddSingleton<IRunspaceManager, RunspaceManager>();
builder.Services.AddHostedService<AgentService>();
```

**Existing CORS pattern** (lines 98–106):
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "tauri://localhost", "https://tauri.localhost")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

**Existing middleware + MapHealthChecks pattern** (lines 108–113):
```csharp
var app = builder.Build();
app.UseCors();
app.MapHealthChecks("/health");
await app.RunAsync();
```

**Where to insert Phase 6 additions** — after `app.UseCors()`, before `await app.RunAsync()`:
```csharp
app.UseWebSockets(new WebSocketOptions
{
    AllowedOrigins = { "http://localhost:5173", "tauri://localhost", "https://tauri.localhost" }
});
// Map route groups here (WorkflowEndpoints, RunspaceEndpoints, DeviceEndpoints)
// WebSocket middleware inline here
```

---

### `Helix.Orchestration.Agent/Api/WorkflowEndpoints.cs` (controller, request-response)

**Analog:** `Helix.Orchestration.SiteServer/Endpoints/ExperimentEndpoints.cs`

**Imports pattern** (lines 1–6):
```csharp
using Helix.Orchestration.Core.WSL;
using Helix.Orchestration.SiteServer.Agent;
using Helix.Orchestration.SiteServer.Contracts;

namespace Helix.Orchestration.SiteServer.Endpoints;
```
Copy structure; replace namespace with `Helix.Orchestration.Agent.Api`.

**Extension-method registration pattern** (lines 12–14):
```csharp
public static class WorkflowEndpoints
{
    public static void MapWorkflowEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/workflows").WithTags("Workflows");
        // ...
    }
}
```
This matches how `ExperimentEndpoints.MapExperimentEndpoints(this WebApplication app)` is structured.

**POST endpoint with try/catch + Results.Problem pattern** (lines 18–48):
```csharp
group.MapPost("/compile", async Task<IResult> (
    WslDocument document,
    IWslCompiler compiler,
    CancellationToken ct) =>
{
    try
    {
        // inject DeviceRegistry from DI
        var result = compiler.Compile(document, deviceRegistry);
        return result.Success
            ? Results.Ok(result.Definition)
            : Results.UnprocessableEntity(result.Diagnostics);
    }
    catch (Exception ex)
    {
        return Results.Problem(title: "Compilation failed", detail: ex.Message, statusCode: 500);
    }
})
.WithName("CompileWorkflow")
.Produces<RunspaceDefinition>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status422UnprocessableEntity)
.ProducesProblem(StatusCodes.Status500InternalServerError);
```

**Error handling pattern** — `Results.Problem(title, detail, statusCode)` as used on lines 26–30 of ExperimentEndpoints.cs. `Results.NotFound(new { Error = "..." })` for 404 cases (lines 74–75 of ExperimentEndpoints.cs).

---

### `Helix.Orchestration.Agent/Api/RunspaceEndpoints.cs` (controller, request-response)

**Analog:** `Helix.Orchestration.SiteServer/Endpoints/CampaignEndpoints.cs`

**MapGroup + multi-verb pattern** (lines 8–14 of CampaignEndpoints.cs):
```csharp
public static class RunspaceEndpoints
{
    public static void MapRunspaceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/runspaces").WithTags("Runspaces");

        group.MapPost("/", async Task<IResult> (...) => { ... });
        group.MapGet("/{id}", async Task<IResult> (...) => { ... });
        group.MapPost("/{id}/pause", async Task<IResult> (...) => { ... });
        group.MapPost("/{id}/resume", async Task<IResult> (...) => { ... });
        group.MapPost("/{id}/abort", async Task<IResult> (...) => { ... });
    }
}
```

**KeyNotFoundException -> 404 pattern** (lines 61–78 of CampaignEndpoints.cs):
```csharp
catch (KeyNotFoundException)
{
    return Results.NotFound(new { Error = $"Runspace '{id}' not found." });
}
catch (Exception ex)
{
    return Results.Problem(title: "...", detail: ex.Message, statusCode: 500);
}
```

**IRunspaceManager injection** — inject `IRunspaceManager manager` as a parameter. Available methods: `CreateAsync(definition, ct)`, `Get(runspaceId)`, `ListActive()`, `AbortAsync(runspaceId, ct)`.

**IRunspace available methods** (from `IRunspace.cs`): `StartAsync(ct)`, `PauseAsync(ct)`, `ResumeAsync(ct)`, `AbortAsync(ct)`, `RunspaceId`, `Status`, `Definition`, `CreatedAt`, `StartedAt`, `EndedAt`, `Fault`.

---

### `Helix.Orchestration.Agent/Api/DeviceEndpoints.cs` (controller, request-response)

**Analog:** `Helix.Orchestration.SiteServer/Endpoints/DeviceEndpoints.cs` (lines 1–36)

**Exact structural match** (lines 9–35):
```csharp
public static class DeviceEndpoints
{
    public static void MapDeviceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/devices").WithTags("Devices");

        group.MapGet("/", IResult (CapabilityRegistry registry) =>
        {
            try
            {
                var devices = registry.GetAllCapabilityNames()
                    .Select(name => new { adapterId = name, capabilities = new[] { name } })
                    .ToList();
                return Results.Ok(devices);
            }
            catch (Exception ex)
            {
                return Results.Problem(title: "Failed to retrieve devices", detail: ex.Message, statusCode: 500);
            }
        })
        .WithName("GetDevices")
        .Produces<IEnumerable<object>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
```

**CapabilityRegistry injection** — inject `CapabilityRegistry registry`. Key method: `registry.GetAllCapabilityNames()` returns `IEnumerable<string>`. Also inject `ICapabilityInvoker` for `GET /devices/{id}/capabilities`.

---

### `Helix.Orchestration.Agent/Events/IRunspaceEventEmitter.cs` (service, event-driven)

**Analog:** `Helix.Orchestration.Core/Safety/ISafetyEngine.cs`

**Interface definition pattern** (lines 1–54 of ISafetyEngine.cs):
```csharp
namespace Helix.Orchestration.Agent.Events;

/// <summary>
/// In-process event bus for runspace lifecycle events.
/// </summary>
public interface IRunspaceEventEmitter
{
    /// <summary>Publishes a runspace event to all subscribers.</summary>
    void Publish(RunspaceEvent evt);

    /// <summary>Observable stream of all published events.</summary>
    IObservable<RunspaceEvent> Events { get; }
}
```

**XML doc style** — triple-slash summary on every member, matching `ISafetyEngine.cs` style.

---

### `Helix.Orchestration.Agent/Events/RunspaceEventEmitter.cs` (service, event-driven)

**Analog:** `Helix.Orchestration.Core/Capabilities/CapabilityRegistry.cs`

**ConcurrentDictionary singleton pattern** (lines 1–15 of CapabilityRegistry.cs):
```csharp
using System.Collections.Concurrent;

namespace Helix.Orchestration.Agent.Events;

public sealed class RunspaceEventEmitter : IRunspaceEventEmitter
{
    // Use Subject<T> from System.Reactive or a simple ConcurrentBag<Action<RunspaceEvent>>
    // For minimal deps: use a ConcurrentBag of subscribers + lock-free publish
}
```

**Note:** `System.Reactive` is not currently in the Agent project. Use `System.Threading.Channels.Channel<RunspaceEvent>` (built-in) as the backing store instead of `IObservable<T>`. The interface can expose `IAsyncEnumerable<RunspaceEvent>` or a `Channel<RunspaceEvent>` reader as the stream — the planner should decide. Alternatively, `ConcurrentBag<Action<RunspaceEvent>>` for synchronous fan-out is simpler. The `CapabilityRegistry` pattern of `ConcurrentDictionary` + `TryAdd/TryRemove` is the closest threading model in the codebase.

---

### `Helix.Orchestration.Agent/Events/RunspaceEvent.cs` (model, event-driven)

**Analog:** `Helix.Orchestration.Core/WSL/CompilationResult.cs`

**Sealed record hierarchy pattern** (lines 1–34 of CompilationResult.cs):
```csharp
namespace Helix.Orchestration.Agent.Events;

/// <summary>Base type for all runspace lifecycle events.</summary>
public abstract record RunspaceEvent(string Type, string RunspaceId, DateTimeOffset Timestamp);

public sealed record RunspaceStartedEvent(string RunspaceId)
    : RunspaceEvent("runspace-started", RunspaceId, DateTimeOffset.UtcNow);

public sealed record RunspaceCompletedEvent(string RunspaceId)
    : RunspaceEvent("runspace-completed", RunspaceId, DateTimeOffset.UtcNow);

public sealed record StepStartedEvent(string RunspaceId, string StepId, string StepLabel)
    : RunspaceEvent("step-started", RunspaceId, DateTimeOffset.UtcNow);

// etc. — sealed record per event subtype
```

**JSON serialization** — `System.Text.Json` is already used throughout. For polymorphic deserialization add `[JsonDerivedType]` attributes on the base record, or use `type` discriminator for client-side parsing.

---

### `Helix.Orchestration.Agent/AgentService.cs` (service, event-driven — promote from stub)

**Analog:** `Helix.Orchestration.Agent/AgentService.cs` (lines 1–20, promote in-place)

**Current stub** (lines 1–20 of AgentService.cs):
```csharp
public sealed class AgentService(ILogger<AgentService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) { ... return Task.CompletedTask; }
    public Task StopAsync(CancellationToken cancellationToken) { ... return Task.CompletedTask; }
}
```

**Promoted pattern** — inject `IRunspaceEventEmitter` via primary constructor. Add `ConcurrentDictionary<string, WebSocket>` field. Add `HandleClientAsync(WebSocket ws, CancellationToken ct)` method. Subscribe to emitter in `StartAsync`, unsubscribe in `StopAsync`.

**DurableRunspace constructor pattern** (lines 41–45 of DurableRunspace.cs — primary constructor style):
```csharp
public sealed class AgentService(
    IRunspaceEventEmitter emitter,
    ILogger<AgentService> logger) : IHostedService
{
    private readonly ConcurrentDictionary<string, WebSocket> _clients = new();
    // ...
}
```
Use the same primary constructor + `ILogger<T>` pattern as the existing `AgentService` and `TrendAggregator`.

**`TrendAggregator` constructor pattern** (lines 9–11 of TrendAggregator.cs):
```csharp
public sealed class TrendAggregator(
    InMemoryTrendProvider trendProvider,
    ILogger<TrendAggregator> logger)
```

---

### `Helix.Orchestration.Core/WSL/WslCompiler.cs` (service, transform — complete stubs)

**Analog:** `Helix.Orchestration.Core/WSL/WslCompiler.cs` (self — lines 1–218)

**Existing `Compile` method structure** (lines 14–51):
```csharp
public CompilationResult Compile(WslDocument document, DeviceRegistry deviceRegistry)
{
    var diagnostics = new List<CompilationDiagnostic>();
    ValidateSchema(document, diagnostics);
    ValidateGraph(document, diagnostics);
    ValidateNodeParameters(document, diagnostics);
    ValidateDeviceBindings(document, deviceRegistry, diagnostics);
    ValidateVariableReferences(document, diagnostics);

    if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        return CompilationResult.Failed(diagnostics);

    var definition = new RunspaceDefinition(...);
    return CompilationResult.Succeeded(definition, diagnostics);
}
```

**Add before `CompilationResult.Succeeded`** — new private methods following the same `ValidateXxx(document, diagnostics)` signature style:
- `InjectSafetyEnvelope(document, ISafetyEngine safety, diagnostics)` — snapshot `safety.GetViolationsAsync()`, inject guard nodes read-only
- `ExpandLoopNodes(document, diagnostics)` — unroll Loop nodes with `iterations` parameter
- `FlattenBranchNodes(document, diagnostics)` — resolve Branch/Parallel into conditional edge sets

**Diagnostic code pattern** (lines 57–67):
```csharp
diagnostics.Add(new CompilationDiagnostic(DiagnosticSeverity.Error, "message", nodeId, "WSL0XX"));
```
Next available code range: WSL050+ for safety/loop/branch transformations.

---

### `helix-desktop/src/hooks/useAgentWebSocket.ts` (hook, streaming)

**Analog:** `helix-desktop/src/hooks/useAgentHealth.ts` (lines 1–41)

**Hook structure pattern** (full file):
```typescript
import { useState, useEffect } from "react";

export interface AgentHealthState { ... }

export function useAgentHealth(baseUrl: string, intervalMs = 5000): AgentHealthState {
  const [state, setState] = useState<AgentHealthState>({ ... });

  useEffect(() => {
    const check = async () => {
      try {
        const res = await fetch(...);
        setState({ ... });
      } catch (e) {
        setState({ healthy: false, error: (e as Error).message, lastChecked: new Date() });
      }
    };
    check();
    const id = setInterval(check, intervalMs);
    return () => clearInterval(id);  // cleanup
  }, [baseUrl, intervalMs]);

  return state;
}
```

**Adapt for WebSocket** — replace `setInterval` + `fetch` with `new WebSocket(wsUrl)` + `ws.onmessage`. Cleanup returns `() => ws.close()`. State fields: `runspaceEvents`, `trendValues`, `safetyEvents` (arrays, capped with `.slice(-N)`). URL transform: `baseUrl.replace(/^http/, "ws") + "/ws"`.

**Test pattern** (useAgentHealth.test.ts lines 1–117):
```typescript
import { renderHook, waitFor } from "@testing-library/react";
import { describe, it, expect, vi, afterEach } from "vitest";

afterEach(() => { vi.restoreAllMocks(); });

it("...", async () => {
  vi.stubGlobal("fetch", vi.fn(() => Promise.resolve({ ok: true, status: 200 } as Response)));
  const { result } = renderHook(() => useAgentWebSocket("http://localhost:5080", 999999));
  await waitFor(() => { expect(result.current.someField).toBe(...); });
});
```
For WebSocket tests: `vi.stubGlobal("WebSocket", vi.fn(...))` — same stubGlobal pattern, constructor mock. Do NOT use `vi.useFakeTimers()` (breaks `waitFor` — established in Phase 5 SUMMARY 02).

---

### `helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx` (component, event-driven)

**Analog:** `helix-desktop/src/components/DeviceRegistry.tsx` (lines 1–61)

**Component structure pattern** (full file):
```tsx
import { useState, useEffect } from "react";
import type { DeviceAdapter } from "../types/agent";

interface DeviceRegistryProps { agentBaseUrl: string; }

export default function DeviceRegistry({ agentBaseUrl }: DeviceRegistryProps) {
  const [devices, setDevices] = useState<DeviceAdapter[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    // async setup
    return () => { /* cleanup */ };
  }, [agentBaseUrl]);

  if (loading && devices.length === 0) return <p>Loading...</p>;
  return (
    <section aria-label="Device Registry">
      {error && <p role="alert" className="error">{error}</p>}
      ...
    </section>
  );
}
```

**Adapt for WorkflowBuilder** — replace `useEffect` fetch pattern with Rete.js `useEffect` + `useRef<HTMLDivElement>` mount guard. Use named export (not default) matching research pattern: `export function WorkflowBuilder(...)`. Cleanup: `return () => editorRef.current?.area.destroy()`.

**Error display pattern** — `{error && <p role="alert" className="error">{error}</p>}` — keep `role="alert"` for tests (established in Phase 5 component tests).

**Test pattern** (DeviceRegistry.test.tsx lines 1–99):
```tsx
import { render, screen, waitFor } from "@testing-library/react";
import { describe, it, expect, vi, afterEach } from "vitest";

afterEach(() => { vi.restoreAllMocks(); });

it("renders when fetch returns data", async () => {
  vi.stubGlobal("fetch", vi.fn(() => Promise.resolve({ ok: true, text: () => Promise.resolve("Healthy") } as unknown as Response)));
  render(<WorkflowBuilder onExport={vi.fn()} />);
  await waitFor(() => { expect(screen.getByText(/something/i)).toBeInTheDocument(); });
});
```
For Rete.js tests: `vi.mock("rete")` — do not test canvas rendering; test WSL export logic independently.

---

### `helix-desktop/src/components/ExecutionPanel/ExecutionPanel.tsx` (component, request-response)

**Analog:** `helix-desktop/src/components/ConnectionConfig.tsx` (lines 1–52)

**Form + async action pattern** (full file):
```tsx
import { useState } from "react";
import { LazyStore } from "@tauri-apps/plugin-store";
import type { AgentConnection } from "../types/agent";

export default function ConnectionConfig({ connection, onSave }: ConnectionConfigProps) {
  const [error, setError] = useState<string | null>(null);

  const handleSave = async () => {
    if (!host || port < 1) { setError("Invalid host or port"); return; }
    setError(null);
    await store.set("agentConnection", updated);
    await store.save();
    onSave(updated);
  };

  return (
    <section aria-label="Agent Connection">
      {error && <p role="alert" className="error">{error}</p>}
      <button onClick={handleSave}>Save</button>
    </section>
  );
}
```

**Adapt for ExecutionPanel** — replace `LazyStore` save with `fetch` POST to Agent REST. Props: `baseUrl: string`, `runspaceEvents: RunspaceEvent[]`. Buttons: Start, Pause, Resume, Abort each call the appropriate REST endpoint via `fetch`. Display step list from `runspaceEvents`.

**Test pattern** (ConnectionConfig.test.tsx lines 1–86):
```tsx
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
// vi.hoisted for mocks that reference module-level variables
const { mockFn } = vi.hoisted(() => ({ mockFn: vi.fn().mockResolvedValue(undefined) }));
vi.mock("@tauri-apps/plugin-store", () => {
  const LazyStoreMock = vi.fn(function (this: Record<string, unknown>, _name: string) {
    this.set = mockFn; this.save = mockFn;
  });
  return { LazyStore: LazyStoreMock };
});
```
ExecutionPanel tests: `vi.stubGlobal("fetch", ...)` for REST calls; `fireEvent.click(screen.getByRole("button", { name: /start/i }))` pattern.

---

### `helix-desktop/src/components/TrendCharts/ScalarTrendChart.tsx` (component, streaming)

**Analog:** `helix-desktop/src/components/DeviceRegistry.tsx` (structure only — no recharts analog exists)

**Pure display component pattern** — no state, no effects, no fetch. Props-only rendering:
```tsx
interface ScalarTrendChartProps {
  trendName: string;
  data: Array<{ t: number; v: number }>;
  unit?: string;
}

export function ScalarTrendChart({ trendName, data, unit }: ScalarTrendChartProps) {
  return (
    <section aria-label={`Trend: ${trendName}`} data-testid="scalar-trend-chart">
      {/* recharts ResponsiveContainer + LineChart */}
    </section>
  );
}
```

**Test pattern** — assert `data-testid="scalar-trend-chart"` renders; do NOT assert SVG paths (brittle). Per RESEARCH.md: "test by asserting chart container renders with correct data-testid".

**No close analog exists** — recharts is new to the project. See RESEARCH.md Pattern 6 for the recharts `LineChart` structure. Apply the `isAnimationActive={false}` directive on all `<Line>` components (Pitfall 6).

---

### `helix-desktop/src/components/SafetyPanel/SafetyPanel.tsx` (component, streaming)

**Analog:** `helix-desktop/src/components/DeviceRegistry.tsx` (lines 19–61)

**List-render + loading/error/empty pattern** (lines 44–60 of DeviceRegistry.tsx):
```tsx
if (loading && devices.length === 0) return <p>Loading...</p>;
return (
  <section aria-label="Device Registry">
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
```

**Adapt for SafetyPanel** — Props: `safetyEvents: SafetyEvent[]`. Map events to interlock rule list. Show `role="alert"` badge when any rule is in `"Triggered"` state. Key by `ruleId`.

---

### `helix-desktop/src/lib/workflowLibrary.ts` (utility, file-I/O)

**Analog:** `helix-desktop/src/components/ConnectionConfig.tsx` (lines 10–27 — LazyStore usage)

**LazyStore save/load pattern** (lines 10–27 of ConnectionConfig.tsx):
```typescript
import { LazyStore } from "@tauri-apps/plugin-store";
const store = new LazyStore("helix-config.json");

const handleSave = async () => {
  await store.set("agentConnection", updated);
  await store.save();
};
```

**Adapt for workflowLibrary** — replace `LazyStore` (key-value) with `@tauri-apps/plugin-fs` `writeTextFile`/`readTextFile` (file-based). Module-level utility functions (not a class or hook):
```typescript
import { writeTextFile, readTextFile, BaseDirectory } from "@tauri-apps/plugin-fs";

export async function saveWorkflow(name: string, doc: WslDocument): Promise<void> {
  await writeTextFile(`workflows/${name}.json`, JSON.stringify(doc, null, 2), {
    baseDir: BaseDirectory.AppData,
  });
}
```

**Test pattern** — `vi.mock("@tauri-apps/plugin-fs", ...)` mocking `writeTextFile`/`readTextFile` using the same `vi.hoisted()` + regular function constructor style established in `ConnectionConfig.test.tsx` lines 7–18.

---

### `helix-desktop/src-tauri/Cargo.toml` (config — add dependency)

**Analog:** `helix-desktop/src-tauri/Cargo.toml` (lines 24–31)

**Existing dependencies block** (lines 24–31):
```toml
[dependencies]
tauri = { version = "2", features = [] }
tauri-plugin-store = "2"
serde = { version = "1", features = ["derive"] }
serde_json = "1"
tokio = { version = "1", features = ["full"] }
tonic = "0.14"
```

**Add after `tauri-plugin-store`:**
```toml
tauri-plugin-fs = "2"
```

---

### `helix-desktop/src-tauri/src/lib.rs` (config — add plugin)

**Analog:** `helix-desktop/src-tauri/src/lib.rs` (lines 1–14)

**Existing plugin registration pattern** (lines 9–13):
```rust
tauri::Builder::default()
    .plugin(tauri_plugin_store::Builder::new().build())
    .invoke_handler(tauri::generate_handler![load_config, save_config])
    .run(tauri::generate_context!())
    .expect("error while running tauri application");
```

**Add fs plugin** — chain before `.invoke_handler`:
```rust
.plugin(tauri_plugin_fs::init())
```

---

### `helix-desktop/src-tauri/capabilities/default.json` (config — add fs permissions)

**Analog:** `helix-desktop/src-tauri/capabilities/default.json` (lines 1–10)

**Existing permissions array** (lines 6–9):
```json
"permissions": [
  "core:default",
  "store:default"
]
```

**Extend with fs permissions:**
```json
"permissions": [
  "core:default",
  "store:default",
  "fs:default",
  { "identifier": "fs:allow-read-text-file", "allow": [{ "path": "$APPDATA/workflows/*" }] },
  { "identifier": "fs:allow-write-text-file", "allow": [{ "path": "$APPDATA/workflows/*" }] },
  { "identifier": "fs:allow-mkdir", "allow": [{ "path": "$APPDATA/workflows" }] },
  { "identifier": "fs:allow-read-dir", "allow": [{ "path": "$APPDATA/workflows" }] }
]
```

---

## Shared Patterns

### Primary Constructor + ILogger Injection (C#)
**Source:** `Helix.Orchestration.Agent/Streaming/TrendAggregator.cs` lines 9–11
**Apply to:** `WorkflowEndpoints.cs`, `RunspaceEndpoints.cs`, `DeviceEndpoints.cs`, `RunspaceEventEmitter.cs`, `AgentService.cs`
```csharp
public sealed class MyService(
    IDependency dep,
    ILogger<MyService> logger)
```

### Minimal API Extension Method Registration (C#)
**Source:** `Helix.Orchestration.SiteServer/Endpoints/CampaignEndpoints.cs` lines 8–9
**Apply to:** `WorkflowEndpoints.cs`, `RunspaceEndpoints.cs`, `DeviceEndpoints.cs`
```csharp
public static class XyzEndpoints
{
    public static void MapXyzEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/xyz").WithTags("Xyz");
        // ...
    }
}
```
**Registration in Program.cs** (matches `app.MapExperimentEndpoints()` pattern in SiteServer Program.cs line 51):
```csharp
app.MapWorkflowEndpoints();
app.MapRunspaceEndpoints();
app.MapDeviceEndpoints();
```

### Results.Problem Error Handling (C#)
**Source:** `Helix.Orchestration.SiteServer/Endpoints/CampaignEndpoints.cs` lines 72–80
**Apply to:** All Agent endpoint files
```csharp
catch (KeyNotFoundException)
{
    return Results.NotFound(new { Error = $"Resource '{id}' not found." });
}
catch (Exception ex)
{
    return Results.Problem(title: "...", detail: ex.Message, statusCode: 500);
}
```

### useEffect + cleanup pattern (TypeScript)
**Source:** `helix-desktop/src/hooks/useAgentHealth.ts` lines 16–38
**Apply to:** `useAgentWebSocket.ts`, `useReteEditor.ts`, all new component hooks
```typescript
useEffect(() => {
  // setup
  return () => { /* cleanup */ };
}, [dep1, dep2]);
```

### vi.stubGlobal + waitFor test pattern (TypeScript)
**Source:** `helix-desktop/src/hooks/useAgentHealth.test.ts` lines 25–44
**Apply to:** All new hook and component tests
```typescript
vi.stubGlobal("fetch", vi.fn(() => Promise.resolve({ ok: true, status: 200 } as Response)));
await waitFor(() => { expect(result.current.someState).toBe(expectedValue); });
```
**NEVER use `vi.useFakeTimers()`** — breaks `@testing-library/react`'s `waitFor`. Established in Phase 5.

### vi.hoisted + constructor mock pattern (TypeScript)
**Source:** `helix-desktop/src/components/ConnectionConfig.test.tsx` lines 7–18
**Apply to:** Any test that mocks a class with `new` (`LazyStore`, `WebSocket`, Rete.js classes)
```typescript
const { mockFn } = vi.hoisted(() => ({ mockFn: vi.fn().mockResolvedValue(undefined) }));
vi.mock("some-module", () => {
  const MockClass = vi.fn(function (this: Record<string, unknown>) {
    this.method = mockFn;
  });
  return { MockClass };
});
```

### LazyStore persistence pattern (TypeScript)
**Source:** `helix-desktop/src/components/ConnectionConfig.tsx` lines 1–27
**Apply to:** `workflowLibrary.ts` (pattern only — actual implementation uses plugin-fs, not LazyStore)
```typescript
import { LazyStore } from "@tauri-apps/plugin-store";
const store = new LazyStore("filename.json");
await store.set("key", value);
await store.save();
```

### role="alert" error display pattern (TSX)
**Source:** `helix-desktop/src/components/ConnectionConfig.tsx` line 33; `DeviceRegistry.tsx` line 51
**Apply to:** All new React components with error states
```tsx
{error && <p role="alert" className="error">{error}</p>}
```

---

## No Analog Found

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| `helix-desktop/src/components/TrendCharts/ScalarTrendChart.tsx` | component | streaming | No recharts charts exist in project yet; use RESEARCH.md Pattern 6 |
| `helix-desktop/src/components/TrendCharts/SpectralChart.tsx` | component | streaming | No recharts charts exist in project yet; use RESEARCH.md Pattern 6 (AreaChart variant) |

---

## Metadata

**Analog search scope:** `Helix.Orchestration.Agent/`, `Helix.Orchestration.SiteServer/Endpoints/`, `Helix.Orchestration.Core/`, `helix-desktop/src/`, `helix-desktop/src-tauri/`
**Files scanned:** 24
**Pattern extraction date:** 2026-05-29
