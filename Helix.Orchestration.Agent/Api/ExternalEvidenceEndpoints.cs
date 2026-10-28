using System.Security.Cryptography;
using System.Text.Json;
using Helix.Orchestration.Agent.Campaigns;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Helix.Orchestration.Agent.Api;

/// <summary>
/// Agent-owned ingress for correlating external evidence with a pending activity.
/// This endpoint records evidence only; it does not control or verify an offline instrument.
/// </summary>
public static class ExternalEvidenceEndpoints
{
    private const string IngressNotice =
        "Records evidence correlation and delivery deduplication only; it does not assert offline instrument control or physical completion.";

    public static void MapExternalEvidenceEndpoints(this WebApplication app)
    {
        app.MapPost("/external-evidence", async Task<IResult> (
            ExternalEvidenceRequest request,
            IExternalEvidenceStore store,
            CancellationToken cancellationToken) =>
        {
            ExternalEvidenceReceipt receipt;
            try
            {
                receipt = CreateReceipt(request);
            }
            catch (ArgumentException exception)
            {
                return Results.BadRequest(new
                {
                    disposition = EvidenceDisposition.Rejected.ToString(),
                    projection = (EvidenceProjection?)null,
                    replayed = false,
                    notice = IngressNotice,
                    error = exception.Message
                });
            }

            var result = await store.RecordReceiptAsync(receipt, request.ExpectedRevision, cancellationToken);
            var projection = await CreateProjectionAsync(store, result.Activity, cancellationToken);
            return Results.Ok(new ExternalEvidenceResponse(
                result.Receipt.Disposition.ToString(), projection, result.IsReplay, IngressNotice));
        })
        .WithName("RecordExternalEvidence")
        .WithSummary("Correlate and deduplicate external evidence for a pending activity")
        .Produces<ExternalEvidenceResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Processes one ingress submission using only the durable evidence store contract.
    /// Exposed separately so the correlation and replay behavior can be tested without a host.
    /// </summary>
    public static async Task<ExternalEvidenceResponse> ProcessAsync(
        ExternalEvidenceRequest request,
        IExternalEvidenceStore store,
        CancellationToken cancellationToken = default)
    {
        var receipt = CreateReceipt(request);
        var result = await store.RecordReceiptAsync(receipt, request.ExpectedRevision, cancellationToken);
        var projection = await CreateProjectionAsync(store, result.Activity, cancellationToken);
        return new ExternalEvidenceResponse(
            result.Receipt.Disposition.ToString(), projection, result.IsReplay, IngressNotice);
    }

    private static ExternalEvidenceReceipt CreateReceipt(ExternalEvidenceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateIdentifier(request.CampaignId, nameof(request.CampaignId));
        ValidateIdentifier(request.ActivityId, nameof(request.ActivityId));
        ValidateIdentifier(request.FlowId, nameof(request.FlowId));
        ValidateIdentifier(request.AwaitingStepId, nameof(request.AwaitingStepId));
        ValidateIdentifier(request.Source, nameof(request.Source));
        ValidateIdentifier(request.EventId, nameof(request.EventId));
        if (request.ExpectedRevision < 1)
            throw new ArgumentOutOfRangeException(nameof(request.ExpectedRevision), "Expected revision must be positive.");
        if (request.Payload.ValueKind == JsonValueKind.Undefined)
            throw new ArgumentException("Evidence payload is required.", nameof(request.Payload));

        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(request.Payload);
        var payloadHash = "sha256:" + Convert.ToHexString(SHA256.HashData(payloadBytes)).ToLowerInvariant();
        return new ExternalEvidenceReceipt(
            request.Source,
            request.EventId,
            payloadHash,
            request.CampaignId,
            request.ActivityId,
            request.FlowId,
            request.AwaitingStepId,
            EvidenceDisposition.Rejected);
    }

    private static async Task<EvidenceProjection?> CreateProjectionAsync(
        IExternalEvidenceStore store,
        PendingActivity? activity,
        CancellationToken cancellationToken)
    {
        if (activity is null)
            return null;

        var audit = await store.LoadAuditAsync(activity.CampaignId, activity.ActivityId, cancellationToken);
        return new EvidenceProjection(
            activity.CampaignId,
            activity.ActivityId,
            activity.FlowId,
            activity.AwaitingStepId,
            activity.CommandId,
            activity.Revision,
            activity.Lifecycle.ToString(),
            activity.PhysicalOutcome.ToString(),
            audit.Select(fact => new EvidenceAuditEntry(fact.Disposition, fact.OccurredAt)).ToArray());
    }

    private static void ValidateIdentifier(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("All subject and delivery identifiers are required.", name);
    }
}

public sealed record ExternalEvidenceRequest(
    string CampaignId,
    string ActivityId,
    string FlowId,
    string AwaitingStepId,
    long ExpectedRevision,
    string Source,
    string EventId,
    JsonElement Payload);

public sealed record ExternalEvidenceResponse(
    string Disposition,
    EvidenceProjection? Projection,
    bool Replayed,
    string Notice);

public sealed record EvidenceProjection(
    string CampaignId,
    string ActivityId,
    string FlowId,
    string AwaitingStepId,
    string CommandId,
    long Revision,
    string Lifecycle,
    string PhysicalOutcome,
    IReadOnlyList<EvidenceAuditEntry> Audit);

public sealed record EvidenceAuditEntry(string Disposition, DateTimeOffset OccurredAt);
