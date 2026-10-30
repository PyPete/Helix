using Dapper;
using Helix.Orchestration.Core.Execution;

namespace Helix.Orchestration.Infrastructure.Persistence;

/// <summary>Independently persisted simulator effect state used only for observation and reconciliation.</summary>
public sealed class PostgresSimulatorStateStore(DbConnectionFactory connectionFactory) : ISimulatorStateStore
{
    public async Task<SimulatorObservation> ApplyAsync(
        string deviceId,
        string stateKey,
        Guid commandId,
        string canonicalValue,
        string unit,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string sql = """
            INSERT INTO simulator_device_state (device_id, state_key, last_applied_command_id, canonical_value, unit, generation, observed_at)
            VALUES (@deviceId, @stateKey, @commandId, @canonicalValue, @unit, 1, NOW())
            ON CONFLICT (device_id, state_key) DO UPDATE SET
                last_applied_command_id = EXCLUDED.last_applied_command_id,
                canonical_value = EXCLUDED.canonical_value,
                unit = EXCLUDED.unit,
                generation = simulator_device_state.generation + 1,
                observed_at = EXCLUDED.observed_at
            WHERE simulator_device_state.last_applied_command_id <> EXCLUDED.last_applied_command_id
            RETURNING device_id AS DeviceId, state_key AS StateKey,
                      last_applied_command_id AS LastAppliedCommandId,
                      canonical_value AS CanonicalValue, unit AS Unit,
                      generation AS Generation, observed_at AS ObservedAtUtc;
            """;
        var observation = await connection.QuerySingleOrDefaultAsync<SimulatorObservationRow>(new CommandDefinition(
            sql, new { deviceId, stateKey, commandId, canonicalValue, unit }, transaction, cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        if (observation is not null)
            return observation.ToObservation();

        // Idempotent application returns no row because the original command is already the observed effect.
        return await GetAsync(deviceId, stateKey, cancellationToken)
            ?? throw new InvalidOperationException("Simulator effect disappeared while confirming idempotent application.");
    }

    public async Task<SimulatorObservation?> GetAsync(
        string deviceId,
        string stateKey,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT device_id AS DeviceId, state_key AS StateKey, last_applied_command_id AS LastAppliedCommandId,
                   canonical_value AS CanonicalValue, unit AS Unit, generation AS Generation,
                   observed_at AS ObservedAtUtc
            FROM simulator_device_state
            WHERE device_id = @deviceId AND state_key = @stateKey;
            """;
        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        var observation = await connection.QuerySingleOrDefaultAsync<SimulatorObservationRow>(
            new CommandDefinition(sql, new { deviceId, stateKey }, cancellationToken: cancellationToken));
        return observation?.ToObservation();
    }

    private sealed class SimulatorObservationRow
    {
        public string DeviceId { get; init; } = string.Empty;
        public string StateKey { get; init; } = string.Empty;
        public Guid LastAppliedCommandId { get; init; }
        public string CanonicalValue { get; init; } = string.Empty;
        public string Unit { get; init; } = string.Empty;
        public long Generation { get; init; }
        public DateTime ObservedAtUtc { get; init; }

        public SimulatorObservation ToObservation() => new(
            DeviceId,
            StateKey,
            LastAppliedCommandId,
            CanonicalValue,
            Unit,
            Generation,
            new DateTimeOffset(ObservedAtUtc.ToUniversalTime()));
    }
}
