using Helix.Orchestration.Core.Devices;
using Helix.Orchestration.Core.Safety;
using Helix.Orchestration.Core.WSL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Helix.Orchestration.Agent.Api;

/// <summary>
/// Minimal API endpoint group for WSL workflow compilation.
/// </summary>
public static class WorkflowEndpoints
{
    /// <summary>
    /// Maps the /workflows endpoint group onto <paramref name="app"/>.
    /// </summary>
    public static void MapWorkflowEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/workflows")
            .WithTags("Workflows");

        // POST /workflows/compile — accepts a WslDocument, returns RunspaceDefinition or 422 diagnostics
        group.MapPost("/compile", async Task<IResult> (
            WslDocument document,
            IWslCompiler compiler,
            DeviceRegistry deviceRegistry,
            ISafetyEngine safetyEngine,
            CancellationToken ct) =>
        {
            try
            {
                var result = compiler.Compile(document, deviceRegistry, safetyEngine);

                if (result.Success)
                    return Results.Ok(result.Definition);

                return Results.UnprocessableEntity(result.Diagnostics);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Compilation error",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("CompileWorkflow")
        .WithSummary("Compile a WSL document into an executable RunspaceDefinition")
        .Produces<RunspaceDefinition>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
