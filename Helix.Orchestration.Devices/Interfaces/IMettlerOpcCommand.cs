namespace Helix.Orchestration.Devices.Interfaces;

/// <summary>
/// OPC connection state returned by <see cref="IMettlerOpcCommand.CreateSession"/>.
/// Declared locally from POP.Shared.DeviceEnums.OpcConnectionEnum.
/// </summary>
public enum OpcConnectionEnum
{
    Connected,
    NotConnected,
    Incorrect
}

/// <summary>
/// Delegate for OPC data-change subscription callbacks.
/// Declared locally because Helix does not reference Opc.UaFx — it uses OPCFoundation.NetStandard.Opc.Ua.Client.
/// </summary>
/// <param name="sender">The monitored item that raised the event.</param>
/// <param name="e">Event arguments (null when invoked from a test double).</param>
public delegate void OpcDataChangeReceivedEventHandler(object sender, object? e);

/// <summary>
/// Mettler-Toledo OPC-UA command abstraction used by EasyMax, Fbrm, and ReactIr drivers.
/// Declared locally from POP.Shared.DeviceInterfaces.IMettlerOpcCommand contract.
/// </summary>
public interface IMettlerOpcCommand
{
    /// <summary>Creates an OPC session to the given endpoint URL.</summary>
    (bool flag, OpcConnectionEnum connectionEnum, string reason) CreateSession(string endPoint);

    /// <summary>Closes the current OPC session.</summary>
    bool CloseSession();

    /// <summary>Reads a typed value from the specified OPC node.</summary>
    T GetAtNode<T>(string node);

    /// <summary>Calls an OPC method on the given object node.</summary>
    void CallMethod(string objectId, string methodId, params object[] parameters);

    /// <summary>Returns the names of connected devices beneath a parent node.</summary>
    IEnumerable<string> ConnectedDevices(string parentNode, string deviceName, bool stripBlank = true);

    /// <summary>Subscribes to data-change notifications on an OPC node.</summary>
    void SubscribeToNode(string node, OpcDataChangeReceivedEventHandler callback);

    /// <summary>Returns raw spectral data (x-axis and y-axis double arrays) for the given nodes.</summary>
    List<Tuple<double[], double[]>> ReturnRaw(IEnumerable<string> spectraNodes);

    /// <summary>Returns trend values (name, value, unit) from the specified trend node.</summary>
    List<(string name, double value, string unit)> GetTrendsAtNode(string trendNode);
}
