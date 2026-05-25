using Helix.Orchestration.Contracts.Control;

namespace Helix.Orchestration.SiteServer.Agent;

public interface IAgentControlClient
{
    Task<SubmissionAck> SubmitExperimentAsync(ExperimentSubmission request, CancellationToken ct = default);

    Task<RunspaceStatusResponse> GetRunspaceStatusAsync(RunspaceQuery request, CancellationToken ct = default);

    Task<CommandAck> AbortRunspaceAsync(RunspaceCommand request, CancellationToken ct = default);
}
