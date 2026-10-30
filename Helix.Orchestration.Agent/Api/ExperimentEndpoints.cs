using Helix.Orchestration.Agent.Events;
using Helix.Orchestration.Agent.Experiments;
using Helix.Orchestration.Agent.Security;
using Helix.Orchestration.Core.Devices;
using Helix.Orchestration.Core.Runspace;
using Helix.Orchestration.Core.WSL;
using Helix.Orchestration.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Text.Json;

namespace Helix.Orchestration.Agent.Api;

/// <summary>
/// Minimal API endpoint group for experiment lifecycle management.
/// </summary>
public static class ExperimentEndpoints
{
    /// <summary>
    /// A scientist's workflow proposal. The client never supplies a compiled
    /// definition or an authorization token.
    /// </summary>
    public sealed record ExperimentSubmissionRequest(
        string Name,
        WslDocument Workflow,
        string ApprovedPolicyRevision,
        Dictionary<string, object?>? Metadata = null);

    /// <summary>
    /// Maps the /experiments endpoint group onto <paramref name="app"/>.
    /// </summary>
    public static void MapExperimentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/experiments")
            .WithTags("Experiments");

        // POST /experiments — start a new experiment, create its runspace, return context
        group.MapPost("/", async Task<IResult> (
            ExperimentSubmissionRequest request,
            HttpContext httpContext,
            IExperimentManager experimentManager,
            IRunspaceManager runspaceManager,
            IRunspaceEventEmitter emitter,
            IWslCompiler compiler,
            DeviceRegistry deviceRegistry,
            CancellationToken ct) =>
        {
            try
            {
                if (httpContext.Items[DesktopSessionAuthenticationHandler.ActorItemKey] is not VerifiedDesktopActor actor)
                    return Results.Unauthorized();
                if (request.Workflow is null || string.IsNullOrWhiteSpace(request.ApprovedPolicyRevision))
                    return Results.BadRequest(new { code = "experiment.invalid_submission", message = "A workflow and approved policy revision are required." });

                // Minimal-API binding represents object-valued WSL parameters as
                // JsonElement. Normalize the transport representation before strict
                // canonical WSL validation; this does not accept alternate names or
                // values, it only restores the authored scalar types.
                var workflow = NormalizeTransportValues(request.Workflow);
                var compilation = compiler.Compile(workflow, deviceRegistry);
                if (!compilation.Success || compilation.Definition is null)
                    return Results.BadRequest(new { code = "experiment.workflow_rejected", diagnostics = compilation.Diagnostics });

                var definition = compilation.Definition;
                var definitionHash = definition.Metadata.TryGetValue("definitionHash", out var hash)
                    ? hash?.ToString() ?? string.Empty
                    : string.Empty;
                if (string.IsNullOrWhiteSpace(definitionHash))
                    return Results.Problem(title: "Workflow compilation did not produce a canonical definition hash.", statusCode: StatusCodes.Status500InternalServerError);

                var ctx = await experimentManager.StartAsync(
                    new ExperimentStartRequest(request.Name, definition.DefinitionId, request.Metadata),
                    ct);
                experimentManager.SetCanonicalDefinition(ctx.ExperimentId, new CanonicalExperimentDefinition(
                    definition,
                    request.ApprovedPolicyRevision,
                    definitionHash,
                    actor.Sid,
                    DateTimeOffset.UtcNow));

                var runspace = await runspaceManager.CreateAsync(definition, ct);
                emitter.Publish(new RunspaceStartedEvent(runspace.RunspaceId)
                    with { ExperimentId = ctx.ExperimentId });
                // The execution body resolves its Agent-held canonical definition
                // from the active runspace. Establish that binding before StartAsync
                // can schedule the body, never afterwards.
                experimentManager.SetActiveRunspace(ctx.ExperimentId, runspace.RunspaceId);
                await runspace.StartAsync(ct);

                // Re-fetch to include the updated ActiveRunspaceId
                var updated = experimentManager.Get(ctx.ExperimentId) ?? ctx;

                return Results.Ok(new
                {
                    experimentId = updated.ExperimentId,
                    name = updated.Name,
                    lifecycle = updated.Lifecycle.ToString(),
                    activePhase = updated.ActivePhase?.ToString(),
                    activeRunspaceId = updated.ActiveRunspaceId,
                    definitionHash
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to start experiment",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("CreateExperiment")
        .WithSummary("Start a new experiment and create its runspace")
        .Produces<object>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // GET /experiments/{id} — return experiment context or 404
        group.MapGet("/{id}", IResult (string id, IExperimentManager experimentManager) =>
        {
            var ctx = experimentManager.Get(id);
            if (ctx is null)
                return Results.NotFound(new { Error = $"Experiment '{id}' not found." });

            return Results.Ok(new
            {
                experimentId = ctx.ExperimentId,
                name = ctx.Name,
                lifecycle = ctx.Lifecycle.ToString(),
                activePhase = ctx.ActivePhase?.ToString(),
                activeRunspaceId = ctx.ActiveRunspaceId,
                createdAt = ctx.CreatedAt,
                startedAt = ctx.StartedAt,
                endedAt = ctx.EndedAt
            });
        })
        .WithName("GetExperiment")
        .WithSummary("Get experiment context by ID")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // GET /experiments — return all active experiments
        group.MapGet("/", IResult (IExperimentManager experimentManager) =>
        {
            var active = experimentManager.ListActive();
            return Results.Ok(active.Select(ctx => new
            {
                experimentId = ctx.ExperimentId,
                name = ctx.Name,
                lifecycle = ctx.Lifecycle.ToString(),
                activePhase = ctx.ActivePhase?.ToString(),
                activeRunspaceId = ctx.ActiveRunspaceId
            }));
        })
        .WithName("ListExperiments")
        .WithSummary("List all active experiments")
        .Produces<IEnumerable<object>>(StatusCodes.Status200OK);

        // POST /experiments/{id}/pause
        group.MapPost("/{id}/pause", async Task<IResult> (
            string id,
            IExperimentManager experimentManager,
            IRunspaceManager runspaceManager,
            CancellationToken ct) =>
        {
            try
            {
                var ctx = experimentManager.Get(id);
                if (ctx is null)
                    return Results.NotFound(new { Error = $"Experiment '{id}' not found." });

                if (ctx.ActiveRunspaceId is { } runspaceId)
                {
                    var rs = runspaceManager.Get(runspaceId);
                    if (rs is not null)
                        await rs.PauseAsync(ct);
                }

                await experimentManager.PauseAsync(id, ct);
                return Results.Ok();
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to pause experiment",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("PauseExperiment")
        .WithSummary("Pause an active experiment and its runspace")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // POST /experiments/{id}/resume
        group.MapPost("/{id}/resume", async Task<IResult> (
            string id,
            IExperimentManager experimentManager,
            IRunspaceManager runspaceManager,
            CancellationToken ct) =>
        {
            try
            {
                var ctx = experimentManager.Get(id);
                if (ctx is null)
                    return Results.NotFound(new { Error = $"Experiment '{id}' not found." });

                if (ctx.ActiveRunspaceId is { } runspaceId)
                {
                    var rs = runspaceManager.Get(runspaceId);
                    if (rs is not null)
                        await rs.ResumeAsync(ct);
                }

                await experimentManager.ResumeAsync(id, ct);
                return Results.Ok();
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to resume experiment",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("ResumeExperiment")
        .WithSummary("Resume a paused experiment and its runspace")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // POST /experiments/{id}/abort
        group.MapPost("/{id}/abort", async Task<IResult> (
            string id,
            IExperimentManager experimentManager,
            IRunspaceManager runspaceManager,
            CancellationToken ct) =>
        {
            try
            {
                var ctx = experimentManager.Get(id);
                if (ctx is null)
                    return Results.NotFound(new { Error = $"Experiment '{id}' not found." });

                if (ctx.ActiveRunspaceId is { } runspaceId)
                    await runspaceManager.AbortAsync(runspaceId, ct);

                await experimentManager.AbortAsync(id, ct);
                return Results.Ok();
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to abort experiment",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("AbortExperiment")
        .WithSummary("Abort a running or paused experiment")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // GET /experiments/{id}/replay — return ordered event log for replay
        group.MapGet("/{id}/replay", async Task<IResult> (
            string id,
            [FromServices] IExperimentEventRepository eventRepository,
            CancellationToken ct) =>
        {
            try
            {
                var events = await eventRepository.GetReplayLogAsync(id, ct);
                return Results.Ok(events);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to retrieve experiment replay log",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("GetExperimentReplay")
        .WithSummary("Get ordered event replay log for an experiment")
        .Produces<IReadOnlyList<ExperimentEventRecord>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static WslDocument NormalizeTransportValues(WslDocument workflow) => workflow with
    {
        Nodes = workflow.Nodes.Select(node => node with
        {
            Parameters = node.Parameters.ToDictionary(
                pair => pair.Key,
                pair => NormalizeTransportValue(pair.Value),
                StringComparer.Ordinal)
        }).ToArray()
    };

    private static object? NormalizeTransportValue(object? value) => value is JsonElement element
        ? element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(item => NormalizeTransportValue(item)).ToArray(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                property => property.Name,
                property => NormalizeTransportValue(property.Value),
                StringComparer.Ordinal),
            _ => value
        }
        : value;}
