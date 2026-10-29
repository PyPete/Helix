using Helix.Orchestration.Agent.Security;
using Helix.Orchestration.Agent.Execution;
using Helix.Orchestration.Core.Execution;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Helix.Orchestration.Agent.Api;

public sealed record CommandProposalRequest(Guid CommandId, Guid ExecutionId, Guid CorrelationId, string DeviceId, string Capability, string CanonicalParameters, string PolicyRevision);
public sealed record PolicyApprovalRequest(string PolicyRevision);

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

        app.MapPost("/executions/{executionId:guid}/control/{state}", (Guid executionId, string state, ExecutionControlService controls) =>
        {
            if (!Enum.TryParse<ExecutionControlState>(state, ignoreCase: true, out var requested))
                return Results.BadRequest(new { error = "Unknown execution control state." });
            return Results.Ok(new { executionId, state = controls.Set(executionId, requested).ToString() });
        }).WithTags("Execution control");
    }
}
