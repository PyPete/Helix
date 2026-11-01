using Dapper;
using Helix.Orchestration.Core.ScientificSetup;
using Npgsql;

namespace Helix.Orchestration.Infrastructure.Persistence;

public sealed class PostgresPolicyRevisionProvider(DbConnectionFactory factory, IPolicyApprovalAuthority authority) : IPolicyRevisionProvider
{
    public async Task<PolicyRevisionEnvelope> RegisterAsync(PolicyRevisionEnvelope policy, CancellationToken cancellationToken = default)
    {
        policy.Validate();
        await using var connection = factory.Create();
        await connection.OpenAsync(cancellationToken);
        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO scientific_policy_revisions(revision_id,policy_id,schema,canonical_content,content_hash,created_by_sid,source,recorded_at) VALUES (@RevisionId,@PolicyId,@Schema,@CanonicalContent,@ContentHash,@CreatedBySid,@Source,@RecordedAtUtc);",
                policy, cancellationToken: cancellationToken));
            return policy;
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            var existing = await ResolveAsync(policy.RevisionId, policy.CreatedBySid, cancellationToken);
            if (existing is not null && existing.PolicyId == policy.PolicyId && string.Equals(existing.ContentHash, policy.ContentHash, StringComparison.Ordinal))
                return existing;
            throw new InvalidOperationException("A policy identity cannot be registered with different immutable content.", exception);
        }
    }

    public async Task<PolicyRevisionEnvelope?> ResolveAsync(Guid revisionId, string actorSid, CancellationToken cancellationToken = default)
    {
        await using var connection = factory.Create();
        var row = await connection.QuerySingleOrDefaultAsync<PolicyRow>(new CommandDefinition(
            "SELECT revision_id AS RevisionId,policy_id AS PolicyId,schema AS Schema,canonical_content AS CanonicalContent,content_hash AS ContentHash,created_by_sid AS CreatedBySid,source AS Source,recorded_at AS RecordedAtUtc FROM scientific_policy_revisions WHERE revision_id=@revisionId AND created_by_sid=@actorSid;",
            new { revisionId, actorSid }, cancellationToken: cancellationToken));
        return row is null ? null : new PolicyRevisionEnvelope(row.RevisionId, row.PolicyId, row.Schema, row.CanonicalContent, row.ContentHash, row.CreatedBySid, row.Source, new DateTimeOffset(DateTime.SpecifyKind(row.RecordedAtUtc, DateTimeKind.Utc)));
    }

    public async Task<PolicyRevisionEnvelope> ResolveApprovedAsync(Guid revisionId, PolicyApprovalProvenance provenance, string actorSid, string scope, CancellationToken cancellationToken = default)
    {
        var policy = await ResolveAsync(revisionId, actorSid, cancellationToken) ?? throw new KeyNotFoundException("Policy revision is unavailable in this scope.");
        policy.Validate();
        if (provenance.PolicyRevisionId != policy.RevisionId || !string.Equals(provenance.PolicyContentHash, policy.ContentHash, StringComparison.Ordinal) ||
            !string.Equals(provenance.PolicySchema, policy.Schema, StringComparison.Ordinal) || provenance.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            throw new UnauthorizedAccessException("Policy approval provenance does not bind valid exact policy content.");
        var result = await authority.VerifyAsync(provenance, policy, actorSid, scope, DateTimeOffset.UtcNow, cancellationToken);
        if (!result.IsApproved)
            throw new UnauthorizedAccessException(result.RejectionReason ?? "Policy approval is unavailable or rejected.");
        return policy;
    }
}

/// <summary>Production intentionally has no Phase 2 approval issuer; callers fail closed.</summary>
public sealed class UnavailablePolicyApprovalAuthority : IPolicyApprovalAuthority
{
    public Task<PolicyApprovalValidation> VerifyAsync(PolicyApprovalProvenance provenance, PolicyRevisionEnvelope policy, string actorSid, string scope, DateTimeOffset evaluatedAtUtc, CancellationToken cancellationToken = default) =>
        Task.FromResult(PolicyApprovalValidation.Unavailable("No authoritative policy approval provider is configured."));
}

internal sealed class PolicyRow
{
    public Guid RevisionId { get; init; }
    public Guid PolicyId { get; init; }
    public string Schema { get; init; } = string.Empty;
    public byte[] CanonicalContent { get; init; } = [];
    public string ContentHash { get; init; } = string.Empty;
    public string CreatedBySid { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public DateTime RecordedAtUtc { get; init; }
}
