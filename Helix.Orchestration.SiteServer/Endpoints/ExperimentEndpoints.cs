using Helix.Orchestration.Core.WSL;
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

        // POST /api/experiments — Submit new experiment for execution
        group.MapPost("/", async Task<IResult> (
            SubmitExperimentRequest request,
            IAgentProxy agentProxy,
            CancellationToken ct) =>
        {
            try
            {
                // Phase 9: Parse and compile WSL (stub: create minimal RunspaceDefinition)
                var definition = new RunspaceDefinition(
                    DefinitionId: request.ExperimentId ?? Guid.NewGuid().ToString("N"),
                    Name: request.ExperimentName ?? "Unnamed Experiment",
                    Nodes: Array.Empty<WslNode>(),
                    Edges: Array.Empty<WslEdge>(),
                    Metadata: new Dictionary<string, object?>());

                var runspaceId = await agentProxy.SubmitExperimentAsync(definition, ct);

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
