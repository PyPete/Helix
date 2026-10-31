using Helix.Orchestration.Agent.Security;
using Helix.Orchestration.Core.ScientificSetup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Helix.Orchestration.Agent.Api;

public sealed record ApparatusCreateRequest(Guid OperationId, string Name, string? Description);
public sealed record ApparatusPublishRequest(Guid OperationId, Guid? ExpectedRevisionId, string? Note);
public sealed record ScientificOperationResponse(Guid OperationId, Guid ApparatusId, Guid? RevisionId, string Status);
public sealed record ScientificApparatusResponse(Guid ApparatusId, string Name, Guid? RevisionId);

public static class ScientificSetupEndpoints
{
    public static void MapScientificSetupEndpoints(this WebApplication app)
    {
        var routes = app.MapGroup("/api/scientific-setup/apparatus");
        routes.MapPost("/", CreateAsync);
        routes.MapPost("/{id:guid}/publications", PublishAsync);
        routes.MapGet("/", ListAsync);
        routes.MapGet("/{id:guid}", ReadAsync);
        routes.MapGet("/{id:guid}/publications", HistoryAsync);
        routes.MapGet("/{id:guid}/publications/{revision:guid}", ReadRevisionAsync);
        app.MapGet("/api/scientific-setup/operations/{operationId:guid}", OperationAsync);
    }

    private static async Task<IResult> CreateAsync(ApparatusCreateRequest request, HttpContext context, IScientificSetupRepository repository, CancellationToken ct)
    {
        if (context.Request.ContentLength > 65536) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        if (request.OperationId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest(new { error = "Operation ID and name are required." });
        var actor = Actor(context);
        try
        {
            var result = await repository.CreateAsync(new ApparatusDefinition(Guid.NewGuid(), request.Name.Trim(), []), actor.Sid, request.OperationId, ct);
            return Results.Created($"/api/scientific-setup/apparatus/{result.Apparatus.ApparatusId}", Receipt(result.Receipt, result.Apparatus.ApparatusId, null));
        }
        catch (ArgumentException exception) { return Results.BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Results.Conflict(new { error = exception.Message }); }
    }

    private static async Task<IResult> PublishAsync(Guid id, ApparatusPublishRequest request, HttpContext context, IScientificSetupRepository repository, CancellationToken ct)
    {
        if (request.OperationId == Guid.Empty) return Results.BadRequest(new { error = "Operation ID is required." });
        var actor = Actor(context);
        try
        {
            var apparatus = await repository.ReadLatestAsync(id, actor.Sid, ct);
            if (apparatus is null) return Results.NotFound();
            var result = await repository.PublishAsync(id, request.ExpectedRevisionId, apparatus.Name, actor.Sid, request.OperationId, ct);
            return Results.Ok(Receipt(result.Receipt, id, result.Revision.RevisionId));
        }
        catch (KeyNotFoundException) { return Results.NotFound(); }
        catch (InvalidOperationException exception) { return Results.Conflict(new { error = exception.Message }); }
    }

    private static async Task<IResult> ListAsync(HttpContext context, IScientificSetupRepository repository, CancellationToken ct) => Results.Ok((await repository.ListAsync(Actor(context).Sid, ct)).Select(Summary));
    private static async Task<IResult> ReadAsync(Guid id, HttpContext context, IScientificSetupRepository repository, CancellationToken ct) { var value = await repository.ReadLatestAsync(id, Actor(context).Sid, ct); return value is null ? Results.NotFound() : Results.Ok(Summary(value)); }
    private static async Task<IResult> HistoryAsync(Guid id, HttpContext context, IScientificSetupRepository repository, CancellationToken ct) { var values = await repository.HistoryAsync(id, Actor(context).Sid, ct); return Results.Ok(values.Select(value => Receipt(value, id, RevisionId(value)))); }
    private static async Task<IResult> ReadRevisionAsync(Guid id, Guid revision, HttpContext context, IScientificSetupRepository repository, CancellationToken ct) { var value = await repository.ReadAsync(id, revision, Actor(context).Sid, ct); return value is null ? Results.NotFound() : Results.Ok(new ScientificApparatusResponse(value.ApparatusId, value.Name, value.RevisionId)); }
    private static async Task<IResult> OperationAsync(Guid operationId, HttpContext context, IScientificSetupRepository repository, CancellationToken ct) { var value = await repository.OperationAsync(operationId, Actor(context).Sid, ct); return value is null ? Results.NotFound() : Results.Ok(Receipt(value, value.ResourceId, RevisionId(value))); }
    private static VerifiedDesktopActor Actor(HttpContext context) => context.Items[DesktopSessionAuthenticationHandler.ActorItemKey] as VerifiedDesktopActor ?? throw new UnauthorizedAccessException();
    private static ScientificApparatusResponse Summary(ScientificApparatusSummary value) => new(value.ApparatusId, value.Name, value.RevisionId);
    private static ScientificOperationResponse Receipt(ScientificOperationReceipt value, Guid apparatusId, Guid? revisionId) => new(value.OperationId, apparatusId, revisionId, "completed");
    private static Guid? RevisionId(ScientificOperationReceipt value) => value.Kind == "apparatus.publish" && JsonSerializer.Deserialize<ScientificRevision>(value.Payload) is { } revision ? revision.RevisionId : null;
}
