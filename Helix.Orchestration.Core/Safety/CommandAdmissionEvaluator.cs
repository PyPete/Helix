using Helix.Orchestration.Core.Execution;

namespace Helix.Orchestration.Core.Safety;

/// <summary>
/// Evaluates every required command-admission gate. Unknown safety, lease,
/// resource, budget, termination, or health state is fail-closed as a hold.
/// </summary>
public sealed class CommandAdmissionEvaluator
{
    private static readonly string[] RequiredChecks =
    [
        "capability", "safety", "lease", "resource", "budget", "termination", "health"
    ];

    public CommandAdmissionResult Evaluate(CommandAdmissionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var occurredAtUtc = DateTimeOffset.UtcNow;
        var checks = RequiredChecks.Select(check => CreateEvidence(
            check,
            context.GetStatus(check),
            occurredAtUtc,
            context.ContextRevision)).ToArray();

        var state = checks.Any(check => check.Status == CommandCheckStatus.Failed)
            ? CommandAdmissionState.Rejected
            : checks.Any(check => check.Status == CommandCheckStatus.Unknown)
                ? CommandAdmissionState.Held
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
