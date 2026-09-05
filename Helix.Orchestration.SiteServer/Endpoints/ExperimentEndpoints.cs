using System.Text.Json;
using Helix.Orchestration.Core.Devices;
using Helix.Orchestration.Core.WSL;
using Helix.Orchestration.Infrastructure.Persistence;
using Helix.Orchestration.SiteServer.Agent;
using Helix.Orchestration.SiteServer.Contracts;

namespace Helix.Orchestration.SiteServer.Endpoints;

/// <summary>
/// Minimal API endpoints for experiment lifecycle management.
/// </summary>
public static class ExperimentEndpoints
{
    public static void MapExperimentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/experiments")
            .WithTags("Experiments");

        // GET /api/experiments — List all experiments (history browser, E4)
        group.MapGet("/", async Task<IResult> (
            IExperimentRepository experimentRepository,
            CancellationToken ct) =>
        {
            try
            {
                var records = await experimentRepository.GetAllAsync(ct);
                var dtos = records.Select(r => new ExperimentSummaryDto
                {
                    RunspaceId   = r.RunspaceId,
                    CampaignId   = r.CampaignId,
                    Name         = r.Name,
                    Status       = r.Status,
                    CreatedAt    = r.CreatedAt,
                    StartedAt    = r.StartedAt,
                    EndedAt      = r.EndedAt,
                    FaultMessage = r.FaultMessage,
                });
                return Results.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to retrieve experiment list",
                    detail: ex.Message, statusCode: 500);
            }
        })
        .WithName("GetAllExperiments")
        .WithSummary("List all experiments (history browser)")
        .Produces<IEnumerable<ExperimentSummaryDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // POST /api/experiments — Submit new experiment for execution
        group.MapPost("/", async Task<IResult> (
            SubmitExperimentRequest request,
            IAgentProxy agentProxy,
            IWslCompiler wslCompiler,
            DeviceRegistry deviceRegistry,
            CancellationToken ct) =>
        {
            try
            {
                // Deserialize WslSource as a WslDocument JSON payload.
                WslDocument document;
                try
                {
                    document = JsonSerializer.Deserialize<WslDocument>(request.WslSource,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? throw new InvalidOperationException("WslSource deserialized to null.");
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { Error = "Invalid WslSource JSON", Detail = ex.Message });
                }

                var compiled = wslCompiler.Compile(document, deviceRegistry);
                if (!compiled.Success)
                    return Results.BadRequest(new { Errors = compiled.Diagnostics });

                var runspaceId = await agentProxy.SubmitExperimentAsync(compiled.Definition!, ct);

                return Results.Ok(new { RunspaceId = runspaceId });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to submit experiment",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("SubmitExperiment")
        .WithSummary("Submit a new experiment for execution")
        .Produces<object>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // GET /api/experiments/{id} — Query experiment/runspace status
        group.MapGet("/{id}", async Task<IResult> (
            string id,
            IAgentProxy agentProxy,
            CancellationToken ct) =>
        {
            try
            {
                var status = await agentProxy.GetRunspaceStatusAsync(id, ct);

                var response = new ExperimentStatusResponse
                {
                    RunspaceId = status.RunspaceId,
                    Status = status.Status,
                    CreatedAt = status.CreatedAt,
                    StartedAt = status.StartedAt,
                    EndedAt = status.EndedAt,
                    FaultMessage = status.FaultMessage
                };

                return Results.Ok(response);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new { Error = $"Experiment '{id}' not found." });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to retrieve experiment status",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("GetExperimentStatus")
        .WithSummary("Retrieve the status of a running or completed experiment")
        .Produces<ExperimentStatusResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // DELETE /api/experiments/{id} — Abort a running experiment
        group.MapDelete("/{id}", async Task<IResult> (
            string id,
            IAgentProxy agentProxy,
            CancellationToken ct) =>
        {
            try
            {
                await agentProxy.AbortRunspaceAsync(id, ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new { Error = $"Experiment '{id}' not found." });
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
        .WithSummary("Abort a running experiment")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
