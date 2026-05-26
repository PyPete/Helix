using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Helix.Orchestration.Agent.Telemetry;

/// <summary>
/// Centralized OpenTelemetry primitives for the Helix Agent runtime.
/// </summary>
public static class HelixTelemetry
{
    public const string TelemetryName = "Helix.Orchestration.Agent";

    public static readonly ActivitySource AgentActivitySource = new(TelemetryName);
    public static readonly Meter AgentMeter = new(TelemetryName);

    public static readonly Counter<long> TrendsPublished = AgentMeter.CreateCounter<long>(
        name: "helix.agent.trends.published",
        unit: "count",
        description: "Number of trend messages published by the agent.");

    public static readonly Counter<long> CapabilityInvocations = AgentMeter.CreateCounter<long>(
        name: "helix.agent.capability.invocations",
        unit: "count",
        description: "Number of capability invocations attempted by the agent.");

    public static readonly Counter<long> RunspaceCompletions = AgentMeter.CreateCounter<long>(
        name: "helix.agent.runspace.completions",
        unit: "count",
        description: "Number of runspaces that completed successfully.");

    public static readonly Counter<long> RunspaceFaults = AgentMeter.CreateCounter<long>(
        name: "helix.agent.runspace.faults",
        unit: "count",
        description: "Number of runspaces that faulted.");

    public static readonly Counter<long> AdapterHealthFailures = AgentMeter.CreateCounter<long>(
        name: "helix.agent.adapters.health.failures",
        unit: "count",
        description: "Number of adapter health check failures observed by the agent.");
}
