using Helix.Orchestration.Agent.Events;
using Helix.Orchestration.Core.Capabilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Helix.Orchestration.Agent.Api;

/// <summary>
/// Request body for POST /devices/{id}/execute.
/// </summary>
/// <param name="Capability">The capability verb name to invoke.</param>
/// <param name="Parameters">Optional parameters passed to the capability.</param>
internal sealed record ExecuteRequest(string Capability, Dictionary<string, object>? Parameters);

/// <summary>
/// Minimal API endpoint group for device and capability discovery.
/// </summary>
public static class DeviceEndpoints
{
    /// <summary>
    /// Maps the /devices endpoint group onto <paramref name="app"/>.
    /// </summary>
    public static void MapDeviceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/devices")
            .WithTags("Devices");

        // GET /devices — return all registered adapters from CapabilityRegistry
        group.MapGet("/", IResult (CapabilityRegistry registry) =>
        {
            try
            {
                var adapters = registry.GetAllCapabilityNames()
                    .Select(name => new { adapterId = name, capabilities = new[] { name } })
                    .ToList();

                return Results.Ok(adapters);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to retrieve devices",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("GetDevices")
        .WithSummary("List all registered device adapters and their capabilities")
        .Produces<IEnumerable<object>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // GET /devices/{id}/capabilities — return CapabilityDescriptor[] for one adapter
        group.MapGet("/{id}/capabilities", IResult (string id, CapabilityRegistry registry) =>
        {
            try
            {
                var allNames = registry.GetAllCapabilityNames().ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (!allNames.Contains(id))
                    return Results.NotFound(new { Error = $"Device '{id}' not found." });

                // Build stub CapabilityDescriptor per registered capability name.
                // Full metadata will be populated when CapabilityDescriptor registration is wired in a later plan.
                var descriptors = allNames
                    .Select(verbName => new CapabilityDescriptor(
                        verbName,
                        verbName,
                        string.Empty,
                        CapabilityCategory.Utility,
                        Array.Empty<ParameterSchema>(),
                        false,
                        false))
                    .ToArray();

                return Results.Ok(new { adapterId = id, capabilities = descriptors });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to retrieve device capabilities",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("GetDeviceCapabilities")
        .WithSummary("Get capabilities for a specific device adapter")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // POST /devices/{id}/execute — invoke a capability on a device adapter
        group.MapPost("/{id}/execute", async Task<IResult> (
            string id,
            ExecuteRequest request,
            CapabilityRegistry registry,
            IRunspaceEventEmitter? emitter) =>
        {
            try
            {
                var allNames = registry.GetAllCapabilityNames().ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (!allNames.Contains(id))
                    return Results.NotFound(new { Error = $"Device '{id}' not found." });

                var invocationResult = await registry.InvokeAsync(request.Capability, request.Parameters);

                emitter?.Publish(new ManualActionEvent(
                    id,
                    request.Capability,
                    request.Parameters ?? new Dictionary<string, object>(),
                    invocationResult.Success,
                    invocationResult.ReturnValue,
                    invocationResult.ErrorMessage));

                return Results.Ok(new
                {
                    success = invocationResult.Success,
                    result = invocationResult.ReturnValue,
                    error = invocationResult.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to execute device capability",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("ExecuteDeviceCapability")
        .WithSummary("Execute a device capability")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
