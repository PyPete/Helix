using Helix.Orchestration.Devices.Model;

namespace Helix.Orchestration.Devices.Interfaces;

/// <summary>
/// Local copy of the POP ICrystallineSqlClient interface. Abstracts the SQL Server transport for Crystalline reactors.
/// </summary>
public interface ICrystallineSqlClient
{
    bool CreateSession(string endPoint);
    bool CloseSession();
    bool SubscribeToReactor(string reactor);
    bool ReactorLive(string reactor);
    IEnumerable<(string TrendName, double Value, string Unit)> GetRuntimeDataForReactor(string reactor);
    (DeviceResult Result, byte[]? Image) GrabImage(string reactor);
    IReadOnlyList<(string Name, string Value)> GetDeviceInfo();
    IReadOnlyList<(string Name, string Value)> GetDeviceInfo(string reactor);
    Dictionary<int, (string Name, string Unit)> FindVariableNames(string reactor);
    IEnumerable<string> FindReactors();
}
