﻿﻿﻿﻿﻿# Phase 2: VapourtecR4 Reference Driver — Plan

**Phase:** 2
**Goal:** Prove the DeviceAdapterBase architecture end-to-end with one real device driver (VapourtecR4), so that all subsequent drivers (Phase 3) can follow the same pattern.
**Requirements:** B1, B2, B6, B7, B8
**Depends on:** Phase 1 (complete)
**Mode:** standard

---

## Source Audit

| Source | Item | Covered by |
|--------|------|------------|
| GOAL | End-to-end VapourtecR4 via DeviceAdapterBase | T4, T6, T7 |
| REQ B1 | Driver inventory audit document | T1 |
| REQ B2 | VapourtecR4 full POP vocabulary + status + fault detection (10 capabilities incl. SelectLoopFill/Inject) | T4 |
| REQ B6 | ISerialTransport.Open baudRate parameter | T2 |
| REQ B7 | DI lifetime mismatch — transports must be Singleton | T3 |
| REQ B8 | VapourtecR4 unit tests | T7 |
| ROADMAP | TrendAggregator scalar discard fix | T5 |
| ROADMAP | HelixAdapterCapabilityBridge wiring VapourtecR4 into CapabilityRegistry | T6 |

---

## Codebase Facts (executor reference)

### Key types — do not explore, use directly

**DeviceAdapterBase** (`Helix.Orchestration.Devices/Abstractions/DeviceAdapterBase.cs`)
- Constructor: `protected DeviceAdapterBase(IDeviceTransport transport, ILogger logger)`
- Abstract: `protected abstract void RegisterCapabilities(CapabilityDispatchBuilder builder)`
- `CapabilityDispatchBuilder.Register(CapabilityDescriptor, Func<JsonElement, string?, CancellationToken, Task<CapabilityInvocationResult>>)`
- `CapabilityDispatchBuilder.RegisterSimple(name, description, Func<CancellationToken, Task<CapabilityInvocationResult>>)`
- `StreamTrendsAsync` returns `IAsyncEnumerable<TrendSample>` — override to emit device telemetry

**IHelixAdapter** (`Helix.Orchestration.Devices/Abstractions/IHelixAdapter.cs`)
- `CapabilityInvocationResult.Successful(JsonElement? returnValue = null)` — note: `Successful`, not `Succeeded`
- `CapabilityInvocationResult.Failed(string errorMessage)`
- `TrendSample { TrendName, Value (object), Timestamp, Unit? }`

**SerialDeviceTransport** (`Helix.Orchestration.Devices/Transports/Serial/SerialDeviceTransport.cs`)
- Constructor (DI): `SerialDeviceTransport(SerialTransportOptions options, ILogger<SerialDeviceTransport> logger)`
- Test constructor (internal): `SerialDeviceTransport(options, logger, ISerialPortConnectionFactory)`
- `SendReceiveAsync(byte[] command, CancellationToken)` — sends bytes, reads until `SerialTransportOptions.CommandTerminator`
- `SerialTransportOptions.BaudRate` — already present; baud rate is set via options, not via an `Open(baudRate)` call

**ISerialTransport** (`Helix.Orchestration.Devices/Transport/ISerialTransport.cs`) — legacy interface
- Currently: `void Open(string portName, int baudRate = 9600)` — signature already has baudRate as optional param
- `SerialTransport` in Agent (`Helix.Orchestration.Agent/Transport/SerialTransport.cs`) already implements `Open(string portName, int baudRate = 9600)`
- B6 status: interface signature is already correct. The gap is that `DeviceAdapterExtensions.cs` still registers the old `ISerialTransport`/`SerialTransport` types instead of the new `SerialDeviceTransport`. T2 documents this finding and removes the dead registration.

**CapabilityRegistry** (`Helix.Orchestration.Core/Capabilities/CapabilityRegistry.cs`)
- `Register(string capabilityName, ICapabilityInvoker invoker)`
- Core's `ICapabilityInvoker.InvokeAsync(string, object?, CancellationToken)` returns `Core.CapabilityInvocationResult`
- Devices' `IHelixAdapter.InvokeCapabilityAsync(string, JsonElement, string?, CancellationToken)` returns `Devices.CapabilityInvocationResult`
- These are two separate types — a bridge class is required (see T6)

**DeviceAdapterExtensions** (`Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs`)
- Currently registers `ISerialTransport`/`IModbusTransport`/`ITcpTransport` as `Transient` (legacy types)
- Builds a `CapabilityRegistry` singleton but registers nothing into it
- Phase 2 replaces this with `AddSingleton` for `SerialDeviceTransport` and wires VapourtecR4 + bridge

**TrendAggregator** (`Helix.Orchestration.Agent/Streaming/TrendAggregator.cs`)
- `ProcessAsync(string, TrendValue, CancellationToken)` — switch on `TrendValue` subtypes
- `ScalarValue<T>` hits the `default` branch and is silently discarded via `LogDebug`
- Fix: add `case ScalarValue<double>` (and `float`, `int`) branches that call `InMemoryTrendProvider.PushScalar`
- `InMemoryTrendProvider.GetScalarTrend<T>` currently throws `NotSupportedException` — also needs a `PushScalar` path

**TrendValue hierarchy** (`Helix.Orchestration.Core/Trends/TrendValue.cs`)
- `ScalarValue<T>(T Value) : TrendValue where T : struct, IComparable<T>`
- `SpectralValue`, `SpectralMatrixValue`, `StructuredResultValue` — already handled

**InMemoryTrendProvider** (`Helix.Orchestration.Agent/Trends/InMemoryTrendProvider.cs`)
- Has `Push1D`, `Push2D`, `PushDiodeArraySlice` — no scalar push method yet
- `GetScalarTrend<T>` throws `NotSupportedException`

**Test seam pattern** (from `SerialTransportTests.cs` and `DeviceAdapterBaseTests.cs`)
- Use `NSubstitute` (not Moq)
- Inject `ISerialPortConnectionFactory` mock to provide pre-canned response bytes via `FakeSerialPortConnection`
- `FakeSerialPortConnection.EnqueueRead(string)` queues ASCII response payloads
- `FakeSerialPortConnection.Writes` captures sent bytes

---

## Tasks

### T1 — Driver Audit Document
**Requirement:** B1
**Files:**
- `Helix.Orchestration/docs/driver-inventory.md` — new file; driver inventory markdown

**Implementation:**

Create `Helix.Orchestration/docs/driver-inventory.md` (note: the `docs/` directory at repo root already has handover documents; this new file goes inside the solution-internal docs path `Helix.Orchestration/docs/` — create that directory if it does not exist).

The document must contain a table with one row per known driver derived from analysis of `docs/DEVICE-MIGRATION-HANDOVER.md`, `docs/PHASE13-DEVICE-INTEGRATION-LAYER-HANDOVER.md`, and the codebase. Columns: Driver name, Protocol, Transport interface, Connection type, **Full POP vocabulary (from source)**, **Phase 2 implemented commands**, POP.DeviceCore source file, Phase 2 status.

The vocabulary columns are split to distinguish what the source defines from what Phase 2 delivers:
- **Full POP vocabulary (from source)**: all commands known from the source driver / handover docs
- **Phase 2 implemented**: only the subset implemented in this phase; leave blank or "N/A" for drivers not in Phase 2

Include rows for:
1. **VapourtecR4** — Serial / ISerialTransport / RS-232 @ 19200 baud / Full POP vocabulary: PN,PF,FW,KP,FR,PL,HT,HF,HP,GA,ST,GV,SV,PN,PF,FR,ST,GV / Phase 2 implemented: PN,PF,FW,KP,FR,PL,HT,HF,HP,GA,ST,GV,SV / `POP.DeviceCore/Drivers/VapourtecR4.cs` / Phase 2 (this plan). Note: PN,PF,FR,ST,GV are in the full vocabulary but are not implemented until Phase 3.
2. **KnauerAzura** — TCP / ITcpTransport / TCP / ACK/NAK command protocol, gradient config, UV config, run state polling / N/A (Phase 3) / `POP.DeviceCore/Drivers/KnauerAzura.cs` / Phase 3
3. **SystagOpcUaClient** — OPC UA / IOpcTransport → OpcDeviceTransport / OPC UA endpoint / setpoint writes via OPC UA node IDs, change subscriptions for live updates / N/A (Phase 3) / `POP.DeviceCore/Drivers/SystagOpcUaClient.cs` / Phase 3
4. **ModbusDeviceBase subclasses** — Modbus / IModbusTransport / RS-485 / register read/write / N/A (Phase 3) / `POP.DeviceCore/Comm/` (no named concrete drivers in documentation) / Phase 3 (audit pending)
5. **SetPointDeviceBase subclasses** (StirrerDeviceBase, VacPumpDeviceBase) — Serial or TCP depending on hardware / Start/Stop/SetPoint/Limits / N/A (Phase 3) / documented in `DEVICE-MIGRATION-HANDOVER.md` / Phase 3

Include a "Parity Gaps" section listing gaps identified in `DEVICE-MIGRATION-HANDOVER.md` (SelectReagent/SelectSolvent valve encoding, live measurement read-back via GF/GT, pump configuration, fault callbacks).

Include a "New Architecture" section summarising DeviceAdapterBase composition model replacing the deleted inheritance base classes.

**Test:**
File exists at `Helix.Orchestration/docs/driver-inventory.md` with all five driver rows populated, and with separate "Full POP vocabulary" and "Phase 2 implemented" columns for VapourtecR4.

---

### T2 — Verify ISerialTransport.Open Signature and Remove Dead Legacy Transport Registrations
**Requirement:** B6
**Files:**
- `Helix.Orchestration.Devices/Transport/ISerialTransport.cs` — verify (no change needed; already has baudRate)
- `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs` — remove legacy `ISerialTransport`, `IModbusTransport`, `ITcpTransport` Transient registrations; these are legacy types no longer needed now that DeviceAdapterBase uses `IDeviceTransport`/`SerialDeviceTransport` directly

**Implementation:**

First, read `Helix.Orchestration.Devices/Transport/ISerialTransport.cs`. The current signature is:
```
void Open(string portName, int baudRate = 9600);
```
This already matches the B6 requirement. No change to the interface is needed. Document this finding in a brief code comment at the top of the interface: `// baudRate parameter added; see B6.`

Second, edit `DeviceAdapterExtensions.cs`. Remove these three lines from `AddMigratedDeviceAdapters()`:
```csharp
services.AddTransient<ISerialTransport, SerialTransport>();
services.AddTransient<IModbusTransport, ModbusTransport>();
services.AddTransient<ITcpTransport, TcpTransport>();
```
Also remove the using directives that are now unused: `Helix.Orchestration.Agent.Transport` and `Helix.Orchestration.Devices.Transport`. Keep the `CapabilityRegistry` singleton registration (T6 will augment it).

The `SerialTransport`, `ModbusTransport`, `TcpTransport` classes in `Helix.Orchestration.Agent/Transport/` are NOT deleted — they may still be referenced by legacy code or tests. Only the DI registrations are removed.

**Test:**
`dotnet build Helix.Orchestration.slnx` exits 0. No compile errors from removed usings.

---

### T3 — Fix DI Lifetime: Adapters and Transports as Singleton
**Requirement:** B7
**Files:**
- `Helix.Orchestration.Devices/Hosting/DevicesServiceCollectionExtensions.cs` — change `AddScoped` to `AddSingleton` for transport and adapter registrations

**Implementation:**

Read `Helix.Orchestration.Devices/Hosting/DevicesServiceCollectionExtensions.cs`. In every `AddHelix*Adapter` method (`AddHelixSerialAdapter`, `AddHelixTcpAdapter`, `AddHelixModbusAdapter`, `AddHelixOpcAdapter`, `AddHelixCDllAdapter`), change:
```csharp
services.AddScoped<IDeviceTransport, {ConcreteTransport}>();
services.AddScoped<TAdapter>();
services.AddScoped<IHelixAdapter>(sp => sp.GetRequiredService<TAdapter>());
services.AddScoped<TransportWatchdog>();
```
to:
```csharp
services.AddSingleton<IDeviceTransport, {ConcreteTransport}>();
services.AddSingleton<TAdapter>();
services.AddSingleton<IHelixAdapter>(sp => sp.GetRequiredService<TAdapter>());
services.AddSingleton<TransportWatchdog>();
```

All five methods get the same treatment. `ReconnectPolicy` and options singletons are unchanged.

Add an XML doc comment above each changed block: `// Singleton lifetime required: adapters are captured by CapabilityRegistry which is Singleton.`

Add a comment block at the top of the file explaining the lifetime requirement:
```csharp
// DI Lifetime Note (B7):
// CapabilityRegistry is registered as Singleton in DeviceAdapterExtensions.AddMigratedDeviceAdapters().
// Transports and adapters registered via these extension methods must also be Singleton.
// Scoped or Transient transports captured inside a Singleton would produce a captive dependency —
// the transport would be created once and never refreshed across requests or scopes.
// Hot-reconnect is handled by TransportWatchdog at the transport level, not by DI re-creation.
```

**Test:**
`dotnet build Helix.Orchestration.slnx` exits 0. Existing `DevicesServiceCollectionExtensions` tests (if any) still pass.

---

### T4 — Implement VapourtecR4.cs
**Requirement:** B2
**Files:**
- `Helix.Orchestration.Devices/Drivers/VapourtecR4.cs` — new file; full DeviceAdapterBase driver

**Implementation:**

Create `Helix.Orchestration.Devices/Drivers/` directory if it does not exist. Create `VapourtecR4.cs` in namespace `Helix.Orchestration.Devices.Drivers`.

The class signature:
```csharp
public sealed class VapourtecR4 : DeviceAdapterBase
```

Constructor:
```csharp
public VapourtecR4(SerialDeviceTransport transport, ILogger<VapourtecR4> logger)
    : base(transport, logger)
```
The constructor takes `SerialDeviceTransport` (concrete type, not interface) so DI resolves unambiguously. Cast to `IDeviceTransport` is handled by `base`.

**Encoding helpers (private static):**
- `static byte[] Encode(string command)` — `Encoding.ASCII.GetBytes(command + "\r\n")` (VapourtecR4 terminates with CRLF)
- `static string Decode(byte[] response)` — `Encoding.ASCII.GetString(response).Trim()` (full trim: device HT responses can have a leading space)

**SOURCE REFERENCE: `POP.DeviceCore/Drivers/VapourtecR4.cs` (PopNextGen repo)**
The actual POP command map from the real implementation:
- `PN` = PowerOn (start pumps + heaters)
- `PF` = PowerOff (stop pumps + heaters)
- `FW` = ForwardCommand prefix (addresses second unit, pumps C/D: `FW {cmd}`)
- `KP` = SwitchValves (`KP {n}` where n=0-9; valve position encoding below)
- `FR` = SetFlowRate (`FR {pump_index} {rate_ul_min * 1000}` as integer)
- `PL` = SetPressureLimit (`PL {mbar}`)
- `HT` = TempHistory (response: `4913,U,-1000,U,-1000,U,201,U,-1000&...` -- comma-separated, ampersand-delimited 10s history at 1Hz)
- `HF` = FlowHistory (response: `19911,0,0&...` -- comma-separated, ampersand-delimited)
- `HP` = PressureHistory (response: `50821,5,628,22&...`)
- `GA` = GetAllStatus
- `ST` = SetTemp (`R4 ST {reactor_index} {target_celsius}` -- note `R4` prefix)
- `GV` = GetFirmwareVersion (response: `v1.64 8-Jan-2010` style)
- `SV` = Solenoid valve control

**Note: `GF` and `GT` do not exist in the real device.** The ROADMAP listed them in error; the actual commands for temperatures/flows are `HT` and `HF` respectively.

**Phase 2 capability set (10 capabilities) -- `RegisterCapabilities` implementation:**

All send via `Transport.SendReceiveAsync(Encode(cmd), ct)` and parse ASCII response. `Encode(s)` = `Encoding.ASCII.GetBytes(s)`. `Decode(b)` = `Encoding.ASCII.GetString(b).TrimEnd('\\r', '\\n', ' ')`.

1. **"Start"** -- sends `PN \r\n` then `FW PN \r\n` (second unit). Returns `Successful()` if first response starts with `OK`; `Failed(response)` otherwise. `RegisterSimple("Start", "Power on pumps and heaters", ...)`.

2. **"Stop"** -- sends `PF\r\n` then `FW PF\r\n`. Returns `Successful()` if response starts with `OK`. `RegisterSimple("Stop", "Power off pumps and heaters", ...)`.

3. **"SetFlowRate"** -- takes `{ "pump": int, "rateMlMin": double }`. Pump 0=A, 1=B; pumps 2=C, 3=D use `FW FR {pump-2} {rate}`. Sends `FR {pump} {(int)(rateMlMin * 1000)} \r\n` (wire unit is �l/min). Returns `Successful()` if response starts with `OK`; `Failed(...)` otherwise. Clamp `rateMlMin` to [0.0, 9.99] (matching device limits from source `_availableLimits`). Validate `pump` in [0, 3] and return `Failed` if out of range (T-2-01).

4. **"SetTemperature"** -- takes `{ "reactor": int, "targetCelsius": int }`. Sends `R4 ST {reactor} {targetCelsius} \r\n`. Returns `Successful()` if response starts with `OK`.

5. **"SelectReagent"** -- takes `{ "channel": int }` (0=A,1=B,2=C,3=D). Maps: A -> `KP 1\r\n`, B -> `KP 3\r\n`, C -> `FW KP 1\r\n`, D -> `FW KP 3\r\n`. Returns `Successful()` if response starts with `OK`.

6. **"SelectSolvent"** -- takes `{ "channel": int }` (0=A,1=B,2=C,3=D). Maps: A -> `KP 0\r\n`, B -> `KP 2\r\n`, C -> `FW KP 0\r\n`, D -> `FW KP 2\r\n`. Returns `Successful()` if response starts with `OK`.

7. **"SelectLoopFill"** -- takes `{ "loop": int }` (0=A,1=B,2=C,3=D). Maps: A -> `KP 4\r\n`, B -> `KP 6\r\n`, C -> `FW KP 4\r\n`, D -> `FW KP 6\r\n`. Returns `Successful()` if response starts with `OK`.

8. **"SelectLoopInject"** -- takes `{ "loop": int }` (0=A,1=B,2=C,3=D). Maps: A -> `KP 5\r\n`, B -> `KP 7\r\n`, C -> `FW KP 5\r\n`, D -> `FW KP 7\r\n`. Returns `Successful()` if response starts with `OK`.

9. **"GetFirmwareVersion"** -- sends `GV\r\n`. Returns `Successful(JsonSerializer.SerializeToElement(decoded))`. Response should start with `v` for a valid R4 device.

10. **"GetTemperatures"** -- sends `HT\r\n`. Response format: `4913,U,-1000,U,-1000,U,201,U,-1000&...` (ampersand-separated history, comma-separated fields). Take last `&`-delimited entry. Field index 0 is a **timestamp** (not a temperature). Reactor temperatures are at field indices [2,4,6,8] for reactors A,B,C,D respectively. Divide by 10.0. Skip entries where the field is `-1000` (probe not fitted) or non-numeric (`"U"` = unknown) � use `double.TryParse` to handle both cases naturally. Return JSON: `{ "C": 20.1 }` for the example response above (only reactors with valid readings). Returns `Successful(jsonElement)`.

**StreamTrendsAsync override:**
Override to poll `HT` and `HF` every 2 seconds and emit TrendSamples:
- `vapourtec.temperature.A/B/C/D` -- parsed from `HT`, unit "C"
- `vapourtec.flow.pumpA`, `vapourtec.flow.pumpB` -- from `HF` last entry fields [1],[2] divided by 1000.0, unit "ml/min"

If transport is not connected or command returns empty, skip cycle and continue (do not throw).

**Deferred to Phase 3:** `GA` (GetAllStatus), `SV` (Solenoid), `PL` (SetPressureLimit) exist in the real device POP vocabulary but are not registered as capabilities in Phase 2. They appear in the T1 driver inventory under "Full POP vocabulary" but not "Phase 2 implemented".

**Fault detection in capabilities:**
Any capability response starting with `ERR` returns `CapabilityInvocationResult.Failed("Device error: {response}")` rather than `Successful`.

**Imports needed:** `System.Text`, `System.Text.Json`, `Helix.Orchestration.Devices.Abstractions`, `Helix.Orchestration.Devices.Model`, `Helix.Orchestration.Devices.Transports.Serial`, `Microsoft.Extensions.Logging`.

**Test:**
`dotnet build Helix.Orchestration.slnx` exits 0. Class compiles and is discoverable via `new VapourtecR4(transport, logger)`.

---

### T5 — Fix TrendAggregator Scalar Routing
**Requirement:** (ROADMAP deliverable — resolves TrendAggregator silent discard)
**Files:**
- `Helix.Orchestration.Agent/Trends/InMemoryTrendProvider.cs` — add `PushScalar<T>` method and backing store
- `Helix.Orchestration.Agent/Streaming/TrendAggregator.cs` — add `ScalarValue<T>` case routing to `PushScalar`

**Implementation:**

**InMemoryTrendProvider changes:**

Add a scalar backing store. The `InMemoryTrendProvider` currently uses `ConcurrentDictionary<string, InMemorySpectralTrendProvider>`. Add a parallel scalar store:

```csharp
private readonly ConcurrentDictionary<string, object> _scalarLatest = new(StringComparer.OrdinalIgnoreCase);
```

Add a `PushScalar<T>` method:
```csharp
public void PushScalar<T>(string trendName, T value) where T : struct, IComparable<T>
{
    _scalarLatest[trendName] = value;
}
```

Add a public accessor for testability:
```csharp
public bool TryGetScalar(string trendName, out object? value)
    => _scalarLatest.TryGetValue(trendName, out value);
```

Update `IsTrendAvailableAsync` to also check `_scalarLatest.ContainsKey(trendName)`.

Update `GetAllTrendNames()` to include `_scalarLatest.Keys`.

Do NOT remove the `NotSupportedException` from `GetScalarTrend<T>` or `GetTrend<T>` — those typed-reader paths are out of scope for this phase (Phase 4 scope per CONCERNS.md). Only the push/write path needs to work.

**TrendAggregator changes:**

In the `switch (value)` block, add cases before the `default` branch:

```csharp
case ScalarValue<double> scalar:
    trendProvider.PushScalar(trendName, scalar.Value);
    logger.LogDebug("Published scalar (double) for trend {TrendName}: {Value}", trendName, scalar.Value);
    break;

case ScalarValue<float> scalarF:
    trendProvider.PushScalar(trendName, scalarF.Value);
    logger.LogDebug("Published scalar (float) for trend {TrendName}: {Value}", trendName, scalarF.Value);
    break;

case ScalarValue<int> scalarI:
    trendProvider.PushScalar(trendName, scalarI.Value);
    logger.LogDebug("Published scalar (int) for trend {TrendName}: {Value}", trendName, scalarI.Value);
    break;
```

The `default` branch remains for truly unsupported types; its log level should be changed from `LogDebug` to `LogWarning` so silent discards are visible.

**Test:**
Unit test (see T7 for `TrendAggregatorScalarTests.cs`): `ProcessAsync` with a `ScalarValue<double>` no longer hits the default branch; `InMemoryTrendProvider.TryGetScalar("my.temp", out var v)` returns true and `v` equals 42.5.

---

### T6 — Wire VapourtecR4 into CapabilityRegistry via Bridge
**Requirement:** B7 (DI wiring), B2 (capabilities reachable from registry)
**Files:**
- `Helix.Orchestration.Agent/Adapters/Devices/HelixAdapterCapabilityBridge.cs` — new file; bridge between `IHelixAdapter` and Core's `ICapabilityInvoker`
- `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs` — register VapourtecR4 as Singleton and populate CapabilityRegistry

**Implementation:**

**HelixAdapterCapabilityBridge.cs:**

Create in namespace `Helix.Orchestration.Agent.Adapters.Devices`:

```csharp
internal sealed class HelixAdapterCapabilityBridge(
    IHelixAdapter adapter,
    string capabilityName) : Core.Capabilities.ICapabilityInvoker
{
    public async Task<Core.Capabilities.CapabilityInvocationResult> InvokeAsync(
        string capabilityName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        // Deserialise parameters: object? -> JsonElement
        JsonElement jsonParams = parameters is JsonElement je
            ? je
            : JsonSerializer.SerializeToElement(parameters ?? new { });

        var result = await adapter.InvokeCapabilityAsync(capabilityName, jsonParams, null, cancellationToken);

        return result.Success
            ? Core.Capabilities.CapabilityInvocationResult.Succeeded(result.ReturnValue)
            : Core.Capabilities.CapabilityInvocationResult.Failed(
                result.ErrorMessage ?? "Capability invocation failed.");
    }

    /// <summary>
    /// Returns true when the underlying adapter is healthy AND it exposes the requested capability.
    /// Availability is checked at adapter granularity (not per-command), because
    /// SerialDeviceTransport has a single transport health state shared by all capabilities.
    /// A capability name not present in adapter.GetCapabilities() will return false even if
    /// the adapter itself is healthy.
    /// </summary>
    public async Task<bool> IsAvailableAsync(string capabilityName)
        => adapter.GetCapabilities().Any(c => c.Name == capabilityName)
           && await adapter.HealthCheckAsync();
}
```

Note: use fully qualified `Core.Capabilities.CapabilityInvocationResult` to avoid ambiguity with `Devices.Abstractions.CapabilityInvocationResult`. Add using aliases at the top of the file if preferred:
```csharp
using CoreResult = Helix.Orchestration.Core.Capabilities.CapabilityInvocationResult;
```

**DeviceAdapterExtensions.cs augmentation:**

Add to `AddMigratedDeviceAdapters()` after the existing `CapabilityRegistry` singleton registration:

```csharp
// Register VapourtecR4 (B2, B7)
// SerialDeviceTransport options must be provided via IConfiguration or environment.
// For now register a named options factory. Adjust PortName from config in production wiring.
services.AddSingleton(new SerialTransportOptions
{
    PortName = "COM3",             // placeholder; override via appsettings or env var
    BaudRate = 19200,
    CommandTerminator = "\r\n"
});
services.AddSingleton<SerialDeviceTransport>();
services.AddSingleton<VapourtecR4>();

// Populate CapabilityRegistry with VapourtecR4 capabilities
services.AddSingleton<CapabilityRegistry>(sp =>
{
    var registry = new CapabilityRegistry();
    var adapter = sp.GetRequiredService<VapourtecR4>();
    var logger = sp.GetRequiredService<ILogger<CapabilityRegistry>>();

    foreach (var capability in adapter.GetCapabilities())
    {
        var bridge = new HelixAdapterCapabilityBridge(adapter, capability.Name);
        registry.Register(capability.Name, bridge);
        logger.LogInformation("Registered capability: {CapabilityName}", capability.Name);
    }

    logger.LogInformation("CapabilityRegistry initialized with {Count} VapourtecR4 capabilities.",
        adapter.GetCapabilities().Count);
    return registry;
});
```

Remove the old `CapabilityRegistry` singleton lambda (the Phase 1 placeholder that logged "0 adapters").

Add required usings: `Helix.Orchestration.Devices.Drivers`, `Helix.Orchestration.Devices.Transports.Serial`.

Note: there will be a duplicate `AddSingleton<CapabilityRegistry>` from the old placeholder — delete the old lambda entirely and replace with the new one above. There must be exactly one `AddSingleton<CapabilityRegistry>` call.

**Test:**
`dotnet build` exits 0. Agent startup log contains lines matching: `Registered capability: Start`, `Registered capability: SetFlowRate`, etc.

---

### T7 — VapourtecR4Tests.cs and TrendAggregator Scalar Tests
**Requirement:** B8
**Files:**
- `tests/Helix.Orchestration.Devices.Tests/Drivers/VapourtecR4Tests.cs` — new file; driver unit tests
- `tests/Helix.Orchestration.Agent.Tests/Streaming/TrendAggregatorScalarTests.cs` — new file; TrendAggregator scalar routing tests

**Important:** `TrendAggregator` and `InMemoryTrendProvider` live in `Helix.Orchestration.Agent`, not in `Helix.Orchestration.Devices`. The scalar tests must go in `Helix.Orchestration.Agent.Tests`, which already references `Helix.Orchestration.Agent` and already contains a `Trends/` subdirectory. Do NOT place `TrendAggregatorScalarTests.cs` in the Devices.Tests project — it does not reference the Agent assembly and will not compile there.

**Implementation:**

Namespaces:
- VapourtecR4 tests: `Helix.Orchestration.Devices.Tests.Drivers`
- TrendAggregator scalar tests: `Helix.Orchestration.Agent.Tests.Streaming`

Use `NSubstitute` for any non-transport mocks. Use the `FakeSerialPortConnection` / `FakeSerialPortConnectionFactory` pattern already established in `SerialTransportTests.cs` — replicate the inner classes locally (or move them to a shared `TestHelpers/` file if preferred, but keeping them local is acceptable for this phase).

**VapourtecR4Tests.cs — required test cases:**

Helper: `CreateDriver(params (string response)[] responses)` — builds a `SerialDeviceTransport` with a `FakeSerialPortConnectionFactory` that enqueues the given ASCII responses (each appended with `\r\n`), wraps it in a `VapourtecR4` with `NullLogger<VapourtecR4>.Instance`, calls `StartAsync()` on the driver (to connect the transport), and returns the driver.

```
[Fact] Start_SendsPnCommand_ReturnsSuccess
  Arrange: enqueue response "OK\r\n", then "OK\r\n" (FW PN response)
  Act: InvokeCapabilityAsync("Start", ...)
  Assert: result.Success == true
  Assert: first write bytes == ASCII("PN \r\n")

[Fact] Start_ErrResponse_ReturnsFailure
  Arrange: enqueue "ERR ER103\r\n"
  Act: InvokeCapabilityAsync("Start", ...)
  Assert: result.Success == false; ErrorMessage contains "ERR"

[Fact] Stop_SendsPfCommand_ReturnsSuccess
  Arrange: enqueue "OK\r\n", then "OK\r\n"
  Act: InvokeCapabilityAsync("Stop", ...)
  Assert: result.Success == true
  Assert: first write bytes == ASCII("PF\r\n")

[Fact] SetFlowRate_PumpA_SendsFrCommand_WithCorrectRate
  Arrange: enqueue "OK\r\n"
  Act: InvokeCapabilityAsync("SetFlowRate", JsonElement for {"pump":0,"rateMlMin":1.5}, ...)
  Assert: result.Success == true
  Assert: captured write bytes == ASCII("FR 0 1500 \r\n")  (rate * 1000 as integer)

[Fact] SetFlowRate_PumpC_SendsFwFrCommand
  Arrange: enqueue "OK\r\n"
  Act: InvokeCapabilityAsync("SetFlowRate", JsonElement for {"pump":2,"rateMlMin":0.5}, ...)
  Assert: captured write bytes start with ASCII("FW FR")

[Fact] SetTemperature_SendsR4StCommand_WithCorrectParams
  Arrange: enqueue "OK\r\n"
  Act: InvokeCapabilityAsync("SetTemperature", JsonElement for {"reactor":1,"targetCelsius":60}, ...)
  Assert: result.Success == true
  Assert: captured write bytes == ASCII("R4 ST 1 60 \r\n")

[Fact] SelectReagent_ChannelA_SendsKp1Command
  Arrange: enqueue "OK\r\n"
  Act: InvokeCapabilityAsync("SelectReagent", JsonElement for {"channel":0}, ...)
  Assert: result.Success == true
  Assert: captured write bytes == ASCII("KP 1\r\n")

[Fact] SelectReagent_ChannelC_SendsFwKp1Command
  Arrange: enqueue "OK\r\n"
  Act: InvokeCapabilityAsync("SelectReagent", JsonElement for {"channel":2}, ...)
  Assert: captured write bytes == ASCII("FW KP 1\r\n")

[Fact] SelectSolvent_ChannelA_SendsKp0Command
  Arrange: enqueue "OK\r\n"
  Act: InvokeCapabilityAsync("SelectSolvent", JsonElement for {"channel":0}, ...)
  Assert: captured write bytes == ASCII("KP 0\r\n")

[Fact] SelectLoopFill_LoopA_SendsKp4Command
  Arrange: enqueue "OK\r\n"
  Act: InvokeCapabilityAsync("SelectLoopFill", JsonElement for {"loop":0}, ...)
  Assert: result.Success == true
  Assert: captured write bytes == ASCII("KP 4\r\n")

[Fact] SelectLoopFill_LoopC_SendsFwKp4Command
  Arrange: enqueue "OK\r\n"
  Act: InvokeCapabilityAsync("SelectLoopFill", JsonElement for {"loop":2}, ...)
  Assert: captured write bytes == ASCII("FW KP 4\r\n")

[Fact] SelectLoopInject_LoopA_SendsKp5Command
  Arrange: enqueue "OK\r\n"
  Act: InvokeCapabilityAsync("SelectLoopInject", JsonElement for {"loop":0}, ...)
  Assert: result.Success == true
  Assert: captured write bytes == ASCII("KP 5\r\n")

[Fact] GetFirmwareVersion_SendsGvCommand_ReturnsVersionString
  Arrange: enqueue "v1.64 8-Jan-2010\r\n"
  Act: InvokeCapabilityAsync("GetFirmwareVersion", ...)
  Assert: result.Success == true; ReturnValue contains "v1.64"
  Assert: captured write bytes == ASCII("GV\r\n")

[Fact] GetTemperatures_ParsesHtResponse_ReturnsNonNullReactors
  Arrange: enqueue "4913,U,-1000,U,-1000,U,201,U,-1000\r\n"  (no & -- single entry)
  Act: InvokeCapabilityAsync("GetTemperatures", ...)
  Assert: result.Success == true
  Assert: returned JSON contains ONLY "C": 20.1 (field index 6 = "201", divided by 10.0)
  Assert: "A", "B", "D" are absent -- field index 0 ("4913") is the TIMESTAMP; indices 2 and 4 are "-1000" (probe not fitted); index 8 is "-1000"
  Note: "U" fields at indices 1,3,5,7 are non-numeric; double.TryParse returns false so they are skipped

[Fact] InvokeCapabilityAsync_ErrResponse_ReturnsFailure
  Arrange: enqueue "ERR INVALID_COMMAND\r\n"
  Act: InvokeCapabilityAsync("Start", ...)
  Assert: result.Success == false; ErrorMessage contains "ERR"

[Fact] StreamTrendsAsync_EmitsTemperatureTrendSamples
  Arrange: FakeSerialPortConnection enqueues two HT responses: "4913,U,-1000,U,-1000,U,201,U,-1000\r\n"
  Act: collect first 2 TrendSamples via CancellationTokenSource(TimeSpan.FromSeconds(10))
  Assert: samples contain TrendName "vapourtec.temperature.A" with Value ~49.13
```
```

**TrendAggregatorScalarTests.cs — required test cases:**

File location: `tests/Helix.Orchestration.Agent.Tests/Streaming/TrendAggregatorScalarTests.cs`

The `Helix.Orchestration.Agent.Tests` project already references `Helix.Orchestration.Agent` and already has a `Trends/` subdirectory (containing `InMemoryTrendProvidersTests.cs`). No `.csproj` edits are needed.

```
[Fact] ProcessAsync_ScalarDouble_PushesToInMemoryProvider
  Arrange: InMemoryTrendProvider, TrendAggregator
  Act: ProcessAsync("my.temp", new ScalarValue<double>(42.5))
  Assert: trendProvider.TryGetScalar("my.temp", out var v) returns true; v equals 42.5

[Fact] ProcessAsync_ScalarFloat_PushesToInMemoryProvider
  (same pattern with ScalarValue<float>)

[Fact] ProcessAsync_UnsupportedType_DoesNotThrow
  Act: ProcessAsync("x", a mock TrendValue subtype not in the switch)
  Assert: no exception thrown (the default branch must not throw)
```

Use `TryGetScalar(string, out object?)` (added in T5) to access the scalar store — no `InternalsVisibleTo` attribute required.

**Test:**
- `dotnet test tests/Helix.Orchestration.Devices.Tests/` — all new VapourtecR4Tests pass, no `[Fact(Skip)]`
- `dotnet test tests/Helix.Orchestration.Agent.Tests/` — all new TrendAggregatorScalarTests pass, no `[Fact(Skip)]`

---

## Execution Order

```
Wave 1 (parallel — no dependencies between them):
  T1   Driver audit document
  T2   Verify ISerialTransport.Open / remove dead DI registrations
  T3   Fix DI lifetime in DevicesServiceCollectionExtensions

Wave 2 (depends on T2, T3 completing first):
  T4   Implement VapourtecR4.cs  (uses SerialDeviceTransport — confirmed by T2/T3)
  T5   Fix TrendAggregator scalar routing

Wave 3 (depends on T4):
  T6   Wire VapourtecR4 into CapabilityRegistry (bridge + DI registration)

Wave 4 (depends on T4, T5, T6):
  T7   VapourtecR4Tests.cs + TrendAggregatorScalarTests.cs
```

---

## Success Criteria

- [ ] `dotnet build Helix.Orchestration.slnx -c Debug` exits 0 with all projects included
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/` passes all tests; zero skips for new VapourtecR4Tests
- [ ] `dotnet test tests/Helix.Orchestration.Agent.Tests/` passes all tests; zero skips for new TrendAggregatorScalarTests
- [ ] `VapourtecR4Tests.GetFirmwareVersion_SendsGvCommand_ReturnsVersionString` passes (ROADMAP smoke test criterion)
- [ ] `Helix.Orchestration/docs/driver-inventory.md` exists and lists all 5 driver categories with protocol, transport, command vocabulary (split into full POP and Phase 2 implemented columns), and phase assignment
- [ ] Agent startup log contains `Registered capability: GetFirmwareVersion` (and at least 9 other VapourtecR4 capability names; 10 total)
- [ ] `grep -r "AddScoped<IDeviceTransport" Helix.Orchestration.Devices/` returns 0 matches (lifetime fixed)
- [ ] `grep "AddTransient<ISerialTransport" Helix.Orchestration.Agent/` returns 0 matches (dead registration removed)
- [ ] TrendAggregator `ProcessAsync` with `ScalarValue<double>` hits `PushScalar`, not the `default` branch
- [ ] `VapourtecR4.StreamTrendsAsync` emits `TrendSample` entries for `vapourtec.temperature.*` and `vapourtec.flow.pump*` (A/B from HF, C/D from FW HF)

---

## Risks

- **GA response format is undocumented.** The five-field parse (`running fault message reagentValve solventValve`) is inferred from the `VapourtecStatus` record fields and prior handover docs. If the real device uses a different format, the parse logic in T4 will need revision. Mitigation: the test for malformed response (T7) catches parse errors; the `Failed` result path is tested explicitly.

- **SerialTransportOptions PortName placeholder.** T6 registers `new SerialTransportOptions { PortName = "COM3" }` as a placeholder. In a real deployment this must come from `IConfiguration`. This is acceptable for Phase 2 (integration test, not production). Phase 4 or Phase 10 should move this to a configuration binding.

- **Two `CapabilityInvocationResult` types.** `Helix.Orchestration.Core.Capabilities.CapabilityInvocationResult` and `Helix.Orchestration.Devices.Abstractions.CapabilityInvocationResult` coexist. The bridge in T6 must use fully qualified names or aliases to avoid ambiguity. Any file that imports both namespaces will fail to compile without resolution.

- **`InMemoryTrendProvider` scalar reader still throws.** After T5, `PushScalar` works but `GetScalarTrend<T>` still throws `NotSupportedException`. This is intentional (Phase 4 scope). Any code that calls `GetScalarTrend` after T5 will still fault; this is a known gap documented in CONCERNS.md.

---

## Threat Model

### Trust Boundaries

| Boundary | Description |
|----------|-------------|
| Agent process → serial port | Commands sent to VapourtecR4 are formatted strings with integer/float parameters. A malformed or injected command string could cause unexpected device behaviour. |
| Capability name → CapabilityRegistry lookup | Capability names are strings from workflow steps. An unregistered name returns a Failed result; no crash. |
| JSON parameters → VapourtecR4 handler | `JsonElement` parameters are parsed in capability handlers. Malformed JSON or missing fields will throw `KeyNotFoundException` or `InvalidOperationException`. |

### STRIDE Threat Register

| Threat ID | Category | Component | Disposition | Mitigation Plan |
|-----------|----------|-----------|-------------|-----------------|
| T-2-01 | Tampering | VapourtecR4 command encoding | mitigate | Validate `pump` and `reactor` parameters are in range [0, 3] before encoding; return `Failed` if out of range — prevents sending malformed POP commands |
| T-2-02 | Tampering | SetTemperature target | mitigate | Validate `targetCelsius` in range [0, 150] before sending `ST` command; return `Failed` with descriptive error if out of range |
| T-2-03 | Denial of Service | SerialDeviceTransport blocking on read | accept | `SendReceiveAsync` reads until `CommandTerminator`; if device never sends CRLF, the read loop will block. Transport-level timeout via `SerialTransportOptions.ReadTimeoutMs` (default 500ms) bounds this. Acceptable for Phase 2. |
| T-2-04 | Information Disclosure | CapabilityRegistry capability names | accept | Capability names are internal API identifiers, not secrets. No sensitive data in names. |
| T-2-05 | Elevation of Privilege | ISerialTransport registered in DI | accept | Transport is Singleton; only the Agent process (trusted) accesses it. No cross-process boundary. |
| T-2-SC | Tampering | No new npm/pip/cargo installs in this phase | accept | Phase 2 adds no new package dependencies. No package legitimacy gate required. |

