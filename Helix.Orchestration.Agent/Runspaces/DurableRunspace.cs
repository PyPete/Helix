using System.Diagnostics;
using Helix.Orchestration.Agent.Telemetry;
using Helix.Orchestration.Core.Capabilities;
using Helix.Orchestration.Core.Experiment;
using Helix.Orchestration.Core.Leasing;
using Helix.Orchestration.Core.Runspace;
using Helix.Orchestration.Core.WSL;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Agent.Runspaces;

/// <summary>
/// Runtime runspace implementation that executes a compiled runspace definition against an experiment context.
/// </summary>
public sealed class DurableRunspace : IRunspace
{
    private readonly IExperimentContext _experimentContext;
    private readonly ICapabilityInvoker _capabilityInvoker;
    private readonly ILogger<DefinitionCompiler> _definitionCompilerLogger;
    private readonly ILogger<DurableRunspace> _logger;
    private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private CancellationTokenSource? _cts;

    private static class LogEvents
    {
        public static readonly EventId RunspaceStarting = new(2300, nameof(RunspaceStarting));
        public static readonly EventId RunspaceCompleted = new(2301, nameof(RunspaceCompleted));
        public static readonly EventId RunspaceCanceled = new(2302, nameof(RunspaceCanceled));
        public static readonly EventId RunspaceFaulted = new(2303, nameof(RunspaceFaulted));
        public static readonly EventId RunspacePaused = new(2304, nameof(RunspacePaused));
        public static readonly EventId RunspaceResumed = new(2305, nameof(RunspaceResumed));
        public static readonly EventId RunspaceAborting = new(2306, nameof(RunspaceAborting));
    }

    /// <summary>Unique runspace identifier.</summary>
    public string RunspaceId { get; } = Guid.NewGuid().ToString("N");

    /// <summary>Compiled runspace definition being executed.</summary>
    public RunspaceDefinition Definition { get; }

    /// <summary>Current runspace status.</summary>
    public RunspaceStatus Status { get; private set; } = RunspaceStatus.Idle;

    /// <summary>Creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;

    /// <summary>Execution start timestamp.</summary>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>Execution end timestamp.</summary>
    public DateTimeOffset? EndedAt { get; private set; }

    /// <summary>Terminal fault captured during execution, if any.</summary>
    public Exception? Fault { get; private set; }

    /// <summary>
    /// Creates a durable runspace runtime instance.
    /// </summary>
    public DurableRunspace(
        RunspaceDefinition definition,
        IExperimentContext experimentContext,
        IDeviceLeasingService leasingService,
        ICapabilityInvoker capabilityInvoker,
        ILogger<DefinitionCompiler> definitionCompilerLogger,
        ILogger<DurableRunspace> logger)
    {
        Definition = definition;
        _experimentContext = experimentContext;
        _capabilityInvoker = capabilityInvoker;
        _definitionCompilerLogger = definitionCompilerLogger;
        _logger = logger;
    }

    /// <summary>
    /// Starts runspace execution.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (Status != RunspaceStatus.Idle)
            throw new InvalidOperationException($"Cannot start runspace in state {Status}.");

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Status = RunspaceStatus.Running;
        StartedAt = DateTimeOffset.UtcNow;

        _logger.LogInformation(LogEvents.RunspaceStarting, "Starting runspace {RunspaceId} for definition {DefinitionId}", RunspaceId, Definition.DefinitionId);

        _ = ExecuteAsync(_cts.Token);
        return Task.CompletedTask;
    }

    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var activity = HelixTelemetry.AgentActivitySource.StartActivity("runspace.execute", ActivityKind.Internal);
        activity?.SetTag("helix.runspace.id", RunspaceId);
        activity?.SetTag("helix.definition.id", Definition.DefinitionId);

        try
        {
            await _experimentContext.TransitionToAsync(ExperimentLifecycle.Running);
            var compiler = new DefinitionCompiler(_capabilityInvoker, _experimentContext, _definitionCompilerLogger);
            var executor = compiler.Compile(Definition);
            await executor(cancellationToken);
            await _experimentContext.TransitionToAsync(ExperimentLifecycle.Completed);
            Status = RunspaceStatus.Completed;

            HelixTelemetry.RunspaceCompletions.Add(1,
                KeyValuePair.Create<string, object?>("runspace.id", RunspaceId));

            _logger.LogInformation(LogEvents.RunspaceCompleted, "Runspace {RunspaceId} completed successfully", RunspaceId);
        }
        catch (OperationCanceledException oce)
        {
            Fault = oce;
            await _experimentContext.TransitionToAsync(ExperimentLifecycle.Failed);
            Status = RunspaceStatus.Faulted;

            HelixTelemetry.RunspaceFaults.Add(1,
                KeyValuePair.Create<string, object?>("runspace.id", RunspaceId),
                KeyValuePair.Create<string, object?>("fault.kind", "canceled"));

            _logger.LogError(LogEvents.RunspaceCanceled, oce, "Runspace {RunspaceId} canceled", RunspaceId);
        }
        catch (Exception ex)
        {
            Fault = ex;
            await _experimentContext.TransitionToAsync(ExperimentLifecycle.Failed);
            Status = RunspaceStatus.Faulted;

            HelixTelemetry.RunspaceFaults.Add(1,
                KeyValuePair.Create<string, object?>("runspace.id", RunspaceId),
                KeyValuePair.Create<string, object?>("fault.kind", "exception"));

            _logger.LogError(LogEvents.RunspaceFaulted, ex, "Runspace {RunspaceId} faulted", RunspaceId);
        }
        finally
        {
            EndedAt = DateTimeOffset.UtcNow;
            _completion.TrySetResult();
        }
    }

    /// <summary>
    /// Pauses runspace execution.
    /// </summary>
    public Task PauseAsync(CancellationToken cancellationToken = default)
    {
        if (Status == RunspaceStatus.Running)
        {
            Status = RunspaceStatus.Paused;
            _logger.LogInformation(LogEvents.RunspacePaused, "Runspace {RunspaceId} paused", RunspaceId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Resumes runspace execution.
    /// </summary>
    public Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        if (Status == RunspaceStatus.Paused)
        {
            Status = RunspaceStatus.Running;
            _logger.LogInformation(LogEvents.RunspaceResumed, "Runspace {RunspaceId} resumed", RunspaceId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Aborts runspace execution and waits for terminal completion.
    /// </summary>
    public async Task AbortAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(LogEvents.RunspaceAborting, "Aborting runspace {RunspaceId}", RunspaceId);

        if (_cts is not null)
            await _cts.CancelAsync();

        await WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Waits for runspace terminal completion.
    /// </summary>
    public Task WaitAsync(CancellationToken cancellationToken = default) => _completion.Task.WaitAsync(cancellationToken);

    /// <summary>
    /// Disposes runspace resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (Status is RunspaceStatus.Running or RunspaceStatus.Paused)
            await AbortAsync();

        _cts?.Dispose();
    }
}
