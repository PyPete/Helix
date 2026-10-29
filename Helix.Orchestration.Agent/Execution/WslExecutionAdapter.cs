using Helix.Orchestration.Core.Runspace;
using Helix.Orchestration.Core.WSL;
using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Core.Leasing;

namespace Helix.Orchestration.Agent.Execution;

/// <summary>Retired compatibility entry point for direct WSL execution.</summary>
public sealed class WslExecutionAdapter
{
    /// <summary>
    /// Retained solely for source compatibility with callers that constructed the
    /// legacy adapter. Dependencies are deliberately not retained or used.
    /// </summary>
    public WslExecutionAdapter(
        IDeviceLeasingService leasingService,
        IRunspaceManager runspaceManager,
        IWorkflowExecutor workflowExecutor)
    {
        ArgumentNullException.ThrowIfNull(leasingService);
        ArgumentNullException.ThrowIfNull(runspaceManager);
        ArgumentNullException.ThrowIfNull(workflowExecutor);
    }

    /// <summary>Rejects the retired direct WSL execution path.</summary>
    /// <remarks>
    /// A compiled document and locally acquired leases are not command authority.
    /// Execution must begin through the canonical experiment submission path, which
    /// creates a command intent and repeats admission at the dispatch fence. Keeping
    /// this API fail-closed prevents callers of the legacy adapter from creating a
    /// second executor that can reach device capabilities without that boundary.
    /// </remarks>
    public Task<IRunspace> ExecuteAsync(
        WslDocument document,
        string holderName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(holderName);

        return Task.FromException<IRunspace>(new NotSupportedException(
            "Direct WSL execution is disabled. Submit the workflow through the canonical experiment admission path."));
    }
}
