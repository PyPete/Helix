using Helix.Orchestration.Agent.ScientificSetup;
using Helix.Orchestration.Agent.Security;
using Helix.Orchestration.Core.ScientificSetup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Helix.Orchestration.Agent.Api;

/// <summary>Authenticated read surface for immutable frozen evidence. Deliberately contains no approval or execution route.</summary>
public static class FrozenSetupEndpoints
{
    public static void MapFrozenSetupEndpoints(this WebApplication app)
    {
        app.MapGet("/api/scientific-setup/assemblies/{id:guid}", ReadAssemblyAsync);
        app.MapGet("/api/scientific-setup/policies/{revisionId:guid}", ReadPolicyAsync);
    }

    private static async Task<IResult> ReadAssemblyAsync(Guid id, HttpContext context, FrozenSetupService service, CancellationToken cancellationToken)
    {
        var manifest = await service.ReadAsync(id, Actor(context).Sid, cancellationToken);
        return manifest is null ? Results.NotFound() : Results.Ok(manifest);
    }

    private static async Task<IResult> ReadPolicyAsync(Guid revisionId, HttpContext context, IPolicyRevisionProvider policies, CancellationToken cancellationToken)
    {
        var policy = await policies.ResolveAsync(revisionId, Actor(context).Sid, cancellationToken);
        return policy is null ? Results.NotFound() : Results.Ok(new { policy.RevisionId, policy.PolicyId, policy.Schema, policy.ContentHash, policy.RecordedAtUtc });
    }

    private static VerifiedDesktopActor Actor(HttpContext context) =>
        context.Items[DesktopSessionAuthenticationHandler.ActorItemKey] as VerifiedDesktopActor
        ?? throw new UnauthorizedAccessException();
}
