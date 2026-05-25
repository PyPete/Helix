using System.Collections.Concurrent;
using Helix.Orchestration.Core.Runspace;
using Helix.Orchestration.Core.WSL;
using Helix.Orchestration.Simulation;

namespace Helix.Orchestration.SiteServer.Agent;

/// <summary>
/// In-process agent proxy for Phase 9. Wraps InMemoryRunspace and SimulatedTrendProvider.
/// </summary>
public sealed class SimAgentProxy : IAgentProxy
{
    private readonly ConcurrentDictionary<string, IRunspace> _runspaces = new();
    private readonly SimulatedTrendProvider _trendProvider;

    public SimAgentProxy(SimulatedTrendProvider trendProvider)
    {
        _trendProvider = trendProvider;
    }

    public Task<string> SubmitExperimentAsync(RunspaceDefinition definition, CancellationToken ct = default)
    {
        // Create an InMemoryRunspace with a stub body (Phase 9: no real workflow execution)
        var runspace = new InMemoryRunspace(definition, async token =>
        {
            // Stub: simulate workflow execution by waiting 2 seconds
            await Task.Delay(TimeSpan.FromSeconds(2), token);
        });

        _runspaces[runspace.RunspaceId] = runspace;

        // Start the runspace immediately
        _ = runspace.StartAsync(ct);

        return Task.FromResult(runspace.RunspaceId);
    }

    public Task<RunspaceStatusDto> GetRunspaceStatusAsync(string runspaceId, CancellationToken ct = default)
    {
        if (!_runspaces.TryGetValue(runspaceId, out var runspace))
        {
            throw new KeyNotFoundException($"Runspace '{runspaceId}' not found.");
        }

        var dto = new RunspaceStatusDto(
            RunspaceId: runspace.RunspaceId,
            Status: runspace.Status.ToString(),
            CreatedAt: runspace.CreatedAt,
            StartedAt: runspace.StartedAt,
            EndedAt: runspace.EndedAt,
            FaultMessage: runspace.Fault?.Message
        );

        return Task.FromResult(dto);
    }

    public async Task AbortRunspaceAsync(string runspaceId, CancellationToken ct = default)
    {
        if (!_runspaces.TryGetValue(runspaceId, out var runspace))
        {
            throw new KeyNotFoundException($"Runspace '{runspaceId}' not found.");
        }

        await runspace.AbortAsync(ct);
    }

    public IEnumerable<DeviceDescriptorDto> GetRegisteredDevices()
    {
        // Phase 9: return simulated device descriptors
        return new[]
        {
            new DeviceDescriptorDto(
                DeviceId: "sim-reactor-001",
                DeviceName: "Simulated Reactor",
                DeviceKind: "Reactor",
                Capabilities: new[] { "SetTemperature", "GetTemperature", "GetPressure" }
            ),
            new DeviceDescriptorDto(
                DeviceId: "sim-pump-001",
                DeviceName: "Simulated Pump",
                DeviceKind: "Pump",
                Capabilities: new[] { "SetFlowRate", "GetFlowRate" }
            ),
            new DeviceDescriptorDto(
                DeviceId: "sim-spectrometer-001",
                DeviceName: "Simulated MIR Spectrometer",
                DeviceKind: "Spectrometer",
                Capabilities: new[] { "AcquireSpectrum", "GetLatestSpectrum" }
            )
        };
    }
}
