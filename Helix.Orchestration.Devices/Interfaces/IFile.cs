namespace Helix.Orchestration.Devices.Interfaces;

/// <summary>
/// Local copy of the POP IFile interface. Abstracts file reading for IfmMasterController configuration.
/// </summary>
public interface IFile
{
    string ReadAllText(string path);
    Task<string> ReadAllTextAsync(string path);
}
