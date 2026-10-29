using Helix.Orchestration.Core.Execution;

namespace Helix.Orchestration.Core.Safety;

/// <summary>
/// Evaluates every required command-admission gate. Unknown safety, lease,
/// resource, budget, termination, or health state is fail-closed as a hold.
/// </summary>
public sealed class CommandAdmissionEvaluator
{
    private readonly TimeProvider _timeProvider;

    private static readonly string[] RequiredChecks =
    [
        "capability", "safety", "lease", "resource", "budget", "termination", "health"
    ];

    public CommandAdmissionEvaluator(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public CommandAdmissionResult Evaluate(CommandAdmissionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var occurredAtUtc = _timeProvider.GetUtcNow();
        var checks = RequiredChecks.Select(check => CreateEvidence(
            check,
            context.GetStatus(check),
            occurredAtUtc,
            context.ContextRevision)).ToArray();

        // An uncertain physical safety boundary must take precedence over an
        // otherwise-invalid proposal: the operator needs the hold reason before
        // deciding whether it is safe to correct and resubmit the proposal.
        var hasUnknown = checks.Any(check => check.Status == CommandCheckStatus.Unknown);
        var hasFailure = checks.Any(check => check.Status == CommandCheckStatus.Failed);
        var state = hasUnknown
            ? CommandAdmissionState.Held
            : hasFailure
                ? CommandAdmissionState.Rejected
                : CommandAdmissionState.Allowed;

        return new CommandAdmissionResult(state, checks);
    }

    private static CommandCheckEvidence CreateEvidence(
        string check,
        CommandCheckStatus status,
        DateTimeOffset occurredAtUtc,
        string contextRevision)
    {
        var code = status switch
        {
            CommandCheckStatus.Passed => $"{check}.passed",
            CommandCheckStatus.Failed => $"{check}.failed",
            _ => $"{check}.unknown"
        };

        var reason = status switch
        {
            CommandCheckStatus.Passed => $"{check} check passed.",
            CommandCheckStatus.Failed => $"{check} check failed.",
            _ => $"{check} state is unknown."
        };

        return new CommandCheckEvidence(check, status, code, reason, occurredAtUtc, contextRevision);
    }
}

public sealed record CommandAdmissionContext(
    IReadOnlyDictionary<string, CommandCheckStatus> CheckStatuses,
    string ContextRevision)
{
    public CommandCheckStatus GetStatus(string check) =>
        CheckStatuses.TryGetValue(check, out var status) ? status : CommandCheckStatus.Unknown;
}

public sealed record CommandAdmissionResult(
    CommandAdmissionState State,
    IReadOnlyList<CommandCheckEvidence> Checks);
