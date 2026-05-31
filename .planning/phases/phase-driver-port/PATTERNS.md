# Phase Driver-Port: Pattern Map

**Mapped:** 2026-05-31
**Files analyzed:** 44 new drivers + 44 test files + 1 DI registration update
**Analogs found:** 44 / 44 (all have close analog; VapourtecR4 = serial, KnauerAzura = TCP, SystagOpcUaClient = OPC-UA)

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `Helix.Orchestration.Devices/Drivers/{Serial32}.cs` (32 files) | driver | request-response | `Drivers/VapourtecR4.cs` | exact |
| `Helix.Orchestration.Devices/Drivers/Microsaic.cs` (1 file) | driver | request-response | `Drivers/KnauerAzura.cs` | exact |
| `Helix.Orchestration.Devices/Drivers/{OpcUA3}.cs` (3 files: EasyMax, Fbrm, ReactIr) | driver | event-driven + request-response | `Drivers/SystagOpcUaClient.cs` | exact |
| `Helix.Orchestration.Devices/Drivers/{Special8}.cs` (8 files) | driver | request-response / TBD | `Drivers/VapourtecR4.cs` | role-match (transport TBD) |
| `tests/Helix.Orchestration.Devices.Tests/Drivers/{Name}Tests.cs` (44 files) | test | request-response | `Tests/Drivers/VapourtecR4Tests.cs` (serial) / `KnauerAzuraTests.cs` (TCP/mock) | exact |
| `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs` | DI config | — | existing file (additive) | exact |

---

## Pattern Assignments

### 1. Serial Driver Pattern

**Analog:** `Helix.Orchestration.Devices/Drivers/VapourtecR4.cs`

**Namespace and imports** (VapourtecR4.cs lines 1-6):
```csharp
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;
```

**Class declaration and constructor** (VapourtecR4.cs lines 13-24):
```csharp
public sealed class VapourtecR4 : DeviceAdapterBase
{
    public VapourtecR4(SerialDeviceTransport transport, ILogger<VapourtecR4> logger)
        : base(transport, logger)
    {
    }
```

> For serial drivers: constructor takes `SerialDeviceTransport` (the concrete class, not `IDeviceTransport`).
> The second constructor with `IDeviceTransport` is added only when tests use NSubstitute mocking
> (see KnauerAzura pattern). Serial drivers that use the `FakeSerialPortConnection` approach do NOT
> need the `internal IDeviceTransport` constructor.

**Encoding helpers** (VapourtecR4.cs lines 26-29):
```csharp
private static byte[] Encode(string command) => Encoding.ASCII.GetBytes(command);

private static string Decode(byte[] response) => Encoding.ASCII.GetString(response).Trim();
```

> Copy command terminator exactly from PopNextGen source. VapourtecR4 includes `\r\n` in the
> string passed to `Encode()`. KnauerAzura appends `"\r"` inside `Encode()` itself. Match whichever
> pattern the POP source uses.

**Simple capability (no parameters)** (VapourtecR4.cs lines 33-46):
```csharp
builder.RegisterSimple(
    "Start",
    "Start the Vapourtec R4 pumps running",
    async ct =>
    {
        var resp1 = Decode(await Transport.SendReceiveAsync(Encode("PN \r\n"), ct));
        if (resp1.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
            return CapabilityInvocationResult.Failed($"Device error: {resp1}");
        if (!resp1.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
            return CapabilityInvocationResult.Failed(resp1);

        // second command if needed
        var resp2 = Decode(await Transport.SendReceiveAsync(Encode("FW PN \r\n"), ct));
        if (resp2.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
            return CapabilityInvocationResult.Failed($"Device error: {resp2}");

        return CapabilityInvocationResult.Successful();
    });
```

**Parameterized capability** (VapourtecR4.cs lines 73-101):
```csharp
builder.Register(
    CapabilityDescriptor.CreateWithSchemas(
        "SetFlowRate",
        "Set the flow rate for a pump channel (0-3) in ml/min (0.0-9.99)",
        parameterSchema: "{\"type\":\"object\",\"properties\":{\"pump\":{\"type\":\"integer\",\"minimum\":0,\"maximum\":3},\"rateMlMin\":{\"type\":\"number\",\"minimum\":0,\"maximum\":9.99}},\"required\":[\"pump\",\"rateMlMin\"]}"),
    async (parameters, _, ct) =>
    {
        var pump = parameters.GetProperty("pump").GetInt32();
        var rateMlMin = parameters.GetProperty("rateMlMin").GetDouble();

        if (pump < 0 || pump > 3)
            return CapabilityInvocationResult.Failed($"Pump index {pump} is out of range [0,3]");

        // ... build cmd string ...

        var resp = Decode(await Transport.SendReceiveAsync(Encode(cmd), ct));
        if (resp.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
            return CapabilityInvocationResult.Failed($"Device error: {resp}");
        if (!resp.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
            return CapabilityInvocationResult.Failed(resp);

        return CapabilityInvocationResult.Successful();
    });
```

**Capability returning a value** (VapourtecR4.cs lines 195-204):
```csharp
builder.RegisterSimple(
    "GetFirmwareVersion",
    "Query the R4 firmware version string",
    async ct =>
    {
        var resp = Decode(await Transport.SendReceiveAsync(Encode("GV\r\n"), ct));
        if (resp.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
            return CapabilityInvocationResult.Failed($"Device error: {resp}");

        return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(resp));
    });
```

**Trend streaming — serial poll loop** (VapourtecR4.cs lines 253-320):
```csharp
public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
    [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        var now = DateTimeOffset.UtcNow;

        string? htResponse = null;
        try
        {
            htResponse = Decode(await Transport.SendReceiveAsync(Encode("HT\r\n"), cancellationToken));
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "VapourtecR4: failed to poll HT; skipping temperature trends");
        }

        if (!string.IsNullOrWhiteSpace(htResponse) &&
            !htResponse.StartsWith("ERR", StringComparison.OrdinalIgnoreCase))
        {
            // ... parse and yield ...
            yield return new TrendSample
            {
                TrendName = $"vapourtec.temperature.{label}",
                Value = raw / 10.0,
                Timestamp = now,
                Unit = "C"
            };
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            yield break;
        }
    }
}
```

> Rules for trend streaming:
> - Use `[System.Runtime.CompilerServices.EnumeratorCancellation]` attribute on `cancellationToken`.
> - Wrap each poll in try/catch; log warning and continue on failure (do not break the loop).
> - Catch `OperationCanceledException` on the `Task.Delay` and `yield break`.
> - Poll interval: **2 seconds** (matching VapourtecR4). Override only if POP source documents a different interval.
> - `TrendName` format: `{deviceprefix}.{measurementtype}.{channel}` (e.g., `vapourtec.temperature.A`, `knauer.pump.pressure`).
> - `Value` type is `object` — pass `double`, `string`, etc. directly.
> - `Unit`: use SI unit string or `null` if unitless. Use `"C"` for Celsius, `"ml/min"`, `"bar"`, `"rpm"`, `"%"`.

---

### 2. TCP Driver Pattern

**Analog:** `Helix.Orchestration.Devices/Drivers/KnauerAzura.cs`

**Imports and namespace** (KnauerAzura.cs lines 1-8):
```csharp
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Tcp;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;
```

**Dual constructors — production TCP + internal test mock** (KnauerAzura.cs lines 14-31):
```csharp
public sealed class KnauerAzura : DeviceAdapterBase
{
    public KnauerAzura(TcpDeviceTransport transport, ILogger<KnauerAzura> logger)
        : base(transport, logger)
    {
    }

    /// <summary>Internal constructor for testing with a mock transport.</summary>
    internal KnauerAzura(IDeviceTransport transport, ILogger<KnauerAzura> logger)
        : base(transport, logger)
    {
    }
```

> TCP and OPC-UA drivers MUST have the `internal IDeviceTransport` constructor because their tests
> use NSubstitute (`Substitute.For<IDeviceTransport>()`), not `FakeSerialPortConnection`.

**Encode/Decode with embedded terminator** (KnauerAzura.cs lines 33-39):
```csharp
private static byte[] Encode(string command) => Encoding.ASCII.GetBytes(command + "\r");

private static string Decode(byte[] response) => Encoding.ASCII.GetString(response).Trim();

private static bool IsOk(string response) =>
    response.Contains(":OK", StringComparison.OrdinalIgnoreCase);
```

**Trend streaming — TCP poll with delay-first pattern** (KnauerAzura.cs lines 148-200):
```csharp
public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        // Delay first, then poll (different from VapourtecR4's poll-first pattern)
        try
        {
            await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            yield break;
        }

        string? statusResponse = null;
        try
        {
            statusResponse = Decode(await Transport.SendReceiveAsync(Encode("STATUS?"), cancellationToken));
        }
        catch (OperationCanceledException) { yield break; }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "KnauerAzura: failed to poll STATUS?; skipping trend sample");
            continue;
        }

        if (string.IsNullOrWhiteSpace(statusResponse)) continue;

        var now = DateTimeOffset.UtcNow;
        // ... parse ...
        yield return new TrendSample { TrendName = "knauer.pump.flowrate", Value = flowRateMlMin, Timestamp = now, Unit = "ml/min" };
        yield return new TrendSample { TrendName = "knauer.pump.pressure",  Value = pressureBar,   Timestamp = now, Unit = "bar" };
        yield return new TrendSample { TrendName = "knauer.pump.state",     Value = stateName,     Timestamp = now, Unit = null };
    }
}
```

---

### 3. OPC-UA Driver Pattern

**Analog:** `Helix.Orchestration.Devices/Drivers/SystagOpcUaClient.cs`

**Imports** (SystagOpcUaClient.cs lines 1-9):
```csharp
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Transports.Opc;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;
```

**Dual constructors** (SystagOpcUaClient.cs lines 15-32): same pattern as KnauerAzura — production takes `OpcDeviceTransport`, internal takes `IDeviceTransport`.

**OPC command helpers** (SystagOpcUaClient.cs lines 34-46):
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

> OPC-UA uses UTF-8 encoding (not ASCII). The operation envelope `{ nodeId, operation }` is mandatory.

**OPC trend streaming via DataReceived observable + Channel** (SystagOpcUaClient.cs lines 113-140):
```csharp
public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    if (Transport is not OpcDeviceTransport opcTransport)
        yield break;

    var channel = Channel.CreateUnbounded<TrendSample>();

    using var sub = opcTransport.DataReceived.Subscribe(data =>
    {
        try
        {
            using var doc = JsonDocument.Parse(data);
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

> OPC-UA drivers do NOT poll. They subscribe via `opcTransport.DataReceived` and forward through a Channel.
> The type-check `if (Transport is not OpcDeviceTransport opcTransport) yield break;` is required.

---

### 4. Test File Pattern

**Serial driver tests analog:** `tests/Helix.Orchestration.Devices.Tests/Drivers/VapourtecR4Tests.cs`
**TCP/OPC-UA driver tests analog:** `tests/Helix.Orchestration.Devices.Tests/Drivers/KnauerAzuraTests.cs`

#### 4a. Serial test — FakeSerialPortConnection approach

**Imports and namespace** (VapourtecR4Tests.cs lines 1-9):
```csharp
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Drivers;
using Helix.Orchestration.Devices.Transports.Serial;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;
```

**CreateDriver helper for serial tests** (VapourtecR4Tests.cs lines 11-28):
```csharp
private static VapourtecR4 CreateDriver(params string[] responses)
{
    var port = new FakeSerialPortConnection();
    foreach (var r in responses)
        port.EnqueueRead(r + "\r\n");
    var factory = new FakeSerialPortConnectionFactory(port);
    var transport = new SerialDeviceTransport(
        new SerialTransportOptions { PortName = "COM1", BaudRate = 19200, CommandTerminator = "\r\n" },
        NullLogger<SerialDeviceTransport>.Instance,
        factory);
    var driver = new VapourtecR4(transport, NullLogger<VapourtecR4>.Instance);
    transport.ConnectAsync().GetAwaiter().GetResult();
    return driver;
}

private static JsonElement Json(string json)
{
    using var doc = JsonDocument.Parse(json);
    return doc.RootElement.Clone();
}

private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);
```

**Individual test structure** (VapourtecR4Tests.cs lines 34-46):
```csharp
[Fact]
public async Task Start_SendsPnCommand_ReturnsSuccess()
{
    var port = new FakeSerialPortConnection();
    port.EnqueueRead("OK\r\n");
    port.EnqueueRead("OK\r\n");
    var transport = MakeTransport(port);
    await transport.ConnectAsync();
    var driver = new VapourtecR4(transport, NullLogger<VapourtecR4>.Instance);

    var result = await driver.InvokeCapabilityAsync("Start", Json("{}"));

    Assert.True(result.Success);
    Assert.Equal(Ascii("PN \r\n"), port.Writes[0]);
}
```

**Test naming convention:** `{CapabilityName}_{Condition}_{ExpectedOutcome}`
Examples: `Start_SendsPnCommand_ReturnsSuccess`, `SetFlowRate_InvalidPump_ReturnsFailure`, `GetTemperatures_ParsesHtResponse_ReturnsNonNullReactors`

**Asserting command bytes sent** (VapourtecR4Tests.cs lines 43-44):
```csharp
Assert.Equal(Ascii("PN \r\n"), port.Writes[0]);         // exact byte match
Assert.StartsWith("FW FR", Encoding.ASCII.GetString(port.Writes[0]));  // prefix match
```

**Asserting capability result fields:**
```csharp
Assert.True(result.Success);
Assert.False(result.Success);
Assert.Contains("ERR", result.ErrorMessage);
Assert.Contains("out of range", result.ErrorMessage);
Assert.NotNull(result.ReturnValue);
Assert.Contains("v1.64", result.ReturnValue.ToString());
```

**FakeSerialPortConnection + FakeSerialPortConnectionFactory** (VapourtecR4Tests.cs lines 185-260):
```csharp
private static SerialDeviceTransport MakeTransport(FakeSerialPortConnection port)
    => new SerialDeviceTransport(
        new SerialTransportOptions { PortName = "COM1", BaudRate = 19200, CommandTerminator = "\r\n" },
        NullLogger<SerialDeviceTransport>.Instance,
        new FakeSerialPortConnectionFactory(port));

private sealed class FakeSerialPortConnectionFactory : ISerialPortConnectionFactory
{
    private readonly FakeSerialPortConnection _connection;
    public FakeSerialPortConnectionFactory(FakeSerialPortConnection connection) => _connection = connection;
    public ISerialPortConnection Create(SerialTransportOptions options) => _connection;
}

private sealed class FakeSerialPortConnection : ISerialPortConnection
{
    private readonly Queue<byte[]> _reads = new();
    private readonly List<byte[]> _writes = new();
    private readonly FakeSerialStream _stream;

    public FakeSerialPortConnection()
    {
        _stream = new FakeSerialStream(_reads, _writes, () => WriteException, () => ReadDelay);
    }

    public bool IsOpen { get; private set; }
    public Stream BaseStream => _stream;
    public Exception? WriteException { get; set; }
    public TimeSpan ReadDelay { get; set; }
    public IReadOnlyList<byte[]> Writes => _writes;

    public void EnqueueRead(string payload) => _reads.Enqueue(Encoding.ASCII.GetBytes(payload));
    public void Open() => IsOpen = true;
    public void Close() => IsOpen = false;
    public void Dispose() => IsOpen = false;
}
```

> Copy `FakeSerialStream` verbatim from VapourtecR4Tests.cs lines 222-260. It is boilerplate shared
> by all serial driver tests. Do NOT re-implement it — copy it exactly.

#### 4b. TCP/NSubstitute test — mock IDeviceTransport approach

**Imports** (KnauerAzuraTests.cs lines 1-9):
```csharp
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Drivers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Helix.Orchestration.Devices.Tests.Drivers;
```

**CreateDriver helper — NSubstitute queue** (KnauerAzuraTests.cs lines 11-23):
```csharp
private static (KnauerAzura driver, IDeviceTransport transport) CreateDriver(params string[] responses)
{
    var transport = Substitute.For<IDeviceTransport>();
    var queue = new Queue<byte[]>(responses.Select(r => Encoding.ASCII.GetBytes(r)));
    transport.SendReceiveAsync(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
             .Returns(_ => Task.FromResult(queue.Dequeue()));
    var driver = new KnauerAzura(transport, NullLogger<KnauerAzura>.Instance);
    return (driver, transport);
}
```

**Asserting command strings sent to NSubstitute mock** (KnauerAzuraTests.cs lines 31-37):
```csharp
private static string SentCommand(IDeviceTransport transport, int callIndex = 0)
{
    var calls = transport.ReceivedCalls()
        .Where(c => c.GetMethodInfo().Name == "SendReceiveAsync")
        .ToList();
    return Encoding.ASCII.GetString((byte[])calls[callIndex].GetArguments()[0]);
}
```

**Usage in assertions** (KnauerAzuraTests.cs lines 40-44):
```csharp
var result = await driver.InvokeCapabilityAsync("RemoteControl", Json("{}"));
Assert.True(result.Success);
Assert.Contains("REMOTE:60000000", SentCommand(transport));
// or for multi-command capabilities:
Assert.Contains("START:2,2", SentCommand(transport, 0));
Assert.Contains("UNHOLD", SentCommand(transport, 1));
```

> Use the NSubstitute approach for TCP and OPC-UA drivers. Use the `FakeSerialPortConnection`
> approach for serial drivers. Never mix them.

---

### 5. DI Registration Pattern

**File:** `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs`

**Pattern for each new driver** (from DeviceAdapterExtensions.cs, lines 30-95 for existing drivers):

**Serial driver registration block:**
```csharp
// {DriverName} - Serial ({port}, {baudRate}, {terminator})
// PortName is a placeholder; override via appsettings or environment variable in production.
services.AddSingleton(new SerialTransportOptions
{
    PortName = "COM3",           // placeholder — real port from config
    BaudRate = 9600,             // match POP source default
    CommandTerminator = "\r\n"   // match POP source terminator
});
services.AddSingleton<SerialDeviceTransport>();
services.AddSingleton<{DriverName}>();
```

**TCP driver registration block** (KnauerAzura pattern):
```csharp
services.AddSingleton(sp => new TcpDeviceTransport(
    new TcpTransportOptions
    {
        Host = "192.168.1.101",         // placeholder
        Port = 10001,                    // match POP source default
        FramingMode = TcpFramingMode.Delimiter,
        DelimiterByte = 0x0D            // \r terminator
    },
    sp.GetRequiredService<ILogger<TcpDeviceTransport>>()));
services.AddSingleton(sp => new {DriverName}(
    sp.GetRequiredService<TcpDeviceTransport>(),
    sp.GetRequiredService<ILogger<{DriverName}>>()));
```

**OPC-UA driver registration block** (SystagOpcUaClient pattern):
```csharp
services.AddSingleton(sp => new OpcDeviceTransport(
    new OpcTransportOptions
    {
        EndpointUrl = "opc.tcp://localhost:4840",  // placeholder
        AutoAcceptUntrustedCertificates = false
    },
    sp.GetRequiredService<ILogger<OpcDeviceTransport>>()));
services.AddSingleton(sp => new {DriverName}(
    sp.GetRequiredService<OpcDeviceTransport>(),
    sp.GetRequiredService<ILogger<{DriverName}>>()));
```

**Capability registration block — must be added inside the `CapabilityRegistry` factory** (DeviceAdapterExtensions.cs lines 57-75):
```csharp
// {DriverName} (Plan 0X)
var {varName} = sp.GetRequiredService<{DriverName}>();
foreach (var cap in {varName}.GetCapabilities())
{
    var key = $"{deviceprefix}.{cap.Name}";
    registry.Register(key, new HelixAdapterCapabilityBridge({varName}));
    logger.LogInformation("Registered capability: {CapabilityName}", key);
}
```

**Device prefix derivation rule** (from CONTEXT.md decisions):
- Take the lowercase class name: `AalborgDfm` → `aalborgdfm`, `Heidolph` → `heidolph`
- Exact examples: `vapourtec`, `knauer`, `systag`, `alicat`, `heidolph`, `pilotone`

**DI lifetime constraint** (from DevicesServiceCollectionExtensions.cs header comment):
> All transports and adapters MUST be registered as Singleton. Scoped or Transient transports
> captured inside the Singleton CapabilityRegistry would produce a captive dependency.

**Alternative: use `DevicesServiceCollectionExtensions` helpers** (Hosting/DevicesServiceCollectionExtensions.cs):
```csharp
// Preferred for new drivers — uses the generic helpers instead of manual factory lambdas:
services.AddHelixSerialAdapter<AalborgDfm>(opts => {
    opts.PortName = "COM4";
    opts.BaudRate = 9600;
    opts.CommandTerminator = "\r\n";
});
// Then register the capability prefix block inside CapabilityRegistry as above.
```

---

## Shared Patterns

### CapabilityInvocationResult — all three forms

**Source:** `Helix.Orchestration.Devices/Abstractions/IHelixAdapter.cs` (lines 44-60)

```csharp
// Success with no return value
return CapabilityInvocationResult.Successful();

// Success with a return value (JSON element)
return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(resp));
return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(new { state, flowRateMlMin, pressureBar }));

// Failure
return CapabilityInvocationResult.Failed($"Device error: {resp}");
return CapabilityInvocationResult.Failed($"Pump index {pump} is out of range [0,3]");
```

> `CapabilityInvocationResult.Successful()` maps from POP `Info.Success()`.
> `CapabilityInvocationResult.Failed(msg)` maps from POP `Info.Failure(msg)`.
> There is no `Successful(string)` overload — always wrap strings in `JsonSerializer.SerializeToElement`.

### TrendSample — fields and conventions

**Source:** `Helix.Orchestration.Devices/Abstractions/IHelixAdapter.cs` (lines 64-82)

```csharp
public sealed class TrendSample
{
    public required string TrendName { get; init; }    // "deviceprefix.measurementtype.channel"
    public required object Value { get; init; }         // double, string, int — not wrapped
    public required DateTimeOffset Timestamp { get; init; }  // always DateTimeOffset.UtcNow
    public string? Unit { get; init; }                 // "C", "ml/min", "bar", "rpm", "%", null
}
```

**TrendName convention:**
```
{deviceprefix}.{measurementtype}           → "knauer.pump.pressure"
{deviceprefix}.{measurementtype}.{channel} → "vapourtec.temperature.A"
```

**Unit strings used in existing drivers:**
| Measurement | Unit string |
|---|---|
| Temperature (Celsius) | `"C"` |
| Flow rate | `"ml/min"` |
| Pressure | `"bar"` |
| Speed/RPM | `"rpm"` |
| Percentage | `"%"` |
| Pump state (string) | `null` |
| Dimensionless / unknown | `""` or `null` |

### Error handling — TransportFaultException

**Source:** `Helix.Orchestration.Devices/Abstractions/DeviceAdapterBase.cs` (lines 54-70)

The base class `InvokeCapabilityAsync` wraps all capability handlers in try/catch:
```csharp
catch (TransportFaultException ex)
{
    _logger.LogError(ex, "Transport fault while invoking capability '{CapabilityName}'", name);
    return CapabilityInvocationResult.Failed($"Transport fault: {ex.Message}");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error invoking capability '{CapabilityName}'", name);
    return CapabilityInvocationResult.Failed($"Invocation error: {ex.Message}");
}
```

> Driver capability handlers do NOT need their own try/catch for transport errors. The base class
> handles `TransportFaultException` and generic `Exception`. Only add try/catch inside a handler
> if there is response-parsing logic that can throw (e.g., `int.Parse`, `JsonDocument.Parse`).

### Namespace convention

All driver files use:
```
namespace Helix.Orchestration.Devices.Drivers;
```

All test files use:
```
namespace Helix.Orchestration.Devices.Tests.Drivers;
```

### PopNextGen API mapping — definitive reference

| PopNextGen (POP.DeviceCore) | Helix |
|---|---|
| `IPort.SendCommand(cmd, timeout)` | `await Transport.SendReceiveAsync(Encode(cmd), ct)` |
| `Info.Success()` | `CapabilityInvocationResult.Successful()` |
| `Info.Success(value)` | `CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(value))` |
| `Info.Failure(msg)` | `CapabilityInvocationResult.Failed(msg)` |
| `GetTrends()` returning `List<Trend<IValue>>` | `StreamTrendsAsync()` with 2s poll loop |
| Mock `IPort` in POP tests | `Substitute.For<IDeviceTransport>()` in Helix TCP/OPC tests |
| Mock `IPort` in POP serial tests | `FakeSerialPortConnection` + `FakeSerialPortConnectionFactory` |
| `Encoding.ASCII` (ASCII commands) | `Encoding.ASCII.GetBytes(cmd)` / `Encoding.ASCII.GetString(resp)` |
| `Encoding.UTF8` (OPC-UA JSON) | `Encoding.UTF8.GetBytes(json)` / `Encoding.UTF8.GetString(resp)` |

---

## Transport Constructor Reference

### SerialDeviceTransport

```csharp
new SerialDeviceTransport(
    new SerialTransportOptions
    {
        PortName = "COM3",                          // required
        BaudRate = 9600,                             // default: 9600
        DataBits = 8,                                // default: 8
        Parity = Parity.None,                        // default: None
        StopBits = StopBits.One,                     // default: One
        CommandTerminator = "\r\n",                  // default: "\r\n"
        ReadTimeoutMs = 500,                         // default: 500
        WriteTimeoutMs = 500,                        // default: 500
        RtsEnable = false,                           // default: false
        DtrEnable = false,                           // default: false
        Handshake = Handshake.None                   // default: None
    },
    logger)
```

### TcpDeviceTransport

```csharp
new TcpDeviceTransport(
    new TcpTransportOptions
    {
        Host = "192.168.1.101",                      // required
        Port = 10001,                                 // required
        ConnectTimeoutMs = 3000,                     // default: 3000
        SendTimeoutMs = 5000,                        // default: 5000
        ReceiveTimeoutMs = 5000,                     // default: 5000
        FramingMode = TcpFramingMode.Delimiter,      // default: Delimiter
        DelimiterByte = 0x0A,                        // default: 0x0A ('\n'); Knauer uses 0x0D ('\r')
        EnableBackgroundRead = true                  // default: true
    },
    logger)
```

### OpcDeviceTransport

```csharp
new OpcDeviceTransport(
    new OpcTransportOptions
    {
        EndpointUrl = "opc.tcp://localhost:4840",    // required
        SessionTimeoutMs = 30_000,                   // default: 30000
        SubscriptionPublishingIntervalMs = 200,      // default: 200
        ReconnectPeriodMs = 5000,                    // default: 5000
        SecurityMode = OpcSecurityMode.None,         // default: None
        ApplicationName = "Helix.Orchestration.Devices",  // default
        AutoAcceptUntrustedCertificates = false      // default: false (keep false for production)
    },
    logger)
```

### ModbusDeviceTransport

```csharp
new ModbusDeviceTransport(
    new ModbusTransportOptions
    {
        Mode = ModbusMode.Rtu,      // or ModbusMode.Tcp
        SlaveId = 1,                 // default: 1
        // RTU:
        PortName = "COM4",
        BaudRate = 9600,
        Parity = Parity.Even,        // default: Even (Modbus standard)
        StopBits = StopBits.One,
        // TCP:
        TcpHost = "192.168.1.50",
        TcpPort = 502,               // default: 502
        ReadTimeoutMs = 1000,
        WriteTimeoutMs = 1000
    },
    logger)
```

### CDllDeviceTransport

```csharp
new CDllDeviceTransport(
    new CDllTransportOptions
    {
        DllPath = null,              // optional directory
        DllName = "usbtc08.dll",     // required
        UseArchitectureSubfolder = false,
        FunctionMap = new Dictionary<string, string>
        {
            { "Init",    "PortavoInitDll" },
            { "Connect", "PortavoConnect" }
        }
    },
    logger)
```

---

## No Analog Found

No files in this phase are truly without analog. The 8 "Special transport" drivers (Plan 03) may require
a custom `IDeviceTransport` implementation if the PopNextGen source uses a protocol not covered by the
5 existing transports (Serial, TCP, OPC-UA, Modbus, CDll). Until the POP source is inspected:

| File | Role | Data Flow | Note |
|---|---|---|---|
| `Drivers/AtlasReactionSystem.cs` | driver | TBD | Inspect POP source before writing |
| `Drivers/Crystalline.cs` | driver | TBD | Inspect POP source before writing |
| `Drivers/IfmMasterController.cs` | driver | TBD | Inspect POP source before writing |
| `Drivers/OceanViewDriver.cs` | driver | TBD | Inspect POP source before writing |
| `Drivers/OceanViewNir.cs` | driver | TBD | Inspect POP source before writing |
| `Drivers/OceanViewUv.cs` | driver | TBD | Inspect POP source before writing |
| `Drivers/PicoTc08.cs` | driver | TBD | May use CDllDeviceTransport (P/Invoke) |
| `Drivers/PicoTc08LowLevelWrapper.cs` | driver | TBD | May use CDllDeviceTransport (P/Invoke) |

If a special driver maps to an existing transport: use the closest analog above.
If a new `IDeviceTransport` implementation is needed: it belongs in `Helix.Orchestration.Devices/Transports/{Protocol}/`.

---

## Metadata

**Analog search scope:** `Helix.Orchestration.Devices/Drivers/`, `Helix.Orchestration.Devices/Abstractions/`, `Helix.Orchestration.Devices/Transports/`, `tests/Helix.Orchestration.Devices.Tests/Drivers/`, `Helix.Orchestration.Agent/Adapters/Devices/`
**Files scanned:** 14 source files read in full
**Pattern extraction date:** 2026-05-31
