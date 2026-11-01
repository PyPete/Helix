using Helix.Orchestration.Agent;
using Helix.Orchestration.Agent.Security;
using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Scientific;

/// <summary>
/// Owns an isolated database and the production Agent pipeline used by the scientific tracer.
/// The Agent connects as the constrained runtime role; this fixture alone applies migrations as owner.
/// </summary>
public sealed class ScientificPostgresFixture : IAsyncLifetime
{
    private const int NativeAgentPort = 5080;
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("helix_scientific_tracer")
        .WithUsername("helix")
        .WithPassword("helix_tracer_password")
        .Build();
    private WebApplication? _agent;

    public string OwnerConnectionString { get; private set; } = string.Empty;
    public string RuntimeConnectionString { get; private set; } = string.Empty;
    public Uri AgentAddress { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        OwnerConnectionString = _container.GetConnectionString();
        RuntimeConnectionString = $"{OwnerConnectionString};Options=-c role=helix_scientific_runtime";
        await ApplyMigrationsAsync();
        await StartAgentAsync();
    }

    public async Task RestartAgentAsync()
    {
        if (_agent is not null)
        {
            await _agent.StopAsync();
            await _agent.DisposeAsync();
            _agent = null;
        }

        await StartAgentAsync();
    }

    public HttpClient CreateAuthenticatedClient(string? actorSid = null)
    {
        var issue = _agent!.Services.GetRequiredService<DesktopSessionService>()
            .Issue(actorSid ?? $"S-1-5-21-tracer-{Guid.NewGuid():N}", "Tracer Scientist", TimeSpan.FromMinutes(15));
        var client = new HttpClient { BaseAddress = AgentAddress };
        client.DefaultRequestHeaders.Authorization = new("Bearer", issue.Token);
        return client;
    }

    public async Task<int> OwnerOperationCountAsync(Guid operationId)
    {
        await using var connection = new NpgsqlConnection(OwnerConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT count(*) FROM scientific_operations WHERE operation_id = @operationId;", connection);
        command.Parameters.AddWithValue("operationId", operationId);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task<int> OwnerApparatusCountAsync(Guid apparatusId)
    {
        await using var connection = new NpgsqlConnection(OwnerConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT count(*) FROM scientific_apparatus WHERE apparatus_id = @apparatusId;", connection);
        command.Parameters.AddWithValue("apparatusId", apparatusId);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task AssertRuntimeMutationIsForbiddenAsync(Guid apparatusId)
    {
        await using var connection = new NpgsqlConnection(RuntimeConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("DELETE FROM scientific_apparatus WHERE apparatus_id = @apparatusId;", connection);
        command.Parameters.AddWithValue("apparatusId", apparatusId);
        var exception = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, exception.SqlState);
    }

    public async Task AssertRuntimeRevisionMutationIsForbiddenAsync(Guid apparatusId)
    {
        await using var connection = new NpgsqlConnection(RuntimeConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("DELETE FROM scientific_apparatus_revisions WHERE apparatus_id = @apparatusId;", connection);
        command.Parameters.AddWithValue("apparatusId", apparatusId);
        var exception = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, exception.SqlState);
    }

    public async Task AssertRuntimeCanAppendAdmissionEvidenceAsync()
    {
        var ledger = new PostgresCommandLedger(new DbConnectionFactory(RuntimeConnectionString));
        var evidence = await ledger.AppendAdmissionAsync(
            new CommandIntent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "simulator",
                "SetFlow",
                "{\"flowRate\":1.0}",
                "S-1-5-21-tracer",
                "Tracer Scientist",
                "scientific-tracer",
                "simulation-policy-v1",
                DateTimeOffset.UtcNow),
            [],
            CommandAdmissionState.Allowed);

        Assert.Equal(CommandAdmissionState.Allowed, evidence.State);
    }

    public async Task AssertNoHardwareOrCapabilityDispatchAsync()
    {
        await using var connection = new NpgsqlConnection(OwnerConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT count(*) FROM command_dispatches;", connection);
        Assert.Equal(0L, Convert.ToInt64(await command.ExecuteScalarAsync()));
    }

    public async Task DisposeAsync()
    {
        if (_agent is not null)
        {
            await _agent.StopAsync();
            await _agent.DisposeAsync();
        }
        await _container.DisposeAsync();
    }

    private async Task ApplyMigrationsAsync()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "migrations");
        var migrations = Directory.EnumerateFiles(directory, "*.sql")
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToArray();
        Assert.NotEmpty(migrations);

        await using var connection = new NpgsqlConnection(OwnerConnectionString);
        await connection.OpenAsync();
        // The disposable owner provisions the constrained runtime identity before
        // migrations so grants are verified exactly as production requires.
        await using (var role = new NpgsqlCommand("DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'helix_scientific_runtime') THEN CREATE ROLE helix_scientific_runtime; END IF; END $$;", connection))
            await role.ExecuteNonQueryAsync();
        foreach (var migration in migrations)
        {
            await using var command = new NpgsqlCommand(await File.ReadAllTextAsync(migration), connection);
            await command.ExecuteNonQueryAsync();
        }

    }

    private async Task StartAgentAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{NativeAgentPort}");
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:HelixOrchestration"] = RuntimeConnectionString,
            ["Kestrel:Endpoints:Http:Url"] = $"http://127.0.0.1:{NativeAgentPort}",
            ["Mqtt:BrokerHost"] = "127.0.0.1",
            ["Mqtt:BrokerPort"] = "1"
        });
        AgentApplicationComposition.ConfigureServices(builder);
        _agent = builder.Build();
        AgentApplicationComposition.ConfigurePipeline(_agent);
        await _agent.StartAsync();
        AgentAddress = new Uri($"http://127.0.0.1:{NativeAgentPort}");
    }
}

[CollectionDefinition(nameof(ScientificPostgresCollection))]
public sealed class ScientificPostgresCollection : ICollectionFixture<ScientificPostgresFixture>;

