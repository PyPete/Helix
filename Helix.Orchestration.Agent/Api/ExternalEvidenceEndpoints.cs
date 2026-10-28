using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using Helix.Orchestration.Agent.Campaigns;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Helix.Orchestration.Agent.Api;

/// <summary>
/// Agent-owned ingress for correlating external evidence with a pending activity.
/// This endpoint records evidence only; it does not control or verify an offline instrument.
/// </summary>
public static class ExternalEvidenceEndpoints
{
    public const string AuthenticationScheme = "ExternalEvidenceSource";
    public const string SourceClaimType = "external_evidence_source";

    private const string IngressNotice =
        "Records evidence correlation and delivery deduplication only; it does not assert offline instrument control or physical completion.";

    public static void MapExternalEvidenceEndpoints(this WebApplication app)
    {
        app.MapPost("/external-evidence", async Task<IResult> (
            ExternalEvidenceRequest request,
            IExternalEvidenceStore store,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var authenticatedSource = httpContext.User.FindFirstValue(SourceClaimType);
            if (authenticatedSource is null ||
                !string.Equals(authenticatedSource, request.Source, StringComparison.Ordinal))
                return Results.Forbid();

            try
            {
                var response = await ProcessAsync(request, authenticatedSource, store, cancellationToken);
                return Results.Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
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
        })
        .WithName("RecordExternalEvidence")
        .WithSummary("Correlate and deduplicate external evidence for a pending activity")
        .Produces<ExternalEvidenceResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .RequireAuthorization();
    }

    public static void MapBoundedActivityEndpoints(this WebApplication app)
    {
        app.MapPost("/campaigns/bounded-activity", async Task<IResult> (
            IExternalEvidenceStore store,
            CancellationToken cancellationToken) =>
        {
            var id = Guid.NewGuid().ToString("N");
            var activity = new PendingActivity(
                $"campaign-{id}",
                $"activity-{id}",
                $"flow-{id}",
                $"awaiting-step-{id}",
                $"command-{id}",
                1,
                ActivityLifecycle.PendingExternalEvidence,
                PhysicalOutcome.unknown);

            var created = await store.CreatePendingActivityAsync(activity, cancellationToken);
            var projection = await CreateProjectionAsync(store, created, cancellationToken);
            return Results.Ok(projection);
        })
        .WithName("CreateBoundedActivity")
        .WithSummary("Create a bounded simulated activity awaiting external evidence")
        .Produces<EvidenceProjection>(StatusCodes.Status200OK);

        app.MapGet("/campaigns/{campaignId}/activities/{activityId}", async Task<IResult> (
            string campaignId,
            string activityId,
            IExternalEvidenceStore store,
            CancellationToken cancellationToken) =>
        {
            var activity = await store.LoadActivityAsync(campaignId, activityId, cancellationToken);
            if (activity is null)
                return Results.NotFound();

            var projection = await CreateProjectionAsync(store, activity, cancellationToken);
            return Results.Ok(projection);
        })
        .WithName("GetCampaignActivity")
        .WithSummary("Load the durable campaign activity projection")
        .Produces<EvidenceProjection>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }

    public static IServiceCollection AddExternalEvidenceAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = AuthenticationScheme;
                options.DefaultChallengeScheme = AuthenticationScheme;
                options.DefaultForbidScheme = AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, ExternalEvidenceSourceAuthenticationHandler>(
                AuthenticationScheme, _ => { });
        services.AddAuthorization();
        return services;
    }

    /// <summary>
    /// Processes one ingress submission using only the durable evidence store contract.
    /// Exposed separately so the correlation and replay behavior can be tested without a host.
    /// </summary>
    public static async Task<ExternalEvidenceResponse> ProcessAsync(
        ExternalEvidenceRequest request,
        string authenticatedSource,
        IExternalEvidenceStore store,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(authenticatedSource) ||
            !string.Equals(request.Source, authenticatedSource, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Evidence source does not match the authenticated source.");

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

public sealed class ExternalEvidenceSourceAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SourceHeader = "X-External-Evidence-Source";
    public const string TokenHeader = "X-External-Evidence-Token";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var source = Request.Headers[SourceHeader].ToString();
        var suppliedToken = Request.Headers[TokenHeader].ToString();
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(suppliedToken))
            return Task.FromResult(AuthenticateResult.NoResult());

        var configuredToken = configuration[$"ExternalEvidence:Sources:{source}"];
        if (string.IsNullOrEmpty(configuredToken) || !TokensMatch(configuredToken, suppliedToken))
            return Task.FromResult(AuthenticateResult.Fail("External evidence source credentials are invalid."));

        var identity = new ClaimsIdentity(
            [new Claim(ExternalEvidenceEndpoints.SourceClaimType, source)], Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static bool TokensMatch(string configuredToken, string suppliedToken)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(configuredToken);
        var suppliedBytes = Encoding.UTF8.GetBytes(suppliedToken);
        return expectedBytes.Length == suppliedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
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
