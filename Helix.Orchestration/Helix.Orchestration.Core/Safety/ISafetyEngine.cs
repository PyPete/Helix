namespace Helix.Orchestration.Core.Safety;

/// <summary>
/// Defines the contract for the safety engine.
/// Manages interlocks, unsafe condition detection, and emergency stop handling.
/// </summary>
public interface ISafetyEngine
{
    /// <summary>
    /// Registers an interlock rule.
    /// </summary>
    /// <param name="rule">The interlock rule to register.</param>
    void RegisterInterlock(InterlockRule rule);

    /// <summary>
    /// Unregisters an interlock rule.
    /// </summary>
    /// <param name="ruleId">The unique identifier of the interlock rule to remove.</param>
    void UnregisterInterlock(string ruleId);

    /// <summary>
    /// Evaluates all registered interlock rules.
    /// Returns true if all interlocks pass (safe to proceed), false if any interlock fails.
    /// </summary>
    Task<bool> EvaluateInterlocksAsync();

    /// <summary>
    /// Gets all interlock violations (rules that evaluated to false).
    /// </summary>
    Task<IEnumerable<InterlockRule>> GetViolationsAsync();

    /// <summary>
    /// Triggers an emergency stop.
    /// This cancels all running workflows and raises safety alerts.
    /// </summary>
    /// <param name="reason">Reason for the emergency stop.</param>
    Task TriggerEmergencyStopAsync(string reason);

    /// <summary>
    /// Checks if an emergency stop is currently active.
    /// </summary>
    bool IsEmergencyStopActive { get; }

    /// <summary>
    /// Resets the emergency stop condition (requires operator authorization).
    /// </summary>
    Task ResetEmergencyStopAsync();

    /// <summary>
    /// Gets the emergency stop cancellation token.
    /// This token is canceled when an emergency stop is triggered.
    /// </summary>
    CancellationToken EmergencyStopToken { get; }
}
