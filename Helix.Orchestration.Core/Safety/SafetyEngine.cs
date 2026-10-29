using System.Collections.Concurrent;
using Helix.Orchestration.Core.Execution;

namespace Helix.Orchestration.Core.Safety;

/// <summary>
/// Default implementation of ISafetyEngine.
/// Evaluates interlock rules and manages emergency stop conditions.
/// </summary>
public sealed class SafetyEngine : ISafetyEngine, IDisposable
{
    private readonly ConcurrentDictionary<string, InterlockRule> _interlocks = new();
    private CancellationTokenSource _emergencyStopCts = new();
    private bool _isEmergencyStopActive;
    private readonly object _emergencyStopLock = new();
    private long _stateRevision;
    private SafetyState _lastSafetyState = SafetyState.Unknown("No interlocks have been evaluated.", "0");
    private EmergencyStopTransition? _lastEmergencyStopTransition;

    /// <summary>
    /// Gets the last complete interlock evaluation. Consumers must require a
    /// fresh <see cref="SafetyState.OccurredAtUtc"/> before admitting a command.
    /// </summary>
    public SafetyState LastSafetyState => Volatile.Read(ref _lastSafetyState);

    /// <summary>
    /// Gets the most recent E-stop transition without inferring state from log output.
    /// </summary>
    public EmergencyStopTransition? LastEmergencyStopTransition =>
        Volatile.Read(ref _lastEmergencyStopTransition);

    /// <inheritdoc/>
    public bool IsEmergencyStopActive
    {
        get
        {
            lock (_emergencyStopLock)
            {
                return _isEmergencyStopActive;
            }
        }
    }

    /// <inheritdoc/>
    public CancellationToken EmergencyStopToken => _emergencyStopCts.Token;

    /// <inheritdoc/>
    public void RegisterInterlock(InterlockRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        if (!_interlocks.TryAdd(rule.RuleId, rule))
        {
            throw new ArgumentException($"Interlock rule '{rule.RuleId}' is already registered.", nameof(rule));
        }

        PublishSafetyState(SafetyState.Unknown("Interlock configuration changed and requires evaluation.", NextRevision()));
    }

    /// <inheritdoc/>
    public void UnregisterInterlock(string ruleId)
    {
        _interlocks.TryRemove(ruleId, out _);
        PublishSafetyState(SafetyState.Unknown("Interlock configuration changed and requires evaluation.", NextRevision()));
    }

    /// <inheritdoc/>
    public async Task<bool> EvaluateInterlocksAsync()
    {
        if (_interlocks.IsEmpty)
        {
            PublishSafetyState(SafetyState.Unknown("No interlock rules are registered.", NextRevision()));
            return false;
        }

        bool allPassed = true;
        var unknownReasons = new List<string>();
        var unsafeReasons = new List<string>();

        foreach (var rule in _interlocks.Values)
        {
            bool result;
            try
            {
                result = await rule.EvaluateAsync();
            }
            catch (Exception exception)
            {
                allPassed = false;
                unknownReasons.Add($"Interlock '{rule.RuleId}' could not be evaluated: {exception.Message}");
                continue;
            }

            if (!result && rule.Severity == InterlockSeverity.Critical)
            {
                allPassed = false;
                unsafeReasons.Add(rule.Description);
            }
        }

        var revision = NextRevision();
        var state = unknownReasons.Count > 0
            ? SafetyState.Unknown(string.Join("; ", unknownReasons), revision)
            : unsafeReasons.Count > 0
                ? SafetyState.Unsafe(string.Join("; ", unsafeReasons), revision)
                : SafetyState.Safe(revision);
        PublishSafetyState(state);

        return allPassed;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<InterlockRule>> GetViolationsAsync()
    {
        var violations = _interlocks.Values
            .Where(r => r.LastResult.HasValue && !r.LastResult.Value)
            .ToList();

        return Task.FromResult<IEnumerable<InterlockRule>>(violations);
    }

    /// <inheritdoc/>
    public Task TriggerEmergencyStopAsync(string reason)
    {
        lock (_emergencyStopLock)
        {
            if (_isEmergencyStopActive)
            {
                return Task.CompletedTask; // Already in emergency stop
            }

            _isEmergencyStopActive = true;
            _emergencyStopCts.Cancel();
            var transition = new EmergencyStopTransition(true, reason, DateTimeOffset.UtcNow, NextRevision());
            Volatile.Write(ref _lastEmergencyStopTransition, transition);
            PublishSafetyState(SafetyState.Unsafe($"Emergency stop active: {reason}", transition.ContextRevision, transition.OccurredAtUtc));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ResetEmergencyStopAsync()
    {
        lock (_emergencyStopLock)
        {
            if (!_isEmergencyStopActive)
            {
                return Task.CompletedTask; // Not in emergency stop
            }

            _isEmergencyStopActive = false;

            // Create a new CancellationTokenSource for future emergency stops
            _emergencyStopCts.Dispose();
            _emergencyStopCts = new CancellationTokenSource();
            var transition = new EmergencyStopTransition(false, "Emergency stop reset.", DateTimeOffset.UtcNow, NextRevision());
            Volatile.Write(ref _lastEmergencyStopTransition, transition);
            PublishSafetyState(SafetyState.Unknown("Emergency stop reset requires a fresh interlock evaluation.", transition.ContextRevision, transition.OccurredAtUtc));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts a background task that continuously evaluates interlocks.
    /// </summary>
    /// <param name="evaluationInterval">Interval between evaluations.</param>
    /// <param name="cancellationToken">Cancellation token to stop the evaluation loop.</param>
    public async Task StartInterlockMonitoringAsync(TimeSpan evaluationInterval, CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var allPassed = await EvaluateInterlocksAsync();

            if (!allPassed)
            {
                var violations = await GetViolationsAsync();
                var criticalViolations = violations.Where(v => v.Severity == InterlockSeverity.Critical).ToList();

                if (criticalViolations.Any())
                {
                    var reasons = string.Join(", ", criticalViolations.Select(v => v.Description));
                    await TriggerEmergencyStopAsync($"Critical interlock violations: {reasons}");
                }
            }

            await Task.Delay(evaluationInterval, cancellationToken);
        }
    }

    public void Dispose()
    {
        _emergencyStopCts?.Dispose();
    }

    private string NextRevision() => Interlocked.Increment(ref _stateRevision).ToString(System.Globalization.CultureInfo.InvariantCulture);

    private void PublishSafetyState(SafetyState safetyState) => Volatile.Write(ref _lastSafetyState, safetyState);
}

/// <summary>Timestamped result of a complete interlock evaluation.</summary>
public sealed record SafetyState(
    CommandCheckStatus Status,
    string Reason,
    DateTimeOffset OccurredAtUtc,
    string ContextRevision)
{
    public static SafetyState Safe(string contextRevision, DateTimeOffset? occurredAtUtc = null) =>
        new(CommandCheckStatus.Passed, "All registered critical interlocks passed.", occurredAtUtc ?? DateTimeOffset.UtcNow, contextRevision);

    public static SafetyState Unsafe(string reason, string contextRevision, DateTimeOffset? occurredAtUtc = null) =>
        new(CommandCheckStatus.Failed, reason, occurredAtUtc ?? DateTimeOffset.UtcNow, contextRevision);

    public static SafetyState Unknown(string reason, string contextRevision, DateTimeOffset? occurredAtUtc = null) =>
        new(CommandCheckStatus.Unknown, reason, occurredAtUtc ?? DateTimeOffset.UtcNow, contextRevision);
}

/// <summary>Auditable E-stop state transition made synchronously with cancellation.</summary>
public sealed record EmergencyStopTransition(
    bool IsActive,
    string Reason,
    DateTimeOffset OccurredAtUtc,
    string ContextRevision);
