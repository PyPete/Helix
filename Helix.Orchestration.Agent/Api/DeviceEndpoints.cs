using Helix.Orchestration.Agent.Events;
using Helix.Orchestration.Devices.Drivers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using CapabilityDescriptor = Helix.Orchestration.Core.Capabilities.CapabilityDescriptor;
using CapabilityCategory = Helix.Orchestration.Core.Capabilities.CapabilityCategory;
using CapabilityRegistry = Helix.Orchestration.Core.Capabilities.CapabilityRegistry;
using ParameterSchema = Helix.Orchestration.Core.Capabilities.ParameterSchema;

namespace Helix.Orchestration.Agent.Api;

/// <summary>
/// Request body for POST /devices/{id}/execute.
/// </summary>
/// <param name="Capability">The capability verb name to invoke.</param>
/// <param name="Parameters">Optional parameters passed to the capability.</param>
public sealed record ExecuteRequest(string Capability, Dictionary<string, object>? Parameters);

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

        // GET /devices — return one entry per adapter, only if at least one capability is available
        group.MapGet("/", async Task<IResult> (CapabilityRegistry registry) =>
        {
            try
            {
                // Group capability keys by adapter prefix (e.g. "simulator.SetFlow" → "simulator")
                var groups = registry.GetAllCapabilityNames()
                    .GroupBy(name =>
                    {
                        var dot = name.IndexOf('.');
                        return dot > 0 ? name[..dot] : name;
                    });

                var available = new List<object>();
                foreach (var g in groups)
                {
                    // Check availability via HealthCheckAsync on the first capability key.
                    // NullDeviceTransport always returns Connected (simulator is always available).
                    // Serial/TCP transports return Disconnected when hardware is not present.
                    var probe = g.First();
                    if (await registry.IsAvailableAsync(probe))
                        available.Add(new { adapterId = g.Key, capabilities = g.ToArray() });
                }

                return Results.Ok(available);
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
        group.MapGet("/{id}/capabilities", IResult (string id, CapabilityRegistry registry,
            [FromServices] SimulatorDevice? simulator) =>
        {
            try
            {
                // Find all capability keys that belong to this adapter prefix.
                var adapterKeys = registry.GetAllCapabilityNames()
                    .Where(name =>
                    {
                        var dot = name.IndexOf('.');
                        var prefix = dot > 0 ? name[..dot] : name;
                        return string.Equals(prefix, id, StringComparison.OrdinalIgnoreCase);
                    })
                    .ToArray();

                if (adapterKeys.Length == 0)
                    return Results.NotFound(new { Error = $"Device '{id}' not found." });

                // Use rich descriptors from the simulator when available.
                CapabilityDescriptor[] descriptors;
                if (string.Equals(id, "simulator", StringComparison.OrdinalIgnoreCase) && simulator is not null)
                {
                    descriptors = simulator.GetRichCapabilities().ToArray();
                }
                else
                {
                    // Stub descriptors for adapters that haven't wired rich metadata yet.
                    descriptors = adapterKeys
                        .Select(key =>
                        {
                            var dot = key.IndexOf('.');
                            var verbName = dot > 0 ? key[(dot + 1)..] : key;
                            return new CapabilityDescriptor(verbName, verbName, string.Empty,
                                CapabilityCategory.Utility, Array.Empty<ParameterSchema>(), false, false);
                        })
                        .ToArray();
                }

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
            [FromServices] IRunspaceEventEmitter? emitter) =>
        {
            try
            {
                // Guard: check that at least one capability key has this adapter prefix.
                // allNames contains full keys like "simulator.SetFlow"; id is the prefix ("simulator").
                var adapterExists = registry.GetAllCapabilityNames()
                    .Any(name =>
                    {
                        var dot = name.IndexOf('.');
                        return string.Equals(dot > 0 ? name[..dot] : name, id, StringComparison.OrdinalIgnoreCase);
                    });
                if (!adapterExists)
                    return Results.NotFound(new { Error = $"Device '{id}' not found." });

                // Invoke using the full capability key (adapter prefix + "." + verb name).
                var fullKey = $"{id}.{request.Capability}";
                var invocationResult = await registry.InvokeAsync(fullKey, request.Parameters);

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
