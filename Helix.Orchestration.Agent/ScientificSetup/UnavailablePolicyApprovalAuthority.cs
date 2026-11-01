using Helix.Orchestration.Core.ScientificSetup;

namespace Helix.Orchestration.Agent.ScientificSetup;

/// <summary>Production has no authority to issue or accept policy approval in Phase 2.</summary>
public sealed class UnavailablePolicyApprovalAuthority : IPolicyApprovalAuthority
{
    public Task<PolicyApprovalValidation> VerifyAsync(
        PolicyApprovalProvenance provenance,
        PolicyRevisionEnvelope policy,
        string actorSid,
        string scope,
        DateTimeOffset evaluatedAtUtc,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(PolicyApprovalValidation.Unavailable("No authoritative policy approval provider is configured."));
}
