using Helix.Orchestration.Agent.Runspaces;
using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Core.Leasing;
using Helix.Orchestration.Core.Runspace;
using Helix.Orchestration.Core.WSL;

namespace Helix.Orchestration.Agent.Execution;

/// <summary>
/// Bridges a WSL authoring document into runtime execution.
/// </summary>
public sealed class WslExecutionAdapter
{
    private readonly IDeviceLeasingService _leasingService;
    private readonly IRunspaceManager _runspaceManager;
    private readonly IWorkflowExecutor _workflowExecutor;

    public WslExecutionAdapter(
        IDeviceLeasingService leasingService,
        IRunspaceManager runspaceManager,
        IWorkflowExecutor workflowExecutor)
    {
        _leasingService = leasingService ?? throw new ArgumentNullException(nameof(leasingService));
        _runspaceManager = runspaceManager ?? throw new ArgumentNullException(nameof(runspaceManager));
        _workflowExecutor = workflowExecutor ?? throw new ArgumentNullException(nameof(workflowExecutor));
    }

    /// <summary>
    /// Compiles and executes a WSL document as a tracked durable runspace.
    /// </summary>
    public async Task<IRunspace> ExecuteAsync(
        WslDocument document,
        string holderName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(holderName);

        var definition = document.Compile();
        var leases = new List<DeviceLease>();

        try
        {
            foreach (var deviceId in GetDeviceIds(definition))
            {
                var lease = await _leasingService.AcquireAsync(
                    deviceId,
                    holderName,
                    LeaseMode.Exclusive,
                    ttl: TimeSpan.FromMinutes(30),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                leases.Add(lease);
            }

            var runspace = new DurableRunspace(definition, _workflowExecutor);
            _runspaceManager.Track(runspace);

            await runspace.StartAsync(cancellationToken).ConfigureAwait(false);
            return runspace;
        }
        catch
        {
            foreach (var lease in leases)
                await lease.DisposeAsync().ConfigureAwait(false);

            throw;
        }
    }

    private static IEnumerable<string> GetDeviceIds(RunspaceDefinition definition)
    {
        foreach (var node in definition.Nodes)
        {
            if (!node.Parameters.TryGetValue("deviceId", out var value))
                continue;

            if (value is string deviceId && !string.IsNullOrWhiteSpace(deviceId))
                yield return deviceId;
        }
    }
}
