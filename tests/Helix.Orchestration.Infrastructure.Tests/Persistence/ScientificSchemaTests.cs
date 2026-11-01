using Npgsql;
using Xunit;

namespace Helix.Orchestration.Infrastructure.Tests.Persistence;

[Collection(nameof(ScientificPostgresCollection))]
public sealed class ScientificSchemaTests(ScientificPostgresFixture fixture)
{
    [Fact]
    public async Task Runtime_role_cannot_mutate_or_disable_any_immutable_scientific_fact_table()
    {
        await using var connection = await fixture.OpenRuntimeConnectionAsync();

        foreach (var table in ImmutableFactTables)
        {
            await AssertDeniedAsync(connection, $"DELETE FROM public.{table} WHERE false;");
            await AssertDeniedAsync(connection, $"TRUNCATE TABLE public.{table};");
            await AssertDeniedAsync(connection, $"ALTER TABLE public.{table} DISABLE TRIGGER ALL;");
        }

        await AssertDeniedAsync(connection, "ALTER TABLE public.scientific_policy_revisions OWNER TO helix_scientific_runtime;");
        // PostgreSQL accepts a no-op GRANT without grant options; prove it cannot
        // produce an effective privilege for an unrelated role.
        await ExecuteAsync(connection, "GRANT ALL ON public.scientific_policy_revisions TO helix_scientific_untrusted;");
        await using var untrusted = await fixture.OpenUntrustedConnectionAsync();
        await AssertDeniedAsync(untrusted, "DELETE FROM public.scientific_policy_revisions WHERE false;");
        await AssertDeniedAsync(connection, "CREATE TABLE public.runtime_escalation(id integer);");
    }

    [Fact]
    public async Task Runtime_role_can_edit_drafts_and_advance_only_mutable_sample_heads()
    {
        var apparatusId = Guid.NewGuid();
        var sampleId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        await using (var owner = fixture.ConnectionFactory!.Create())
        {
            await owner.OpenAsync();
            await ExecuteAsync(owner, $"SET ROLE helix_scientific_owner; INSERT INTO scientific_apparatus(apparatus_id, actor_sid, name, draft_content) VALUES ('{apparatusId}', 'scientist', 'draft', '{{}}'); INSERT INTO scientific_samples(sample_id, actor_sid, origin, created_at) VALUES ('{sampleId}', 'scientist', 'collection', now()); INSERT INTO scientific_sample_events(event_id, sample_id, event_kind, location, occurred_at, recorded_at, actor_sid, server_sequence) VALUES ('{eventId}', '{sampleId}', 'Collection', 'hood', now(), now(), 'scientist', 1); INSERT INTO scientific_sample_heads(sample_id, server_sequence, event_id) VALUES ('{sampleId}', 1, '{eventId}');");
        }

        await using var runtime = await fixture.OpenRuntimeConnectionAsync();
        await ExecuteAsync(runtime, $"UPDATE scientific_apparatus SET draft_token = draft_token + 1 WHERE apparatus_id = '{apparatusId}'; UPDATE scientific_sample_heads SET server_sequence = 2 WHERE sample_id = '{sampleId}';");
        await AssertDeniedAsync(runtime, $"UPDATE scientific_sample_events SET location = 'changed' WHERE event_id = '{eventId}';");
        await AssertDeniedAsync(runtime, $"UPDATE scientific_samples SET actor_sid = 'changed' WHERE sample_id = '{sampleId}';");
    }

    private static readonly string[] ImmutableFactTables =
    [
        "scientific_apparatus_revisions", "scientific_operations", "scientific_procedure_revisions",
        "scientific_experiment_plan_revisions", "scientific_samples", "scientific_sample_events",
        "scientific_sample_operations", "scientific_sample_transformations", "scientific_sample_lineage_edges",
        "scientific_policy_revisions", "scientific_frozen_setup_manifests"
    ];

    private static async Task AssertDeniedAsync(NpgsqlConnection connection, string sql)
    {
        var exception = await Record.ExceptionAsync(() => ExecuteAsync(connection, sql));
        Assert.True(exception is PostgresException, $"Runtime role unexpectedly completed: {sql}");
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
