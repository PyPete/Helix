using Helix.Orchestration.Contracts.Control;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;

namespace Helix.Orchestration.SiteServer.Agent;

public sealed class GrpcAgentControlClient : IAgentControlClient
{
    private readonly AgentControl.AgentControlClient _inner;

    public GrpcAgentControlClient(IOptions<AgentProxyOptions> options)
    {
        var channel = GrpcChannel.ForAddress(options.Value.GrpcAddress);
        _inner = new AgentControl.AgentControlClient(channel);
    }

    public async Task<SubmissionAck> SubmitExperimentAsync(ExperimentSubmission request, CancellationToken ct = default)
        => await _inner.SubmitExperimentAsync(request, cancellationToken: ct);

    public async Task<RunspaceStatusResponse> GetRunspaceStatusAsync(RunspaceQuery request, CancellationToken ct = default)
        => await _inner.GetRunspaceStatusAsync(request, cancellationToken: ct);

    public async Task<CommandAck> AbortRunspaceAsync(RunspaceCommand request, CancellationToken ct = default)
        => await _inner.AbortRunspaceAsync(request, cancellationToken: ct);
}
