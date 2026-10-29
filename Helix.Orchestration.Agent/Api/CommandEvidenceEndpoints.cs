using Helix.Orchestration.Agent.Security;
using Helix.Orchestration.Agent.Execution;
using Helix.Orchestration.Core.Execution;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Helix.Orchestration.Agent.Api;

public sealed record CommandProposalRequest(Guid CommandId, Guid ExecutionId, Guid CorrelationId, string DeviceId, string Capability, string CanonicalParameters, string PolicyRevision);
public sealed record PolicyApprovalRequest(string PolicyRevision);
public sealed record AddReviewNoteRequest(Guid OperationId, string Text);

public static class CommandEvidenceEndpoints
{
    public static void MapCommandEvidenceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/commands").WithTags("Command evidence");
        group.MapPost("/propose", async Task<IResult> (CommandProposalRequest request, HttpContext context, ICommandDispatcher dispatcher, CancellationToken ct) =>
        {
            var actor = (VerifiedDesktopActor)context.Items[DesktopSessionAuthenticationHandler.ActorItemKey]!;
            var intent = new CommandIntent(request.CommandId, request.ExecutionId, request.CorrelationId, request.DeviceId,
                request.Capability, request.CanonicalParameters, actor.Sid, actor.DisplayName, actor.SessionReference,
                request.PolicyRevision, DateTimeOffset.UtcNow);
            var evidence = await dispatcher.ProposeAsync(intent, ct);
            return Results.Ok(evidence);
        });

        group.MapPost("/{commandId:guid}/approve-policy", async Task<IResult> (Guid commandId, PolicyApprovalRequest request, HttpContext context, ICommandLedger ledger, CancellationToken ct) =>
        {
            var actor = (VerifiedDesktopActor)context.Items[DesktopSessionAuthenticationHandler.ActorItemKey]!;
            var record = await ledger.GetAsync(commandId, ct);
            if (record is null)
                return Results.NotFound();
            if (!string.Equals(record.Intent.PolicyRevision, request.PolicyRevision, StringComparison.Ordinal) ||
                !string.Equals(request.PolicyRevision, "simulation-policy-v1", StringComparison.Ordinal))
                return Results.BadRequest(new { error = "The approved simulation policy does not match the proposal." });
            return Results.Ok(new { commandId, policyRevision = request.PolicyRevision, approvedBy = actor.DisplayName });
        });

        group.MapPost("/{commandId:guid}/dispatch", async Task<IResult> (Guid commandId, ICommandDispatcher dispatcher, CancellationToken ct) =>
            Results.Ok(await dispatcher.DispatchAsync(commandId, ct)));

        group.MapGet("/{commandId:guid}/evidence", async Task<IResult> (Guid commandId, ICommandLedger ledger, CancellationToken ct) =>
        {
            var record = await ledger.GetAsync(commandId, ct);
            return record is null ? Results.NotFound() : Results.Ok(record);
        });

        // A notification is only a wake-up signal. This route is the authoritative,
        // committed-history read used for active, completed, manual and security streams.
        group.MapGet("/evidence", async Task<IResult> (Guid? executionId, string? cursor, int? pageSize, ICommandLedger ledger, CancellationToken ct) =>
        {
            try
            {
                var page = await ledger.GetHistoryAsync(executionId, cursor, pageSize ?? 50, ct);
                return Results.Ok(page);
            }
            catch (NotSupportedException)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            catch (ArgumentException exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }
        });

        group.MapPost("/{commandId:guid}/review-notes", async Task<IResult> (Guid commandId, AddReviewNoteRequest request, HttpContext context, ICommandLedger ledger, CancellationToken ct) =>
        {
            var actor = (VerifiedDesktopActor)context.Items[DesktopSessionAuthenticationHandler.ActorItemKey]!;
            if (await ledger.GetAsync(commandId, ct) is null)
                return Results.NotFound();

            try
            {
                // The session actor is authoritative; request bodies never supply identity.
                var note = await ledger.AppendReviewNoteAsync(
                    request.OperationId, commandId, actor.Sid, actor.DisplayName, request.Text, ct);
                return Results.Ok(note);
            }
            catch (ArgumentException exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }
            catch (NotSupportedException)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        });

        app.MapPost("/executions/{executionId:guid}/control/{state}", async Task<IResult> (Guid executionId, string state, HttpContext context, ExecutionControlService controls, CancellationToken ct) =>
        {
            if (!Enum.TryParse<ExecutionControlState>(state, ignoreCase: true, out var requested))
                return Results.BadRequest(new { error = "Unknown execution control state." });
            var actor = (VerifiedDesktopActor)context.Items[DesktopSessionAuthenticationHandler.ActorItemKey]!;
            var current = await controls.GetSnapshotAsync(executionId, ct);
            var result = await controls.RequestControlAsync(
                executionId, requested, current.Revision, actor.Sid, "Requested through command evidence API.", ct);
            return result.Accepted
                ? Results.Ok(new { executionId, state = result.Snapshot.State.ToString(), result.Snapshot.Revision })
                : Results.Conflict(new { error = result.Code, state = result.Snapshot.State.ToString(), result.Snapshot.Revision });
        }).WithTags("Execution control");
    }
}
