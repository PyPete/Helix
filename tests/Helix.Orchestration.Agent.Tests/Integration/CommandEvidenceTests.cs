using Helix.Orchestration.Agent.Security;
using Helix.Orchestration.Core.Execution;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Integration;

public sealed class CommandEvidenceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"helix-spool-{Guid.NewGuid():N}");

    [Fact]
    public void Spool_RedactsCredentialAndDurablyRecordsAcceptedDenial()
    {
        var service = new DesktopSessionService(_directory, "test-agent");

        var recorded = service.TryRecordDeniedAttempt("POST", "/commands/propose?token=secret", "Invalid desktop session");

        Assert.True(recorded);
        var contents = File.ReadAllText(Path.Combine(_directory, "denied-attempts.ndjson"));
        Assert.DoesNotContain("Bearer", contents, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", contents, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Invalid desktop session", contents, StringComparison.Ordinal);
    }

    [Fact]
    public void Saturation_ClosesAdmissionBeforeUnreservedAttempt()
    {
        var service = new DesktopSessionService(_directory);
        Directory.CreateDirectory(_directory);
        using (var spool = new FileStream(Path.Combine(_directory, "denied-attempts.ndjson"), FileMode.OpenOrCreate, FileAccess.Write))
            spool.SetLength((64L * 1024 * 1024) - (8 * 1024));

        var recorded = service.TryRecordDeniedAttempt("POST", "/commands/propose", "Invalid desktop session");

        Assert.False(recorded);
        Assert.Equal(AuditSpoolState.Saturated, service.SpoolState);
        Assert.False(service.IsAdmissionOpen);
    }

    [Fact]
    public async Task SpoolRecovery_PreservesOriginalIdentityAndOccurrenceTime()
    {
        var service = new DesktopSessionService(_directory, "generation-one");
        Assert.True(service.TryRecordDeniedAttempt("POST", "/commands/propose", "Invalid desktop session"));
        var ledger = new RecordingLedger();

        var result = await service.DrainAsync(ledger);

        var intent = Assert.Single(ledger.Intents);
        Assert.Equal(intent.CommandId, intent.ExecutionId);
        Assert.Equal("security.denied-attempt", intent.Capability);
        Assert.Contains("generation-one", intent.CanonicalParameters, StringComparison.Ordinal);
        Assert.True(result.AdmissionOpen);

        var recreated = new DesktopSessionService(_directory, "generation-two");
        await recreated.DrainAsync(ledger);
        Assert.Single(ledger.Intents);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private sealed class RecordingLedger : ICommandLedger
    {
        public List<CommandIntent> Intents { get; } = [];

        public Task<CommandEvidence> AppendAdmissionAsync(CommandIntent intent, IReadOnlyList<CommandCheckEvidence> checks, CommandAdmissionState state, CancellationToken cancellationToken = default)
        {
            if (!Intents.Any(existing => existing.CommandId == intent.CommandId))
                Intents.Add(intent);
            return Task.FromResult(new CommandEvidence(Guid.NewGuid(), intent.CommandId, Intents.Count, state, checks, DateTimeOffset.UtcNow));
        }

        public Task<CommandEvidenceRecord?> GetAsync(Guid commandId, CancellationToken cancellationToken = default) => Task.FromResult<CommandEvidenceRecord?>(null);
        public Task<CommandDispatchRecord> BeginDispatchAsync(CommandIntent intent, SimulatorCommandExpectation expectation, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CommandDispatchRecord?> GetDispatchAsync(Guid commandId, CancellationToken cancellationToken = default) => Task.FromResult<CommandDispatchRecord?>(null);
        public Task<CommandDispatchRecord> ConfirmDispatchAsync(Guid commandId, SimulatorObservation observation, bool recovered, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CommandDispatchRecord> HoldForReconciliationAsync(Guid commandId, string reason, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<CommandDispatchRecord>> GetIncompleteDispatchesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CommandDispatchRecord>>([]);
    }
}
