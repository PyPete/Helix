using Helix.Orchestration.Devices.Model;

namespace Helix.Orchestration.Devices.Interfaces;

/// <summary>
/// Local copy of the POP IPicoApi interface. Abstracts the usbtc08.dll P/Invoke layer for PicoTech TC-08 thermocouples.
/// </summary>
public interface IPicoApi
{
    /// <summary>Opens all connected TC-08 devices and returns their handles.</summary>
    List<short> FindAndOpenDevices();

    /// <summary>Returns formatted device information for the given handle.</summary>
    string GetDeviceInfo(short handle);

    /// <summary>Sets the thermocouple type for a channel.</summary>
    DeviceResult SetChannel(short handle, short channel, ThermoCoupleType thermoCoupleType);

    /// <summary>Sets multiple channel thermocouple types.</summary>
    List<DeviceResult> SetChannels(short handle, Dictionary<short, ThermoCoupleType> channelTypes);

    /// <summary>Stops and releases a device handle.</summary>
    DeviceResult StopDevice(short handle);

    /// <summary>Gets a single temperature reading from all channels (9-element float array: index 0 = cold junction, 1-8 = channels).</summary>
    (DeviceResult Result, float[] Data, PicoTempUnit TempUnit) GetData(short handle);
}
