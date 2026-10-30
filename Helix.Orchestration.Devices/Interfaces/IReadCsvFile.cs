using System.Data;

namespace Helix.Orchestration.Devices.Interfaces;

/// <summary>
/// Local copy of the POP IReadCsvFile interface. Abstracts CSV reading for AtlasReactionSystem file I/O.
/// </summary>
public interface IReadCsvFile
{
    DataTable Read(string filePath);
}
