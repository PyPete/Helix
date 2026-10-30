using Helix.Orchestration.Core.Capabilities;
using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Core.Safety;

namespace Helix.Orchestration.Agent.Execution;

/// <summary>
/// Supplies the bounded, local state model for the simulator-only Agent composition.
/// Real device deployments must replace this source with an authority backed by their
/// policy, lease, capacity, and device-health services.
/// </summary>
public sealed class SimulatorAdmissionStateSource(CapabilityRegistry capabilities) : IAdmissionStateSource
{
    private const string ApprovedPolicyRevision = "simulation-policy-v1";

    public async Task<AdmissionStateSnapshot?> GetAsync(
        CommandIntent intent,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var capabilityAvailable = await capabilities
            .IsAvailableAsync(intent.Capability)
            .ConfigureAwait(false);
        var health = capabilityAvailable
            ? CommandCheckStatus.Passed
            : CommandCheckStatus.Failed;

        // The simulator has one local, preapproved policy envelope and no competing
        // resource owner. Its static controls remain explicit evidence rather than
        // bypassing the dispatcher or accepting an absent state source.
        return new AdmissionStateSnapshot(
            CommandCheckStatus.Passed,
            ApprovedPolicyRevision,
            CommandCheckStatus.Passed,
            CommandCheckStatus.Passed,
            CommandCheckStatus.Passed,
            CommandCheckStatus.Passed,
            health,
            DateTimeOffset.UtcNow,
            $"simulator:{ApprovedPolicyRevision}:health:{health}");
    }
}
