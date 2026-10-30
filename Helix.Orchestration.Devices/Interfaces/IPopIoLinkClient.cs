namespace Helix.Orchestration.Devices.Interfaces;

/// <summary>
/// Local copy of the POP IPopIoLinkClient interface. Abstracts the IO-Link HTTP transport for IFM master controllers.
/// </summary>
public interface IPopIoLinkClient
{
    /// <summary>Identifies all active sensors connected to the specified endpoint.</summary>
    Task<IEnumerable<(string DeviceId, byte Port)>> FindActiveSensorsFromEndPointAsync(string endPoint);

    /// <summary>Creates a session with the specified endpoint and port number.</summary>
    Task<bool> CreateSessionAsync(string endPoint, byte portNumber);

    /// <summary>Closes the current session.</summary>
    void CloseSession();

    /// <summary>Returns all variable definitions for the connected device.</summary>
    List<(string Id, ushort Index, double Gradient, double Offset)> GetAllVariablesForDevice();

    /// <summary>Gets variable data at the specified index.</summary>
    Task<(bool Success, object? Data)> GetVariableDataAtIndexAsync(ushort index);

    /// <summary>Gets all process data values for the connected device.</summary>
    Task<List<(string Name, object Value)>> GetAllProcessDataForDeviceAsync();

    /// <summary>Returns device identifier information.</summary>
    (string ProductId, uint DeviceId, ushort VendorId) GetDeviceIdentifiers();
}
