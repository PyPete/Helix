using Helix.Orchestration.Agent.Samples;
using Helix.Orchestration.Agent.Security;
using Helix.Orchestration.Core.Samples;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Helix.Orchestration.Agent.Api;

public sealed record SampleConfirmationRequest(Guid OperationId, string Location, DateTimeOffset OccurredAtUtc, string? Label = null, string? Custodian = null, Guid? PlannedReferenceId = null);
public sealed record SampleEventAppendRequest(Guid OperationId, SampleEventKind Kind, string? Label, string? Custodian, string Location, DateTimeOffset OccurredAtUtc, long ExpectedSequence, Guid? SupersedesEventId = null, string? CorrectionReason = null);
public sealed record SampleTransformationRequest(Guid OperationId, Guid TransformationId, SampleTransformationKind Kind, string Operation, IReadOnlyList<Guid> ParentSampleIds, IReadOnlyList<Guid> ChildSampleIds, SampleQuantity? Quantity = null, bool QuantitySupplied = false, DateTimeOffset? OccurredAtUtc = null);

public static class SampleEndpoints
{
    public static void MapSampleEndpoints(this WebApplication app)
    {
        var routes = app.MapGroup("/api/samples");
        routes.MapGet("/", ListAsync);
        routes.MapGet("/{id:guid}", ReadAsync);
        routes.MapPost("/confirmations", ConfirmAsync);
        routes.MapPost("/{id:guid}/events", AppendAsync);
        routes.MapGet("/{id:guid}/events", EventsAsync);
        routes.MapPost("/transformations", TransformAsync);
        routes.MapGet("/{id:guid}/lineage", LineageAsync);
        app.MapGet("/api/scientific-operations/{operationId:guid}", OperationAsync);
    }

    private static async Task<IResult> ListAsync(HttpContext context, SampleService service, int? pageSize, CancellationToken ct) =>
        Results.Ok(await service.ListAsync(Actor(context).Sid, Math.Clamp(pageSize ?? 25, 1, 100), ct));

    private static async Task<IResult> ReadAsync(Guid id, HttpContext context, SampleService service, CancellationToken ct)
    {
        var sample = await service.ReadAsync(id, Actor(context).Sid, ct);
        return sample is null ? Results.NotFound() : Results.Ok(sample);
    }

    private static async Task<IResult> ConfirmAsync(SampleConfirmationRequest request, HttpContext context, SampleService service, CancellationToken ct)
    {
        if (context.Request.ContentLength > 16 * 1024) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        // Planned activity is optional and cannot itself create a fact. Until a later plan owns verified planned-reference resolution,
        // confirmation deliberately stores only the independently admitted collection evidence.
        if (request.PlannedReferenceId is not null) return Results.UnprocessableEntity(new { error = "Planned references are not admitted by this confirmation route." });
        try
        {
            var result = await service.ConfirmAsync(new SampleConfirmation(request.Location, request.OccurredAtUtc, request.Label, request.Custodian), Actor(context).Sid, request.OperationId, ct);
            return result.Replayed ? Results.Ok(new { sample = result.Projection, receipt = result.Receipt }) : Results.Created($"/api/samples/{result.Projection.Identity.SampleId}", new { sample = result.Projection, receipt = result.Receipt });
        }
        catch (ArgumentOutOfRangeException exception) { return Results.BadRequest(new { error = exception.Message }); }
        catch (ArgumentException exception) { return Results.BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Results.Conflict(new { error = exception.Message }); }
    }

    private static async Task<IResult> AppendAsync(Guid id, SampleEventAppendRequest request, HttpContext context, SampleService service, CancellationToken ct)
    {
        if (context.Request.ContentLength > 16 * 1024) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        try { var result = await service.AppendAsync(id, request.Kind, request.Label, request.Custodian, request.Location, request.OccurredAtUtc, request.ExpectedSequence, request.SupersedesEventId, request.CorrectionReason, Actor(context).Sid, request.OperationId, ct); return Results.Ok(new { sample = result.Projection, receipt = result.Receipt }); }
        catch (ArgumentOutOfRangeException exception) { return Results.BadRequest(new { error = exception.Message }); }
        catch (ArgumentException exception) { return Results.BadRequest(new { error = exception.Message }); }
        catch (KeyNotFoundException) { return Results.NotFound(); }
        catch (InvalidOperationException exception) { return Results.Conflict(new { error = exception.Message }); }
    }

    private static async Task<IResult> EventsAsync(Guid id, HttpContext context, SampleService service, long? afterSequence, int? pageSize, CancellationToken ct) =>
        Results.Ok(await service.EventsAsync(id, Actor(context).Sid, afterSequence, Math.Clamp(pageSize ?? 25, 1, 100), ct));

    private static async Task<IResult> TransformAsync(SampleTransformationRequest request, HttpContext context, SampleService service, CancellationToken ct)
    {
        if (context.Request.ContentLength > 16 * 1024) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        try
        {
            var transformation = new SampleTransformation(request.TransformationId, request.Kind, request.Operation, request.ParentSampleIds ?? [], request.ChildSampleIds ?? [], request.Quantity, request.QuantitySupplied, request.OccurredAtUtc);
            var result = await service.TransformAsync(transformation, Actor(context).Sid, request.OperationId, ct);
            var response = new { childIds = result.Children.Select(child => child.Identity.SampleId), operationId = result.Receipt.OperationId };
            return result.Replayed ? Results.Ok(response) : Results.Created($"/api/samples/{result.Children[0].Identity.SampleId}", response);
        }
        catch (ArgumentOutOfRangeException exception) { return Results.BadRequest(new { error = exception.Message }); }
        catch (ArgumentException exception) { return Results.BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException exception) { return Results.Conflict(new { error = exception.Message }); }
    }

    private static async Task<IResult> LineageAsync(Guid id, string direction, Guid? continuation, int? pageSize, HttpContext context, SampleService service, CancellationToken ct)
    {
        if (!Enum.TryParse<SampleLineageDirection>(direction, ignoreCase: false, out var parsedDirection)) return Results.BadRequest(new { error = "Lineage direction is invalid." });
        try { return Results.Ok(await service.LineageAsync(id, parsedDirection, continuation, Math.Clamp(pageSize ?? 25, 1, 100), Actor(context).Sid, ct)); }
        catch (ArgumentOutOfRangeException exception) { return Results.BadRequest(new { error = exception.Message }); }
        catch (ArgumentException exception) { return Results.BadRequest(new { error = exception.Message }); }
        catch (InvalidOperationException) { return Results.NotFound(); }
    }

    private static async Task<IResult> OperationAsync(Guid operationId, string? kind, HttpContext context, SampleService service, CancellationToken ct)
    {
        if (!string.Equals(kind, "sample.confirm", StringComparison.Ordinal) && !string.Equals(kind, "sample.append", StringComparison.Ordinal) && !string.Equals(kind, "sample.transform", StringComparison.Ordinal)) return Results.BadRequest();
        var receipt = await service.OperationAsync(operationId, Actor(context).Sid, ct);
        return receipt is null || receipt.Kind != kind ? Results.NotFound() : Results.Ok(receipt);
    }

    private static VerifiedDesktopActor Actor(HttpContext context) => context.Items[DesktopSessionAuthenticationHandler.ActorItemKey] as VerifiedDesktopActor ?? throw new UnauthorizedAccessException();
}
