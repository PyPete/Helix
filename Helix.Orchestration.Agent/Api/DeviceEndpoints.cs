using System.Text.Json;
using Helix.Orchestration.Agent.Events;
using Helix.Orchestration.Agent.Experiments;
using Helix.Orchestration.Agent.Security;
using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Devices.Drivers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CapabilityDescriptor = Helix.Orchestration.Core.Capabilities.CapabilityDescriptor;

namespace Helix.Orchestration.Agent.Api;

/// <summary>Request body for POST /devices/{id}/execute.</summary>
public sealed record ExecuteRequest(string Capability, Dictionary<string, object>? Parameters);

/// <summary>Minimal API endpoint group for simulator discovery and command proposals.</summary>
public static class DeviceEndpoints
{
    private const string SimulatorId = "simulator";
    private const string SimulationPolicyRevision = "simulation-policy-v1";

    /// <summary>Maps the /devices endpoint group onto <paramref name="app"/>.</summary>
    public static void MapDeviceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/devices").WithTags("Devices");

        // The Agent intentionally exposes only the simulator while the safety
        // boundary is being proven. Hardware adapter registration is not an
        // authority grant and is not surfaced from this API.
        group.MapGet("/", (SimulatorDevice simulator) => Results.Ok(new[]
        {
            new { adapterId = SimulatorId, capabilities = simulator.GetRichCapabilities().Select(capability => $"{SimulatorId}.{capability.VerbName}").ToArray() }
        }))
        .WithName("GetDevices")
        .WithSummary("List the canonical simulator capabilities")
        .Produces<IEnumerable<object>>(StatusCodes.Status200OK);

        group.MapGet("/{id}/capabilities", IResult (string id, SimulatorDevice simulator) =>
        {
            if (!string.Equals(id, SimulatorId, StringComparison.OrdinalIgnoreCase))
                return Results.NotFound(new { Error = $"Device '{id}' not found." });

            CapabilityDescriptor[] descriptors = simulator.GetRichCapabilities().ToArray();
            return Results.Ok(new { adapterId = SimulatorId, capabilities = descriptors });
        })
        .WithName("GetDeviceCapabilities")
        .WithSummary("Get canonical simulator capability descriptors")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // This endpoint deliberately does not receive CapabilityRegistry. A
        // request first becomes immutable evidence, then the Agent dispatcher
        // refreshes admission at its durable dispatch-start fence.
        group.MapPost("/{id}/execute", async Task<IResult> (
            string id,
            ExecuteRequest request,
            HttpContext context,
            SimulatorDevice simulator,
            ICommandDispatcher dispatcher,
            [FromServices] IRunspaceEventEmitter? emitter,
            [FromServices] IExperimentManager? experimentManager,
            CancellationToken cancellationToken) =>
        {
            if (context.Items[DesktopSessionAuthenticationHandler.ActorItemKey] is not VerifiedDesktopActor actor)
                return Results.Unauthorized();

            if (!string.Equals(id, SimulatorId, StringComparison.OrdinalIgnoreCase))
                return Results.NotFound(new { Error = $"Device '{id}' not found." });

            if (string.IsNullOrWhiteSpace(request.Capability) ||
                !simulator.GetRichCapabilities().Any(capability =>
                    string.Equals(capability.VerbName, request.Capability, StringComparison.Ordinal)))
            {
                return Results.NotFound(new { Error = $"Capability '{request.Capability}' was not found on '{SimulatorId}'." });
            }

            try
            {
                var activeExperiment = experimentManager?.GetActive();
                var executionId = activeExperiment?.ActiveRunspaceId is { } runspaceId && Guid.TryParse(runspaceId, out var parsed)
                    ? parsed
                    : Guid.NewGuid();
                var intent = new CommandIntent(
                    Guid.NewGuid(),
                    executionId,
                    Guid.NewGuid(),
                    SimulatorId,
                    request.Capability,
                    Canonicalize(request.Parameters),
                    actor.Sid,
                    actor.DisplayName,
                    actor.SessionReference,
                    SimulationPolicyRevision,
                    DateTimeOffset.UtcNow);

                var evidence = await dispatcher.ProposeAsync(intent, cancellationToken).ConfigureAwait(false);
                if (evidence.State != CommandAdmissionState.Allowed)
                {
                    // Admission evidence is durable, but it is not a device effect.
                    return Results.Conflict(new
                    {
                        success = false,
                        commandId = intent.CommandId,
                        state = evidence.State.ToString(),
                        error = "Command was not admitted for dispatch."
                    });
                }

                await dispatcher.DispatchAsync(intent.CommandId, cancellationToken).ConfigureAwait(false);

                // DispatchAsync returns only after the dispatcher has committed a
                // confirmed observation (or, for reads, completed safely). Publish
                // no success notification before that canonical evidence exists.
                emitter?.Publish(new ManualActionEvent(
                    SimulatorId,
                    request.Capability,
                    request.Parameters ?? new Dictionary<string, object>(),
                    Success: true,
                    Result: null,
                    Error: null,
                    ExperimentId: activeExperiment?.ExperimentId,
                    ExperimentPhase: activeExperiment?.ActivePhase?.ToString()?.ToLowerInvariant()));

                return Results.Ok(new
                {
                    success = true,
                    commandId = intent.CommandId,
                    state = evidence.State.ToString(),
                    result = (object?)null,
                    error = (string?)null
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Once dispatch started, cancellation is intentionally uncertain
                // and reconciliation owns the disposition. This endpoint must not
                // claim an unsuccessful/no-effect outcome.
                return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (Exception exception)
            {
                return Results.Problem(
                    title: "Failed to dispatch simulator command",
                    detail: exception.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("ExecuteDeviceCapability")
        .WithSummary("Propose and dispatch a simulator command through the Agent boundary")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static string Canonicalize(Dictionary<string, object>? parameters)
    {
        var canonical = (parameters ?? [])
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        return JsonSerializer.Serialize(canonical);
    }
}
