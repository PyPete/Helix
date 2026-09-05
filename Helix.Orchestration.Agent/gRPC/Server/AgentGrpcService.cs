using Grpc.Core;
using Helix.Orchestration.Contracts.Control;
using Helix.Orchestration.Core.Devices;
using Helix.Orchestration.Core.Runspace;
using Helix.Orchestration.Core.WSL;
using System.Text.Json;

namespace Helix.Orchestration.Agent.GrpcServer;

public class AgentGrpcService : AgentControl.AgentControlBase
{
    private readonly IRunspaceManager _runspaceManager;
    private readonly DeviceRegistry _deviceRegistry;

    public AgentGrpcService(IRunspaceManager runspaceManager, DeviceRegistry deviceRegistry)
    {
        _runspaceManager = runspaceManager;
        _deviceRegistry = deviceRegistry;
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
        var rs = _runspaceManager.Get(request.RunspaceId);
        if (rs is null)
            return new CommandAck { Success = false, Message = $"Runspace '{request.RunspaceId}' not found" };
        try
        {
            await rs.PauseAsync(context.CancellationToken);
            return new CommandAck { Success = true, Message = "Paused" };
        }
        catch (Exception ex)
        {
            return new CommandAck { Success = false, Message = ex.Message };
        }
    }

    public override async Task<CommandAck> ResumeRunspace(RunspaceCommand request, ServerCallContext context)
    {
        var rs = _runspaceManager.Get(request.RunspaceId);
        if (rs is null)
            return new CommandAck { Success = false, Message = $"Runspace '{request.RunspaceId}' not found" };
        try
        {
            await rs.ResumeAsync(context.CancellationToken);
            return new CommandAck { Success = true, Message = "Resumed" };
        }
        catch (Exception ex)
        {
            return new CommandAck { Success = false, Message = ex.Message };
        }
    }

    public override Task<ListDevicesResponse> ListDevices(ListDevicesRequest request, ServerCallContext context)
    {
        var response = new ListDevicesResponse();
        foreach (var device in _deviceRegistry.GetAllDevices())
        {
            var info = new DeviceInfo
            {
                DeviceId = device.DeviceId,
                DeviceName = device.DeviceAlias,
                DeviceKind = device.DeviceType,
            };
            info.Capabilities.AddRange(device.CapabilityNames);
            response.Devices.Add(info);
        }
        return Task.FromResult(response);
    }
}
