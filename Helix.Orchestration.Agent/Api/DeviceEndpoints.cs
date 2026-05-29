using Helix.Orchestration.Core.Capabilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Helix.Orchestration.Agent.Api;

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

        // GET /devices/{id}/capabilities — return capability list for one adapter
        group.MapGet("/{id}/capabilities", IResult (string id, CapabilityRegistry registry) =>
        {
            try
            {
                var allNames = registry.GetAllCapabilityNames().ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (!allNames.Contains(id))
                    return Results.NotFound(new { Error = $"Device '{id}' not found." });

                return Results.Ok(new { adapterId = id, capabilities = new[] { id } });
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
    }
}
