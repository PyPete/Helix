namespace Helix.Orchestration.Devices.Model;

/// <summary>
/// Vapourtec R4 device status snapshot.
/// </summary>
/// <param name="IsRunning">Whether the device is currently powered on and running.</param>
/// <param name="HasFault">Whether a fault condition is active.</param>
/// <param name="FaultMessage">Optional fault message if HasFault is true.</param>
/// <param name="ReagentValvePosition">Current reagent valve position.</param>
/// <param name="SolventValvePosition">Current solvent valve position.</param>
/// <param name="Timestamp">Timestamp when the status was captured.</param>
public sealed record VapourtecStatus(
    bool IsRunning,
    bool HasFault,
    string? FaultMessage,
    int ReagentValvePosition,
    int SolventValvePosition,
    DateTimeOffset Timestamp);
