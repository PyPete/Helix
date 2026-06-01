namespace Helix.Orchestration.Devices.Interfaces;

/// <summary>
/// Local copy of the POP IFileInfo interface. Represents file metadata for AtlasReactionSystem file enumeration.
/// </summary>
public interface IFileInfo
{
    string FullName { get; }
    DateTime? LastWriteTime { get; }
}
