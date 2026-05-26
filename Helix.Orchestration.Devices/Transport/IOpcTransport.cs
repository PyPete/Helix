using System.Collections.Generic;

namespace Helix.Orchestration.Devices.Transport;

/// <summary>
/// Abstracts OPC UA communication for device drivers without exposing SDK-specific types.
/// </summary>
public interface IOpcTransport
{
    bool IsConnected { get; }

    (bool Success, string Reason) Connect(string endpoint);

    void Disconnect();

    T Read<T>(string nodeId);

    void Write<T>(string nodeId, T value);

    IEnumerable<string> BrowseChildren(string parentNodeId);
}
