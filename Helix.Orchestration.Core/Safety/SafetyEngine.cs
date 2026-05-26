using System.Collections.Concurrent;

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
        if (!_interlocks.TryAdd(rule.RuleId, rule))
        {
            throw new ArgumentException($"Interlock rule '{rule.RuleId}' is already registered.", nameof(rule));
        }
    }

    /// <inheritdoc/>
    public void UnregisterInterlock(string ruleId)
    {
        _interlocks.TryRemove(ruleId, out _);
    }

    /// <inheritdoc/>
    public async Task<bool> EvaluateInterlocksAsync()
    {
        bool allPassed = true;

        foreach (var rule in _interlocks.Values)
        {
            var result = await rule.EvaluateAsync();
            if (!result && rule.Severity == InterlockSeverity.Critical)
            {
                allPassed = false;
            }
        }

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

            // Log or broadcast the emergency stop reason
            // In production, this would send alerts via MQTT, SignalR, or other notification systems
            Console.WriteLine($"[EMERGENCY STOP] {reason} at {DateTimeOffset.UtcNow}");
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

            Console.WriteLine($"[EMERGENCY STOP RESET] at {DateTimeOffset.UtcNow}");
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
}
