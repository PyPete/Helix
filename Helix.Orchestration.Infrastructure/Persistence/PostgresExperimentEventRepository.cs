using Dapper;

namespace Helix.Orchestration.Infrastructure.Persistence;

public sealed class PostgresExperimentEventRepository(DbConnectionFactory connectionFactory) : IExperimentEventRepository
{
    public async Task AppendAsync(ExperimentEventRecord record, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO experiment_events (
                experiment_id, phase, event_type, runspace_id,
                step_id, step_label, is_manual, device_id, capability,
                parameters_json, result_json, occurred_at
            )
            VALUES (
                @ExperimentId, @Phase, @EventType, @RunspaceId,
                @StepId, @StepLabel, @IsManual, @DeviceId, @Capability,
                @ParametersJson::jsonb, @ResultJson::jsonb, @OccurredAt
            );
            """;

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, record, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<ExperimentEventRecord>> GetByExperimentIdAsync(string experimentId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                sequence_num, experiment_id, phase, event_type, runspace_id,
                step_id, step_label, is_manual, device_id, capability,
                parameters_json, result_json, occurred_at
            FROM experiment_events
            WHERE experiment_id = @experimentId
            ORDER BY sequence_num ASC;
            """;

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(ct);
        var rows = await connection.QueryAsync<ExperimentEventRow>(new CommandDefinition(sql, new { experimentId }, cancellationToken: ct));
        return rows.Select(r => r.ToRecord()).ToList();
    }

    public async Task<IReadOnlyList<ExperimentEventRecord>> GetReplayLogAsync(string experimentId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                sequence_num, experiment_id, phase, event_type, runspace_id,
                step_id, step_label, is_manual, device_id, capability,
                parameters_json, result_json, occurred_at
            FROM experiment_events
            WHERE experiment_id = @experimentId
            ORDER BY sequence_num ASC;
            """;

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(ct);
        var rows = await connection.QueryAsync<ExperimentEventRow>(new CommandDefinition(sql, new { experimentId }, cancellationToken: ct));
        return rows.Select(r => r.ToRecord()).ToList();
    }

    private sealed class ExperimentEventRow
    {
        public long SequenceNum { get; set; }
        public string ExperimentId { get; set; } = "";
        public string Phase { get; set; } = "";
        public string EventType { get; set; } = "";
        public string RunspaceId { get; set; } = "";
        public string? StepId { get; set; }
        public string? StepLabel { get; set; }
        public bool? IsManual { get; set; }
        public string? DeviceId { get; set; }
        public string? Capability { get; set; }
        public string? ParametersJson { get; set; }
        public string? ResultJson { get; set; }
        public DateTimeOffset OccurredAt { get; set; }

        public ExperimentEventRecord ToRecord() => new(
            SequenceNum, ExperimentId, Phase, EventType, RunspaceId,
            StepId, StepLabel, IsManual, DeviceId, Capability,
            ParametersJson, ResultJson, OccurredAt);
    }
}
