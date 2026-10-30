// TODO: Add usbtc08.dll to native binaries to enable runtime P/Invoke.
// Without the DLL present at runtime, calls will throw DllNotFoundException.
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Helix.Orchestration.Devices.Interfaces;
using Helix.Orchestration.Devices.Model;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// IPicoApi implementation wrapping PicoTc08LowLevelApiImports (P/Invoke).
/// This is NOT a DeviceAdapterBase — it is the native integration layer for usbtc08.dll.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class PicoTc08LowLevelWrapper : IPicoApi
{
    private const int PicoOk = 1;

    public List<short> FindAndOpenDevices()
    {
        var deviceHandles = new List<short>();
        var handle = PicoTc08LowLevelApiImports.TC08OpenUnit();
        while (handle > 0)
        {
            deviceHandles.Add(handle);
            handle = PicoTc08LowLevelApiImports.TC08OpenUnit();
        }
        return deviceHandles;
    }

    public DeviceResult SetChannel(short handle, short channel, ThermoCoupleType thermoCoupleType)
    {
        var ret = PicoTc08LowLevelApiImports.TC08SetChannel(handle, channel, (char)thermoCoupleType);
        return ret == PicoOk
            ? DeviceResult.Ok()
            : DeviceResult.Fail($"Channel {channel} not set correctly");
    }

    public List<DeviceResult> SetChannels(short handle, Dictionary<short, ThermoCoupleType> channelTypes)
    {
        var results = new List<DeviceResult>();
        foreach (var (channel, type) in channelTypes)
            results.Add(SetChannel(handle, channel, type));
        return results;
    }

    public DeviceResult StopDevice(short handle)
    {
        var ret = PicoTc08LowLevelApiImports.TC08Stop(handle);
        return ret == PicoOk
            ? DeviceResult.Ok()
            : DeviceResult.Warn("Device may not have closed properly");
    }

    public unsafe (DeviceResult Result, float[] Data, PicoTempUnit TempUnit) GetData(short handle)
    {
        var tempBuffer = new float[9];
        short overflow;
        var status = PicoTc08LowLevelApiImports.TC08GetSingle(
            handle, tempBuffer, &overflow, PicoTempUnit.Usbtc08UnitsCentigrade);
        return status != PicoOk
            ? (DeviceResult.Fail("Could not get data"), [], PicoTempUnit.Usbtc08UnitsCentigrade)
            : (DeviceResult.Ok(), tempBuffer, PicoTempUnit.Usbtc08UnitsCentigrade);
    }

    public string GetDeviceInfo(short handle)
    {
        var info = new StringBuilder(256);
        var status = PicoTc08LowLevelApiImports.TC08GetFormattedInfo(handle, info, 256);
        return status == PicoOk ? info.ToString() : string.Empty;
    }
}

/// <summary>
/// Internal P/Invoke declarations for the usbtc08.dll native library.
/// </summary>
[SupportedOSPlatform("windows")]
internal static unsafe class PicoTc08LowLevelApiImports
{
    private const string DriverFilename = "usbtc08.dll";

    [DllImport(DriverFilename, EntryPoint = "usb_tc08_open_unit")]
    public static extern short TC08OpenUnit();

    [DllImport(DriverFilename, EntryPoint = "usb_tc08_get_formatted_info")]
    public static extern short TC08GetFormattedInfo(short handle, StringBuilder unitInfo, short stringLength);

    [DllImport(DriverFilename, EntryPoint = "usb_tc08_get_single")]
    public static extern short TC08GetSingle(short handle, float[] temp, short* overflowFlags, PicoTempUnit units);

    [DllImport(DriverFilename, EntryPoint = "usb_tc08_stop")]
    public static extern short TC08Stop(short handle);

    [DllImport(DriverFilename, EntryPoint = "usb_tc08_set_channel")]
    public static extern short TC08SetChannel(short handle, short channel, char tcType);
}
