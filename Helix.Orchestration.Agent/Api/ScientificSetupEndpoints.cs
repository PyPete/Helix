using Helix.Orchestration.Agent.Security;
using Helix.Orchestration.Core.ScientificSetup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
namespace Helix.Orchestration.Agent.Api;
public sealed record ApparatusCreateRequest(Guid OperationId, string Name, IReadOnlyList<ApparatusComponent> Components);
public sealed record ApparatusPublishRequest(Guid OperationId, long ExpectedToken, string Name);
public static class ScientificSetupEndpoints
{
    public static void MapScientificSetupEndpoints(this WebApplication app)
    {
        var routes = app.MapGroup("/api/scientific-setup/apparatus");
        routes.MapPost("/", async (ApparatusCreateRequest request, HttpContext context, IScientificSetupRepository repository, CancellationToken ct) => { if (context.Request.ContentLength > 65536) return Results.StatusCode(413); var actor = (VerifiedDesktopActor)context.Items[DesktopSessionAuthenticationHandler.ActorItemKey]!; try { var result = await repository.CreateAsync(new ApparatusDefinition(Guid.NewGuid(), request.Name, request.Components), actor.Sid, request.OperationId, ct); return Results.Created($"/api/scientific-setup/apparatus/{result.Apparatus.ApparatusId}", result); } catch (ArgumentException exception) { return Results.BadRequest(new { error = exception.Message }); } });
        routes.MapPost("/{id:guid}/publications", async (Guid id, ApparatusPublishRequest request, HttpContext context, IScientificSetupRepository repository, CancellationToken ct) => { var actor = (VerifiedDesktopActor)context.Items[DesktopSessionAuthenticationHandler.ActorItemKey]!; try { return Results.Ok(await repository.PublishAsync(id, request.ExpectedToken, request.Name, actor.Sid, request.OperationId, ct)); } catch (KeyNotFoundException) { return Results.NotFound(); } catch (InvalidOperationException exception) { return Results.Conflict(new { error = exception.Message }); } });
        routes.MapGet("/{id:guid}/publications/{revision:guid}", async (Guid id, Guid revision, HttpContext context, IScientificSetupRepository repository, CancellationToken ct) => { var actor = (VerifiedDesktopActor)context.Items[DesktopSessionAuthenticationHandler.ActorItemKey]!; var value = await repository.ReadAsync(id, revision, actor.Sid, ct); return value is null ? Results.NotFound() : Results.Ok(value); });
    }
}
