using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Interfaces;
using Helix.Orchestration.Devices.Model;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the PicoTech TC-08 USB thermocouple data logger. Facade over IPicoApi.
/// The IDeviceTransport parameter satisfies the base class requirement but is never used for I/O.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class PicoTc08 : DeviceAdapterBase
{
    private readonly IPicoApi _pico;
    private short _handle;
    private const int Usbtc08MaxChannels = 8;

    private readonly Dictionary<short, ThermoCoupleType> _channelToThermocoupleType;

    public PicoTc08(
        IPicoApi pico,
        IDeviceTransport transport,
        ILogger<PicoTc08> logger)
        : base(transport, logger)
    {
        _pico = pico;
        _channelToThermocoupleType = new Dictionary<short, ThermoCoupleType>();
        for (short i = 1; i <= Usbtc08MaxChannels; i++)
            _channelToThermocoupleType[i] = ThermoCoupleType.K;
    }

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // FindActiveDevices: opens all connected TC-08 devices
        builder.RegisterSimple(
            "FindActiveDevices",
            "Find and open all connected PicoTech TC-08 devices",
            _ =>
            {
                var handles = _pico.FindAndOpenDevices();
                return Task.FromResult(CapabilityInvocationResult.Successful(
                    JsonSerializer.SerializeToElement(handles)));
            });

        // SetActiveDevice: selects which device handle to use
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetActiveDevice",
                "Set the active TC-08 device handle",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"handle\":{\"type\":\"integer\"}},\"required\":[\"handle\"]}"),
            (parameters, _, _) =>
            {
                _handle = (short)parameters.GetProperty("handle").GetInt32();
                return Task.FromResult(CapabilityInvocationResult.Successful());
            });

        // IsDevice: validates the device and initialises channel defaults
        builder.RegisterSimple(
            "IsDevice",
            "Verify the TC-08 device is connected and initialise channel defaults",
            _ =>
            {
                var deviceInfo = _pico.GetDeviceInfo(_handle);
                if (string.IsNullOrEmpty(deviceInfo))
                    return Task.FromResult(CapabilityInvocationResult.Failed("Device is not a PicoTech TC-08"));

                // Set default K-type thermocouple for all channels
                for (short i = 0; i < Usbtc08MaxChannels; i++)
                    _pico.SetChannel(_handle, i, ThermoCoupleType.K);

                return Task.FromResult(CapabilityInvocationResult.Successful(
                    JsonSerializer.SerializeToElement(deviceInfo)));
            });

        // Close: stops and releases the device
        builder.RegisterSimple(
            "Close",
            "Stop and release the TC-08 device",
            _ =>
            {
                var result = _pico.StopDevice(_handle);
                return Task.FromResult(result.State == DeviceResultState.Failure
                    ? CapabilityInvocationResult.Failed(result.Reason)
                    : CapabilityInvocationResult.Successful());
            });

        // SetChannelThermocoupleType: sets thermocouple type for a single channel
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetChannelThermocoupleType",
                "Set the thermocouple type for a specific channel",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"channel\":{\"type\":\"integer\"},\"thermocoupleType\":{\"type\":\"string\"}},\"required\":[\"channel\",\"thermocoupleType\"]}"),
            (parameters, _, _) =>
            {
                var channel = (short)parameters.GetProperty("channel").GetInt32();
                var typeStr = parameters.GetProperty("thermocoupleType").GetString() ?? "K";
                if (!Enum.TryParse<ThermoCoupleType>(typeStr, out var tcType))
                    return Task.FromResult(CapabilityInvocationResult.Failed($"Unknown thermocouple type: {typeStr}"));

                var status = _pico.SetChannel(_handle, channel, tcType);
                if (status.State == DeviceResultState.Failure)
                    return Task.FromResult(CapabilityInvocationResult.Failed("Failed to set thermocouple type"));

                _channelToThermocoupleType[channel] = tcType;
                return Task.FromResult(CapabilityInvocationResult.Successful());
            });

        // SetChannelsThermocoupleType: sets thermocouple types for multiple channels
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "SetChannelsThermocoupleType",
                "Set thermocouple types for multiple channels",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"channels\":{\"type\":\"object\"}},\"required\":[\"channels\"]}"),
            (parameters, _, _) =>
            {
                var channelDict = new Dictionary<short, ThermoCoupleType>();
                foreach (var prop in parameters.GetProperty("channels").EnumerateObject())
                {
                    if (short.TryParse(prop.Name, out var ch) &&
                        Enum.TryParse<ThermoCoupleType>(prop.Value.GetString(), out var tc))
                    {
                        channelDict[ch] = tc;
                    }
                }

                var responses = _pico.SetChannels(_handle, channelDict);
                var hasFailure = responses.Any(r => r.State == DeviceResultState.Failure);

                // Update successfully set channels
                var i = 0;
                foreach (var (ch, tc) in channelDict)
                {
                    if (i < responses.Count && responses[i].State == DeviceResultState.Success)
                        _channelToThermocoupleType[ch] = tc;
                    i++;
                }

                return Task.FromResult(hasFailure
                    ? CapabilityInvocationResult.Failed("One or more thermocouple channels could not be set")
                    : CapabilityInvocationResult.Successful());
            });

        // GetThermocoupleInformation: returns channel-to-type mapping
        builder.RegisterSimple(
            "GetThermocoupleInformation",
            "Get the thermocouple type configuration for all channels",
            _ =>
            {
                var info = _channelToThermocoupleType.ToDictionary(
                    kvp => kvp.Key.ToString(),
                    kvp => kvp.Value.ToString());
                return Task.FromResult(CapabilityInvocationResult.Successful(
                    JsonSerializer.SerializeToElement(info)));
            });

        // GetUniqueId: returns device serial number from formatted info
        builder.RegisterSimple(
            "GetUniqueId",
            "Get the serial number of the active TC-08 device",
            _ =>
            {
                var formattedInfo = _pico.GetDeviceInfo(_handle);
                var parts = formattedInfo.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5)
                    return Task.FromResult(CapabilityInvocationResult.Failed("Cannot get unique information on device"));
                var serial = parts[4].Split(':').Last().Trim();
                return Task.FromResult(CapabilityInvocationResult.Successful(
                    JsonSerializer.SerializeToElement(serial)));
            });

        // GetDeviceInfo: returns formatted device information
        builder.RegisterSimple(
            "GetDeviceInfo",
            "Get formatted device information for the active TC-08",
            _ =>
            {
                var info = _pico.GetDeviceInfo(_handle);
                return Task.FromResult(CapabilityInvocationResult.Successful(
                    JsonSerializer.SerializeToElement(info)));
            });
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            (DeviceResult Result, float[] Data, PicoTempUnit TempUnit) reading = default;
            try
            {
                reading = _pico.GetData(_handle);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "PicoTc08: failed to poll GetData");
            }

            if (reading.Result?.State == DeviceResultState.Success && reading.Data is not null)
            {
                var timestamp = DateTimeOffset.UtcNow;
                for (var chan = 0; chan < Usbtc08MaxChannels; chan++)
                {
                    yield return new TrendSample
                    {
                        TrendName = $"picotc08.temperature.ch{chan + 1}",
                        Value = (double)reading.Data[chan],
                        Timestamp = timestamp,
                        Unit = "C"
                    };
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
