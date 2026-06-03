using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Helix.Orchestration.SiteServer.Agent;
using Helix.Orchestration.SiteServer.Mqtt;

namespace Helix.Orchestration.SiteServer.Endpoints;

/// <summary>
/// DTO returned by GET /api/fumehoods.
/// </summary>
public sealed record FumehoodDto(
    string Id,
    string Name,
    string Status,
    string? ActiveExperimentName,
    string LastSeenAt);

/// <summary>
/// Minimal API endpoints for fumehood status.
/// </summary>
public static class FumehoodEndpoints
{
    public static void MapFumehoodEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/fumehoods").WithTags("Fumehoods");

        // GET /api/fumehoods — Return configured fumehood(s) with live reachability status
        group.MapGet("/", (IOptions<MqttBridgeOptions> mqttOptions, IAgentProxy agentProxy) =>
        {
            var fumehoodId = mqttOptions.Value.FumehoodId;

            string status;
            try
            {
                var devices = agentProxy.GetRegisteredDevices();
                // Any registered devices means the agent is reachable; no running-experiment
                // tracking in v0.1, so always report "idle" when reachable.
                status = devices.Any() ? "idle" : "idle";
            }
            catch
            {
                status = "offline";
            }

            var dto = new FumehoodDto(
                Id: fumehoodId,
                Name: $"Fumehood {fumehoodId}",
                Status: status,
                ActiveExperimentName: null,
                LastSeenAt: DateTime.UtcNow.ToString("o"));

            return Results.Ok(new[] { dto });
        })
        .WithName("GetFumehoods")
        .Produces<IEnumerable<FumehoodDto>>(StatusCodes.Status200OK);
    }
}
