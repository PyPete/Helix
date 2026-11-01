using Helix.Orchestration.Agent.Security;
using Helix.Orchestration.Agent.ScientificSetup;
using Helix.Orchestration.Core.ScientificSetup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Helix.Orchestration.Agent.Api;

public sealed record ApparatusCreateRequest(Guid OperationId, string Name, string? Description, IReadOnlyList<ApparatusComponent>? Components = null, IReadOnlyList<ApparatusConnection>? Connections = null, IReadOnlyList<ScientificAssertion>? Assertions = null);
public sealed record ApparatusSaveDraftRequest(Guid OperationId, long ExpectedToken, string Name, IReadOnlyList<ApparatusComponent>? Components, IReadOnlyList<ApparatusConnection>? Connections, IReadOnlyList<ScientificAssertion>? Assertions);
public sealed record ApparatusAdoptRevisionRequest(Guid OperationId, Guid RevisionId, long ExpectedToken);
public sealed record ApparatusPublishRequest(Guid OperationId, Guid? ExpectedRevisionId, string? Note);
public sealed record ScientificOperationResponse(Guid OperationId, Guid ApparatusId, Guid? RevisionId, string Status);
public sealed record ScientificApparatusResponse(Guid ApparatusId, string Name, Guid? RevisionId);
public sealed record ProcedureCreateRequest(Guid OperationId, string Name, Helix.Orchestration.Core.WSL.WslDocument Document, IReadOnlyList<ProcedureParameterDeclaration> Parameters, IReadOnlyList<PlannedCollectionDescriptor>? PlannedCollections = null);
public sealed record PlanCreateRequest(Guid OperationId, string Name, Guid ProcedureRevisionId, IReadOnlyList<ExperimentParameterValue> Parameters);
public sealed record ProcedureSaveDraftRequest(Guid OperationId, long ExpectedToken, string Name, Helix.Orchestration.Core.WSL.WslDocument Document, IReadOnlyList<ProcedureParameterDeclaration> Parameters, IReadOnlyList<PlannedCollectionDescriptor>? PlannedCollections = null);
public sealed record PlanSaveDraftRequest(Guid OperationId, long ExpectedToken, string Name, Guid ProcedureRevisionId, IReadOnlyList<ExperimentParameterValue> Parameters);
public sealed record ScientificPublishRequest(Guid OperationId, Guid? ExpectedRevisionId, string? Name);
public sealed record ProcedureDeriveRequest(Guid OperationId, Guid SourceRevisionId, string Name);
public sealed record PlanAdoptProcedureRequest(Guid OperationId, Guid ProcedureRevisionId, long ExpectedToken);

public static class ScientificSetupEndpoints
{
    public static void MapScientificSetupEndpoints(this WebApplication app)
    {
        var routes = app.MapGroup("/api/scientific-setup/apparatus");
        routes.MapPost("/", CreateAsync);
        routes.MapGet("/{id:guid}/draft", ReadDraftAsync);
        routes.MapPut("/{id:guid}/draft", SaveDraftAsync);
        routes.MapPost("/{id:guid}/draft/adopt", AdoptRevisionAsync);
        routes.MapPost("/{id:guid}/publications", PublishAsync);
        routes.MapGet("/", ListAsync);
        routes.MapGet("/{id:guid}", ReadAsync);
        routes.MapGet("/{id:guid}/publications", HistoryAsync);
        routes.MapGet("/{id:guid}/publications/{revision:guid}", ReadRevisionAsync);
        app.MapGet("/api/scientific-setup/procedures", ListProceduresAsync);
        app.MapGet("/api/scientific-setup/plans", ListPlansAsync);
        app.MapPost("/api/scientific-setup/procedures", CreateProcedureAsync);
        app.MapPost("/api/scientific-setup/plans", CreatePlanAsync);
        app.MapPost("/api/scientific-setup/procedures/derive", DeriveProcedureAsync);
        app.MapPost("/api/scientific-setup/plans/{id:guid}/adopt-procedure", AdoptProcedureAsync);
        MapContentRoutes(app, "procedures", ReadProcedureDraftAsync, SaveProcedureDraftAsync, PublishProcedureAsync, ReadProcedureAsync, ProcedureHistoryAsync, ReadProcedureRevisionAsync);
        MapContentRoutes(app, "plans", ReadPlanDraftAsync, SavePlanDraftAsync, PublishPlanAsync, ReadPlanAsync, PlanHistoryAsync, ReadPlanRevisionAsync);
        app.MapGet("/api/scientific-setup/operations/{operationId:guid}", OperationAsync);
    }

    private static async Task<IResult> AdoptRevisionAsync(Guid id, ApparatusAdoptRevisionRequest request, HttpContext context, IScientificSetupRepository repository, CancellationToken ct)
    {
        if (request.OperationId == Guid.Empty || request.RevisionId == Guid.Empty) return Results.BadRequest(new { error = "Operation and revision IDs are required." });
        try
        {
            var result = await repository.AdoptRevisionAsync(id, request.RevisionId, request.ExpectedToken, Actor(context).Sid, request.OperationId, ct);
            return Results.Ok(new { receipt = Receipt(result.Receipt, id, null), token = result.Token, apparatus = result.Apparatus });
        }
        catch (KeyNotFoundException) { return Results.NotFound(); }
        catch (InvalidOperationException exception) { return Results.Conflict(new { error = exception.Message }); }
    }

    private static async Task<IResult> ListProceduresAsync(HttpContext context, [Microsoft.AspNetCore.Mvc.FromServices] ScientificSetupService service, CancellationToken ct) => Results.Ok(await service.ListProceduresAsync(Actor(context).Sid, ct));
    private static async Task<IResult> ListPlansAsync(HttpContext context, [Microsoft.AspNetCore.Mvc.FromServices] ScientificSetupService service, CancellationToken ct) => Results.Ok(await service.ListPlansAsync(Actor(context).Sid, ct));

    private static async Task<IResult> CreateProcedureAsync(ProcedureCreateRequest request, HttpContext context, [Microsoft.AspNetCore.Mvc.FromServices] ScientificSetupService service, CancellationToken ct)
    {
        if (context.Request.ContentLength > 65536) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        if (request.OperationId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest();
        try
        {
            var result = await service.CreateProcedureAsync(new ProcedureDefinition(Guid.Empty, request.Name.Trim(), request.Document, request.Parameters, request.PlannedCollections), Actor(context).Sid, request.OperationId, ct);
            return Results.Created($"/api/scientific-setup/procedures/{result.Procedure.ProcedureId}", new { result.Procedure.ProcedureId, result.Token, receipt = result.Receipt });
        }
        catch (ArgumentException exception) { return Results.BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Results.Conflict(new { error = exception.Message }); }
    }

    private static async Task<IResult> CreatePlanAsync(PlanCreateRequest request, HttpContext context, [Microsoft.AspNetCore.Mvc.FromServices] ScientificSetupService service, IScientificSetupRepository repository, CancellationToken ct)
    {
        if (context.Request.ContentLength > 65536) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        if (request.OperationId == Guid.Empty || request.ProcedureRevisionId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest();
        // Plan values are checked against the exact actor-owned publication, never an inferred latest draft.
        var procedureRevision = await repository.ReadProcedureByRevisionAsync(request.ProcedureRevisionId, Actor(context).Sid, ct);
        if (procedureRevision is null) return Results.NotFound();
        using var document = JsonDocument.Parse(procedureRevision.CanonicalContent);
        var content = document.RootElement.TryGetProperty("content", out var envelope) ? envelope : document.RootElement;
        var procedure = JsonSerializer.Deserialize<ProcedureDefinition>(content.GetRawText()) ?? throw new InvalidOperationException("Stored procedure revision is invalid.");
        try
        {
            var result = await service.CreatePlanAsync(new ExperimentPlanDefinition(Guid.Empty, request.Name.Trim(), request.ProcedureRevisionId, request.Parameters), procedure, Actor(context).Sid, request.OperationId, ct);
            return Results.Created($"/api/scientific-setup/plans/{result.Plan.PlanId}", new { result.Plan.PlanId, result.Token, receipt = result.Receipt });
        }
        catch (ArgumentException exception) { return Results.BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Results.Conflict(new { error = exception.Message }); }
    }

    private static async Task<IResult> DeriveProcedureAsync(ProcedureDeriveRequest request, HttpContext context, IScientificSetupRepository repository, CancellationToken ct)
    {
        if (context.Request.ContentLength > 65536) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        if (request.OperationId == Guid.Empty || request.SourceRevisionId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest();
        try
        {
            var value = await repository.DeriveProcedureAsync(request.SourceRevisionId, request.Name.Trim(), Actor(context).Sid, request.OperationId, ct);
            return Results.Created($"/api/scientific-setup/procedures/{value.Procedure.ProcedureId}", new { procedure = value.Procedure, token = value.Token, receipt = value.Receipt });
        }
        catch (KeyNotFoundException) { return Results.NotFound(); }
        catch (ArgumentException exception) { return Results.BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Results.Conflict(new { error = exception.Message }); }
    }

    private static async Task<IResult> AdoptProcedureAsync(Guid id, PlanAdoptProcedureRequest request, HttpContext context, IScientificSetupRepository repository, CancellationToken ct)
    {
        if (context.Request.ContentLength > 65536) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        if (id == Guid.Empty || request.OperationId == Guid.Empty || request.ProcedureRevisionId == Guid.Empty) return Results.BadRequest();
        try
        {
            var value = await repository.AdoptProcedureAsync(id, request.ProcedureRevisionId, request.ExpectedToken, Actor(context).Sid, request.OperationId, ct);
            return Results.Ok(new { plan = value.Plan, token = value.Token, receipt = value.Receipt });
        }
        catch (KeyNotFoundException) { return Results.NotFound(); }
        catch (ArgumentException exception) { return Results.BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Results.Conflict(new { error = exception.Message }); }
    }

    private static void MapContentRoutes(WebApplication app, string kind, Delegate draft, Delegate save, Delegate publish, Delegate read, Delegate history, Delegate revision)
    {
        var group = app.MapGroup($"/api/scientific-setup/{kind}");
        group.MapGet("/{id:guid}/draft", draft);
        group.MapPut("/{id:guid}/draft", save);
        group.MapPost("/{id:guid}/publications", publish);
        group.MapGet("/{id:guid}", read);
        group.MapGet("/{id:guid}/publications", history);
        group.MapGet("/{id:guid}/publications/{revisionId:guid}", revision);
    }

    private static async Task<IResult> ReadProcedureDraftAsync(Guid id, HttpContext context, [Microsoft.AspNetCore.Mvc.FromServices] ScientificSetupService service, CancellationToken ct)
    {
        try { var value = await service.ReadProcedureDraftAsync(id, Actor(context).Sid, ct); return Results.Ok(new { procedure = value.Procedure, token = value.Token }); }
        catch (KeyNotFoundException) { return Results.NotFound(); }
    }

    private static async Task<IResult> SaveProcedureDraftAsync(Guid id, ProcedureSaveDraftRequest request, HttpContext context, [Microsoft.AspNetCore.Mvc.FromServices] ScientificSetupService service, CancellationToken ct)
    {
        if (context.Request.ContentLength > 65536) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        if (id == Guid.Empty || request.OperationId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest();
        try { var value = await service.SaveProcedureDraftAsync(new ProcedureDefinition(id, request.Name.Trim(), request.Document, request.Parameters, request.PlannedCollections), request.ExpectedToken, Actor(context).Sid, request.OperationId, ct); return Results.Ok(new { token = value.Token, receipt = value.Receipt }); }
        catch (ArgumentException exception) { return Results.BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Results.Conflict(new { error = exception.Message }); }
    }

    private static async Task<IResult> PublishProcedureAsync(Guid id, ScientificPublishRequest request, HttpContext context, [Microsoft.AspNetCore.Mvc.FromServices] ScientificSetupService service, IScientificSetupRepository repository, CancellationToken ct)
    {
        if (request.OperationId == Guid.Empty) return Results.BadRequest();
        var latest = await repository.ReadProcedureLatestAsync(id, Actor(context).Sid, ct); if (latest is null) return Results.NotFound();
        try { var value = await service.PublishProcedureAsync(id, request.ExpectedRevisionId, request.Name?.Trim() ?? latest.Name, Actor(context).Sid, request.OperationId, ct); return Results.Ok(new { revision = value.Revision, receipt = value.Receipt }); }
        catch (InvalidOperationException exception) { return Results.Conflict(new { error = exception.Message }); }
    }

    private static async Task<IResult> ReadProcedureAsync(Guid id, HttpContext context, IScientificSetupRepository repository, CancellationToken ct) { var value = await repository.ReadProcedureLatestAsync(id, Actor(context).Sid, ct); return value is null ? Results.NotFound() : Results.Ok(value); }
    private static async Task<IResult> ProcedureHistoryAsync(Guid id, HttpContext context, IScientificSetupRepository repository, CancellationToken ct) => Results.Ok(await repository.ProcedureHistoryAsync(id, Actor(context).Sid, ct));
    private static async Task<IResult> ReadProcedureRevisionAsync(Guid id, Guid revisionId, HttpContext context, IScientificSetupRepository repository, CancellationToken ct) { var value = await repository.ReadProcedureAsync(id, revisionId, Actor(context).Sid, ct); return value is null ? Results.NotFound() : Results.Ok(value); }

    private static async Task<IResult> ReadPlanDraftAsync(Guid id, HttpContext context, [Microsoft.AspNetCore.Mvc.FromServices] ScientificSetupService service, CancellationToken ct)
    {
        try { var value = await service.ReadPlanDraftAsync(id, Actor(context).Sid, ct); return Results.Ok(new { plan = value.Plan, token = value.Token }); }
        catch (KeyNotFoundException) { return Results.NotFound(); }
    }

    private static async Task<IResult> SavePlanDraftAsync(Guid id, PlanSaveDraftRequest request, HttpContext context, [Microsoft.AspNetCore.Mvc.FromServices] ScientificSetupService service, IScientificSetupRepository repository, CancellationToken ct)
    {
        if (context.Request.ContentLength > 65536) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        if (id == Guid.Empty || request.OperationId == Guid.Empty || request.ProcedureRevisionId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest();
        var procedure = await ReadPinnedProcedureAsync(request.ProcedureRevisionId, Actor(context).Sid, repository, ct); if (procedure is null) return Results.NotFound();
        try { var value = await service.SavePlanDraftAsync(new ExperimentPlanDefinition(id, request.Name.Trim(), request.ProcedureRevisionId, request.Parameters), procedure, request.ExpectedToken, Actor(context).Sid, request.OperationId, ct); return Results.Ok(new { token = value.Token, receipt = value.Receipt }); }
        catch (ArgumentException exception) { return Results.BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Results.Conflict(new { error = exception.Message }); }
    }

    private static async Task<IResult> PublishPlanAsync(Guid id, ScientificPublishRequest request, HttpContext context, [Microsoft.AspNetCore.Mvc.FromServices] ScientificSetupService service, IScientificSetupRepository repository, CancellationToken ct)
    {
        if (request.OperationId == Guid.Empty) return Results.BadRequest();
        var latest = await repository.ReadPlanLatestAsync(id, Actor(context).Sid, ct); if (latest is null) return Results.NotFound();
        try { var value = await service.PublishPlanAsync(id, request.ExpectedRevisionId, request.Name?.Trim() ?? latest.Name, Actor(context).Sid, request.OperationId, ct); return Results.Ok(new { revision = value.Revision, receipt = value.Receipt }); }
        catch (InvalidOperationException exception) { return Results.Conflict(new { error = exception.Message }); }
    }

    private static async Task<IResult> ReadPlanAsync(Guid id, HttpContext context, IScientificSetupRepository repository, CancellationToken ct) { var value = await repository.ReadPlanLatestAsync(id, Actor(context).Sid, ct); return value is null ? Results.NotFound() : Results.Ok(value); }
    private static async Task<IResult> PlanHistoryAsync(Guid id, HttpContext context, IScientificSetupRepository repository, CancellationToken ct) => Results.Ok(await repository.PlanHistoryAsync(id, Actor(context).Sid, ct));
    private static async Task<IResult> ReadPlanRevisionAsync(Guid id, Guid revisionId, HttpContext context, IScientificSetupRepository repository, CancellationToken ct) { var value = await repository.ReadPlanAsync(id, revisionId, Actor(context).Sid, ct); return value is null ? Results.NotFound() : Results.Ok(value); }

    private static async Task<ProcedureDefinition?> ReadPinnedProcedureAsync(Guid revisionId, string actorSid, IScientificSetupRepository repository, CancellationToken ct)
    {
        var revision = await repository.ReadProcedureByRevisionAsync(revisionId, actorSid, ct); if (revision is null) return null;
        using var document = JsonDocument.Parse(revision.CanonicalContent);
        var content = document.RootElement.TryGetProperty("content", out var envelope) ? envelope : document.RootElement;
        return JsonSerializer.Deserialize<ProcedureDefinition>(content.GetRawText());
    }

    private static async Task<IResult> CreateAsync(ApparatusCreateRequest request, HttpContext context, IScientificSetupRepository repository, CancellationToken ct)
    {
        if (context.Request.ContentLength > 65536) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        if (request.OperationId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest(new { error = "Operation ID and name are required." });
        var actor = Actor(context);
        try
        {
            var result = await repository.CreateAsync(new ApparatusDefinition(Guid.NewGuid(), request.Name.Trim(), request.Components ?? [], request.Connections, request.Assertions), actor.Sid, request.OperationId, ct);
            return Results.Created($"/api/scientific-setup/apparatus/{result.Apparatus.ApparatusId}", Receipt(result.Receipt, result.Apparatus.ApparatusId, null));
        }
        catch (ArgumentException exception) { return Results.BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Results.Conflict(new { error = exception.Message }); }
    }

    private static async Task<IResult> ReadDraftAsync(Guid id, HttpContext context, IScientificSetupRepository repository, CancellationToken ct)
    {
        try
        {
            var result = await repository.ReadDraftAsync(id, Actor(context).Sid, ct);
            return Results.Ok(new { apparatus = result.Apparatus, token = result.Token });
        }
        catch (KeyNotFoundException) { return Results.NotFound(); }
    }

    private static async Task<IResult> SaveDraftAsync(Guid id, ApparatusSaveDraftRequest request, HttpContext context, IScientificSetupRepository repository, CancellationToken ct)
    {
        if (request.OperationId == Guid.Empty || id == Guid.Empty || string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest(new { error = "Operation ID and name are required." });
        try
        {
            var apparatus = new ApparatusDefinition(id, request.Name.Trim(), request.Components ?? [], request.Connections, request.Assertions);
            var result = await repository.SaveDraftAsync(apparatus, request.ExpectedToken, Actor(context).Sid, request.OperationId, ct);
            return Results.Ok(new { receipt = Receipt(result.Receipt, id, null), token = result.Token });
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
