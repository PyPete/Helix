using Helix.Orchestration.Core.ScientificSetup;

namespace Helix.Orchestration.Agent.ScientificSetup;

/// <summary>Actor-scoped, read-only operation receipt lookup; it cannot retry or issue operations.</summary>
public sealed class ScientificOperationResultService(IScientificSetupRepository repository)
{
    public Task<ScientificOperationReceipt?> ReadAsync(Guid operationId, string actorSid, CancellationToken cancellationToken = default) =>
        repository.OperationAsync(operationId, actorSid, cancellationToken);
}
