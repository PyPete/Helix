using Helix.Orchestration.Devices.BaseClasses;
using Helix.Orchestration.Devices.Interfaces;
using Helix.Orchestration.Devices.Model;
using Helix.Orchestration.Devices.Transport;
using System.Globalization;

namespace Helix.Orchestration.Devices.Drivers;

public sealed class VapourtecR4(ISerialTransport transport, string? name = null)
    : SerialDeviceBase(transport, name), IVapourtec
{
    private readonly Dictionary<string, string> _messages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PowerOn"] = "PN",
        ["PowerOff"] = "PF",
        ["ForwardCommand"] = "FW",
        ["SwitchValves"] = "KP",
        ["PressureHistory"] = "HP",
        ["SetFlowRate"] = "FR",
        ["TempHistory"] = "HT",
        ["FlowHistory"] = "HF",
        ["Status"] = "GA",
        ["SetTemp"] = "ST",
        ["GetFirmware"] = "GV"
    };

    private const double MinFlowRate = 0.0;
    private const double MaxFlowRate = 9.99;

    protected override int BaudRate => 19200;

    public override DeviceKind DeviceKind => DeviceKind.Vapourtec;

    public override DeviceResult IsDevice()
    {
        try
        {
            var firmware = SendCommand(_messages["GetFirmware"], 300).Trim();
            if (string.IsNullOrWhiteSpace(firmware))
                return Failure("Device is not correct");

            var flowHistory = SendCommand(_messages["FlowHistory"], 300).Trim();
            if (string.IsNullOrWhiteSpace(flowHistory))
                return Failure("Device is not correct");

            return firmware.StartsWith('v') || firmware.StartsWith('V')
                ? Success()
                : Failure("Device is not correct");
        }
        catch (Exception ex)
        {
            return Failure($"Identity check failed: {ex.Message}");
        }
    }

    public override (DeviceResult Result, string UniqueIdentifier) GetUniqueId()
    {
        try
        {
            var firmware = SendCommand(_messages["GetFirmware"], 300).Trim();
            if (string.IsNullOrWhiteSpace(firmware))
                return (Warning("Can't get unique information on device"), string.Empty);

            return (Success(), $"vapourtec-r4:{firmware}");
        }
        catch
        {
            return (Warning("Can't get unique information on device"), string.Empty);
        }
    }

    public DeviceResult Start()
    {
        try
        {
            var result = SendCommand(_messages["PowerOn"], 350);
            SendCommand($"{_messages["ForwardCommand"]} {_messages["PowerOn"]}", 350);

            return result.StartsWith("OK", StringComparison.OrdinalIgnoreCase)
                ? Success()
                : Warning("Start may not have completed successfully");
        }
        catch (Exception ex)
        {
            return Failure($"Failed to start device: {ex.Message}");
        }
    }

    public DeviceResult Stop()
    {
        try
        {
            var result = SendCommand(_messages["PowerOff"], 350);
            SendCommand($"{_messages["ForwardCommand"]} {_messages["PowerOff"]}", 350);

            return result.StartsWith("OK", StringComparison.OrdinalIgnoreCase)
                ? Success()
                : Warning("Stop may not have completed successfully");
        }
        catch (Exception ex)
        {
            return Failure($"Failed to stop device: {ex.Message}");
        }
    }

    public DeviceResult SetFlowRate(int pump, double flowRate)
    {
        try
        {
            flowRate = Math.Clamp(flowRate, MinFlowRate, MaxFlowRate);
            var scaled = flowRate * 1000.0;

            var command = pump is 0 or 1
                ? $"{_messages["SetFlowRate"]} {pump} {scaled.ToString("0.###", CultureInfo.InvariantCulture)}"
                : $"{_messages["ForwardCommand"]} {_messages["SetFlowRate"]} {pump - 2} {scaled.ToString("0.###", CultureInfo.InvariantCulture)}";

            var response = SendCommand(command, 400);
            return response.StartsWith("OK", StringComparison.OrdinalIgnoreCase)
                ? Success()
                : Warning("Set Flow Rate may not have completed successfully");
        }
        catch (Exception ex)
        {
            return Failure($"Failed setting flow rate: {ex.Message}");
        }
    }

    public DeviceResult SetTemperature(int reactor, int target)
    {
        try
        {
            var response = SendCommand($"R4 {_messages["SetTemp"]} {reactor} {target}", 300);
            return response.StartsWith("OK", StringComparison.OrdinalIgnoreCase)
                ? Success()
                : Warning("Set Temperature may not have completed successfully");
        }
        catch (Exception ex)
        {
            return Failure($"Failed setting temperature: {ex.Message}");
        }
    }

    public DeviceResult SelectReagent(int channel)
    {
        try
        {
            var response = channel switch
            {
                0 => SendCommand($"{_messages["SwitchValves"]} 1", 300),
                1 => SendCommand($"{_messages["SwitchValves"]} 3", 300),
                2 => SendCommand($"{_messages["ForwardCommand"]} {_messages["SwitchValves"]} 1", 300),
                _ => SendCommand($"{_messages["ForwardCommand"]} {_messages["SwitchValves"]} 3", 300)
            };

            return response.StartsWith("OK", StringComparison.OrdinalIgnoreCase)
                ? Success()
                : Warning("Select Reagent may not have completed successfully");
        }
        catch (Exception ex)
        {
            return Failure($"Failed selecting reagent channel: {ex.Message}");
        }
    }

    public DeviceResult SelectSolvent(int channel)
    {
        try
        {
            var response = channel switch
            {
                0 => SendCommand($"{_messages["SwitchValves"]} 0", 300),
                1 => SendCommand($"{_messages["SwitchValves"]} 2", 300),
                2 => SendCommand($"{_messages["ForwardCommand"]} {_messages["SwitchValves"]} 0", 300),
                _ => SendCommand($"{_messages["ForwardCommand"]} {_messages["SwitchValves"]} 2", 300)
            };

            return response.StartsWith("OK", StringComparison.OrdinalIgnoreCase)
                ? Success()
                : Warning("Select Reagent may not have completed successfully");
        }
        catch (Exception ex)
        {
            return Failure($"Failed selecting solvent channel: {ex.Message}");
        }
    }

    public (DeviceResult Result, VapourtecStatus? Status) GetStatus()
    {
        try
        {
            var statusResponse = SendCommand(_messages["Status"], 300).Trim();
            if (string.IsNullOrWhiteSpace(statusResponse))
                return (Warning("Status may not have completed successfully"), null);

            var hasFault = statusResponse.Contains("ER", StringComparison.OrdinalIgnoreCase)
                           || statusResponse.Contains("FAULT", StringComparison.OrdinalIgnoreCase);
            var isRunning = !hasFault;

            var status = new VapourtecStatus(
                IsRunning: isRunning,
                HasFault: hasFault,
                FaultMessage: hasFault ? statusResponse : null,
                ReagentValvePosition: -1,
                SolventValvePosition: -1,
                Timestamp: DateTimeOffset.UtcNow);

            return (Success(), status);
        }
        catch (Exception ex)
        {
            return (Failure($"Failed to query device status: {ex.Message}"), null);
        }
    }

    public override IReadOnlyList<DeviceMeasurement> GetMeasurements()
    {
        var now = DateTime.UtcNow;
        var measurements = new List<DeviceMeasurement>();

        measurements.AddRange(GetTemperatureMeasurements(now));
        measurements.AddRange(GetFlowMeasurements(now));
        measurements.AddRange(GetPressureMeasurements(now));

        return measurements;
    }

    protected override IReadOnlyList<CommandComponent> GetCommands() =>
        _messages.Select(x => new CommandComponent(x.Key, x.Value)).ToList();

    private IEnumerable<DeviceMeasurement> GetTemperatureMeasurements(DateTime timestamp)
    {
        var response = SendCommand(_messages["TempHistory"], 400);
        if (string.IsNullOrWhiteSpace(response))
            return [];

        var sample = GetLastSample(response);
        if (sample.Length < 9)
            return [];

        var map = new (int Index, string Name)[]
        {
            (2, "Temperature_Reactor_A"),
            (4, "Temperature_Reactor_B"),
            (6, "Temperature_Reactor_C"),
            (8, "Temperature_Reactor_D")
        };

        var results = new List<DeviceMeasurement>();
        foreach (var (index, name) in map)
        {
            if (index >= sample.Length)
                continue;

            var value = sample[index].Trim();
            if (value is "-1000" or "-1000\r\n")
                continue;

            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var tempTenths))
                results.Add(new DeviceMeasurement(name, name, tempTenths / 10.0, "C", timestamp));
        }

        return results;
    }

    private IEnumerable<DeviceMeasurement> GetFlowMeasurements(DateTime timestamp)
    {
        var results = new List<DeviceMeasurement>();

        var mainResponse = SendCommand(_messages["FlowHistory"], 300);
        if (!string.IsNullOrWhiteSpace(mainResponse))
            results.AddRange(ParseFlowSample(mainResponse, isForwardUnit: false, timestamp));

        var forwardResponse = SendCommand($"{_messages["ForwardCommand"]} {_messages["FlowHistory"]}", 300);
        if (!string.IsNullOrWhiteSpace(forwardResponse))
            results.AddRange(ParseFlowSample(forwardResponse, isForwardUnit: true, timestamp));

        return results;
    }

    private IEnumerable<DeviceMeasurement> ParseFlowSample(string response, bool isForwardUnit, DateTime timestamp)
    {
        var sample = GetLastSample(response);
        if (sample.Length < 3)
            return [];

        var names = isForwardUnit
            ? new[] { "PumpC_FlowRate", "PumpD_FlowRate" }
            : new[] { "PumpA_FlowRate", "PumpB_FlowRate" };

        var results = new List<DeviceMeasurement>();
        for (var i = 0; i < 2; i++)
        {
            if (double.TryParse(sample[i + 1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var flowUlMin))
                results.Add(new DeviceMeasurement(names[i], names[i], flowUlMin / 1000.0, "ml/min", timestamp));
        }

        return results;
    }

    private IEnumerable<DeviceMeasurement> GetPressureMeasurements(DateTime timestamp)
    {
        var response = SendCommand(_messages["PressureHistory"], 400);
        if (string.IsNullOrWhiteSpace(response))
            return [];

        var sample = GetLastSample(response);
        if (sample.Length < 4)
            return [];

        var results = new List<DeviceMeasurement>();
        for (var i = 1; i <= 3; i++)
        {
            if (double.TryParse(sample[i].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var pressureMbar))
                results.Add(new DeviceMeasurement($"Pressure_{i}", $"Pressure_{i}", pressureMbar / 1000.0, "bar", timestamp));
        }

        return results;
    }

    private static string[] GetLastSample(string response)
    {
        if (response.Contains('&'))
            return response.Split('&', StringSplitOptions.RemoveEmptyEntries)[^1]
                .Split(',', StringSplitOptions.TrimEntries);

        var lines = response.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        return lines.Length == 0
            ? []
            : lines[0].Split(',', StringSplitOptions.TrimEntries);
    }
}
