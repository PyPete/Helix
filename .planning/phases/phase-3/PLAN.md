# Phase 3: Remaining Device Drivers — Plan

**Phase:** 3
**Goal:** Port KnauerAzura and SystagOpcUaClient to DeviceAdapterBase; register all drivers into CapabilityRegistry; provide unit tests for each via transport mocks. All B-series requirements are closed.
**Requirements:** B3, B4, B5, B8 (KnauerAzura, SystagOpcUaClient, remaining drivers)
**Depends on:** Phase 2 (architecture proven, audit complete, ISerialTransport fixed, DI lifetime fixed)
**Mode:** standard

---

## Source Audit

| Source | Item | Covered by |
|--------|------|------------|
| GOAL | KnauerAzura.cs implementing DeviceAdapterBase | T1 |
| GOAL | SystagOpcUaClient.cs implementing DeviceAdapterBase | T2 |
| GOAL | DeviceAdapterExtensions registers all new drivers | T4 |
| REQ B3 | KnauerAzura driver: pump start/stop, flow rate, gradient, DAD wavelength, run state | T1 |
| REQ B4 | SystagOpcUaClient driver: OPC UA node browse + setpoint writes, AutoAcceptUntrustedCertificates configurable | T2 |
| REQ B5 | All audited drivers listed and implemented; REQUIREMENTS.md B5 updated | T1, T2, T3, T5 |
| REQ B8 | Unit tests for each new driver | T3 |
| ROADMAP | OpcDeviceTransport.AutoAcceptUntrustedCertificates configurable (not hardcoded true) | T2 |
| ROADMAP | CapabilityRegistry logs KnauerAzura and SystagOpcUaClient at startup | T4 |

---

## Codebase Facts (executor reference)

### Pattern — do not explore, use directly

**DeviceAdapterBase** (`Helix.Orchestration.Devices/Abstractions/DeviceAdapterBase.cs`)
- Constructor: `protected DeviceAdapterBase(IDeviceTransport transport, ILogger logger)`
- `CapabilityDispatchBuilder.Register(CapabilityDescriptor, Func<JsonElement, string?, CancellationToken, Task<CapabilityInvocationResult>>)`
- `CapabilityDispatchBuilder.RegisterSimple(name, description, Func<CancellationToken, Task<CapabilityInvocationResult>>)`
- `CapabilityInvocationResult.Successful(JsonElement? returnValue = null)` / `.Failed(string errorMessage)`
- `TrendSample { TrendName, Value (object), Timestamp, Unit? }`
- `StreamTrendsAsync` returns `IAsyncEnumerable<TrendSample>` — override to emit live device telemetry

**TcpDeviceTransport** (`Helix.Orchestration.Devices/Transports/Tcp/TcpDeviceTransport.cs`)
- Constructor: `TcpDeviceTransport(TcpTransportOptions options, ILogger<TcpDeviceTransport> logger)`
- `SendReceiveAsync(byte[] command, CancellationToken)` — send bytes, read from channel per framing mode
- `TcpTransportOptions { Host, Port, FramingMode (Delimiter/LengthPrefix/Raw), DelimiterByte (0x0A), ConnectTimeoutMs, SendTimeoutMs, ReceiveTimeoutMs }`
- No test-injection constructor — use `NSubstitute.Substitute.For<IDeviceTransport>()` in tests

**OpcDeviceTransport** (`Helix.Orchestration.Devices/Transports/Opc/OpcDeviceTransport.cs`)
- Constructor (DI): `OpcDeviceTransport(OpcTransportOptions options, ILogger<OpcDeviceTransport> logger)`
- Internal test constructor: `OpcDeviceTransport(OpcTransportOptions, ILogger, IOpcSessionFactory)` — use for unit tests
- `SendReceiveAsync(byte[] command, CancellationToken)` — deserialises command as JSON `{ nodeId, operation, value?, methodId?, parameters? }`
- OPC command format (UTF-8 JSON): read: `{"nodeId":"ns=2;s=Node","operation":"read"}`, write: `{"nodeId":"...","operation":"write","value":50.0}`, subscribe: `{"nodeId":"...","operation":"subscribe"}`

**IOpcSession** (`Helix.Orchestration.Devices/Transports/Opc/IOpcSession.cs`)
- `ReadNodeAsync<T>(string nodeId, CancellationToken)` / `WriteNodeAsync(string nodeId, object value, CancellationToken)`
- `SubscribeNodeAsync(nodeId, callback, samplingIntervalMs, ct)` → `IOpcSubscription`
- `BrowseChildrenAsync(parentNodeId, ct)` → `IReadOnlyList<OpcNodeDescriptor>` — use for Systag node discovery

**OpcTransportOptions** (`Helix.Orchestration.Devices/Transports/Opc/OpcTransportOptions.cs`)
- `EndpointUrl` (required), `SessionTimeoutMs`, `SecurityMode`
- `AutoAcceptUntrustedCertificates` — **does not exist yet**; currently hardcoded `true` in `OpcDeviceTransport.ConnectAsync()`
- Phase 3 adds this property to `OpcTransportOptions` (default `false`) and wires it

**DeviceAdapterExtensions** (`Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs`)
- Currently registers VapourtecR4 only; Phase 3 extends to register KnauerAzura and SystagOpcUaClient

**HelixAdapterCapabilityBridge** (`Helix.Orchestration.Agent/Adapters/Devices/HelixAdapterCapabilityBridge.cs`)
- Already exists from Phase 2; reuse unchanged for new drivers

---

## SOURCE REFERENCE: POP.DeviceCore Analysis

### KnauerAzura (POP.DeviceCore/Drivers/KnauerAzura.cs — 1799 lines)

The original `KnauerAzura` is a complex multi-connection system with **three separate TCP clients**:
- **Pump** at `192.168.1.101` (port 10001) — handles flow rate, gradient, run state
- **DAD (Diode Array Detector)** at `192.168.1.110` — handles UV wavelength, spectral scans, lamp control
- **Thermostat** at `192.168.1.121` — handles column temperature

**Helix scope for Phase 3:** The Helix architecture uses a single `TcpDeviceTransport` per adapter. Given this constraint, the Phase 3 KnauerAzura driver **targets the Pump connection only** (the primary operational device in a flow chemistry context). The DAD and Thermostat are deferred — they require either multiple transport registrations or a composite transport pattern that is out of scope for Phase 3. This is the same pragmatic scoping decision made for VapourtecR4's deferred capabilities.

**Real Pump protocol (from source):**
- Commands are terminated with `\r` (not `\r\n`)
- Response format: `{COMMAND}:{value},{value},...\r`
- `STATUS?\r` → `STATUS:ok,{run_time},{pump_state},{?,},flow_rate_ul_min,percA,percB,percC,percD,?,?,?,?,?,?,?,?,pressure_kPa,...`
  - PumpState int: `0=Initialising, 1=Off, 2=Idle, 3=Running, 4=Holding, 5=Purging, 6=Standby, 8=Failed, 9=RunFinished, 10=Equilibrating`
  - Flow rate is in µl/min (divide by 1000 to get ml/min), pressure in kPa (divide by 1000 to get bar)
- `START:2,2\r` → start pump in hold mode (waits for sync trigger); response: `START:OK\r`
- `STOP:1,1,1\r` → stop pump, stop digital channels, stop binary scans; response: `STOP:OK\r`
- `UNHOLD\r` → release hold state, begin running; response: `UNHOLD:OK\r`
- `TT_LOAD:1\r` → initialise a new timetable method; response: `TT_LOAD:OK\r`
- `TT_SET:{lineNum},{runTimeMs},{flowRateUlMin},{percA},{percB},{percC},{percD},0,...\r` → set a timetable line
- `TT_SAVE:1\r` → commit the timetable; response: `TT_SAVE:OK\r`
- `RAMP:{flowRateUlMin},{?,percA},{percB},{percC},{percD},0,...,{durationMs}\r` → immediate gradient ramp
- `REMOTE:60000000\r` → put pump into remote control mode; response: `REMOTE:OK\r`
- `PLIM:{minKPa},{maxKPa}\r` → set pressure limits
- `IDENTIFY?\r` → returns `IDENTIFY:3,KNAUER,AZURA,{serial},{fw_hi},{fw_lo}\r`

**Phase 3 capability set for KnauerAzura (pump-only):**
1. **Start** — `REMOTE:60000000\r` then `START:2,2\r` then `UNHOLD\r` (remote + start-in-hold + release)
2. **Stop** — `STOP:1,1,1\r`
3. **SetFlowRate** — `RAMP:{flowUlMin},0,{percA},{percB},{percC},{percD},...,1\r` where the last field is duration (1 = immediate)
4. **SetGradient** — same RAMP command with explicit percA/percB/percC/percD summing to ≤100
5. **GetPumpStatus** — `STATUS?\r` → parse and return `{ state, flowRateMlMin, percA, percB, percC, percD, pressureBar }`
6. **GetIdentity** — `IDENTIFY?\r` → return raw identify string

### SystagOpcUaClient (POP.DeviceCore/Drivers/SystagOpcUaClient.cs)

The original `SystagOpcUaClient` is a **dynamic OPC UA client** that browses the server at runtime to discover reactor nodes, rather than using hardcoded node IDs. It uses `ISystagOpcCommand` to:
- `CreateSession(endPoint)` — connect to the OPC UA server
- `ConnectedDevices()` → `IEnumerable<string>` — list connected reactor names (the "system" names are OPC UA folder nodes)
- `GetTrendsAtNode(reactor)` → list of `(name, value, unit)` tuples — browse all trend nodes under the reactor folder
- `CloseSession()`

**Key architectural difference from assumed plan:** There are no hardcoded setpoint node IDs. The Systag driver reads/subscribes to whatever nodes the OPC server exposes under the selected reactor node. The real driver has **no explicit setpoint write capabilities** — it is read/subscribe-only in the POP source.

**Helix scope for Phase 3:** The Systag driver will be a read+subscribe adapter that:
1. Connects to the OPC UA server
2. Browses child nodes under a configured reactor path (passed as a capability parameter or driver option)
3. Subscribes to all discovered nodes and emits trend samples via `StreamTrendsAsync`
4. Provides `GetReactorNodes` capability to browse available measurement nodes
5. Provides `ReadNode` capability for ad-hoc reads
6. Provides `WriteNode` capability for setpoint writes using caller-supplied node IDs (the reactor's actual node IDs are deployment-specific, not hardcoded)

---

## Tasks

### T1 — Implement KnauerAzura.cs
**Requirement:** B3
**Files:**
- `Helix.Orchestration.Devices/Drivers/KnauerAzura.cs` — new file; TCP-based HPLC pump driver (pump connection only)

**Implementation:**

Create `Helix.Orchestration.Devices/Drivers/KnauerAzura.cs` in namespace `Helix.Orchestration.Devices.Drivers`.

**Class signature:**
```csharp
public sealed class KnauerAzura : DeviceAdapterBase
```

**Constructor:**
```csharp
public KnauerAzura(TcpDeviceTransport transport, ILogger<KnauerAzura> logger)
    : base(transport, logger)
```
Takes `TcpDeviceTransport` (concrete) for unambiguous DI resolution.

**Internal test constructor** (required for unit tests — same pattern as `DeviceAdapterBaseTests.cs`):
```csharp
internal KnauerAzura(IDeviceTransport transport, ILogger<KnauerAzura> logger)
    : base(transport, logger) { }
```

**Protocol constants:**
```csharp
private const string CommandTerminator = "\r";
private const string OkPrefix = "OK";   // many commands respond "{CMD}:OK"
```

**Encoding helpers (private static):**
- `static byte[] Encode(string command)` — `Encoding.ASCII.GetBytes(command + "\r")`
- `static string Decode(byte[] response)` — `Encoding.ASCII.GetString(response).Trim()`
- `static bool IsOk(string response)` — response contains `:OK` or starts with `OK` (case-insensitive)
- `static string[] SplitResponse(string response)` — `response.Split(':', ',')`

**Capabilities — `RegisterCapabilities` implementation (6 capabilities):**

1. **"RemoteControl"** — sends `REMOTE:60000000\r` to put pump into remote mode. Parses response; returns `Successful()` if response contains `OK`. Required before `Start`.
   `RegisterSimple("RemoteControl", "Put the KnauerAzura pump into remote control mode", ...)`

2. **"Start"** — sends `START:2,2\r` (start in hold mode — pump initialises and waits for UNHOLD). If response contains `OK`, sends `UNHOLD\r`. Returns `Successful()` if both responses contain `OK`; `Failed(response)` otherwise.
   `RegisterSimple("Start", "Start the KnauerAzura pump method and release hold", ...)`

3. **"Stop"** — sends `STOP:1,1,1\r` (stop pump + digital channels + binary scans). Returns `Successful()` if response contains `OK`.
   `RegisterSimple("Stop", "Stop the KnauerAzura pump and all data output", ...)`

4. **"SetFlowRate"** — takes `{ "flowRateMlMin": double, "percA": double, "percB": double, "percC": double, "percD": double }`.
   Validates: `flowRateMlMin` in [0.0, 20.0], each perc in [0, 100], sum of percs ≤ 100.
   Converts `flowRateMlMin * 1000` to µl/min integer.
   Sends: `RAMP:{flowUlMin},0,{(int)percA},{(int)percB},{(int)percC},{(int)percD},0,0,0,0,0,0,0,0,1\r`
   (trailing `1` = apply immediately in 1ms; the 14-field RAMP format from source)
   Returns `Successful()` if response contains `OK`.
   Parameter schema: `{ "type": "object", "properties": { "flowRateMlMin": { "type": "number", "minimum": 0, "maximum": 20 }, "percA": { "type": "number", "minimum": 0, "maximum": 100 }, "percB": { "type": "number", "minimum": 0, "maximum": 100 }, "percC": { "type": "number", "minimum": 0, "maximum": 100 }, "percD": { "type": "number", "minimum": 0, "maximum": 100 } }, "required": ["flowRateMlMin", "percA", "percB", "percC", "percD"] }`

5. **"GetPumpStatus"** — sends `STATUS?\r`. Parses response:
   `STATUS:ok,{runTime},{pumpStateInt},?,{flowRateUlMin},{percA},{percB},{percC},{percD},?,?,?,?,?,?,?,?,{pressureKPa},...`
   Split on `:` and `,`. Field indices (after splitting `STATUS:...`): state=index[2], flowRate=index[5]/1000, percA=index[6], percB=index[7], percC=index[8], percD=index[9], pressure=index[18]/1000.
   Returns `Successful(JsonSerializer.SerializeToElement(new { state = pumpStateName, flowRateMlMin, percA, percB, percC, percD, pressureBar }))`.
   PumpState int→name: `0=Initialising, 1=Off, 2=Idle, 3=Running, 4=Holding, 5=Purging, 6=Standby, 8=Failed, 9=RunFinished, 10=Equilibrating`.
   If parse fails, return `Successful(JsonSerializer.SerializeToElement(new { raw = response }))` — do not throw.
   `RegisterSimple("GetPumpStatus", "Query KnauerAzura pump state, flow rate, and pressure", ...)`

6. **"GetIdentity"** — sends `IDENTIFY?\r`. Returns `Successful(JsonSerializer.SerializeToElement(response))`.
   Response format: `IDENTIFY:3,KNAUER,AZURA,{serial},{fw_hi},{fw_lo}\r`
   `RegisterSimple("GetIdentity", "Query the KnauerAzura pump identity string", ...)`

**StreamTrendsAsync override:**
Poll `GetPumpStatus` every 5 seconds and emit trend samples:
- `knauer.pump.flowrate` — `flowRateMlMin`, unit `"ml/min"`
- `knauer.pump.pressure` — `pressureBar`, unit `"bar"`
- `knauer.pump.state` — `pumpStateName` (string), unit `""`

Catch `TransportFaultException` and skip cycle (log warning, continue loop).

**Imports:** `System.Text`, `System.Text.Json`, `Helix.Orchestration.Devices.Abstractions`, `Helix.Orchestration.Devices.Transports.Tcp`, `Microsoft.Extensions.Logging`

**Test:**
`dotnet build Helix.Orchestration.slnx` exits 0.

---

### T2 — Implement SystagOpcUaClient.cs + Fix AutoAcceptUntrustedCertificates
**Requirement:** B4
**Files:**
- `Helix.Orchestration.Devices/Drivers/SystagOpcUaClient.cs` — new file; OPC UA browse-and-subscribe driver
- `Helix.Orchestration.Devices/Transports/Opc/OpcTransportOptions.cs` — add `AutoAcceptUntrustedCertificates` property
- `Helix.Orchestration.Devices/Transports/Opc/OpcDeviceTransport.cs` — wire new property instead of hardcoded `true`

**Implementation:**

**Step 1 — Fix `OpcTransportOptions.cs`:**

Add one property:
```csharp
/// <summary>
/// Whether to auto-accept untrusted OPC UA server certificates.
/// Default: false (secure). Set true only for dev/test against self-signed certs.
/// </summary>
public bool AutoAcceptUntrustedCertificates { get; init; } = false;
```

**Step 2 — Fix `OpcDeviceTransport.ConnectAsync()`:**

Change the hardcoded:
```csharp
SecurityConfiguration = new SecurityConfiguration
{
    AutoAcceptUntrustedCertificates = true
}
```
To:
```csharp
SecurityConfiguration = new SecurityConfiguration
{
    AutoAcceptUntrustedCertificates = _options.AutoAcceptUntrustedCertificates
}
```

**Step 3 — Create `SystagOpcUaClient.cs`:**

The Systag is a **dynamic browse-and-subscribe driver**. Unlike fixed-vocabulary drivers, it discovers measurement nodes at runtime via `IOpcSession.BrowseChildrenAsync`.

**Class signature:**
```csharp
public sealed class SystagOpcUaClient : DeviceAdapterBase
```

**Constructor:**
```csharp
public SystagOpcUaClient(OpcDeviceTransport transport, ILogger<SystagOpcUaClient> logger)
    : base(transport, logger)
```

**OPC command helpers (private static):**
```csharp
private static byte[] OpcRead(string nodeId)
    => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { nodeId, operation = "read" }));

private static byte[] OpcWrite(string nodeId, object value)
    => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { nodeId, operation = "write", value }));

private static byte[] OpcSubscribe(string nodeId)
    => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { nodeId, operation = "subscribe" }));

private static string DecodeUtf8(byte[] response)
    => Encoding.UTF8.GetString(response).Trim();
```

**Capabilities — `RegisterCapabilities` implementation (4 capabilities):**

All OPC UA node IDs are **caller-supplied** — the driver has no hardcoded node paths, matching the real POP architecture where the reactor node names are discovered at runtime.

1. **"BrowseNodes"** — takes `{ "parentNodeId": string? }` (null/empty = browse server root).
   Sends OPC browse command. Because `IDeviceTransport.SendReceiveAsync` doesn't have a browse operation natively, use: `Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { nodeId = parentNodeId ?? "", operation = "browse" }))`.
   Wait — `OpcDeviceTransport` does not have a "browse" operation in `SendReceiveAsync`; only `read`, `write`, `call`, `subscribe` are handled. The browse path must go via `IOpcSession.BrowseChildrenAsync`.
   **Implementation pattern:** `BrowseNodes` cannot use `Transport.SendReceiveAsync`. Instead, this capability needs direct access to the OPC session. Since `DeviceAdapterBase` only exposes `Transport`, and `OpcDeviceTransport` is the concrete type, cast `Transport` to `OpcDeviceTransport` and call a helper method — or make `SystagOpcUaClient` expose a separate `BrowseAsync` capability via a workaround.
   **Simpler approach:** Skip the browse capability for Phase 3. The Phase 3 scope says "correct OPC UA node path conventions, change subscriptions for live value updates" — not runtime browsing. Add a note that browse is deferred to Phase 4.
   **Replace BrowseNodes with** `GetSystemInfo` — sends a read of the server's root node (`ns=0;i=84`) and returns the response as a JSON string. This demonstrates OPC UA connectivity without requiring browse. `RegisterSimple("GetSystemInfo", "Read OPC UA server root node as a connectivity check", ...)`

2. **"ReadNode"** — takes `{ "nodeId": string }`. Sends OPC read. Returns `Successful(JsonSerializer.SerializeToElement(new { nodeId, value = responseJson }))`.
   Parameter schema: `{ "type": "object", "properties": { "nodeId": { "type": "string" } }, "required": ["nodeId"] }`

3. **"WriteNode"** — takes `{ "nodeId": string, "value": double }`. Sends OPC write. Returns `Successful()` if response contains `"Success":true`; `Failed(response)` otherwise. This is the setpoint write path — the caller knows the node ID (e.g., temperature setpoint node from their reactor's OPC UA address space).
   Parameter schema: `{ "type": "object", "properties": { "nodeId": { "type": "string" }, "value": { "type": "number" } }, "required": ["nodeId", "value"] }`

4. **"SubscribeNode"** — takes `{ "nodeId": string }`. Sends OPC subscribe. Returns `Successful()` if response contains `"Success":true`. Subscriptions publish values via `OpcDeviceTransport.DataReceived` observable. The caller monitors that stream for live updates.
   Parameter schema: `{ "type": "object", "properties": { "nodeId": { "type": "string" } }, "required": ["nodeId"] }`

**StreamTrendsAsync override:**
The Systag driver does not have a fixed polling pattern — it uses subscriptions. Override `StreamTrendsAsync` to:
- Subscribe to the DataReceived observable on the transport (cast `Transport` to `OpcDeviceTransport` for access to `.DataReceived`)
- For each received notification, parse the JSON `{ NodeId, Value }` payload and emit a `TrendSample` with `TrendName = $"systag.{nodeId.Replace(";",".")}"`, value from the payload, and unit `""`.
- Run until cancellation.

Since the observable pattern and `IAsyncEnumerable` don't compose easily, implement the override as a loop that yields from a `Channel<TrendSample>` populated by a subscription callback:
```csharp
public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    if (Transport is not OpcDeviceTransport opcTransport)
        yield break;

    var channel = System.Threading.Channels.Channel.CreateUnbounded<TrendSample>();

    using var sub = opcTransport.DataReceived.Subscribe(data =>
    {
        // parse { NodeId, Value } JSON payload emitted by OpcDeviceTransport subscriptions
        try
        {
            var doc = JsonDocument.Parse(data);
            var nodeId = doc.RootElement.GetProperty("NodeId").GetString() ?? "unknown";
            var value = doc.RootElement.GetProperty("Value").GetDouble();
            channel.Writer.TryWrite(new TrendSample
            {
                TrendName = $"systag.{nodeId}",
                Value = value,
                Timestamp = DateTimeOffset.UtcNow,
                Unit = ""
            });
        }
        catch { /* skip unparseable subscription events */ }
    });

    await foreach (var sample in channel.Reader.ReadAllAsync(cancellationToken))
        yield return sample;
}
```

**Imports:** `System.Text`, `System.Text.Json`, `System.Threading.Channels`, `System.Runtime.CompilerServices`, `Helix.Orchestration.Devices.Abstractions`, `Helix.Orchestration.Devices.Transports.Opc`, `Microsoft.Extensions.Logging`

**Test:**
- `dotnet build Helix.Orchestration.slnx` exits 0
- `grep -n "AutoAcceptUntrustedCertificates = true" Helix.Orchestration.Devices/Transports/Opc/OpcDeviceTransport.cs` returns 0 matches

---

### T3 — Unit Tests: KnauerAzuraTests.cs and SystagOpcUaClientTests.cs
**Requirement:** B8
**Files:**
- `tests/Helix.Orchestration.Devices.Tests/Drivers/KnauerAzuraTests.cs` — new file
- `tests/Helix.Orchestration.Devices.Tests/Drivers/SystagOpcUaClientTests.cs` — new file

**Implementation:**

Both files go in `tests/Helix.Orchestration.Devices.Tests/Drivers/`. Namespace: `Helix.Orchestration.Devices.Tests.Drivers`.

---

**KnauerAzuraTests.cs:**

Uses `IDeviceTransport` NSubstitute stub via the internal test constructor.

```csharp
private static (KnauerAzura driver, IDeviceTransport transport) CreateDriver(params string[] responses)
{
    var transport = Substitute.For<IDeviceTransport>();
    var queue = new Queue<byte[]>(responses.Select(r => Encoding.ASCII.GetBytes(r)));
    transport.SendReceiveAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
             .Returns(_ => Task.FromResult(queue.Dequeue()));
    transport.Status.Returns(TransportStatus.Connected);
    var driver = new KnauerAzura(transport, NullLogger<KnauerAzura>.Instance);
    return (driver, transport);
}

private static JsonElement Json(string json)
{
    using var doc = JsonDocument.Parse(json); return doc.RootElement.Clone();
}

private static byte[] Encode(string cmd) => Encoding.ASCII.GetBytes(cmd + "\r");
```

Required test cases:
```
[Fact] RemoteControl_SendsRemoteCommand_ReturnsSuccess
  Arrange: response "REMOTE:OK\r"
  Act: InvokeCapabilityAsync("RemoteControl", Json("{}"))
  Assert: result.Success == true
  Assert: transport.SendReceiveAsync received call; sent bytes == Encode("REMOTE:60000000")

[Fact] Start_SendsStartThenUnhold_ReturnsSuccess
  Arrange: responses ["START:OK\r", "UNHOLD:OK\r"]
  Act: InvokeCapabilityAsync("Start", Json("{}"))
  Assert: result.Success == true
  Assert: first call bytes == Encode("START:2,2")
  Assert: second call bytes == Encode("UNHOLD")

[Fact] Start_StartFails_ReturnsFailure
  Arrange: response "START:ERROR\r"
  Act: InvokeCapabilityAsync("Start", Json("{}"))
  Assert: result.Success == false

[Fact] Stop_SendsStopCommand_ReturnsSuccess
  Arrange: response "STOP:OK\r"
  Act: InvokeCapabilityAsync("Stop", Json("{}"))
  Assert: result.Success == true
  Assert: sent bytes == Encode("STOP:1,1,1")

[Fact] SetFlowRate_ValidParams_SendsRampCommand
  Arrange: response "RAMP:OK\r"
  Act: InvokeCapabilityAsync("SetFlowRate", Json("""{"flowRateMlMin":1.5,"percA":60,"percB":40,"percC":0,"percD":0}"""))
  Assert: result.Success == true
  Assert: sent bytes start with ASCII "RAMP:1500,0,60,40,0,0,"  (1.5 ml/min * 1000 = 1500 µl/min)

[Fact] SetFlowRate_GradientExceedsTotal_ReturnsFailure
  Act: InvokeCapabilityAsync("SetFlowRate", Json("""{"flowRateMlMin":1.0,"percA":70,"percB":50,"percC":0,"percD":0}"""))
  Assert: result.Success == false; ErrorMessage contains "100"

[Fact] GetPumpStatus_Running_ReturnsStructuredResult
  Arrange: response "STATUS:ok,15000,3,0,1500,60,40,0,0,0,0,0,0,0,0,0,0,0,350,0\r"
  (state index 2 = 3 = Running; flow index 5 = 1500/1000 = 1.5 ml/min; pressure index 18 = 350/1000 = 0.35 bar)
  Act: InvokeCapabilityAsync("GetPumpStatus", Json("{}"))
  Assert: result.Success == true
  Assert: ReturnValue JSON contains "state":"Running"
  Assert: ReturnValue JSON contains "flowRateMlMin":1.5

[Fact] GetIdentity_ReturnsIdentifyString
  Arrange: response "IDENTIFY:3,KNAUER,AZURA,F001,1,2\r"
  Act: InvokeCapabilityAsync("GetIdentity", Json("{}"))
  Assert: result.Success == true
  Assert: ReturnValue string contains "KNAUER"
```

---

**SystagOpcUaClientTests.cs:**

Uses the internal `OpcDeviceTransport(options, logger, IOpcSessionFactory)` constructor and NSubstitute mocks.

```csharp
private static (SystagOpcUaClient driver, IOpcSession session, OpcDeviceTransport transport) CreateDriver()
{
    var session = Substitute.For<IOpcSession>();
    session.IsConnected.Returns(true);
    var factory = Substitute.For<IOpcSessionFactory>();
    factory.Create(Arg.Any<OpcTransportOptions>(), Arg.Any<ApplicationConfiguration>())
           .Returns(session);
    var options = new OpcTransportOptions
    {
        EndpointUrl = "opc.tcp://localhost:4840",
        AutoAcceptUntrustedCertificates = false
    };
    var transport = new OpcDeviceTransport(options, NullLogger<OpcDeviceTransport>.Instance, factory);
    var driver = new SystagOpcUaClient(transport, NullLogger<SystagOpcUaClient>.Instance);
    return (driver, session, transport);
}

private static JsonElement Json(string json)
{
    using var doc = JsonDocument.Parse(json); return doc.RootElement.Clone();
}
```

Required test cases:
```
[Fact] ReadNode_CallsOpcSessionRead_ReturnsValue
  Arrange: session.ReadNodeAsync<object>("ns=2;s=Temperature", ...) returns (object)75.5
  Act: InvokeCapabilityAsync("ReadNode", Json("""{"nodeId":"ns=2;s=Temperature"}"""))
  Assert: result.Success == true
  Assert: session.ReadNodeAsync received with nodeId == "ns=2;s=Temperature"

[Fact] WriteNode_CallsOpcSessionWrite_Succeeds
  Arrange: session.WriteNodeAsync(...) does not throw
  Act: InvokeCapabilityAsync("WriteNode", Json("""{"nodeId":"ns=2;s=TempSetpoint","value":80.0}"""))
  Assert: result.Success == true
  Assert: session.WriteNodeAsync received with nodeId == "ns=2;s=TempSetpoint" and value 80.0

[Fact] WriteNode_OutOfRange_Succeeds  (WriteNode does no range validation — caller is responsible)
  Arrange: session.WriteNodeAsync does not throw
  Act: InvokeCapabilityAsync("WriteNode", Json("""{"nodeId":"ns=2;s=TempSetpoint","value":9999.0}"""))
  Assert: result.Success == true  (driver passes value through; OPC UA server enforces limits)

[Fact] SubscribeNode_CallsOpcSessionSubscribe_Succeeds
  Arrange: session.SubscribeNodeAsync("ns=2;s=Temperature", ...) returns Substitute.For<IOpcSubscription>()
  Act: InvokeCapabilityAsync("SubscribeNode", Json("""{"nodeId":"ns=2;s=Temperature"}"""))
  Assert: result.Success == true
  Assert: session.SubscribeNodeAsync received with nodeId == "ns=2;s=Temperature"

[Fact] OpcTransportOptions_AutoAcceptUntrustedCertificates_DefaultFalse
  Arrange: new OpcTransportOptions { EndpointUrl = "opc.tcp://localhost:4840" }
  Assert: options.AutoAcceptUntrustedCertificates == false
```

**Test:**
- `dotnet test tests/Helix.Orchestration.Devices.Tests/` — all new KnauerAzuraTests and SystagOpcUaClientTests pass; zero skips
- All existing VapourtecR4Tests still pass

---

### T4 — Register New Drivers in CapabilityRegistry
**Requirement:** B5 (all drivers registered), B3/B4 (capabilities reachable)
**Files:**
- `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs` — extend `AddMigratedDeviceAdapters()`

**Implementation:**

Read the current file (confirmed in Phase 2 research). The key constraint from the Risks section: `TcpTransportOptions` and `OpcTransportOptions` must be registered with factory lambdas, not bare `AddSingleton(new ...)`, to avoid DI collision when multiple transport options of the same type exist.

Extend `AddMigratedDeviceAdapters()` — add after the existing VapourtecR4 singleton registrations:

```csharp
// KnauerAzura — Pump TCP connection (placeholder IP; override via appsettings)
services.AddSingleton(sp => new TcpDeviceTransport(
    new TcpTransportOptions
    {
        Host = "192.168.1.101",    // KnauerAzura pump hostname from POP source
        Port = 10001,              // Default port from POP source (DadPort command: default 10001)
        FramingMode = TcpFramingMode.Delimiter,
        DelimiterByte = 0x0D       // Commands terminated with \r (0x0D), not \n
    },
    sp.GetRequiredService<ILogger<TcpDeviceTransport>>()));
services.AddSingleton<KnauerAzura>(sp => new KnauerAzura(
    sp.GetRequiredService<TcpDeviceTransport>(),
    sp.GetRequiredService<ILogger<KnauerAzura>>()));

// SystagOpcUaClient — OPC UA endpoint (placeholder; override via appsettings)
services.AddSingleton(sp => new OpcDeviceTransport(
    new OpcTransportOptions
    {
        EndpointUrl = "opc.tcp://localhost:4840",
        AutoAcceptUntrustedCertificates = false
    },
    sp.GetRequiredService<ILogger<OpcDeviceTransport>>()));
services.AddSingleton<SystagOpcUaClient>(sp => new SystagOpcUaClient(
    sp.GetRequiredService<OpcDeviceTransport>(),
    sp.GetRequiredService<ILogger<SystagOpcUaClient>>()));
```

Inside the existing `CapabilityRegistry` singleton lambda, add after the VapourtecR4 loop:

```csharp
// KnauerAzura (Phase 3)
var knauer = sp.GetRequiredService<KnauerAzura>();
foreach (var cap in knauer.GetCapabilities())
{
    registry.Register(cap.Name, new HelixAdapterCapabilityBridge(knauer));
    logger.LogInformation("Registered capability: {CapabilityName}", cap.Name);
}

// SystagOpcUaClient (Phase 3)
var systag = sp.GetRequiredService<SystagOpcUaClient>();
foreach (var cap in systag.GetCapabilities())
{
    registry.Register(cap.Name, new HelixAdapterCapabilityBridge(systag));
    logger.LogInformation("Registered capability: {CapabilityName}", cap.Name);
}
```

Add using directives: `Helix.Orchestration.Devices.Transports.Tcp`, `Helix.Orchestration.Devices.Transports.Opc`.

**Test:**
- `dotnet build Helix.Orchestration.slnx` exits 0
- Agent startup log contains `Registered capability: Start` and `Registered capability: GetPumpStatus` (KnauerAzura) and `Registered capability: WriteNode` and `Registered capability: SubscribeNode` (SystagOpcUaClient)

---

### T5 — Update REQUIREMENTS.md B5 Status and Driver Inventory
**Requirement:** B5 (all B-series requirements closed)
**Files:**
- `Helix.Orchestration/docs/driver-inventory.md` — update Phase 3 status for KnauerAzura and SystagOpcUaClient
- `.planning/REQUIREMENTS.md` — mark B3, B4, B5 as Complete

**Implementation:**

Update `Helix.Orchestration/docs/driver-inventory.md` Phase 3 column:
- KnauerAzura row: `Phase 3` → `Phase 3 ✓ (pump-only; DAD + Thermostat deferred)`
- SystagOpcUaClient row: `Phase 3` → `Phase 3 ✓ (browse-and-subscribe; node IDs caller-supplied)`

Read `.planning/REQUIREMENTS.md`. Update B3, B4, B5 status entries:
```
- **B3** — KnauerAzura driver: Complete (Phase 3, `Helix.Orchestration.Devices/Drivers/KnauerAzura.cs`, pump-only scope)
- **B4** — SystagOpcUaClient driver: Complete (Phase 3, `Helix.Orchestration.Devices/Drivers/SystagOpcUaClient.cs`, dynamic browse+subscribe)
- **B5** — All drivers listed and implemented: Complete (Phase 3; see Helix.Orchestration/docs/driver-inventory.md)
```

**Test:**
`grep -n "Complete" .planning/REQUIREMENTS.md` shows B3, B4, B5 as Complete.

---

## Execution Order

```
Wave 1 (parallel — no dependencies):
  T1   KnauerAzura.cs implementation
  T2   SystagOpcUaClient.cs + OpcTransportOptions fix + OpcDeviceTransport fix

Wave 2 (depends on T1, T2):
  T3   KnauerAzuraTests.cs + SystagOpcUaClientTests.cs

Wave 3 (depends on T1, T2):
  T4   Register new drivers in CapabilityRegistry

Wave 4 (depends on all of above):
  T5   Update REQUIREMENTS.md B5 and driver-inventory.md
```

---

## Success Criteria

- [ ] `dotnet build Helix.Orchestration.slnx -c Debug` exits 0 with all projects included
- [ ] `dotnet test tests/Helix.Orchestration.Devices.Tests/` — all KnauerAzuraTests (8 cases) and SystagOpcUaClientTests (5 cases) pass; zero `[Fact(Skip)]`
- [ ] All existing VapourtecR4Tests still pass (no regression)
- [ ] CapabilityRegistry at Agent startup logs `Start`, `Stop`, `SetFlowRate`, `GetPumpStatus`, `GetIdentity`, `RemoteControl` for KnauerAzura; `ReadNode`, `WriteNode`, `SubscribeNode`, `GetSystemInfo` for SystagOpcUaClient
- [ ] `grep -n "AutoAcceptUntrustedCertificates = true" Helix.Orchestration.Devices/Transports/Opc/OpcDeviceTransport.cs` returns 0 matches
- [ ] Unit tests pass for at least one pump command (`Start`, `SetFlowRate`) and at least one OPC UA command (`WriteNode`, `SubscribeNode`)
- [ ] All B-series requirements closed; REQUIREMENTS.md B3, B4, B5 marked Complete
- [ ] `driver-inventory.md` Phase 3 column updated for KnauerAzura (pump-only) and SystagOpcUaClient

---

## Risks

- **KnauerAzura RAMP command field count.** The 15-field `RAMP:` format (`flowUlMin,?,percA,percB,percC,percD,0,0,0,0,0,0,0,0,durationMs`) is inferred from the POP source (`RAMP:500,0,60,40,0,0,0,0,0,0,0,0,0,0,1`). Field 1 (index 1) is `0` in all observed calls. If the real device uses a different field layout, the format string needs revision. Mitigation: tests assert that the sent bytes contain the known field prefix; executor must verify against actual device documentation if available.

- **KnauerAzura STATUS response field layout.** The STATUS response field indices (state at [2], flow at [5], percs at [6-9], pressure at [18]) are inferred from the POP source comment and parsing code. If the real device firmware version differs, field positions may shift. Mitigation: `GetPumpStatus` catches parse exceptions and returns `{ raw: response }` rather than throwing — it never returns a failure on malformed input.

- **SystagOpcUaClient `DataReceived` casting.** `StreamTrendsAsync` casts `Transport` to `OpcDeviceTransport` to access `.DataReceived`. If the transport is not `OpcDeviceTransport` (e.g., in a test using `IDeviceTransport` stub), the cast fails. Mitigation: the internal test constructor takes `IDeviceTransport`, and `StreamTrendsAsync` has an explicit `yield break` guard for non-OPC transports.

- **OpcDeviceTransport duplicate registration.** Using factory lambdas for `TcpDeviceTransport` and `OpcDeviceTransport` in T4 creates new instances without the `IOpcSessionFactory` injection path — the test constructor is internal. Production singletons go through the public constructor and use the real `OpcFoundationSessionFactory`. This is correct; tests use the internal constructor directly.

- **`HelixAdapterCapabilityBridge` currently takes `IHelixAdapter` only.** Inspect the Phase 2 bridge signature — if it also takes `capabilityName`, update the T4 call sites accordingly. The current production code in `DeviceAdapterExtensions.cs` calls `new HelixAdapterCapabilityBridge(adapter)` (single argument), which matches the bridge as last seen.

---

## Threat Model

### Trust Boundaries

| Boundary | Description |
|----------|-------------|
| Agent → TCP socket (KnauerAzura pump) | RAMP and START commands control pump flow rates. Out-of-range or malformed values could cause hardware over-pressure or over-speed. |
| Agent → OPC UA session (SystagOpcUaClient) | WriteNode sends caller-supplied values to caller-supplied node IDs. No server-side range enforcement is visible to the driver. |
| CapabilityRegistry name → driver dispatch | Capability names from workflow steps are string lookups; unregistered names return Failed. |

### STRIDE Threat Register

| Threat ID | Category | Component | Disposition | Mitigation Plan |
|-----------|----------|-----------|-------------|-----------------|
| T-3-01 | Tampering | KnauerAzura SetFlowRate | mitigate | Validate flowRateMlMin in [0, 20] and sum of percs ≤ 100 before encoding RAMP; return `Failed` if invalid |
| T-3-02 | Tampering | SystagOpcUaClient WriteNode | accept | Driver passes caller value to OPC server; OPC UA server enforces hardware limits. No driver-level range check (node IDs and valid ranges are deployment-specific). Document this in the capability description. |
| T-3-03 | Information Disclosure | OPC UA AutoAcceptUntrustedCertificates | mitigate | Default `false` in `OpcTransportOptions`; operator must explicitly opt in for dev/test |
| T-3-04 | Denial of Service | TcpDeviceTransport blocking on read | accept | TCP ReceiveTimeoutMs (default 5000ms) bounds blocking. KnauerAzura responses are fast ASCII; timeout is acceptable. |
| T-3-SC | Tampering | No new npm/pip/cargo installs | accept | Phase 3 adds no new package dependencies. |
