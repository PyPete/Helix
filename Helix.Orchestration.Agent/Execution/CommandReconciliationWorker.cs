using Helix.Orchestration.Core.Execution;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Agent.Execution;

/// <summary>
/// Startup-only reconciliation for command starts without a durable outcome.
/// It observes independently persisted simulator state and never replays a
/// mutation. An unreadable or non-matching observation remains held.
/// </summary>
public sealed class CommandReconciliationWorker(
    ICommandLedger ledger,
    ISimulatorObservation observations,
    ExecutionControlService controls,
    ILogger<CommandReconciliationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ReconcileAsync(stoppingToken);
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    /// <summary>Exposed for recovery tests; production invokes it once at hosted-service startup.</summary>
    public async Task ReconcileAsync(CancellationToken cancellationToken = default)
    {
        var incomplete = await ledger.GetIncompleteDispatchesAsync(cancellationToken);
        foreach (var dispatch in incomplete)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var intent = await ledger.GetAsync(dispatch.CommandId, cancellationToken);
            if (intent is null)
            {
                logger.LogError("Dispatch {CommandId} has no immutable command intent; retaining recovery hold.", dispatch.CommandId);
                continue;
            }

            // Install the durable visible hold before touching observation. This
            // ordering prevents a restarted dispatcher from consuming resources.
            var hold = await controls.AddReconciliationHoldAsync(dispatch.ExecutionId, dispatch.CommandId, cancellationToken);
            if (!hold.Accepted && hold.Code == "control.aborted")
            {
                await ledger.HoldForReconciliationAsync(dispatch.CommandId, "Execution was aborted before recovery observation.", cancellationToken);
                continue;
            }

            SimulatorObservation? observed = null;
            string? unreadableReason = null;
            try
            {
                observed = await observations.GetAsync(intent.Intent.DeviceId, dispatch.Expectation.StateKey, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                unreadableReason = $"Simulator observation was unreadable: {exception.GetType().Name}.";
                logger.LogWarning(exception, "Cannot observe command {CommandId} during reconciliation.", dispatch.CommandId);
            }

            if (observed is not null && Matches(dispatch, intent.Intent.DeviceId, observed))
            {
                // Confirmation/recovery evidence precedes release of the hold.
                await ledger.ConfirmDispatchAsync(dispatch.CommandId, observed, recovered: true, cancellationToken);
                var release = await controls.ReleaseReconciliationHoldAsync(dispatch.ExecutionId, dispatch.CommandId, cancellationToken);
                if (release.Accepted)
                    logger.LogWarning("RECOVERY CONFIRMED command {CommandId} from simulator generation {Generation}; only its recovery hold was released.",
                        dispatch.CommandId, observed.Generation);
                else
                    logger.LogWarning("RECOVERY CONFIRMED command {CommandId}, but durable state {State} retained its control hold.",
                        dispatch.CommandId, release.Snapshot.State);
                continue;
            }

            var reason = unreadableReason ?? DescribeMismatch(dispatch, observed);
            await ledger.HoldForReconciliationAsync(dispatch.CommandId, reason, cancellationToken);
            logger.LogWarning("Recovery retained hold for command {CommandId}: {Reason}", dispatch.CommandId, reason);
        }
    }

    private static bool Matches(CommandDispatchRecord dispatch, string deviceId, SimulatorObservation observed) =>
        string.Equals(observed.DeviceId, deviceId, StringComparison.Ordinal) &&
        string.Equals(observed.StateKey, dispatch.Expectation.StateKey, StringComparison.Ordinal) &&
        observed.LastAppliedCommandId == dispatch.CommandId &&
        string.Equals(observed.CanonicalValue, dispatch.Expectation.CanonicalValue, StringComparison.Ordinal) &&
        string.Equals(observed.Unit, dispatch.Expectation.Unit, StringComparison.Ordinal) &&
        observed.Generation > 0;

    private static string DescribeMismatch(CommandDispatchRecord dispatch, SimulatorObservation? observed) =>
        observed is null
            ? "No independent simulator observation exists for the uncertain command."
            : $"Simulator observation did not match command {dispatch.CommandId}, state key, canonical value, unit, and generation.";
}
