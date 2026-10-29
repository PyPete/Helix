using System.Security.Claims;
using Helix.Orchestration.Agent.Execution;
using Helix.Orchestration.Core.Devices;
using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Core.Safety;
using Helix.Orchestration.Core.WSL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Helix.Orchestration.Agent.Api;

/// <summary>
/// A compilation response is proposal feedback only. A successful admission
/// snapshot is deliberately not reusable as a command-dispatch credential.
/// </summary>
public sealed record WorkflowCompileResponse(
    RunspaceDefinition? Definition,
    IReadOnlyList<CompilationDiagnostic> Diagnostics,
    CommandAdmissionState AdmissionState,
    IReadOnlyList<CommandCheckEvidence> Checks,
    bool DispatchAuthorized = false);

/// <summary>Minimal API endpoint group for non-authorizing WSL proposal compilation.</summary>
public static class WorkflowEndpoints
{
    /// <summary>Maps the /workflows endpoint group onto <paramref name="app"/>.</summary>
    public static void MapWorkflowEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/workflows")
            .WithTags("Workflows");

        // Compilation is intentionally an authenticated proposal path, not a
        // device path. Dispatch repeats admission checks immediately before an
        // effect and never consumes this response as authority.
        group.MapPost("/compile", async Task<IResult> (
            HttpContext httpContext,
            WslDocument document,
            IWslCompiler compiler,
            DeviceRegistry deviceRegistry,
            ISafetyEngine safetyEngine,
            AdmissionContextProvider admissionContexts,
            CommandAdmissionEvaluator admissionEvaluator,
            CancellationToken ct) =>
        {
            var actorSid = httpContext.User.FindFirstValue(ClaimTypes.Sid)
                ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(actorSid) || httpContext.User.Identity?.IsAuthenticated != true)
            {
                // The request body cannot provide a scientist identity. An
                // unauthenticated caller gets no compilation feedback that
                // could be mistaken for Agent-approved policy evidence.
                return Results.Unauthorized();
            }

            if (compiler is not IAdmissionAwareWslCompiler admissionAwareCompiler)
            {
                return Results.Problem(
                    title: "Safe compiler unavailable",
                    detail: "The configured compiler cannot produce non-authorizing admission feedback.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            try
            {
                // Header input is only a candidate revision. The fresh provider
                // compares it to Agent-held approved-policy state, so neither a
                // header nor a compile response can create authority.
                var policyRevision = httpContext.Request.Headers["X-Helix-Policy-Revision"].ToString();
                var sessionReference = httpContext.User.FindFirstValue("helix.session") ?? "authenticated-session";
                var intent = new CommandIntent(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "simulator",
                    "GetFlow",
                    "{}",
                    actorSid,
                    httpContext.User.Identity.Name ?? actorSid,
                    sessionReference,
                    policyRevision,
                    DateTimeOffset.UtcNow);

                var context = await admissionContexts.CreateAsync(intent, ct).ConfigureAwait(false);
                var result = admissionAwareCompiler.CompileWithFeedback(
                    document,
                    deviceRegistry,
                    admissionEvaluator,
                    context,
                    safetyEngine);

                var response = new WorkflowCompileResponse(
                    result.Compilation.Definition,
                    result.Compilation.Diagnostics,
                    result.Admission.State,
                    result.Admission.Checks);

                return result.Compilation.Success
                    ? Results.Ok(response)
                    : Results.UnprocessableEntity(response);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Compilation error",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("CompileWorkflow")
        .WithSummary("Compile a WSL proposal with fresh policy and safety feedback; never dispatches")
        .Produces<WorkflowCompileResponse>(StatusCodes.Status200OK)
        .Produces<WorkflowCompileResponse>(StatusCodes.Status422UnprocessableEntity)
        .Produces(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
