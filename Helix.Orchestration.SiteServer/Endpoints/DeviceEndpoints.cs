using Helix.Orchestration.SiteServer.Agent;

namespace Helix.Orchestration.SiteServer.Endpoints;

/// <summary>
/// Minimal API endpoints for device discovery and metadata.
/// </summary>
public static class DeviceEndpoints
{
    public static void MapDeviceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/devices")
            .WithTags("Devices");

        // GET /api/devices — List all registered devices
        group.MapGet("/", async Task<IResult> (IAgentProxy agentProxy, CancellationToken ct) =>
        {
            try
            {
                var devices = await agentProxy.GetRegisteredDevicesAsync(ct);
                return Results.Ok(devices);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to retrieve registered devices",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .WithName("GetRegisteredDevices")
        .WithSummary("List all registered devices and their capabilities")
        .Produces<IEnumerable<DeviceDescriptorDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
