using Helix.Orchestration.Infrastructure.Persistence;
using Helix.Orchestration.SiteServer.Agent;
using Helix.Orchestration.SiteServer.Mqtt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

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
        group.MapGet("/", async Task<IResult> (
            IOptions<MqttBridgeOptions> mqttOptions,
            IAgentProxy agentProxy,
            IExperimentRepository experimentRepository,
            CancellationToken ct) =>
        {
            var fumehoodId = mqttOptions.Value.FumehoodId;

            string status;
            string? activeExperimentName = null;
            try
            {
                var devices = await agentProxy.GetRegisteredDevicesAsync(ct);
                status = devices.Any() ? "online" : "offline";

                if (status == "online")
                {
                    var experiments = await experimentRepository.GetAllAsync(ct);
                    var running = experiments.FirstOrDefault(e =>
                        string.Equals(e.Status, "Running", StringComparison.OrdinalIgnoreCase));
                    activeExperimentName = running?.Name;
                }
            }
            catch
            {
                status = "offline";
            }

            var dto = new FumehoodDto(
                Id: fumehoodId,
                Name: $"Fumehood {fumehoodId}",
                Status: status,
                ActiveExperimentName: activeExperimentName,
                LastSeenAt: DateTime.UtcNow.ToString("o"));

            return Results.Ok(new[] { dto });
        })
        .WithName("GetFumehoods")
        .Produces<IEnumerable<FumehoodDto>>(StatusCodes.Status200OK);
    }
}
