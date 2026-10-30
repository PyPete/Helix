using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Interfaces;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the IFM IO-Link Master Controller.
/// Uses IO-Link HTTP transport via IPopIoLinkClient.
/// The IDeviceTransport parameter satisfies the base class requirement but is never used for I/O.
/// </summary>
public sealed class IfmMasterController : DeviceAdapterBase
{
    private readonly IPopIoLinkClient _ioLink;
    private readonly IFile _file;

    private string _endPoint = string.Empty;
    private byte _activeSystem;

    private readonly Dictionary<string, double> _trendMultiplierValues = new();
    private readonly Dictionary<string, string> _trendUnitOfMeasures = new();
    private readonly Dictionary<string, string> _trendFriendlyNames = new();

    public IfmMasterController(
        IPopIoLinkClient ioLink,
        IFile file,
        IDeviceTransport transport,
        ILogger<IfmMasterController> logger)
        : base(transport, logger)
    {
        _ioLink = ioLink;
        _file = file;
    }

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // FindActiveSystems: discovers active IO-Link sensors from an endpoint
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "FindActiveSystems",
                "Discover all active IO-Link sensors at an endpoint",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"endPoint\":{\"type\":\"string\"}},\"required\":[\"endPoint\"]}"),
            async (parameters, _, ct) =>
            {
                var endPoint = parameters.GetProperty("endPoint").GetString() ?? string.Empty;
                var activeSystems = await _ioLink.FindActiveSensorsFromEndPointAsync(endPoint);
                var list = activeSystems.Select(s => new { s.DeviceId, s.Port }).ToList();
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(list));
            });

        // Open: creates a session with an IO-Link endpoint and loads configuration
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "Open",
                "Open a session with an IO-Link master controller endpoint",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"endPoint\":{\"type\":\"string\"},\"portNumber\":{\"type\":\"integer\"}},\"required\":[\"endPoint\",\"portNumber\"]}"),
            async (parameters, _, ct) =>
            {
                var endPoint = parameters.GetProperty("endPoint").GetString() ?? string.Empty;
                var portNumber = (byte)parameters.GetProperty("portNumber").GetInt32();

                var opened = await _ioLink.CreateSessionAsync(endPoint, portNumber);
                if (!opened)
                    return CapabilityInvocationResult.Failed($"Failed to open IO-Link session at {endPoint}:{portNumber}");

                _endPoint = endPoint;
                _activeSystem = portNumber;

                var (productId, deviceId, vendorId) = _ioLink.GetDeviceIdentifiers();
                Logger.LogInformation(
                    "Connected to IO-Link Device. ProductId: {ProductId}, DeviceId: {DeviceId}, VendorId: {VendorId}",
                    productId, deviceId, vendorId);

                // Load io_link.json configuration
                try
                {
                    var myJsonString = await _file.ReadAllTextAsync("PopConfig/io_link.json");
                    var jsonDb = JsonNode.Parse(myJsonString)?.AsObject();
                    if (jsonDb is not null && jsonDb.TryGetPropertyValue(productId, out var tokens) && tokens is not null)
                    {
                        foreach (var row in tokens.AsArray())
                        {
                            if (row is null) continue;
                            var trendName = row["trend_name"]?.GetValue<string>();
                            var friendlyName = row["friendly_name"]?.GetValue<string>();
                            var parsedOk = double.TryParse(row["multiplier"]?.GetValue<string>(), out var multiplier);
                            var unit = row["unit"]?.GetValue<string>();

                            if (parsedOk && !string.IsNullOrEmpty(trendName) && !string.IsNullOrEmpty(friendlyName))
                            {
                                _trendFriendlyNames.TryAdd(trendName, friendlyName);
                                _trendMultiplierValues.TryAdd(trendName, multiplier);
                            }
                            if (!string.IsNullOrEmpty(unit) && !string.IsNullOrEmpty(trendName))
                                _trendUnitOfMeasures.TryAdd(trendName, unit);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to parse io_link.json file");
                }

                return CapabilityInvocationResult.Successful(
                    JsonSerializer.SerializeToElement(new { productId, deviceId, vendorId }));
            });

        // Close: closes the IO-Link session
        builder.RegisterSimple(
            "Close",
            "Close the IO-Link master controller session",
            _ =>
            {
                _ioLink.CloseSession();
                return Task.FromResult(CapabilityInvocationResult.Successful());
            });

        // GetAllProcessData: returns all current process data values
        builder.RegisterSimple(
            "GetAllProcessData",
            "Get all current process data from the IO-Link device",
            async _ =>
            {
                var allData = await _ioLink.GetAllProcessDataForDeviceAsync();
                var result = allData
                    .Where(d => !string.IsNullOrEmpty(d.Name))
                    .Select(d => new { d.Name, Value = d.Value?.ToString() })
                    .ToList();
                return CapabilityInvocationResult.Successful(JsonSerializer.SerializeToElement(result));
            });

        // IsDevice: IO-Link device status check (minimal)
        builder.RegisterSimple(
            "IsDevice",
            "Check IO-Link device status",
            _ => Task.FromResult(CapabilityInvocationResult.Successful(
                JsonSerializer.SerializeToElement("IsDevice not fully implemented for IO-Link"))));
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // Blocking .Result replaced with await — deliberate improvement from POP source.
            List<(string Name, object Value)>? allData = null;
            try
            {
                allData = await _ioLink.GetAllProcessDataForDeviceAsync();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "IfmMasterController: failed to poll GetAllProcessDataForDeviceAsync");
            }

            if (allData is not null)
            {
                var timestamp = DateTimeOffset.UtcNow;
                foreach (var (name, value) in allData)
                {
                    if (string.IsNullOrEmpty(name)) continue;

                    var multiplier = _trendMultiplierValues.TryGetValue(name, out var m) ? m : 1.0;
                    var unit = _trendUnitOfMeasures.TryGetValue(name, out var u) ? u : string.Empty;
                    var friendlyName = _trendFriendlyNames.TryGetValue(name, out var fn) ? fn : name;
                    var trendName = $"ifmmastercontroller.{name}";

                    TrendSample? sample = null;
                    switch (value)
                    {
                        case float f:
                            sample = new TrendSample { TrendName = trendName, Value = f * multiplier, Timestamp = timestamp, Unit = unit };
                            break;
                        case bool b:
                            sample = new TrendSample { TrendName = trendName, Value = b ? 1.0 : 0.0, Timestamp = timestamp, Unit = unit };
                            break;
                        case short s:
                            sample = new TrendSample { TrendName = trendName, Value = s * multiplier, Timestamp = timestamp, Unit = unit };
                            break;
                        case ushort us:
                            sample = new TrendSample { TrendName = trendName, Value = us * multiplier, Timestamp = timestamp, Unit = unit };
                            break;
                        case string str:
                            sample = new TrendSample { TrendName = trendName, Value = str, Timestamp = timestamp, Unit = unit };
                            break;
                    }

                    if (sample is not null)
                        yield return sample;
                }
            }

            var cancelled = false;
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            if (cancelled) yield break;
        }
    }
}
