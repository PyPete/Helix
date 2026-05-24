using Helix.Orchestration.Agent.Runspaces;
using Helix.Orchestration.Contracts.Control;
using Helix.Orchestration.Core.WSL;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Agent.gRPC;

/// <summary>
/// gRPC control-plane service for submitting and controlling runspace execution on the agent.
/// </summary>
public sealed class AgentControlService(
    RunspaceManager runspaceManager,
    ILogger<AgentControlService> logger) : AgentControl.AgentControlBase
{
    private static class LogEvents
    {
        public static readonly EventId SubmitRequested = new(2800, nameof(SubmitRequested));
        public static readonly EventId SubmitAccepted = new(2801, nameof(SubmitAccepted));
        public static readonly EventId SubmitRejected = new(2802, nameof(SubmitRejected));
        public static readonly EventId PauseRequested = new(2803, nameof(PauseRequested));
        public static readonly EventId ResumeRequested = new(2804, nameof(ResumeRequested));
        public static readonly EventId AbortRequested = new(2805, nameof(AbortRequested));
        public static readonly EventId StatusRequested = new(2806, nameof(StatusRequested));
    }

    /// <summary>
    /// Submits a runspace definition payload for execution.
    /// </summary>
    public override async Task<SubmissionAck> SubmitExperiment(ExperimentSubmission request, ServerCallContext context)
    {
        logger.LogInformation(LogEvents.SubmitRequested, "SubmitExperiment requested");

        var definition = System.Text.Json.JsonSerializer.Deserialize<RunspaceDefinition>(request.RunspaceDefinitionJson);
        if (definition is null)
        {
            logger.LogWarning(LogEvents.SubmitRejected, "SubmitExperiment rejected due to invalid runspace definition payload");
            return new SubmissionAck { Accepted = false, Message = "Invalid runspace definition payload." };
        }

        var runspace = await runspaceManager.SubmitAsync(definition, context.CancellationToken);
        logger.LogInformation(LogEvents.SubmitAccepted, "SubmitExperiment accepted with RunspaceId {RunspaceId}", runspace.RunspaceId);

        return new SubmissionAck { Accepted = true, RunspaceId = runspace.RunspaceId, Message = "Accepted" };
    }

    /// <summary>
    /// Requests pause for an existing runspace.
    /// </summary>
    public override async Task<CommandAck> PauseRunspace(RunspaceCommand request, ServerCallContext context)
    {
        logger.LogInformation(LogEvents.PauseRequested, "PauseRunspace requested for {RunspaceId}", request.RunspaceId);
        await runspaceManager.PauseAsync(request.RunspaceId, context.CancellationToken);
        return new CommandAck { Success = true, Message = "Paused" };
    }

    /// <summary>
    /// Requests resume for an existing runspace.
    /// </summary>
    public override async Task<CommandAck> ResumeRunspace(RunspaceCommand request, ServerCallContext context)
    {
        logger.LogInformation(LogEvents.ResumeRequested, "ResumeRunspace requested for {RunspaceId}", request.RunspaceId);
        await runspaceManager.ResumeAsync(request.RunspaceId, context.CancellationToken);
        return new CommandAck { Success = true, Message = "Resumed" };
    }

    /// <summary>
    /// Requests abort for an existing runspace.
    /// </summary>
    public override async Task<CommandAck> AbortRunspace(RunspaceCommand request, ServerCallContext context)
    {
        logger.LogInformation(LogEvents.AbortRequested, "AbortRunspace requested for {RunspaceId}", request.RunspaceId);
        await runspaceManager.AbortAsync(request.RunspaceId, context.CancellationToken);
        return new CommandAck { Success = true, Message = "Aborted" };
    }

    /// <summary>
    /// Retrieves current status for a runspace.
    /// </summary>
    public override Task<RunspaceStatusResponse> GetRunspaceStatus(RunspaceQuery request, ServerCallContext context)
    {
        var status = runspaceManager.GetStatus(request.RunspaceId);
        logger.LogInformation(LogEvents.StatusRequested, "GetRunspaceStatus for {RunspaceId} returned {RunspaceStatus}", request.RunspaceId, status?.ToString() ?? "NotFound");

        var response = new RunspaceStatusResponse
        {
            RunspaceId = request.RunspaceId,
            Status = status?.ToString() ?? "NotFound",
            Message = status is null ? "Runspace not found." : "OK"
        };

        return Task.FromResult(response);
    }
}
