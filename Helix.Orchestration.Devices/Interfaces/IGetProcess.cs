namespace Helix.Orchestration.Devices.Interfaces;

/// <summary>
/// Local copy of the POP IGetProcess interface. Abstracts OS process inspection for AtlasReactionSystem.
/// </summary>
public interface IGetProcess
{
    (bool ProcessFound, string MainWindowTitle) FindProcessByName(string processName);
}
