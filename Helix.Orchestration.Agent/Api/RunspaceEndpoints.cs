using Helix.Orchestration.Core.Runspace;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Helix.Orchestration.Agent.Api;

/// <summary>
/// Minimal API endpoint group for runspace lifecycle management.
/// </summary>
public static class RunspaceEndpoints
{
    /// <summary>
    /// Maps the /runspaces endpoint group onto <paramref name="app"/>.
    /// </summary>
    public static void MapRunspaceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/runspaces")
            .WithTags("Runspaces");

        // POST /runspaces — submit a RunspaceDefinition, start execution, return runspaceId
        // Client-supplied definitions are proposals, never execution authority.
        group.MapPost("/", () => Results.BadRequest(new
        {
            code = "runspace.raw_definition_unsupported",
            message = "Raw runspace definitions cannot be submitted for execution. Submit a WSL workflow to /experiments."
        }))
        .WithName("CreateRunspace")
        .WithSummary("Reject raw runspace definitions")
        .Produces(StatusCodes.Status400BadRequest);

        // GET /runspaces/{id} — return runspace status; 404 when unknown
        group.MapGet("/{id}", IResult (string id, IRunspaceManager manager) =>
        {
            var rs = manager.Get(id);
            if (rs is null)
                return Results.NotFound(new { Error = $"Runspace '{id}' not found." });

            return Results.Ok(new
            {
                runspaceId = rs.RunspaceId,
                status = rs.Status.ToString(),
                startedAt = rs.StartedAt,
                endedAt = rs.EndedAt
            });
        })
        .WithName("GetRunspace")
        .WithSummary("Get runspace status by ID")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST /runspaces/{id}/pause
        group.MapPost("/{id}/pause", async Task<IResult> (
            string id,
            IRunspaceManager manager,
            CancellationToken ct) =>
        {
            try
            {
                var rs = manager.Get(id);
                if (rs is null)
                    return Results.NotFound(new { Error = $"Runspace '{id}' not found." });

                await rs.PauseAsync(ct);
                return Results.Ok();
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to pause runspace",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("PauseRunspace")
        .WithSummary("Pause an active runspace")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // POST /runspaces/{id}/resume
        group.MapPost("/{id}/resume", async Task<IResult> (
            string id,
            IRunspaceManager manager,
            CancellationToken ct) =>
        {
            try
            {
                var rs = manager.Get(id);
                if (rs is null)
                    return Results.NotFound(new { Error = $"Runspace '{id}' not found." });

                await rs.ResumeAsync(ct);
                return Results.Ok();
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to resume runspace",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("ResumeRunspace")
        .WithSummary("Resume a paused runspace")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // POST /runspaces/{id}/abort
        group.MapPost("/{id}/abort", async Task<IResult> (
            string id,
            IRunspaceManager manager,
            CancellationToken ct) =>
        {
            try
            {
                var rs = manager.Get(id);
                if (rs is null)
                    return Results.NotFound(new { Error = $"Runspace '{id}' not found." });

                await rs.AbortAsync(ct);
                return Results.Ok();
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to abort runspace",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("AbortRunspace")
        .WithSummary("Abort a running or paused runspace")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
