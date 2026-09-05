# Codebase Concerns

**Analysis Date:** 2026-09-05

## Tech Debt

**InMemoryTrendProvider — missing ITrendProvider implementations:**
- Issue: Five interface methods throw `NotSupportedException` with explicit TODO comments: `GetTrend<T>()`, `GetScalarTrend<T>()`, `GetImageTrend()`, `GetVideoTrend()`, `GetDerivedTrend()`, and `RegisterDerivedTrend()`.
- Files: `Helix.Orchestration.Agent/Trends/InMemoryTrendProvider.cs`
- Impact: Any workflow that attempts to use scalar trend windows, image trends, video trends, or derived trends will throw at runtime. These methods are on the public `ITrendProvider` interface, so consumers can hold a reference that silently fails to function.
- Fix approach: Implement scalar trend windows using `InMemoryScalarTrend<T>` (the infrastructure already exists in `InMemoryDiodeArrayTrendProvider.cs`). Implement image/video/derived trends or introduce a `NotYetSupportedTrendProvider` that fails at registration time rather than at call time.

**SimAgentProxy and simulation layer wired in production SiteServer:**
- Issue: `SimAgentProxy` submits experiments as stub 2-second delays with no real WSL execution. The experiment POST endpoint in `Helix.Orchestration.SiteServer/Endpoints/ExperimentEndpoints.cs` builds an empty `RunspaceDefinition` with no nodes or edges regardless of the submitted WSL document. Comments read "Phase 9: Parse and compile WSL (stub)".
- Files: `Helix.Orchestration.SiteServer/Agent/SimAgentProxy.cs`, `Helix.Orchestration.SiteServer/Endpoints/ExperimentEndpoints.cs`, `Helix.Orchestration.SiteServer/Program.cs`
- Impact: Submitting an experiment via `/api/experiments` runs a no-op 2-second delay instead of the actual workflow. WSL content sent in the request body is discarded.
- Fix approach: Parse the submitted WSL document in `ExperimentEndpoints.cs` using the WSL compiler from `Helix.Orchestration.Core.WorkflowEngine`, forward to the real `GrpcAgentProxy` when `UseSimulation=false`, and replace the stub body in `SimAgentProxy` with a real simulator that walks the node graph.

**OceanViewDriver — entire driver is a no-op shell:**
- Issue: All 16 public methods throw `NotImplementedException("NetOceanDirect vendor DLL not yet referenced")`. The class compiles and is registered in the DI container but cannot be invoked at runtime.
- Files: `Helix.Orchestration.Devices/Drivers/OceanViewDriver.cs`
- Impact: Any capability that depends on `IOceanViewDriver` will throw immediately. Because `DeviceAdapterExtensions.cs` registers adapters eagerly at startup, a registration error here would prevent the Agent from starting.
- Fix approach: Add the NetOceanDirect NuGet package and uncomment the `OceanDirect.getInstance()` / `OpticalBench.getInstance()` calls. Until then, guard registration with a feature flag or conditional compile symbol so the unimplemented driver is not registered in production.

**PicoTc08LowLevelWrapper — missing native DLL:**
- Issue: All P/Invoke declarations target `usbtc08.dll` which must be present at runtime. If the DLL is absent, every call throws `DllNotFoundException` at the call site rather than at startup.
- Files: `Helix.Orchestration.Devices/Drivers/PicoTc08LowLevelWrapper.cs`
- Impact: TC-08 thermocouple device fails silently until a capability is invoked. `DeviceAdapterExtensions.cs` registers the adapter unconditionally.
- Fix approach: Copy `usbtc08.dll` into the `native/` output directory and add it as a `<None>` item with `CopyToOutputDirectory`. Add a startup health check that probes the DLL existence and disables the adapter cleanly.

**DeviceAdapterExtensions — hardcoded device addresses and ports:**
- Issue: Serial port `COM3` (VapourtecR4), TCP address `192.168.1.101:10001` (KnauerAzura), OPC UA endpoint `opc.tcp://localhost:4840` (Systag), and gRPC default `http://localhost:5080` are hardcoded in `DeviceAdapterExtensions.cs` and `AgentProxyOptions.cs`. Comments note "override via appsettings" but there is no validation that overrides are applied before the transport connects.
- Files: `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs`, `Helix.Orchestration.SiteServer/Agent/AgentProxyOptions.cs`
- Impact: Deploying the agent on a different machine connects to wrong hardware addresses. Integration tests may pass against localhost but fail against lab hardware.
- Fix approach: Bind all transport addresses from `IConfiguration` and throw `InvalidOperationException` at startup if required values are missing, consistent with `InfrastructureServiceExtensions.cs`.

**MqttTrendBridge — trends subscribed only at startup:**
- Issue: `ExecuteAsync` calls `GetAllTrends()` once at startup and subscribes to those observables. Trends registered after startup are never bridged to MQTT.
- Files: `Helix.Orchestration.SiteServer/Mqtt/MqttTrendBridge.cs`
- Impact: If a device comes online after the site server starts, its trend data is not published to MQTT or relayed to WebSocket dashboard clients until the server restarts.
- Fix approach: Subscribe to an `ITrendProvider.TrendAdded` event (if it exists) or poll `GetAllTrends()` on a timer and subscribe to newly discovered trends.

**Fumehood status hardcoded in dashboard:**
- Issue: `helix-dashboard/src/types/dashboard.ts` contains a comment: "Placeholder fumehood status — hardcoded in Phase 7, no API endpoint yet (D-05)."
- Files: `helix-dashboard/src/types/dashboard.ts`
- Impact: Dashboard fumehood status widget always shows static placeholder data, not live state.
- Fix approach: Implement the `/api/fumehood` endpoint in `Helix.Orchestration.SiteServer` (the `MapFumehoodEndpoints()` call exists in `Program.cs` but the implementation is minimal) and connect the dashboard widget.

**WorkspaceLayout save/load falls back to localStorage silently:**
- Issue: `helix-desktop/src/hooks/useWorkspaceLayouts.ts` uses `localStorage` as a fallback when `__TAURI_INTERNALS__` is absent. The detection is a runtime check, not a compile-time guard, so production Tauri builds that somehow miss the global would silently use browser storage that is cleared on profile reset.
- Files: `helix-desktop/src/hooks/useWorkspaceLayouts.ts`
- Impact: Users could lose workspace layout customizations across browser-profile resets or OS reinstalls.
- Fix approach: Log a warning when falling back to localStorage in non-test environments. Consider surfacing a UI indicator when Tauri persistence is unavailable.

## Known Bugs

**ExperimentEndpoints POST ignores submitted WSL document:**
- Symptoms: A POST to `/api/experiments` with a populated `wslDocument` body creates an empty `RunspaceDefinition` with zero nodes and edges. The experiment appears to start and completes after 2 seconds with `Status: Completed` regardless of workflow content.
- Files: `Helix.Orchestration.SiteServer/Endpoints/ExperimentEndpoints.cs` lines 59–65, `Helix.Orchestration.SiteServer/Agent/SimAgentProxy.cs` lines 23–27
- Trigger: Any experiment submission via the SiteServer API.
- Workaround: Submit experiments directly to the Agent via `/api/experiments` on port 5080 when the Agent is running in non-simulation mode.

**AgentService starts CancellationTokenSource in cancelled state:**
- Symptoms: On startup, `_cts.Cancel()` is called before the new `CancellationTokenSource` is created, but the pattern uses `_cts = new CancellationTokenSource()` and then starts loops. The disposal of the initial CTS at line 37 (`_cts.Cancel(); _cts.Dispose(); _cts = new CancellationTokenSource()`) is safe, but any rapid stop/start cycle can race against an in-flight loop that captured the old token.
- Files: `Helix.Orchestration.Agent/AgentService.cs` lines 35–43
- Trigger: Multiple rapid start/stop calls to the hosted service; in practice, only during tests.
- Workaround: None needed in normal deployment since the host lifecycle prevents rapid cycling.

## Security Considerations

**WebSocket origin allowlist includes localhost origins only:**
- Risk: `Helix.Orchestration.Agent/Program.cs` allows `http://localhost:5173`, `tauri://localhost`, and `https://tauri.localhost`. `Helix.Orchestration.SiteServer/Program.cs` allows only `http://localhost:5174`. There is no HTTPS enforcement on the Agent WebSocket endpoint in development, and no configurable per-environment allowlist.
- Files: `Helix.Orchestration.Agent/Program.cs` lines 283–285, `Helix.Orchestration.SiteServer/Program.cs` line 42
- Current mitigation: Localhost-only origins limit the attack surface on a lab intranet.
- Recommendations: Make the allowed origins list configurable via `appsettings.{Environment}.json`. Add HTTPS for production lab deployments. Validate that `app.UseHttpsRedirection()` in SiteServer executes before the WebSocket middleware (currently WebSocket middleware runs before the redirect, which is correct but requires documentation).

**MinIO credentials supplied as plain-text config:**
- Risk: `MinioObjectStorageSink` constructor accepts `AccessKey` and `SecretKey` as plain strings. `InfrastructureServiceExtensions.AddMinioStorage()` reads them from `IConfiguration` which may surface from environment variables, appsettings files, or secrets. If appsettings files are committed, credentials leak.
- Files: `Helix.Orchestration.Infrastructure/Storage/MinioObjectStorageSink.cs`, `Helix.Orchestration.Infrastructure/Persistence/InfrastructureServiceExtensions.cs`
- Current mitigation: `AddMinioStorage` is not called in `Program.cs` by default (only `AddHelixInfrastructure` is wired). MinIO is opt-in.
- Recommendations: Ensure Minio config section uses .NET Secret Manager or environment variable binding in production. Add a CI check that appsettings files do not contain `AccessKey`/`SecretKey` non-placeholder values.

**OPC UA transport accepts untrusted certificates:**
- Risk: `DeviceAdapterExtensions.cs` sets `AutoAcceptUntrustedCertificates = false` for the Systag OPC UA client. However, the default placeholder endpoint `opc.tcp://localhost:4840` suggests the setting may be toggled to `true` in developer convenience builds.
- Files: `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs` lines 48–50
- Current mitigation: `AutoAcceptUntrustedCertificates = false` is the current setting.
- Recommendations: Enforce via configuration validation at startup; throw if a non-localhost OPC UA endpoint is combined with `AutoAcceptUntrustedCertificates = true`.

## Performance Bottlenecks

**TrendAggregator scalar channel is unbounded:**
- Problem: `TrendAggregator._scalarChannel` uses `Channel.CreateUnbounded<ScalarTrendNotification>`. Under high MQTT tick rates (many devices pushing simultaneously), the channel can grow without limit if WebSocket broadcast consumers are slow.
- Files: `Helix.Orchestration.Agent/Streaming/TrendAggregator.cs` lines 16–18
- Cause: No backpressure or capacity cap. `TryWrite` always succeeds.
- Improvement path: Switch to `Channel.CreateBounded<ScalarTrendNotification>(new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.DropOldest })` for live-streaming scenarios where dropping stale samples is acceptable.

**RunspaceEventEmitter channel is unbounded with multiple readers:**
- Problem: `RunspaceEventEmitter._channel` is `CreateUnbounded` with `SingleReader = false`, meaning multiple consumers can race. If events are produced faster than the broadcast loop in `AgentService` drains them, memory grows unbounded.
- Files: `Helix.Orchestration.Agent/Events/RunspaceEventEmitter.cs` lines 11–12
- Cause: `SingleReader = false` prevents the optimized single-reader path and unbounded removes capacity limits.
- Improvement path: If there is always one broadcast loop reader, set `SingleReader = true` and add a bounded capacity.

**MqttTrendBridge.PublishTrendValue allocates a new `JsonSerializerOptions` per call:**
- Problem: `PublishTrendValue` creates `new JsonSerializerOptions { ... }` on every trend publish event. This is allocation-heavy under high trend rates.
- Files: `Helix.Orchestration.SiteServer/Mqtt/MqttTrendBridge.cs` lines 102–106
- Cause: Options instance not cached as a static field.
- Improvement path: Extract to a `private static readonly JsonSerializerOptions _jsonOptions` field, matching the pattern already used in `TrendWebSocketManager`.

## Fragile Areas

**WorkflowBuilder node graph — HelixNodes.tsx is 615 lines:**
- Files: `helix-desktop/src/components/WorkflowBuilder/nodes/HelixNodes.tsx`
- Why fragile: Single file defines all node types, rendering, connection logic, and parameter validation. Any change to node schema must be synchronized across node type definitions, the WSL compiler (`Helix.Orchestration.Core.WorkflowEngine`), and the DevicePalette. There are no automated schema-conformance tests linking frontend node types to backend capability definitions.
- Safe modification: Change one node type at a time and run the full Vitest suite plus a manual WSL export round-trip before committing.
- Test coverage: `WorkflowBuilder.test.tsx` covers high-level add/connect/export flows but does not verify individual node parameter schemas.

**WorkspaceLayout rc-dock integration — 402 lines with layout serialization:**
- Files: `helix-desktop/src/shell/WorkspaceLayout.tsx`
- Why fragile: `DEFAULT_LAYOUT` is a `LayoutBase` cast to `LayoutData` via a type assertion (`as any` comment notes this explicitly). rc-dock materializes `BoxBase → BoxData` internally, so a change to the rc-dock version or the `DEFAULT_LAYOUT` shape can silently break panel initialization. The `.dock-fbox` / `.dock-mbox` overlay trap (documented in `MEMORY.md`) means background CSS on container boxes causes all panels to be hidden.
- Safe modification: Do not set `background` on `.dock-box`, `.dock-hbox`, or `.dock-vbox` CSS classes. Test layout changes in the full Tauri shell, not just Vitest mocks.
- Test coverage: `WorkspaceLayout` has no dedicated unit test; layout behavior is exercised indirectly through `HelixShell.test.tsx`.

**DurableRunspace pause/resume is incomplete:**
- Files: `Helix.Orchestration.Agent/Runspaces/DurableRunspace.cs`
- Why fragile: `PauseAsync` cancels the execution CTS and sets `Status = Paused`. `ResumeAsync` creates a new CTS and re-starts execution. However, `IWorkflowExecutor.ExecuteAsync` is called again from the beginning — there is no checkpoint or continuation mechanism. A paused-then-resumed runspace restarts the workflow from node 0, not from where it paused.
- Safe modification: Do not expose Pause/Resume to users until a checkpoint mechanism is in place. Any code path that calls `PauseAsync` then `ResumeAsync` will silently repeat completed workflow steps.
- Test coverage: No test in `Helix.Orchestration.Agent.Tests` covers pause/resume round-trip.

## Scaling Limits

**InMemoryDiodeArrayTrendProvider — fixed capacity of 100 slices:**
- Current capacity: 100 `DiodeArraySlice` entries per trend stream (constant `Capacity = 100` at line 12).
- Limit: Older slices are dropped when capacity is exceeded. Long experiments with high-frequency diode-array acquisition will silently lose historical trend data.
- Scaling path: Persist slices to object storage (MinIO) above a configurable watermark or replace the in-process store with a time-series backend.

**InMemorySpectralTrendProvider — in-memory only with no upper bound stated:**
- Current capacity: Not explicitly capped (inspect `Helix.Orchestration.Agent/Trends/InMemorySpectralTrendProvider.cs` for actual buffer policy).
- Limit: Spectral data (full 1D or 2D scans) is memory-intensive. Long experiments accumulate all scans in process memory with no eviction.
- Scaling path: Apply the same ring-buffer strategy used in `InMemoryDiodeArrayTrendProvider` or stream directly to object storage.

**SimAgentProxy — runspace dictionary grows without bound:**
- Current capacity: `_runspaces` `ConcurrentDictionary` in `SimAgentProxy` is never pruned. Completed runspaces are retained indefinitely.
- Limit: In a campaign with many iterations, all completed runspaces remain in memory for the server lifetime.
- Scaling path: Evict completed runspaces after a configurable TTL (e.g., 1 hour), or move runspace state to the PostgreSQL `Experiments` table and query by ID.

## Dependencies at Risk

**rc-dock — no active maintenance signal:**
- Risk: `rc-dock` is a React docking library used heavily in `WorkspaceLayout.tsx`. There are known behavioral traps with `.dock-fbox` / `.dock-mbox` overlays (see `MEMORY.md`). If the library is abandoned or breaks on a React major version upgrade, the entire workspace panel system requires replacement.
- Impact: All dockable panels (`DeviceExplorer`, `TrendExplorer`, `ChartPanel`, `LogOutput`, `ExperimentTimeline`, `WorkflowBuilder`, `ExecutionPanel`, `SafetyPanel`, `DeviceManualControl`) would need to be re-wired to an alternative docking system.
- Migration plan: Evaluate `react-mosaic` or `golden-layout` as alternatives. Abstract the panel host behind a thin `IPanelHost` interface to isolate rc-dock specifics.

## Missing Critical Features

**WSL document parsing not wired in SiteServer experiment submission:**
- Problem: The SiteServer `/api/experiments` POST endpoint accepts a `wslDocument` field but never parses or compiles it. The WSL compiler (`Helix.Orchestration.Core.WorkflowEngine`) exists and is tested, but there is no bridge from the HTTP endpoint to the compiler output.
- Blocks: Meaningful end-to-end experiment execution from the dashboard or desktop app through the SiteServer path. Real multi-step workflows cannot run via SiteServer; they must be submitted directly to the Agent.

**Trend scalar unit propagation is empty string:**
- Problem: `TrendAggregator` publishes `ScalarTrendNotification` with `Unit = string.Empty` in all three scalar branches. The `TrendValueWireDto` sent to WebSocket clients therefore always has `unit: ""`.
- Files: `Helix.Orchestration.Agent/Streaming/TrendAggregator.cs` lines 56, 62, 68
- Blocks: Dashboard display of engineering units (°C, bar, mL/min) on live charts.

## Test Coverage Gaps

**AgentService broadcast loops — no unit or integration tests:**
- What's not tested: `BroadcastLoopAsync`, `TrendForwardLoopAsync`, `SafetyPollLoopAsync`, `DeviceHealthPollLoopAsync` in `AgentService`. These are fire-and-forget tasks started in `StartAsync`.
- Files: `Helix.Orchestration.Agent/AgentService.cs`
- Risk: Exceptions in these loops are caught and logged but no test verifies that clients receive events, that safety violations are broadcast, or that the loops recover from transient WebSocket errors.
- Priority: High

**MqttTrendBridge — no tests:**
- What's not tested: MQTT connection, trend subscription, publish on value, WebSocket relay, reconnection after broker unavailability.
- Files: `Helix.Orchestration.SiteServer/Mqtt/MqttTrendBridge.cs`
- Risk: Silent data-loss if the MQTT broker is briefly unavailable. The early-return on connection failure (`return` at line 64) means the bridge never retries.
- Priority: High

**TrendWebSocketManager — no tests:**
- What's not tested: Registration, dead-socket removal on broadcast, multi-client fan-out, cancellation handling.
- Files: `Helix.Orchestration.SiteServer/WebSockets/TrendWebSocketManager.cs`
- Risk: Memory leak from dead sockets if the removal path has a race; confirmed only by code inspection not test.
- Priority: Medium

**DurableRunspace pause/resume — no tests:**
- What's not tested: Pause then resume causes workflow to re-execute from the start (see Fragile Areas above).
- Files: `Helix.Orchestration.Agent/Runspaces/DurableRunspace.cs`
- Risk: If Pause/Resume is exposed via UI, users unknowingly re-run completed workflow steps, potentially sending duplicate device commands to physical hardware.
- Priority: High

**E2e integration tests are acknowledged stubs:**
- What's not tested: `tests/Helix.Orchestration.Agent.Tests/Integration/E2eWorkflowExecutionTests.cs` header explicitly states these are Phase 8 stubs. No full end-to-end path (HTTP → experiment submission → capability execution → trend recording → status polling) is covered by an automated test.
- Files: `tests/Helix.Orchestration.Agent.Tests/Integration/E2eWorkflowExecutionTests.cs`
- Risk: Regressions in the integration path between SiteServer and Agent are not caught until manual QA.
- Priority: High

---

*Concerns audit: 2026-09-05*
