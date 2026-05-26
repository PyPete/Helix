using Helix.Orchestration.Core.WSL;

namespace Helix.Orchestration.SiteServer.Agent;

/// <summary>
/// Abstraction for Agent proxy — swappable between SimAgentProxy (in-process) and
/// real gRPC client (Phase 11+).
/// </summary>
public interface IAgentProxy
{
    /// <summary>
    /// Submits a compiled RunspaceDefinition for execution. Returns the runspace ID.
    /// </summary>
    Task<string> SubmitExperimentAsync(RunspaceDefinition definition, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the current status of a runspace.
    /// </summary>
    Task<RunspaceStatusDto> GetRunspaceStatusAsync(string runspaceId, CancellationToken ct = default);

    /// <summary>
    /// Requests graceful abort of a running runspace.
    /// </summary>
    Task AbortRunspaceAsync(string runspaceId, CancellationToken ct = default);

    /// <summary>
    /// Lists all registered devices (for Phase 9, returns sim device descriptors).
    /// </summary>
    IEnumerable<DeviceDescriptorDto> GetRegisteredDevices();
}

/// <summary>
/// DTO for runspace status response.
/// </summary>
public sealed record RunspaceStatusDto(
    string RunspaceId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt,
    string? FaultMessage
);

/// <summary>
/// DTO for device descriptor.
/// </summary>
public sealed record DeviceDescriptorDto(
    string DeviceId,
    string DeviceName,
    string DeviceKind,
    IReadOnlyList<string> Capabilities
);
