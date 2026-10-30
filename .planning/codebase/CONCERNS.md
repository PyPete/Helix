---
last_mapped_commit: 25fb557bd430bea160370af41b8deb877170076b
last_mapped_at: 2026-10-29
---
# Codebase Concerns

**Analysis Date:** 2026-10-29

## Tech Debt

**Unsupported and placeholder device integrations:**
- Issue: `OceanViewDriver` deliberately compiles without its vendor SDK and every operational member throws `NotImplementedException`; the Pico TC-08 wrapper also declares that its native DLL has not been supplied.
- Files: `Helix.Orchestration.Devices/Drivers/OceanViewDriver.cs`, `Helix.Orchestration.Devices/Drivers/PicoTc08LowLevelWrapper.cs`
- Impact: registering either integration in a production device configuration produces runtime failures rather than a capability-level “unsupported” result.
- Fix approach: ship and validate the vendor/native dependencies through explicit project/runtime packaging, or remove these drivers from discoverable production registrations until implemented.

**Incomplete simulation trend contract:**
- Issue: simulation explicitly does not implement derived trends or diode-array chromatogram extraction.
- Files: `Helix.Orchestration.Simulation/SimulatedTrendProvider.cs`
- Impact: workflows and UI paths that require these supported domain abstractions cannot be validated against the simulator.
- Fix approach: implement the declared provider operations and add coverage beside `tests/Helix.Orchestration.SiteServer.Tests/Simulation/SimulatedTrendProviderTests.cs`.

**E2E test suite is only a placeholder:**
- Issue: the end-to-end workflow test file labels key trend-provider operations as TODO stubs; its nominal capability tests construct workflow definitions with no nodes or edges, so they do not exercise a capability step.
- Files: `tests/Helix.Orchestration.Agent.Tests/Integration/E2eWorkflowExecutionTests.cs`
- Impact: cross-process workflow execution, device invocation, event persistence, and abort behavior lack an executable regression gate.
- Fix approach: replace stubs with containerized/in-process agent-to-site-server scenarios using deterministic fake adapters and non-empty compiled workflow definitions.

**Build and test artifacts reside in the working tree:**
- Issue: generated `bin/`, `obj/`, coverage XML, TRX test results, and test-result directories are present throughout the repository workspace.
- Files: `Helix.Orchestration.Infrastructure/bin/`, `Helix.Orchestration.Infrastructure/obj/`, `tests/Helix.Orchestration.SiteServer.Tests/obj/`, `coverage-results/`, `test-results/`, `test-results-hang-test/`
- Impact: source discovery is noisy, repository state is easier to pollute, and generated test artifacts can be mistaken for authoritative source inputs.
- Fix approach: ensure generated paths are ignored and CI writes reports to an artifact directory outside tracked source paths.

## Known Bugs

**Blocking legacy serial transport operations:**
- Symptoms: the older agent serial transport synchronously blocks with `SemaphoreSlim.Wait()` in transport calls.
- Files: `Helix.Orchestration.Agent/Transport/SerialTransport.cs`
- Trigger: calling the transport in an ASP.NET/hosted-service execution path while a competing operation holds the lock.
- Workaround: use the asynchronous device transport implementations under `Helix.Orchestration.Devices/Transports/Serial/` rather than `Helix.Orchestration.Agent/Transport/SerialTransport.cs`.

**Recorder drops data under sustained ingestion:**
- Symptoms: the recorder logs and discards MQTT events when its bounded channel is full.
- Files: `Helix.Orchestration.Recorder/Services/MqttRecorderService.cs`
- Trigger: incoming events outpace object-storage flush throughput or the downstream writer is unavailable.
- Workaround: no durable fallback is implemented; increase channel/flush capacity only delays loss.

## Security Considerations

**Unauthenticated control-plane APIs:**
- Risk: agent endpoints include workflow/runspace/experiment creation and device execution routes, while the hosting code maps them without authentication or authorization middleware/requirements.
- Files: `Helix.Orchestration.Agent/Program.cs`, `Helix.Orchestration.Agent/Api/DeviceEndpoints.cs`, `Helix.Orchestration.Agent/Api/RunspaceEndpoints.cs`, `Helix.Orchestration.Agent/Api/ExperimentEndpoints.cs`
- Current mitigation: agent CORS and WebSocket origins are limited to the configured desktop client origins in `Helix.Orchestration.Agent/Program.cs`; no corresponding access-control enforcement is present for agent route mapping.
- Recommendations: require authenticated identities and role/device-scoped authorization before all command routes; make the agent network-private until this is deployed.

**Permissive agent CORS policy:**
- Risk: the configured policy permits every method and header for the three desktop development/application origins; those browser clients can reach a device-control API that has no route-level identity enforcement.
- Files: `Helix.Orchestration.Agent/Program.cs`
- Current mitigation: not detected in route-level authorization.
- Recommendations: retain only required production origins/methods/headers and pair CORS with server-side authentication rather than treating it as access control.

**Native library loading boundary:**
- Risk: the C DLL transport resolves and invokes configured native exports; an untrusted or misconfigured library path can execute native code in the agent process.
- Files: `Helix.Orchestration.Devices/Transports/CDll/CDllDeviceTransport.cs`, `Helix.Orchestration.Devices/Transports/CDll/CDllTransportOptions.cs`
- Current mitigation: transport faults are logged; no allowlist is evidenced by the transport options.
- Recommendations: allow only administrator-managed absolute paths, validate publisher/hash before loading, and isolate vendor drivers in a restricted process where practical.

## Performance Bottlenecks

**Polling-heavy workflow and device supervision:**
- Problem: waits, safety evaluation, lease expiration, and transport watchdogs run periodic polling loops.
- Files: `Helix.Orchestration.Core/Execution/WaitFor.cs`, `Helix.Orchestration.Core/Safety/SafetyEngine.cs`, `Helix.Orchestration.Agent/Leasing/LeaseExpiryWorker.cs`, `Helix.Orchestration.Devices/FaultTolerance/TransportWatchdog.cs`
- Cause: each active workflow/device schedules independent `Task.Delay` cycles rather than being event-driven or centrally scheduled.
- Improvement path: use observable/device-state notifications where available, consolidate timers, and load-test interval choices at expected device and workflow counts.

**Unbounded in-memory operational state:**
- Problem: core implementations named `InMemoryRunspace`, `InMemoryGlobalStateStore`, and `InMemoryDeviceLeasingService` hold operational state only in process memory.
- Files: `Helix.Orchestration.Core/Runspace/InMemoryRunspace.cs`, `Helix.Orchestration.Core/State/InMemoryGlobalStateStore.cs`, `Helix.Orchestration.Core/Leasing/InMemoryDeviceLeasingService.cs`
- Cause: no durable/shared backing store is used for these runtime coordinators.
- Improvement path: define retention bounds and implement durable, multi-instance-backed stores before scaling agents or relying on restart recovery.

## Fragile Areas

**Fire-and-forget MQTT publication from reactive callbacks:**
- Files: `Helix.Orchestration.SiteServer/Mqtt/MqttTrendBridge.cs`
- Why fragile: callbacks initiate asynchronous publication without an awaitable delivery path, so shutdown, ordering, backpressure, and retry behavior are difficult to coordinate.
- Safe modification: put publication behind a bounded channel/hosted worker that owns MQTT connection lifecycle and defines a loss/retry policy.
- Test coverage: no focused tests were found for `MqttTrendBridge`.

**Safety emergency-stop observability:**
- Files: `Helix.Orchestration.Core/Safety/SafetyEngine.cs`
- Why fragile: emergency-stop and reset events are emitted with `Console.WriteLine`, bypassing the configured logging, telemetry, and durable audit-event mechanisms used elsewhere.
- Safe modification: retain safety state semantics, then route events through injected structured logging and an auditable event sink; preserve a last-resort local output only if required.
- Test coverage: unit tests exist in `tests/Helix.Orchestration.Core.Tests/Safety/SafetyEngineTests.cs`, but no production logging/audit sink behavior is covered.

## Scaling Limits

**Single-process agent coordination:**
- Current capacity: capacity limits are not specified in configuration or code.
- Limit: in-memory leases, runspaces, and state cannot coordinate ownership or recovery across multiple agent instances.
- Scaling path: move coordination to durable shared services and add fencing/lease semantics that survive process restarts.

## Dependencies at Risk

**Vendor-only hardware SDKs:**
- Risk: OceanView and Pico integrations require dependencies outside normal project package restoration.
- Impact: a clean deployment can compile yet lack runtime functionality for those device types.
- Migration plan: package vendor SDKs through an internal, versioned distribution with license checks and run hardware-contract tests in a compatible environment.

## Missing Critical Features

**Production identity and authorization boundary:**
- Problem: no authentication/authorization enforcement is evidenced around device-control and orchestration endpoints.
- Blocks: safely exposing `Helix.Orchestration.Agent` or `Helix.Orchestration.SiteServer` control APIs beyond a strictly trusted network.

## Test Coverage Gaps

**Real hardware, native, and external-broker paths:**
- What's not tested: actual OceanView/Pico vendor dependencies, native C DLL execution, and live MQTT/MinIO/PostgreSQL failure/recovery behavior.
- Files: `Helix.Orchestration.Devices/Drivers/OceanViewDriver.cs`, `Helix.Orchestration.Devices/Drivers/PicoTc08LowLevelWrapper.cs`, `Helix.Orchestration.Devices/Transports/CDll/CDllDeviceTransport.cs`, `Helix.Orchestration.Recorder/Services/MqttRecorderService.cs`, `Helix.Orchestration.Infrastructure/Storage/MinioObjectStorageSink.cs`
- Risk: production-only integration and data-loss failures can pass the primarily mocked/unit test suite unnoticed.
- Priority: High

---

*Concerns audit: 2026-10-29*
