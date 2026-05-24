using System.Collections.Concurrent;
using Helix.Orchestration.Core.Capabilities;
using Helix.Orchestration.Core.Experiment;
using Helix.Orchestration.Core.Leasing;
using Helix.Orchestration.Core.Runspace;
using Helix.Orchestration.Core.WSL;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Agent.Runspaces;

/// <summary>
/// Coordinates creation and lifecycle control for active runspaces.
/// </summary>
public sealed class RunspaceManager(
    IDeviceLeasingService leasingService,
    ICapabilityInvoker capabilityInvoker,
    ILogger<RunspaceManager> logger,
    ILogger<DurableRunspace> runspaceLogger,
    ILogger<DefinitionCompiler> definitionCompilerLogger)
{
    private readonly ConcurrentDictionary<string, IRunspace> _runspaces = new();

    private static class LogEvents
    {
        public static readonly EventId RunspaceSubmitted = new(2500, nameof(RunspaceSubmitted));
        public static readonly EventId RunspacePauseRequested = new(2501, nameof(RunspacePauseRequested));
        public static readonly EventId RunspaceResumeRequested = new(2502, nameof(RunspaceResumeRequested));
        public static readonly EventId RunspaceAbortRequested = new(2503, nameof(RunspaceAbortRequested));
        public static readonly EventId RunspaceStatusQueried = new(2504, nameof(RunspaceStatusQueried));
    }

    /// <summary>
    /// Submits and starts a new runspace for the supplied definition.
    /// </summary>
    public async Task<IRunspace> SubmitAsync(RunspaceDefinition definition, CancellationToken cancellationToken = default)
    {
        var context = new ExperimentContext(definition.DefinitionId, definition.Name);
        var runspace = new DurableRunspace(definition, context, leasingService, capabilityInvoker, definitionCompilerLogger, runspaceLogger);

        if (!_runspaces.TryAdd(runspace.RunspaceId, runspace))
            throw new InvalidOperationException($"Runspace {runspace.RunspaceId} already exists.");

        logger.LogInformation(LogEvents.RunspaceSubmitted, "Submitted runspace {RunspaceId} for definition {DefinitionId}", runspace.RunspaceId, definition.DefinitionId);
        await runspace.StartAsync(cancellationToken);
        return runspace;
    }

    /// <summary>
    /// Requests pause for a runspace if it exists.
    /// </summary>
    public async Task PauseAsync(string runspaceId, CancellationToken cancellationToken = default)
    {
        if (_runspaces.TryGetValue(runspaceId, out var runspace))
        {
            logger.LogInformation(LogEvents.RunspacePauseRequested, "Pause requested for runspace {RunspaceId}", runspaceId);
            await runspace.PauseAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Requests resume for a runspace if it exists.
    /// </summary>
    public async Task ResumeAsync(string runspaceId, CancellationToken cancellationToken = default)
    {
        if (_runspaces.TryGetValue(runspaceId, out var runspace))
        {
            logger.LogInformation(LogEvents.RunspaceResumeRequested, "Resume requested for runspace {RunspaceId}", runspaceId);
            await runspace.ResumeAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Requests abort for a runspace if it exists.
    /// </summary>
    public async Task AbortAsync(string runspaceId, CancellationToken cancellationToken = default)
    {
        if (_runspaces.TryGetValue(runspaceId, out var runspace))
        {
            logger.LogInformation(LogEvents.RunspaceAbortRequested, "Abort requested for runspace {RunspaceId}", runspaceId);
            await runspace.AbortAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Gets status for a runspace.
    /// </summary>
    public RunspaceStatus? GetStatus(string runspaceId)
    {
        var status = _runspaces.TryGetValue(runspaceId, out var runspace)
            ? runspace.Status
            : null;

        logger.LogInformation(LogEvents.RunspaceStatusQueried, "Status query for runspace {RunspaceId} returned {RunspaceStatus}", runspaceId, status?.ToString() ?? "NotFound");
        return status;
    }

    /// <summary>
    /// Returns all active runspaces.
    /// </summary>
    public IReadOnlyCollection<IRunspace> GetAll() => _runspaces.Values.ToArray();
}
