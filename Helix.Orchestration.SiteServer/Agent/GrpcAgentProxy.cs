using System.Text.Json;
using Grpc.Core;
using Helix.Orchestration.Contracts.Control;
using Helix.Orchestration.Core.WSL;

namespace Helix.Orchestration.SiteServer.Agent;

public sealed class GrpcAgentProxy(IAgentControlClient client) : IAgentProxy
{
    public async Task<string> SubmitExperimentAsync(RunspaceDefinition definition, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(definition);
        var response = await client.SubmitExperimentAsync(new ExperimentSubmission
        {
            RunspaceDefinitionJson = payload
        }, ct);

        if (!response.Accepted || string.IsNullOrWhiteSpace(response.RunspaceId))
            throw new InvalidOperationException(response.Message is { Length: > 0 } ? response.Message : "Agent rejected experiment submission.");

        return response.RunspaceId;
    }

    public async Task<RunspaceStatusDto> GetRunspaceStatusAsync(string runspaceId, CancellationToken ct = default)
    {
        try
        {
            var response = await client.GetRunspaceStatusAsync(new RunspaceQuery
            {
                RunspaceId = runspaceId
            }, ct);

            if (response.Status.Equals("NotFound", StringComparison.OrdinalIgnoreCase))
                throw new KeyNotFoundException($"Runspace '{runspaceId}' not found.");

            return new RunspaceStatusDto(
                RunspaceId: response.RunspaceId,
                Status: response.Status,
                CreatedAt: DateTimeOffset.UtcNow,
                StartedAt: null,
                EndedAt: null,
                FaultMessage: string.IsNullOrWhiteSpace(response.Message) || response.Message.Equals("OK", StringComparison.OrdinalIgnoreCase)
                    ? null
                    : response.Message);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            throw new KeyNotFoundException($"Runspace '{runspaceId}' not found.", ex);
        }
    }

    public async Task AbortRunspaceAsync(string runspaceId, CancellationToken ct = default)
    {
        try
        {
            var response = await client.AbortRunspaceAsync(new RunspaceCommand
            {
                RunspaceId = runspaceId
            }, ct);

            if (!response.Success)
                throw new InvalidOperationException(response.Message is { Length: > 0 } ? response.Message : "Agent failed to abort runspace.");
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            throw new KeyNotFoundException($"Runspace '{runspaceId}' not found.", ex);
        }
    }

    public async Task<IEnumerable<DeviceDescriptorDto>> GetRegisteredDevicesAsync(CancellationToken ct = default)
    {
        var response = await client.ListDevicesAsync(new ListDevicesRequest(), ct);
        return response.Devices.Select(d => new DeviceDescriptorDto(
            DeviceId: d.DeviceId,
            DeviceName: d.DeviceName,
            DeviceKind: d.DeviceKind,
            Capabilities: d.Capabilities.ToList()));
    }
}
