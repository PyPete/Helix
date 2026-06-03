using Grpc.Core;
using Helix.Orchestration.Contracts.Control;
using Helix.Orchestration.Core.Runspace;
using Helix.Orchestration.Core.WSL;
using System.Text.Json;

namespace Helix.Orchestration.Agent.GrpcServer;

public class AgentGrpcService : AgentControl.AgentControlBase
{
    private readonly IRunspaceManager _runspaceManager;

    public AgentGrpcService(IRunspaceManager runspaceManager)
    {
        _runspaceManager = runspaceManager;
    }

    public override async Task<SubmissionAck> SubmitExperiment(ExperimentSubmission request, ServerCallContext context)
    {
        try
        {
            var definition = JsonSerializer.Deserialize<RunspaceDefinition>(request.RunspaceDefinitionJson)
                ?? throw new InvalidOperationException("Null RunspaceDefinition");
            var runspace = await _runspaceManager.CreateAsync(definition, context.CancellationToken);
            return new SubmissionAck { Accepted = true, RunspaceId = runspace.RunspaceId, Message = "Accepted" };
        }
        catch (Exception ex)
        {
            return new SubmissionAck { Accepted = false, Message = ex.Message };
        }
    }

    public override async Task<RunspaceStatusResponse> GetRunspaceStatus(RunspaceQuery request, ServerCallContext context)
    {
        await Task.CompletedTask;
        var rs = _runspaceManager.Get(request.RunspaceId);
        if (rs is null)
            return new RunspaceStatusResponse { RunspaceId = request.RunspaceId, Status = "NotFound", Message = "Runspace not found" };
        return new RunspaceStatusResponse { RunspaceId = rs.RunspaceId, Status = rs.Status.ToString(), Message = string.Empty };
    }

    public override async Task<CommandAck> AbortRunspace(RunspaceCommand request, ServerCallContext context)
    {
        try
        {
            await _runspaceManager.AbortAsync(request.RunspaceId, context.CancellationToken);
            return new CommandAck { Success = true, Message = "Aborted" };
        }
        catch (Exception ex)
        {
            return new CommandAck { Success = false, Message = ex.Message };
        }
    }

    public override async Task<CommandAck> PauseRunspace(RunspaceCommand request, ServerCallContext context)
    {
        await Task.CompletedTask;
        return new CommandAck { Success = true, Message = "Pause not implemented" };
    }

    public override async Task<CommandAck> ResumeRunspace(RunspaceCommand request, ServerCallContext context)
    {
        await Task.CompletedTask;
        return new CommandAck { Success = true, Message = "Resume not implemented" };
    }
}
