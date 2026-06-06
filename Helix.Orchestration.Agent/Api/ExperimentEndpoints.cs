using Helix.Orchestration.Agent.Events;
using Helix.Orchestration.Agent.Experiments;
using Helix.Orchestration.Core.Runspace;
using Helix.Orchestration.Core.WSL;
using Helix.Orchestration.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Helix.Orchestration.Agent.Api;

/// <summary>
/// Minimal API endpoint group for experiment lifecycle management.
/// </summary>
public static class ExperimentEndpoints
{
    /// <summary>
    /// Maps the /experiments endpoint group onto <paramref name="app"/>.
    /// </summary>
    public static void MapExperimentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/experiments")
            .WithTags("Experiments");

        // POST /experiments — start a new experiment, create its runspace, return context
        group.MapPost("/", async Task<IResult> (
            ExperimentStartRequest request,
            IExperimentManager experimentManager,
            IRunspaceManager runspaceManager,
            IRunspaceEventEmitter emitter,
            CancellationToken ct) =>
        {
            try
            {
                var ctx = await experimentManager.StartAsync(request, ct);

                // Create a stub RunspaceDefinition for the experiment main phase
                var definition = new RunspaceDefinition(
                    DefinitionId: ctx.ExperimentId,
                    Name: ctx.Name,
                    Nodes: Array.Empty<WslNode>(),
                    Edges: Array.Empty<WslEdge>(),
                    Metadata: new Dictionary<string, object?>());

                var runspace = await runspaceManager.CreateAsync(definition, ct);
                emitter.Publish(new RunspaceStartedEvent(runspace.RunspaceId));
                await runspace.StartAsync(ct);

                experimentManager.SetActiveRunspace(ctx.ExperimentId, runspace.RunspaceId);

                // Re-fetch to include the updated ActiveRunspaceId
                var updated = experimentManager.Get(ctx.ExperimentId) ?? ctx;

                return Results.Ok(new
                {
                    experimentId = updated.ExperimentId,
                    name = updated.Name,
                    lifecycle = updated.Lifecycle.ToString(),
                    activePhase = updated.ActivePhase?.ToString(),
                    activeRunspaceId = updated.ActiveRunspaceId
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
}
