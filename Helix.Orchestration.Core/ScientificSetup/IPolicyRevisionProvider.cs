using System.Security.Cryptography;
using System.Text;

namespace Helix.Orchestration.Core.ScientificSetup;

/// <summary>Immutable scientific policy content; registering it is not approval.</summary>
public sealed record PolicyRevisionEnvelope(
    Guid RevisionId,
    Guid PolicyId,
    string Schema,
    byte[] CanonicalContent,
    string ContentHash,
    string CreatedBySid,
    string Source,
    DateTimeOffset RecordedAtUtc)
{
    public void Validate()
    {
        if (RevisionId == Guid.Empty || PolicyId == Guid.Empty || string.IsNullOrWhiteSpace(CreatedBySid) || string.IsNullOrWhiteSpace(Source))
            throw new ArgumentException("Policy identity, creator, and source are required.");
        if (!string.Equals(Schema, ScientificContentCanonicalizer.Schema, StringComparison.Ordinal) || CanonicalContent.Length == 0)
            throw new ArgumentException("Policy content must use the supported scientific schema.");
        var actualHash = Convert.ToHexString(SHA256.HashData(CanonicalContent)).ToLowerInvariant();
        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(actualHash), Encoding.ASCII.GetBytes(ContentHash)))
            throw new ArgumentException("Policy content hash does not match its exact bytes.");
    }
}

/// <summary>Authoritative approval evidence for one exact immutable policy revision.</summary>
public sealed record PolicyApprovalProvenance(
    Guid ApprovalId,
    Guid PolicyRevisionId,
    string PolicyContentHash,
    string PolicySchema,
    string ApprovingSid,
    string AuthorityId,
    string Scope,
    DateTimeOffset ApprovedAtUtc,
    DateTimeOffset ExpiresAtUtc);

public sealed record PolicyApprovalValidation(bool IsApproved, bool IsUnavailable, string? RejectionReason = null)
{
    public static PolicyApprovalValidation Rejected(string reason) => new(false, false, reason);
    public static PolicyApprovalValidation Unavailable(string reason) => new(false, true, reason);
    public static PolicyApprovalValidation Approved() => new(true, false);
}

/// <summary>External authority seam. Implementations, not callers, decide whether approval evidence is trusted.</summary>
public interface IPolicyApprovalAuthority
{
    Task<PolicyApprovalValidation> VerifyAsync(
        PolicyApprovalProvenance provenance,
        PolicyRevisionEnvelope policy,
        string actorSid,
        string scope,
        DateTimeOffset evaluatedAtUtc,
        CancellationToken cancellationToken = default);
}

/// <summary>Backend-only immutable policy-content and approved-resolution port.</summary>
public interface IPolicyRevisionProvider
{
    Task<PolicyRevisionEnvelope> RegisterAsync(PolicyRevisionEnvelope policy, CancellationToken cancellationToken = default);
    Task<PolicyRevisionEnvelope?> ResolveAsync(Guid revisionId, string actorSid, CancellationToken cancellationToken = default);
    Task<PolicyRevisionEnvelope> ResolveApprovedAsync(Guid revisionId, PolicyApprovalProvenance provenance, string actorSid, string scope, CancellationToken cancellationToken = default);
}
