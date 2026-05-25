using Helix.Orchestration.Devices.Transport;
using Opc.UaFx.Client;

namespace Helix.Orchestration.Agent.Transport;

public sealed class OpcTransport : IOpcTransport, IDisposable
{
    private OpcClient? _client;

    public bool IsConnected => _client?.State == OpcClientState.Connected;

    public (bool Success, string Reason) Connect(string endpoint)
    {
        try
        {
            _client = new OpcClient(endpoint);
            _client.Connect();
            return IsConnected
                ? (true, string.Empty)
                : (false, "Client failed to connect.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public void Disconnect()
    {
        _client?.Disconnect();
    }

    public T Read<T>(string nodeId)
    {
        if (_client is null)
            throw new InvalidOperationException("OPC client is not initialized.");

        var value = _client.ReadNode(nodeId).Value;
        if (value is T typed)
            return typed;

        return (T)Convert.ChangeType(value, typeof(T));
    }

    public void Write<T>(string nodeId, T value)
    {
        if (_client is null)
            throw new InvalidOperationException("OPC client is not initialized.");

        _client.WriteNode(nodeId, value);
    }

    public IEnumerable<string> BrowseChildren(string parentNodeId)
    {
        if (_client is null)
            throw new InvalidOperationException("OPC client is not initialized.");

        var node = _client.BrowseNode(parentNodeId);
        return node.Children().Select(c => c.DisplayName.ToString());
    }

    public void Dispose()
    {
        Disconnect();
        _client?.Dispose();
    }
}
