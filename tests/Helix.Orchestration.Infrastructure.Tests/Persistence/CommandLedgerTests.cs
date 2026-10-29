using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Infrastructure.Persistence;
using Npgsql;
using Xunit;

namespace Helix.Orchestration.Infrastructure.Tests.Persistence;

[Collection(nameof(PostgresRepositoriesTestsCollection))]
public sealed class CommandLedgerTests
{
    private readonly PostgresFixture _fixture;

    public CommandLedgerTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AppendAdmissionAsync_ConcurrentSameCommandId_ReturnsOnePersistedEvidence()
    {
        var commandId = Guid.NewGuid();
        var intent = CreateIntent(commandId);
        var ledger = CreateLedger();
        using var callersReady = new CountdownEvent(8);
        var releaseCallers = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var appends = Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
        {
            callersReady.Signal();
            await releaseCallers.Task;
            return await ledger.AppendAdmissionAsync(intent, [], CommandAdmissionState.Allowed);
        })).ToArray();

        Assert.True(callersReady.Wait(TimeSpan.FromSeconds(10)), "Concurrent callers did not reach the dispatch barrier.");
        releaseCallers.SetResult();
        var evidence = await Task.WhenAll(appends);

        var persistedEvidenceId = Assert.Single(evidence.Select(item => item.EvidenceId).Distinct());
        var persistedSequence = Assert.Single(evidence.Select(item => item.Sequence).Distinct());
        var loaded = await ledger.GetAsync(commandId);

        Assert.NotNull(loaded);
        Assert.Equal(persistedEvidenceId, loaded!.Evidence.EvidenceId);
        Assert.Equal(persistedSequence, loaded.Evidence.Sequence);
        Assert.Equal(1L, await CountRowsAsync(commandId));
    }

    [Fact]
    public async Task AppendAdmissionAsync_SameCommandIdWithChangedImmutableIntent_ThrowsAndPreservesFirstFact()
    {
        var commandId = Guid.NewGuid();
        var ledger = CreateLedger();
        var original = CreateIntent(commandId);
        var firstEvidence = await ledger.AppendAdmissionAsync(original, [], CommandAdmissionState.Allowed);
        var changedIntent = original with { CanonicalParameters = "{\"flowRate\": 2.0}" };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ledger.AppendAdmissionAsync(changedIntent, [], CommandAdmissionState.Allowed));

        var loaded = await ledger.GetAsync(commandId);
        Assert.Contains("immutable intent", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(loaded);
        Assert.Equal(original.CanonicalParameters, loaded!.Intent.CanonicalParameters);
        Assert.Equal(firstEvidence.EvidenceId, loaded.Evidence.EvidenceId);
        Assert.Equal(1L, await CountRowsAsync(commandId));
    }

    private PostgresCommandLedger CreateLedger() => new(_fixture.ConnectionFactory
        ?? throw new InvalidOperationException("PostgreSQL fixture did not provide a connection factory."));

    private static CommandIntent CreateIntent(Guid commandId) => new(
        commandId,
        Guid.NewGuid(),
        Guid.NewGuid(),
        "simulator",
        "SetFlow",
        "{\"flowRate\": 1.0}",
        "scientist",
        "Scientist",
        "session",
        "simulation-policy-v1",
        DateTimeOffset.UtcNow);

    private async Task<long> CountRowsAsync(Guid commandId)
    {
        await using var connection = (_fixture.ConnectionFactory
            ?? throw new InvalidOperationException("PostgreSQL fixture did not provide a connection factory.")).Create();
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT COUNT(*) FROM command_evidence WHERE command_id = @commandId;", connection);
        command.Parameters.AddWithValue("commandId", commandId);
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }
}
