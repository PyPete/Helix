# Phase 6 — WSL Compiler, Device Parity & Trend Ingestion Foundation Handover

## Scope Delivered

Phase 6 delivered foundational compiler/runtime and device parity improvements:

1. WSL compiler foundation
2. Device registry for compile-time device binding
3. Vapourtec parity uplift (serial protocol alignment)
4. MQTT trend ingestion background service foundation
5. Expanded device test coverage

## WSL Compiler Foundation

Added compiler contract and diagnostics pipeline:

- `IWslCompiler`
- `WslCompiler`
- `CompilationResult`
- `CompilationDiagnostic`
- `DiagnosticSeverity`

Compiler behavior includes:

- Schema/version validation
- Graph shape validation
- Node parameter validation
- Device alias/binding validation via `DeviceRegistry`

Outcome: deterministic compile result object with aggregated diagnostics and success/failure semantics.

## Device Binding Foundation

Added:

- `DeviceDescriptor`
- `DeviceRegistry`

This enables WSL compile-time checks of logical device aliases and capability availability boundaries.

## Vapourtec R4 Parity Review & Alignment

User review correctly identified drift from legacy POP behavior. The migrated driver has now been realigned to the legacy command model in key areas.

### Changes Applied

- Restored legacy command vocabulary and structure:
  - `FW` (forward command)
  - `KP` (valve switching)
  - `GA` (status)
  - `HF` / `HT` / `HP` history reads
- Restored dual-unit behavior where required:
  - `Start()` sends main and forwarded power-on
  - `Stop()` sends main and forwarded power-off
  - Forwarded flow commands for pumps C/D
- Restored flow-rate scaling behavior:
  - API flow input remains in mL/min style range
  - command payload scales by `*1000` to legacy wire units
- Updated valve routing logic to match legacy `KP` mappings
- Reworked measurements to parse history frames instead of synthetic direct reads:
  - Temperatures from `HT`
  - Flows from `HF` and `FW HF`
  - Pressures from `HP`
- Kept `BaudRate = 19200` parity

### Status Behavior

- `GetStatus()` now uses `GA` and maps fault/running state from response content.
- Current model remains intentionally simplified compared with full legacy internals.

## Agent Trend Ingestion Foundation

Added `MqttTrendIngestionService : BackgroundService`:

- Connects to MQTT broker
- Subscribes to configured topic filters
- Routes payloads to `InMemoryTrendProvider`
- Includes reconnect loop and graceful shutdown

Config support added in `Helix.Orchestration.Agent/appsettings.json`:

- `Mqtt:BrokerHost`
- `Mqtt:BrokerPort`
- `Mqtt:TopicFilters`

Service is registered in Agent hosted services.

## Testing

Created `Helix.Orchestration.Devices.Tests` with xUnit + Moq and expanded Vapourtec tests to cover:

- Identity checks
- Main + forwarded power commands
- Flow/temperature command encoding
- Valve mapping via `KP`/`FW KP`
- Status mapping via `GA`
- History-based measurement parsing
- Device metadata and close path

Latest run for this test suite: passing.

## Known Remaining Gaps

Not yet ported from legacy POP complexity:

- Full ramp orchestration model (`CancellationTokenSource` maps, pause/resume/stop semantics)
- Full setpoint device mode tracking for pumps/reactors
- Complete trend metadata richness from POP enums
- Additional Vapourtec operations (e.g., loop fill/inject and pressure limit APIs) as first-class interface methods

These are now cleanly separable incremental tasks rather than blocking architecture work.

## Recommended Next Steps

1. Add explicit IVapourtec contract operations for loop fill/inject and pressure limit.
2. Introduce ramp orchestration abstraction in Devices layer without leaking UI-era concerns.
3. Add integration tests for real serial transcripts (golden frame parsing for `HF/HT/HP/GA`).
4. Wire MQTT ingestion to configurable typed payload contracts and schema validation.
