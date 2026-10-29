using Grpc.Core;
using Helix.Orchestration.Agent.Experiments;
using Helix.Orchestration.Contracts.Control;
using Helix.Orchestration.Core.Devices;
using Helix.Orchestration.Core.Runspace;

namespace Helix.Orchestration.Agent.GrpcServer;

public class AgentGrpcService : AgentControl.AgentControlBase
{
    private readonly IRunspaceManager _runspaceManager;
    private readonly DeviceRegistry _deviceRegistry;
    private readonly IExperimentManager? _experimentManager;

    public AgentGrpcService(IRunspaceManager runspaceManager, DeviceRegistry deviceRegistry)
        : this(runspaceManager, deviceRegistry, null)
    {
    }

    public AgentGrpcService(
        IRunspaceManager runspaceManager,
        DeviceRegistry deviceRegistry,
        IExperimentManager? experimentManager)
    {
        _runspaceManager = runspaceManager ?? throw new ArgumentNullException(nameof(runspaceManager));
        _deviceRegistry = deviceRegistry ?? throw new ArgumentNullException(nameof(deviceRegistry));
        _experimentManager = experimentManager;
    }

    public override async Task<SubmissionAck> SubmitExperiment(ExperimentSubmission request, ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RunspaceDefinitionJson) ||
                _experimentManager is null ||
                !_experimentManager.TryGetCanonicalDefinition(request.RunspaceDefinitionJson, out var canonical) ||
                canonical is null)
            {
                return new SubmissionAck
                {
                    Accepted = false,
                    Message = "Raw workflow definitions are unsupported. Submit a canonical experiment reference accepted by the Agent."
                };
            }

            var runspace = await _runspaceManager.CreateAsync(canonical.Definition, context.CancellationToken);
            _experimentManager.SetActiveRunspace(request.RunspaceDefinitionJson, runspace.RunspaceId);
            return new SubmissionAck { Accepted = true, RunspaceId = runspace.RunspaceId, Message = "Accepted" };
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            return new SubmissionAck { Accepted = false, Message = "Submission cancelled." };
        }
        catch (Exception)
        {
            return new SubmissionAck { Accepted = false, Message = "Submission was rejected." };
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
