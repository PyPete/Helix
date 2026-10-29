using Helix.Orchestration.Agent.Security;
using Helix.Orchestration.Core.Execution;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Helix.Orchestration.Agent.Api;

public sealed record CommandProposalRequest(Guid CommandId, Guid ExecutionId, Guid CorrelationId, string DeviceId, string Capability, string CanonicalParameters, string PolicyRevision);

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

        group.MapPost("/{commandId:guid}/dispatch", async Task<IResult> (Guid commandId, ICommandDispatcher dispatcher, CancellationToken ct) =>
            Results.Ok(await dispatcher.DispatchAsync(commandId, ct)));
    }
}
