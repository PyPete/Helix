namespace Helix.Orchestration.Devices.Interfaces;

/// <summary>
/// Local copy of the POP IDirectoryInfo interface. Abstracts directory enumeration for AtlasReactionSystem file I/O.
/// </summary>
public interface IDirectoryInfo
{
    bool CheckFolderExists(string path);
    IEnumerable<IFileInfo> GetFiles();
    void SetPath(string path);
}
